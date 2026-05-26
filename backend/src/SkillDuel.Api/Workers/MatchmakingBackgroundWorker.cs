using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SkillDuel.Application.Services;
using SkillDuel.Domain.Constants;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SkillDuel.Api.Workers;

public class MatchmakingBackgroundWorker : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MatchmakingBackgroundWorker> _logger;

    public MatchmakingBackgroundWorker(
        IConnectionMultiplexer redis,
        IServiceProvider serviceProvider,
        ILogger<MatchmakingBackgroundWorker> logger)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Matchmaking Event-Driven Background Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Perform Block Pop / Event Pop against trigger queue.
                var result = await _db.ListRightPopLeftPushAsync(
                    RedisKeysExtensions.MatchmakingTriggerQueue,
                    "skillduel:matchmaking:processing"
                );

                if (result.HasValue)
                {
                    // Create scoped services to run evaluation safely
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var processor = scope.ServiceProvider.GetRequiredService<MatchmakingProcessor>();
                        await processor.EvaluateAllQueuesAsync();
                    }

                    // Remove processed trigger token
                    await _db.ListRemoveAsync("skillduel:matchmaking:processing", result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Matchmaking Background Worker.");
                await Task.Delay(2000, stoppingToken); // Backoff delay on error
            }
        }
    }
}
