using BaseballApp.Data;
using BaseballApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BaseballApp.Services;

public interface IRankingCacheService
{
    /// <summary>
    /// 更新指定賽季的打者排行榜快取
    /// </summary>
    Task UpdateBattingRankingsAsync(string seasonId);

    /// <summary>
    /// 更新指定賽季的投手排行榜快取
    /// </summary>
    Task UpdatePitchingRankingsAsync(string seasonId);

    /// <summary>
    /// 更新所有賽季的排行榜快取
    /// </summary>
    Task UpdateAllRankingsAsync();

    /// <summary>
    /// 取得打者排行榜(從快取)
    /// </summary>
    Task<List<BattingRankingItem>> GetBattingRankingsFromCacheAsync(string seasonId, int minQualifiedPA = 0);

    /// <summary>
    /// 取得所有打者的統計數據(從快取,用於計算PR值)
    /// </summary>
    Task<List<BattingRankingCache>> GetBattingStatsFromCacheAsync(string seasonId);

    /// <summary>
    /// 取得投手排行榜(從快取)
    /// </summary>
    Task<List<PitchingRankingItem>> GetPitchingRankingsFromCacheAsync(string seasonId, decimal minQualifiedIP = 0);

    /// <summary>
    /// 檢查快取是否需要更新（超過指定小時數）
    /// </summary>
    Task<bool> IsCacheStaleAsync(string seasonId, int hoursThreshold = 24);
}

public class RankingCacheService : IRankingCacheService
{
    private readonly BaseballDbContext _context;
    private readonly IBaseballDbService _baseballDbService;
    private readonly ILogger<RankingCacheService> _logger;

    public RankingCacheService(
        BaseballDbContext context,
        IBaseballDbService baseballDbService,
        ILogger<RankingCacheService> logger)
    {
        _context = context;
        _baseballDbService = baseballDbService;
        _logger = logger;
    }

