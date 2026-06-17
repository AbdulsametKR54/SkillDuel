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
            var isDisabled = Environment.GetEnvironmentVariable("MATCHMAKING_DISABLE");
            if (!string.IsNullOrEmpty(isDisabled) && isDisabled.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Matchmaking Worker is disabled via kill-switch.");
                await Task.Delay(10000, stoppingToken);
                continue;
            }

            try
            {
                // BLPOP: trigger gelene kadar blokla (5 saniye timeout, sonra tekrar bekle)
                var result = await _db.ListLeftPopAsync(RedisKeys.MatchmakingTriggerQueue);

                if (result.HasValue)
                {
                    _logger.LogInformation("Matchmaking trigger received, evaluating queues...");
                    using var scope = _serviceProvider.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<MatchmakingProcessor>();
                    await processor.EvaluateAllQueuesAsync();
                    await _db.ListRemoveAsync(RedisKeys.MatchmakingProcessingQueue, result);
                }
                else
                {
                    _logger.LogDebug("No trigger, sleeping...");
                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Matchmaking Background Worker error.");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
