using BaseballApp.Services;

namespace BaseballApp.BackgroundServices;

/// <summary>
/// 定期更新排行榜快取的背景服務
/// </summary>
public class RankingCacheUpdateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RankingCacheUpdateService> _logger;
    private readonly TimeSpan _updateInterval;

    public RankingCacheUpdateService(
        IServiceProvider serviceProvider,
        ILogger<RankingCacheUpdateService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // 從配置讀取更新間隔，預設為每天凌晨 3 點更新
        var intervalHours = configuration.GetValue<int>("RankingCache:UpdateIntervalHours", 24);
        _updateInterval = TimeSpan.FromHours(intervalHours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("排行榜快取更新服務已啟動");

        // 等待到下一個預定更新時間（例如：凌晨 3 點）
        await WaitForScheduledTime(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("開始定期更新排行榜快取");
                
                // 使用 Scope 來取得服務實例
                using (var scope = _serviceProvider.CreateScope())
                {
                    var rankingCacheService = scope.ServiceProvider.GetRequiredService<IRankingCacheService>();
                    
                    // 更新所有賽季的排行榜快取
                    await rankingCacheService.UpdateAllRankingsAsync();
                }

                _logger.LogInformation("排行榜快取更新完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "定期更新排行榜快取時發生錯誤");
            }

            // 等待下一個更新週期
            await Task.Delay(_updateInterval, stoppingToken);
        }

        _logger.LogInformation("排行榜快取更新服務已停止");
    }

    /// <summary>
    /// 等待到指定的更新時間（例如：每天凌晨 3 點）
    /// </summary>
    private async Task WaitForScheduledTime(CancellationToken stoppingToken)
    {
        var now = DateTime.Now;
        var scheduledHour = 3; // 凌晨 3 點
        
        var nextRun = now.Date.AddHours(scheduledHour);
        if (now.Hour >= scheduledHour)
        {
            // 如果已經過了今天的更新時間，則設定為明天
            nextRun = nextRun.AddDays(1);
        }

        var delay = nextRun - now;
        _logger.LogInformation($"排行榜快取將在 {nextRun:yyyy-MM-dd HH:mm:ss} 首次更新（{delay.TotalHours:F1} 小時後）");

        await Task.Delay(delay, stoppingToken);
    }
}