    public async Task UpdateBattingRankingsAsync(string seasonId)
    {
        try
        {
            _logger.LogInformation($"開始更新打者排行榜快取：{seasonId}");

            // 計算打者統計
            var batterEntities = await _baseballDbService.GetAllBattersAsync(seasonId);
            
            // 一次性取得所有PA記錄,避免重複查詢
            var allPAs = await _baseballDbService.GetPAAsync(seasonId: seasonId);
            
            var allStats = new List<BattingStats>();
            var batterHBPMap = new Dictionary<string, int>();
            var batterSFMap = new Dictionary<string, int>();

            foreach (var batter in batterEntities)
            {
                var stats = await _baseballDbService.CalculateBattingStatsAsync(batter.PlayerId, seasonId);
                allStats.Add(stats);

                // 從已載入的PA記錄中篩選並計算HBP和SF
                var batterPAs = allPAs.Where(pa => pa.BatterId == batter.PlayerId).ToList();
                batterHBPMap[batter.PlayerId] = batterPAs.Count(pa => pa.Result == "HBP");
                batterSFMap[batter.PlayerId] = batterPAs.Count(pa => pa.Result == "SF");
            }

            // 依安打數排序並賦予排名
            var rankedStats = allStats
                .OrderByDescending(s => s.Hits)
                .ThenByDescending(s => s.BattingAverage)
                .Select((stats, index) =>
                {
                    var playerId = batterEntities.FirstOrDefault(b => b.PlayerName == stats.PlayerName)?.PlayerId ?? "";
                    return new BattingRankingCache
                    {
                        SeasonId = seasonId,
                        PlayerId = playerId,
                        PlayerName = stats.PlayerName,
                        Rank = index + 1,
                        Games = stats.Games,
                        PA = stats.PlateAppearances,
                        AB = stats.AtBats,
                        H = stats.Hits,
                        TwoB = stats.Doubles,
                        ThreeB = stats.Triples,
                        HR = stats.HomeRuns,
                        RBI = stats.RBIs,
                        R = stats.Runs,
                        SO = stats.Strikeouts,
                        BB = stats.Walks,
                        HBP = batterHBPMap.GetValueOrDefault(playerId, 0),
                        SF = batterSFMap.GetValueOrDefault(playerId, 0),
                        SB = stats.StolenBases,
                        AVG = (decimal)stats.BattingAverage,
                        OBP = (decimal)stats.OnBasePercentage,
                        SLG = (decimal)stats.SluggingPercentage,
                        OPS = (decimal)stats.OPS,
                        UpdatedAt = DateTime.Now
                    };
                })
                .ToList();

            // 刪除舊的快取資料
            var oldCache = await _context.BattingRankingCaches
                .Where(c => c.SeasonId == seasonId)
                .ToListAsync();
            _context.BattingRankingCaches.RemoveRange(oldCache);

            // 插入新的快取資料
            await _context.BattingRankingCaches.AddRangeAsync(rankedStats);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"打者排行榜快取更新完成：{seasonId}，共 {rankedStats.Count} 筆");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"更新打者排行榜快取時發生錯誤：{seasonId}");
            throw;
        }
    }

    public async Task UpdatePitchingRankingsAsync(string seasonId)
    {
        try
        {
            _logger.LogInformation($"開始更新投手排行榜快取：{seasonId}");

            var pitchers = await _baseballDbService.GetAllPitchersAsync(seasonId);
            var pitcherBoxes = await _baseballDbService.GetPitcherBoxAsync(seasonId: seasonId);

            var rankedStats = pitcherBoxes
                .GroupBy(pb => pb.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key,
                    PlayerName = pitchers.FirstOrDefault(p => p.PlayerId == g.Key)?.PlayerName ?? "Unknown",
                    Games = g.Select(x => x.GameSeq).Distinct().Count(),
                    IPOuts = g.Sum(x => x.IPOuts ?? 0),
                    IP = (decimal)(g.Sum(x => x.IPOuts ?? 0) / 3) + (decimal)(g.Sum(x => x.IPOuts ?? 0) % 3) / 10m,
                    H = g.Sum(x => x.H ?? 0),
                    HR = g.Sum(x => x.HR ?? 0),
                    BB = g.Sum(x => x.BB ?? 0),
                    SO = g.Sum(x => x.SO ?? 0),
                    R = g.Sum(x => x.R ?? 0),
                    ER = g.Sum(x => x.ER ?? 0),
                    ERA = g.Sum(x => x.IPOuts ?? 0) > 0 ? 
                        Math.Round((decimal)g.Sum(x => x.ER ?? 0) * 27 / g.Sum(x => x.IPOuts ?? 0), 2) : 0,
                    WHIP = g.Sum(x => x.IPOuts ?? 0) > 0 ? 
                        Math.Round((decimal)(g.Sum(x => x.H ?? 0) + g.Sum(x => x.BB ?? 0)) * 3 / g.Sum(x => x.IPOuts ?? 0), 2) : 0
                })
                .OrderBy(x => x.ERA)
                .ThenByDescending(x => x.IP)
                .Select((p, index) => new PitchingRankingCache
                {
                    SeasonId = seasonId,
                    PlayerId = p.PlayerId ?? "",
                    PlayerName = p.PlayerName,
                    Rank = index + 1,
                    Games = p.Games,
                    IP = p.IP,
                    IPOuts = p.IPOuts,
                    H = p.H,
                    HR = p.HR,
                    BB = p.BB,
                    SO = p.SO,
                    R = p.R,
                    ER = p.ER,
                    W = 0, // TODO: 需要從比賽結果計算勝敗
                    L = 0,
                    ERA = p.ERA,
                    WHIP = p.WHIP,
                    UpdatedAt = DateTime.Now
                })
                .ToList();

            // 刪除舊的快取資料
            var oldCache = await _context.PitchingRankingCaches
                .Where(c => c.SeasonId == seasonId)
                .ToListAsync();
            _context.PitchingRankingCaches.RemoveRange(oldCache);

            // 插入新的快取資料
            await _context.PitchingRankingCaches.AddRangeAsync(rankedStats);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"投手排行榜快取更新完成：{seasonId}，共 {rankedStats.Count} 筆");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"更新投手排行榜快取時發生錯誤：{seasonId}");
            throw;
        }
    }

    public async Task UpdateAllRankingsAsync()
    {
        try
        {
            _logger.LogInformation("開始更新所有賽季的排行榜快取");

            var seasons = await _baseballDbService.GetAllSeasonsAsync();

            foreach (var season in seasons)
            {
                await UpdateBattingRankingsAsync(season.SeasonId);
                await UpdatePitchingRankingsAsync(season.SeasonId);
            }

            // 更新 "ALL" (全部賽季)
            await UpdateBattingRankingsAsync("ALL");
            await UpdatePitchingRankingsAsync("ALL");

            _logger.LogInformation("所有賽季的排行榜快取更新完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新所有排行榜快取時發生錯誤");
            throw;
        }
    }

    public async Task<List<BattingRankingItem>> GetBattingRankingsFromCacheAsync(string seasonId, int minQualifiedPA = 0)
    {
        try
        {
            var query = _context.BattingRankingCaches
                .Where(c => c.SeasonId == seasonId);

            if (minQualifiedPA > 0)
            {
                query = query.Where(c => c.PA >= minQualifiedPA);
            }

            var rankings = await query
                .OrderBy(c => c.Rank)
                .Select(c => new BattingRankingItem
                {
                    Rank = c.Rank,
                    PlayerId = c.PlayerId,
                    PlayerName = c.PlayerName,
                    Games = c.Games,
                    PA = c.PA,
                    AB = c.AB,
                    H = c.H,
                    HR = c.HR,
                    RBI = c.RBI,
                    BB = c.BB,
                    SO = c.SO,
                    AVG = c.AVG,
                    OBP = c.OBP,
                    SLG = c.SLG
                })
                .ToListAsync();

            return rankings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"從快取讀取打者排行榜時發生錯誤：{seasonId}");
            return new List<BattingRankingItem>();
        }
    }

    public async Task<List<BattingRankingCache>> GetBattingStatsFromCacheAsync(string seasonId)
    {
        try
        {
            return await _context.BattingRankingCaches
                .Where(c => c.SeasonId == seasonId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"從快取讀取打者統計數據時發生錯誤：{seasonId}");
            return new List<BattingRankingCache>();
        }
    }

    public async Task<List<PitchingRankingItem>> GetPitchingRankingsFromCacheAsync(string seasonId, decimal minQualifiedIP = 0)
    {
        try
        {
            var query = _context.PitchingRankingCaches
                .Where(c => c.SeasonId == seasonId);

            if (minQualifiedIP > 0)
            {
                query = query.Where(c => c.IP >= minQualifiedIP);
            }

            var rankings = await query
                .OrderBy(c => c.Rank)
                .Select(c => new PitchingRankingItem
                {
                    Rank = c.Rank,
                    PlayerId = c.PlayerId,
                    PlayerName = c.PlayerName,
                    Games = c.Games,
                    IP = c.IP,
                    H = c.H,
                    HR = c.HR,
                    BB = c.BB,
                    SO = c.SO,
                    R = c.R,
                    ER = c.ER,
                    ERA = c.ERA,
                    WHIP = c.WHIP
                })
                .ToListAsync();

            return rankings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"從快取讀取投手排行榜時發生錯誤：{seasonId}");
            return new List<PitchingRankingItem>();
        }
    }

    public async Task<bool> IsCacheStaleAsync(string seasonId, int hoursThreshold = 24)
    {
        try
        {
            var latestBattingCache = await _context.BattingRankingCaches
                .Where(c => c.SeasonId == seasonId)
                .OrderByDescending(c => c.UpdatedAt)
                .FirstOrDefaultAsync();

            if (latestBattingCache == null)
            {
                return true; // 沒有快取，需要更新
            }

            var age = DateTime.Now - latestBattingCache.UpdatedAt;
            return age.TotalHours > hoursThreshold;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"檢查快取狀態時發生錯誤：{seasonId}");
            return true; // 發生錯誤時假設需要更新
        }
    }
}
