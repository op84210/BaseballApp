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
    /// 更新球隊賽季排行榜快取（從 tblTeamGameStats 聚合）
    /// </summary>
    Task UpdateTeamRankingsAsync(string? seasonId = null);

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
    /// 取得所有投手的統計數據(從快取,用於計算PR值)
    /// </summary>
    Task<List<PitchingRankingCache>> GetPitchingStatsFromCacheAsync(string seasonId);

    /// <summary>
    /// 查詢球隊賽季打者統計資料（從 tblTeamSeasonRankingCache）
    /// </summary>
    Task<TeamSeasonStatsDto?> GetTeamSeasonBattingStatsAsync(string seasonId, string teamId);

    /// <summary>
    /// 取得所有球隊賽季打者統計資料（用於計算球隊打者PR值）
    /// </summary>
    Task<List<TeamSeasonStatsDto>> GetAllTeamSeasonBattingStatsAsync(string seasonId);

    /// <summary>
    /// 查詢球隊賽季投手統計資料（從 tblTeamSeasonRankingCache）
    /// </summary>
    Task<TeamSeasonPitchingStatsDto?> GetTeamSeasonPitchingStatsAsync(string seasonId, string teamId);

    /// <summary>
    /// 取得所有球隊賽季投手統計資料（用於計算球隊PR值）
    /// </summary>
    Task<List<TeamSeasonPitchingStatsDto>> GetAllTeamSeasonPitchingStatsAsync(string seasonId);

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
                    HBP = g.Sum(x => x.HB ?? 0),
                    BF = g.Sum(x => x.BF ?? 0),
                    ERA = g.Sum(x => x.IPOuts ?? 0) > 0 ? 
                        Math.Round((decimal)g.Sum(x => x.ER ?? 0) * 27 / g.Sum(x => x.IPOuts ?? 0), 2) : 0,
                    WHIP = g.Sum(x => x.IPOuts ?? 0) > 0 ? 
                        Math.Round((decimal)(g.Sum(x => x.H ?? 0) + g.Sum(x => x.BB ?? 0)) * 3 / g.Sum(x => x.IPOuts ?? 0), 2) : 0,
                    K9 = g.Sum(x => x.IPOuts ?? 0) > 0 ?
                        Math.Round((decimal)g.Sum(x => x.SO ?? 0) * 27 / g.Sum(x => x.IPOuts ?? 0), 2) : 0,
                    BB9 = g.Sum(x => x.IPOuts ?? 0) > 0 ?
                        Math.Round((decimal)g.Sum(x => x.BB ?? 0) * 27 / g.Sum(x => x.IPOuts ?? 0), 2) : 0,
                    KBBRatio = g.Sum(x => x.BB ?? 0) > 0 ?
                        Math.Round((decimal)g.Sum(x => x.SO ?? 0) / g.Sum(x => x.BB ?? 0), 2) : g.Sum(x => x.SO ?? 0),
                    BAA = (g.Sum(x => x.BF ?? 0) - g.Sum(x => x.BB ?? 0) - g.Sum(x => x.HB ?? 0)) > 0 ?
                        Math.Round((decimal)g.Sum(x => x.H ?? 0) / (g.Sum(x => x.BF ?? 0) - g.Sum(x => x.BB ?? 0) - g.Sum(x => x.HB ?? 0)), 3) : 0
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
                    K9 = p.K9,
                    BB9 = p.BB9,
                    KBBRatio = p.KBBRatio,
                    BAA = p.BAA,
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

    public async Task UpdateTeamRankingsAsync(string? seasonId = null)
    {
        try
        {
            _logger.LogInformation($"開始更新球隊排行榜快取: {seasonId ?? "ALL"}");

            // 執行 SQL 更新球隊快取（與 DataEtl 的 RebuildTeamSeasonRankingCache 相同邏輯）
            var sql = @"
                -- 清除指定賽季或全部賽季的快取
                DELETE FROM tblTeamSeasonRankingCache
                WHERE {0};

                INSERT INTO tblTeamSeasonRankingCache(
                    seasonId, teamId, teamName,
                    rank, gamesPlayed, wins, losses,
                    runsScored, runsAllowed,
                    pa, ab, h, twoB, threeB, hr, bb, so, hbp, sf, sb, cs,
                    ipOuts, er, hitsAllowed, bbAllowed, soPitching, hrAllowed,
                    winPct, avg, obp, slg, ops, era, fip, runDiff, updatedAt
                )
                SELECT
                    tgs.seasonId,
                    tgs.teamId,
                    MAX(tgs.teamName) as teamName,
                    0 as rank,
                    COUNT(*) as gamesPlayed,
                    SUM(CASE WHEN tgs.teamScore > tgs.opponentScore THEN 1 ELSE 0 END) as wins,
                    SUM(CASE WHEN tgs.teamScore < tgs.opponentScore THEN 1 ELSE 0 END) as losses,
                    SUM(tgs.teamScore) as runsScored,
                    SUM(tgs.opponentScore) as runsAllowed,
                    SUM(tgs.pa) as pa,
                    SUM(tgs.ab) as ab,
                    SUM(tgs.h) as h,
                    SUM(tgs.twoB) as twoB,
                    SUM(tgs.threeB) as threeB,
                    SUM(tgs.hr) as hr,
                    SUM(tgs.bb) as bb,
                    SUM(tgs.so) as so,
                    SUM(tgs.hbp) as hbp,
                    SUM(tgs.sf) as sf,
                    SUM(tgs.sb) as sb,
                    SUM(tgs.cs) as cs,
                    SUM(tgs.ipOuts) as ipOuts,
                    SUM(tgs.er) as er,
                    SUM(tgs.hitsAllowed) as hitsAllowed,
                    SUM(tgs.bbAllowed) as bbAllowed,
                    SUM(tgs.soPitching) as soPitching,
                    SUM(tgs.hrAllowed) as hrAllowed,
                    CASE WHEN COUNT(*) > 0 THEN CAST(SUM(CASE WHEN tgs.teamScore > tgs.opponentScore THEN 1 ELSE 0 END) AS REAL) / COUNT(*) ELSE 0 END as winPct,
                    CASE WHEN SUM(tgs.ab) > 0 THEN CAST(SUM(tgs.h) AS REAL) / SUM(tgs.ab) ELSE 0 END as avg,
                    CASE WHEN (SUM(tgs.ab) + SUM(tgs.bb) + SUM(tgs.hbp) + SUM(tgs.sf)) > 0
                         THEN CAST((SUM(tgs.h) + SUM(tgs.bb) + SUM(tgs.hbp)) AS REAL) / (SUM(tgs.ab) + SUM(tgs.bb) + SUM(tgs.hbp) + SUM(tgs.sf))
                         ELSE 0 END as obp,
                    CASE WHEN SUM(tgs.ab) > 0
                         THEN CAST((SUM(tgs.h) - (SUM(tgs.twoB)+SUM(tgs.threeB)+SUM(tgs.hr)) + 2*SUM(tgs.twoB) + 3*SUM(tgs.threeB) + 4*SUM(tgs.hr)) AS REAL) / SUM(tgs.ab)
                         ELSE 0 END as slg,
                    0 as ops,
                    CASE WHEN SUM(tgs.ipOuts) > 0 THEN 9.0 * CAST(SUM(tgs.er) AS REAL) / (CAST(SUM(tgs.ipOuts) AS REAL) / 3.0) ELSE 0 END as era,
                    NULL as fip,
                    SUM(tgs.teamScore) - SUM(tgs.opponentScore) as runDiff,
                    strftime('%Y-%m-%dT%H:%M:%SZ','now') as updatedAt
                FROM tblTeamGameStats tgs
                WHERE {1}
                GROUP BY tgs.seasonId, tgs.teamId;

                UPDATE tblTeamSeasonRankingCache
                SET ops = obp + slg
                WHERE {0};

                WITH ranked AS (
                    SELECT seasonId, teamId,
                           ROW_NUMBER() OVER (PARTITION BY seasonId ORDER BY winPct DESC, runDiff DESC) AS rnk
                    FROM tblTeamSeasonRankingCache
                    WHERE {0}
                )
                UPDATE tblTeamSeasonRankingCache AS t
                SET rank = (SELECT rnk FROM ranked WHERE ranked.seasonId = t.seasonId AND ranked.teamId = t.teamId)
                WHERE {0};
            ";

            var whereClause = string.IsNullOrEmpty(seasonId) ? "1=1" : $"seasonId = '{seasonId}'";
            var formattedSql = string.Format(sql, whereClause, whereClause);

            await _context.Database.ExecuteSqlRawAsync(formattedSql);

            _logger.LogInformation($"球隊排行榜快取更新完成: {seasonId ?? "ALL"}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"更新球隊排行榜快取失敗: {seasonId}");
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

            // 更新球隊排行榜（所有賽季）
            await UpdateTeamRankingsAsync();

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

    public async Task<List<PitchingRankingCache>> GetPitchingStatsFromCacheAsync(string seasonId)
    {
        try
        {
            return await _context.PitchingRankingCaches
                .Where(c => c.SeasonId == seasonId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"從快取讀取投手統計數據時發生錯誤：{seasonId}");
            return new List<PitchingRankingCache>();
        }
    }

    public async Task<TeamSeasonStatsDto?> GetTeamSeasonBattingStatsAsync(string seasonId, string teamId)
    {
        try
        {
            var result = await _context.Database
                        .SqlQuery<TeamSeasonStatsQueryResult>($@"
                            SELECT 
                                avg, obp, slg, ops,
                                CAST(hr AS REAL) as hr,
                                CAST(so AS REAL) as so,
                                CAST(bb AS REAL) as bb
                            FROM tblTeamSeasonRankingCache
                            WHERE seasonId = {seasonId} AND teamId = {teamId}
                        ")
                        .FirstOrDefaultAsync();

            if (result == null)
            {
                return null;
            }

            // 查詢球隊該季的打者人數和RBI總數
            int playerCount;
            int totalRBI;
            
            if (seasonId == "ALL")
            {
                // 當 seasonId 為 "ALL" 時，查詢該球隊歷史所有不同的打者
                var teamPlayerIds = await _context.PlayerTeams
                    .Where(pt => pt.TeamId == teamId)
                    .Select(pt => pt.PlayerId)
                    .Distinct()
                    .ToListAsync();

                playerCount = await _context.PAs
                    .Where(pa => pa.BatterId != null && teamPlayerIds.Contains(pa.BatterId))
                    .Select(pa => pa.BatterId)
                    .Distinct()
                    .CountAsync();

                totalRBI = await _context.PAs
                    .Where(pa => pa.BatterId != null && teamPlayerIds.Contains(pa.BatterId))
                    .SumAsync(pa => pa.RBI ?? 0);
            }
            else
            {
                // 查詢該球隊該特定賽季的打者人數
                var teamPlayerIds = await _context.PlayerTeams
                    .Where(pt => pt.TeamId == teamId && pt.SeasonId == seasonId)
                    .Select(pt => pt.PlayerId)
                    .Distinct()
                    .ToListAsync();

                playerCount = await _context.PAs
                    .Where(pa => pa.SeasonId == seasonId && pa.BatterId != null && teamPlayerIds.Contains(pa.BatterId))
                    .Select(pa => pa.BatterId)
                    .Distinct()
                    .CountAsync();

                totalRBI = await _context.PAs
                    .Where(pa => pa.SeasonId == seasonId && pa.BatterId != null && teamPlayerIds.Contains(pa.BatterId))
                    .SumAsync(pa => pa.RBI ?? 0);
            }

            // 計算平均每人的統計值
            // 注意：AVG, OBP, SLG, OPS 已經是比率，不需要除以打者人數
            var avgHRPerPlayer = playerCount > 0 ? (result.Hr ?? 0) / playerCount : 0;
            var avgRBIPerPlayer = playerCount > 0 ? (double)totalRBI / playerCount : 0;
            var avgSOPerPlayer = playerCount > 0 ? (result.So ?? 0) / playerCount : 0;
            var avgBBPerPlayer = playerCount > 0 ? (result.Bb ?? 0) / playerCount : 0;

            return new TeamSeasonStatsDto
            {
                AVG = (decimal)(result.Avg ?? 0),
                OBP = (decimal)(result.Obp ?? 0),
                SLG = (decimal)(result.Slg ?? 0),
                OPS = (decimal)(result.Ops ?? 0),
                HR = (decimal)avgHRPerPlayer,
                RBI = (decimal)avgRBIPerPlayer,
                SO = (decimal)avgSOPerPlayer,
                BB = (decimal)avgBBPerPlayer
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"查詢球隊打者統計資料時發生錯誤: seasonId={seasonId}, teamId={teamId}");
            return null;
        }
    }

    public async Task<TeamSeasonPitchingStatsDto?> GetTeamSeasonPitchingStatsAsync(string seasonId, string teamId)
    {
        try
        {
            var result = await _context.Database
                .SqlQuery<TeamSeasonPitchingStatsQueryResult>($@"
                    SELECT 
                        era, 
                        CASE WHEN ipOuts > 0 
                            THEN CAST((hitsAllowed + bbAllowed) AS REAL) * 3 / ipOuts 
                            ELSE 0 END as whip,
                        CASE WHEN ipOuts > 0 
                            THEN CAST(soPitching AS REAL) * 27 / ipOuts 
                            ELSE 0 END as k9,
                        CASE WHEN ipOuts > 0 
                            THEN CAST(bbAllowed AS REAL) * 27 / ipOuts 
                            ELSE 0 END as bb9,
                        CASE WHEN bbAllowed > 0 
                            THEN CAST(soPitching AS REAL) / bbAllowed 
                            ELSE CAST(soPitching AS REAL) END as kbbRatio,
                        CASE WHEN (pa - bbAllowed - COALESCE(hbp, 0)) > 0
                            THEN CAST(hitsAllowed AS REAL) / (pa - bbAllowed - COALESCE(hbp, 0))
                            ELSE 0 END as baa,
                        CAST(soPitching AS REAL) / NULLIF(gamesPlayed, 0) as so
                    FROM tblTeamSeasonRankingCache
                    WHERE seasonId = {seasonId} AND teamId = {teamId}
                ")
                .FirstOrDefaultAsync();

            if (result == null)
            {
                return null;
            }

            return new TeamSeasonPitchingStatsDto
            {
                ERA = (decimal)(result.Era ?? 0),
                WHIP = (decimal)(result.Whip ?? 0),
                K9 = (decimal)(result.K9 ?? 0),
                BB9 = (decimal)(result.Bb9 ?? 0),
                KBBRatio = (decimal)(result.KbbRatio ?? 0),
                BAA = (decimal)(result.Baa ?? 0),
                SO = (decimal)(result.So ?? 0)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"查詢球隊投手統計資料時發生錯誤: seasonId={seasonId}, teamId={teamId}");
            return null;
        }
    }

    public async Task<List<TeamSeasonStatsDto>> GetAllTeamSeasonBattingStatsAsync(string seasonId)
    {
        try
        {
            // 使用 SQL 一次性查詢所有球隊及其統計數據
            var results = await _context.Database
                .SqlQuery<dynamic>($@"
                    SELECT 
                        t.teamId, t.avg, t.obp, t.slg, t.ops,
                        t.hr, t.so, t.bb
                    FROM tblTeamSeasonRankingCache t
                    WHERE t.seasonId = {seasonId}
                ")
                .ToListAsync();

            var teamStats = new List<TeamSeasonStatsDto>();
            foreach (var r in results)
            {
                var teamId = (string)r.teamId;
                var avg = (double?)r.avg ?? 0;
                var obp = (double?)r.obp ?? 0;
                var slg = (double?)r.slg ?? 0;
                var ops = (double?)r.ops ?? 0;
                var hr = (double?)r.hr ?? 0;
                var so = (double?)r.so ?? 0;
                var bb = (double?)r.bb ?? 0;

                // 查詢該球隊的打者人數
                int playerCount = 0;
                int totalRBI = 0;

                if (seasonId == "ALL")
                {
                    // 查詢該球隊歷史所有不同的打者
                    playerCount = await _context.Database
                        .SqlQuery<int>($@"
                            SELECT COUNT(DISTINCT pa.BatterId)
                            FROM tblPA pa
                            WHERE pa.BatterId IN (
                                SELECT DISTINCT PlayerId 
                                FROM tblPlayerTeam 
                                WHERE TeamId = {teamId}
                            )
                        ")
                        .FirstOrDefaultAsync();

                    // 查詢所有RBI
                    totalRBI = await _context.Database
                        .SqlQuery<int>($@"
                            SELECT COALESCE(SUM(pa.RBI), 0)
                            FROM tblPA pa
                            WHERE pa.BatterId IN (
                                SELECT DISTINCT PlayerId 
                                FROM tblPlayerTeam 
                                WHERE TeamId = {teamId}
                            )
                        ")
                        .FirstOrDefaultAsync();
                }
                else
                {
                    // 查詢該球隊該特定賽季的打者人數
                    playerCount = await _context.Database
                        .SqlQuery<int>($@"
                            SELECT COUNT(DISTINCT pa.BatterId)
                            FROM tblPA pa
                            WHERE pa.SeasonId = {seasonId} 
                            AND pa.BatterId IN (
                                SELECT DISTINCT PlayerId 
                                FROM tblPlayerTeam 
                                WHERE TeamId = {teamId} AND SeasonId = {seasonId}
                            )
                        ")
                        .FirstOrDefaultAsync();

                    // 查詢該季的RBI
                    totalRBI = await _context.Database
                        .SqlQuery<int>($@"
                            SELECT COALESCE(SUM(pa.RBI), 0)
                            FROM tblPA pa
                            WHERE pa.SeasonId = {seasonId} 
                            AND pa.BatterId IN (
                                SELECT DISTINCT PlayerId 
                                FROM tblPlayerTeam 
                                WHERE TeamId = {teamId} AND SeasonId = {seasonId}
                            )
                        ")
                        .FirstOrDefaultAsync();
                }

                var avgRBIPerPlayer = playerCount > 0 ? (double)totalRBI / playerCount : 0;

                teamStats.Add(new TeamSeasonStatsDto
                {
                    AVG = (decimal)avg,
                    OBP = (decimal)obp,
                    SLG = (decimal)slg,
                    OPS = (decimal)ops,
                    HR = (decimal)(playerCount > 0 ? hr / playerCount : 0),
                    RBI = (decimal)avgRBIPerPlayer,
                    SO = (decimal)(playerCount > 0 ? so / playerCount : 0),
                    BB = (decimal)(playerCount > 0 ? bb / playerCount : 0)
                });
            }

            return teamStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"查詢所有球隊打者統計資料時發生錯誤: seasonId={seasonId}");
            return new List<TeamSeasonStatsDto>();
        }
    }

    public async Task<List<TeamSeasonPitchingStatsDto>> GetAllTeamSeasonPitchingStatsAsync(string seasonId)
    {
        try
        {
            var results = await _context.Database
                .SqlQuery<TeamSeasonPitchingStatsQueryResult>($@"
                    SELECT 
                        era, 
                        CASE WHEN ipOuts > 0 
                            THEN CAST((hitsAllowed + bbAllowed) AS REAL) * 3 / ipOuts 
                            ELSE 0 END as whip,
                        CASE WHEN ipOuts > 0 
                            THEN CAST(soPitching AS REAL) * 27 / ipOuts 
                            ELSE 0 END as k9,
                        CASE WHEN ipOuts > 0 
                            THEN CAST(bbAllowed AS REAL) * 27 / ipOuts 
                            ELSE 0 END as bb9,
                        CASE WHEN bbAllowed > 0 
                            THEN CAST(soPitching AS REAL) / bbAllowed 
                            ELSE CAST(soPitching AS REAL) END as kbbRatio,
                        CASE WHEN (pa - bbAllowed - COALESCE(hbp, 0)) > 0
                            THEN CAST(hitsAllowed AS REAL) / (pa - bbAllowed - COALESCE(hbp, 0))
                            ELSE 0 END as baa,
                        CAST(soPitching AS REAL) / NULLIF(gamesPlayed, 0) as so
                    FROM tblTeamSeasonRankingCache
                    WHERE seasonId = {seasonId}
                ")
                .ToListAsync();

            return results.Select(r => new TeamSeasonPitchingStatsDto
            {
                ERA = (decimal)(r.Era ?? 0),
                WHIP = (decimal)(r.Whip ?? 0),
                K9 = (decimal)(r.K9 ?? 0),
                BB9 = (decimal)(r.Bb9 ?? 0),
                KBBRatio = (decimal)(r.KbbRatio ?? 0),
                BAA = (decimal)(r.Baa ?? 0),
                SO = (decimal)(r.So ?? 0)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"查詢所有球隊投手統計資料時發生錯誤: seasonId={seasonId}");
            return new List<TeamSeasonPitchingStatsDto>();
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
