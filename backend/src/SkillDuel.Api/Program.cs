using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SkillDuel.Application.Interfaces;
using SkillDuel.Application.Services;
using SkillDuel.Infrastructure.Authentication;
using SkillDuel.Infrastructure.Repositories;
using SkillDuel.Infrastructure.Data;
using FluentValidation;
using FluentValidation.AspNetCore;
using SkillDuel.Application.Validators;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;
using Hangfire;
using Hangfire.PostgreSql;
using SkillDuel.Api.Hubs;
using SkillDuel.Api.Jobs;
using SkillDuel.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// CORS – allow both dev and production origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://skill-duel-pi.vercel.app"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Serilog
builder.Host.UseSerilog((context, configuration) => 
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddDbContext<SkillDuelDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Http Client
builder.Services.AddHttpClient();
builder.Services.AddScoped<TriviaSeeder>();

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379"));

// Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1;
    options.SchedulePollingInterval = TimeSpan.FromSeconds(30);
});

// SignalR
builder.Services.AddSignalR();

// Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IGameSessionRepository, GameSessionRepository>();
builder.Services.AddScoped<IGameRoundRepository, GameRoundRepository>();
builder.Services.AddScoped<IPlayerAnswerRepository, PlayerAnswerRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IUserCategoryStatRepository, UserCategoryStatRepository>();
builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtProvider, JwtProvider>();
builder.Services.AddScoped<IMatchmakingService, MatchmakingService>();
builder.Services.AddScoped<MatchmakingProcessor>();
builder.Services.AddHostedService<SkillDuel.Api.Workers.MatchmakingBackgroundWorker>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IGameNotificationService, GameNotificationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<CleanupExpiredRoomsJob>();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
        
        // SignalR için Token'ı Query String'den okuma
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new MyHangfireAuthorizationFilter() }
});

app.MapControllers();
app.MapHub<GameHub>("/hubs/game");

// Wait for Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var logger = app.Services.GetRequiredService<Serilog.ILogger>();
await WaitForDatabaseAsync(connectionString, logger);


// Register Matchmaking Recurring Job
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    
    // Explicitly delete any old, orphaned recurring jobs referencing the obsolete type
    recurringJobManager.RemoveIfExists("matchmaking-job");
    recurringJobManager.RemoveIfExists("matchmaking");

    recurringJobManager.AddOrUpdate<CleanupExpiredRoomsJob>(
        "cleanup-expired-rooms-job",
        job => job.RunAsync(),
        "*/10 * * * * *" // Her 10 saniyede bir
    );
}

// Seed Data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SkillDuel.Infrastructure.Data.SkillDuelDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<SkillDuel.Infrastructure.Data.TriviaSeeder>();
    await SkillDuel.Infrastructure.Data.DbInitializer.SeedAsync(context, seeder);
}

app.Run();

async Task WaitForDatabaseAsync(string? connStr, Serilog.ILogger log)
{
    if (string.IsNullOrEmpty(connStr)) return;
    int retries = 0;
    int maxRetries = 60;
    while (retries < maxRetries)
    {
        try
        {
            using var connection = new Npgsql.NpgsqlConnection(connStr);
            await connection.OpenAsync();
            log.Information("Database is ready!");
            return;
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "57P03")
        {
            retries++;
            log.Warning("Database is starting up (57P03). Waiting... ({Retry}/{MaxRetries})", retries, maxRetries);
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            retries++;
            log.Warning("Waiting for database connection... ({Retry}/{MaxRetries}) Error: {Message}", retries, maxRetries, ex.Message);
            await Task.Delay(2000);
        }
    }
    log.Error("Database did not become ready after {MaxRetries} retries. Giving up.", maxRetries);
}

// Basit Dashboard Yetkilendirme (Geliştirme için her şeye izin verir)
public class MyHangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context) => true;
}
