using BaseballApp.Data;
using BaseballApp.Models;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace BaseballApp.Services;

public interface IBaseballDbService
{
    Task<IEnumerable<Game>> GetGamesAsync(string? seasonId = null);
    Task<IEnumerable<Game>> GetGamesAsync(string? seasonId = null, string? teamId = null);
    Task<IEnumerable<BatterBox>> GetBatterBoxAsync(string? playerId = null, string? seasonId = null);
    Task<IEnumerable<PitcherBox>> GetPitcherBoxAsync(string? playerId = null, string? seasonId = null);
    Task<IEnumerable<PA>> GetPAAsync(string? batterId = null, int? gameSeq = null, string? seasonId = null);
    Task<IEnumerable<Event>> GetEventsAsync(int paId);
    Task<BattingStats> CalculateBattingStatsAsync(string playerId, string? seasonId = null);
    Task<IEnumerable<Batter>> GetAllBattersAsync(string? seasonId = null);
    Task<Batter?> GetBatterAsync(string playerId);
    Task<IEnumerable<Pitcher>> GetAllPitchersAsync(string? seasonId = null);
    Task<Pitcher?> GetPitcherAsync(string playerId);
    Task<IEnumerable<Team>> GetAllTeamsAsync(string? seasonId = null);
    Task<IEnumerable<Stadium>> GetAllStadiumsAsync();
    Task<Dictionary<string, string>> GetBatterNameMapAsync();
    Task<Dictionary<string, string>> GetPitcherNameMapAsync();
    Task<IEnumerable<BattingStats>> GetTopBattersAsync(string? seasonId = null, int topN = 10);
    Task<IEnumerable<PA>> GetPAsByGameAsync(string seasonId, int gameSeq);
    Task<IEnumerable<Season>> GetAllSeasonsAsync(string? playerId = null);
}

public class BaseballDbService : IBaseballDbService
{
    private readonly BaseballDbContext _context;
    private readonly ILogger<BaseballDbService> _logger;

    public BaseballDbService(BaseballDbContext context, ILogger<BaseballDbService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Game>> GetGamesAsync(string? seasonId = null)
    {
        try
        {
            var query = _context.Games.AsQueryable();

            if (!string.IsNullOrEmpty(seasonId))
            {
                query = query.Where(g => g.SeasonId == seasonId);
            }

            return await query
                .OrderBy(g => g.Date)
                .ThenBy(g => g.Seq)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取賽事資料時發生錯誤");
            return Enumerable.Empty<Game>();
        }
    }

     public async Task<IEnumerable<Game>> GetGamesAsync(string? seasonId = null, string? teamId = null){
        try
        {
            var query = _context.Games.AsQueryable();

            if (!string.IsNullOrEmpty(seasonId))
            {
                query = query.Where(g => g.SeasonId == seasonId);
            }

            if (!string.IsNullOrEmpty(teamId))
            {
                query = query.Where(g => g.AwayTeamId == teamId || g.HomeTeamId == teamId);
            }

            return await query
                .OrderBy(g => g.Date)
                .ThenBy(g => g.Seq)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取賽事資料時發生錯誤");
            return Enumerable.Empty<Game>();
        }
     }

    public async Task<IEnumerable<BatterBox>> GetBatterBoxAsync(string? playerId = null, string? seasonId = null)
    {
        try
        {
            var query = _context.BatterBoxes.AsQueryable();

            if (!string.IsNullOrEmpty(playerId))
            {
                query = query.Where(bb => bb.PlayerId == playerId);
            }

            if (!string.IsNullOrEmpty(seasonId))
            {
                query = query.Where(bb => bb.SeasonId == seasonId);
            }

            return await query
                .OrderBy(bb => bb.GameSeq)
                .ThenBy(bb => bb.Order)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取打擊者資料時發生錯誤");
            return Enumerable.Empty<BatterBox>();
        }
    }

    /// <summary>
    /// 取得投手資料，並可依球員編號及賽季篩選
    /// </summary>
    /// <param name="playerId">
    /// 球員編號
    /// </param>
    /// <param name="seasonId">
    /// 賽季識別碼，格式例如 "CPBL-2024-HE"
    /// </param>
    /// <returns>
    /// 所有投手資料的集合
    /// </returns>
    public async Task<IEnumerable<PitcherBox>> GetPitcherBoxAsync(string? playerId = null, string? seasonId = null)
    {
        try
        {
            var query = _context.PitcherBoxes
                        .Include(pb => pb.Game!)
                            .ThenInclude(g => g.AwayTeam)
                        .Include(pb => pb.Game!)
                            .ThenInclude(g => g.HomeTeam)
                        .Include(pb => pb.Pitcher!)
                        .AsQueryable();

            if (!string.IsNullOrEmpty(playerId))
            {
                query = query.Where(pb => pb.PlayerId == playerId);
            }

            if (!string.IsNullOrEmpty(seasonId) && seasonId != "ALL")
            {
                query = query.Where(pb => pb.SeasonId == seasonId);
            }

            return await query
                .OrderBy(pb => pb.GameSeq)
                .ThenBy(pb => pb.Order)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取投手資料時發生錯誤");
            return Enumerable.Empty<PitcherBox>();
        }
    }

    public async Task<IEnumerable<PA>> GetPAAsync(string? batterId = null, int? gameSeq = null, string? seasonId = null)
    {
        try
        {
            var query = _context.PAs
                .Include(pa => pa.Game!)
                    .ThenInclude(g => g.Stadium)
                .Include(pa => pa.Game!)
                    .ThenInclude(g => g.HomeTeam)
                .Include(pa => pa.Game!)
                    .ThenInclude(g => g.AwayTeam)
                .AsQueryable();

            if (!string.IsNullOrEmpty(batterId))
            {
                query = query.Where(pa => pa.BatterId == batterId);
            }

            if (gameSeq.HasValue)
            {
                query = query.Where(pa => pa.GameSeq == gameSeq.Value);
            }

            if (!string.IsNullOrEmpty(seasonId) && seasonId != "ALL")
            {
                query = query.Where(pa => pa.SeasonId == seasonId);
            }

            return await query
                .OrderBy(pa => pa.GameSeq)
                .ThenBy(pa => pa.Inning)
                .ThenBy(pa => pa.PaSeq)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取打席資料時發生錯誤");
            return Enumerable.Empty<PA>();
        }
    }

    public async Task<IEnumerable<Event>> GetEventsAsync(int paId)
    {
        try
        {
            return await _context.Events
                .Where(e => e.PaId == paId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取事件資料時發生錯誤，paId={PaId}", paId);
            return Enumerable.Empty<Event>();
        }
    }

    /// <summary>
    /// 取得所有打者資料，並可依賽季篩選
    /// </summary>
    /// <param name="seasonId">
    /// 賽季識別碼，格式例如 "CPBL-2024-HE"
    /// </param>
    /// <returns>
    /// 所有打者資料的集合
    /// </returns>
    public async Task<IEnumerable<Batter>> GetAllBattersAsync(string? seasonId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(seasonId) || seasonId == "ALL")
            {
                return await _context.Batters
                    .Include(b => b.PlayerTeams)
                    .ToListAsync();
            }

            List<string> batterIds = new List<string>();

            // 依據賽季篩選：取得該賽季有出賽的打者
            if (seasonId != null)
            {
                batterIds = await _context.BatterBoxes
                    .Where(bb => bb.SeasonId == seasonId)
                    .Select(bb => bb.PlayerId)
                    .Where(id => id != null)
                    .Distinct()
                    .ToListAsync();
            }
            else
            {
                batterIds = await _context.Batters
                    .Select(b => b.PlayerId)
                    .ToListAsync();
            }

            return await _context.Batters
                .Include(b => b.PlayerTeams)
                .Where(b => batterIds.Contains(b.PlayerId))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取打者資料時發生錯誤");
            return Enumerable.Empty<Batter>();
        }
    }

    /// <summary>
    /// 取得指定打者資料
    /// </summary>
    /// <param name="playerId">
    /// 球員識別碼
    /// </param>
    /// <returns>
    /// 指定打者的資料集合
    /// </returns>
    public async Task<Batter?> GetBatterAsync(string playerId)
    {
        try
        {
            return await _context.Batters
                .Include(b => b.PlayerTeams)
                .Where(b => b.PlayerId == playerId)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取打者資料時發生錯誤，playerId={PlayerId}", playerId);
            return null;
        }
    }

    /// <summary>
    /// 取得所有投手資料，並可依賽季篩選
    /// </summary>
    /// <param name="seasonId">
    /// 賽季識別碼，格式例如 "CPBL-2024-HE"
    /// </param>
    /// <returns>
    /// 所有投手資料的集合
    /// </returns>
    public async Task<IEnumerable<Pitcher>> GetAllPitchersAsync(string? seasonId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(seasonId) || seasonId == "ALL")
            {
                return await _context.Pitchers.ToListAsync();
            }

            // 依賽季篩選：取得該賽季有出賽的投手
            var pitcherIds = await _context.PitcherBoxes
                .Where(pb => pb.SeasonId == seasonId)
                .Select(pb => pb.PlayerId)
                .Distinct()
                .ToListAsync();

            return await _context.Pitchers
                .Where(p => pitcherIds.Contains(p.PlayerId))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取投手資料時發生錯誤");
            return Enumerable.Empty<Pitcher>();
        }
    }

    /// <summary>
    /// 取得指定投手資料
    /// </summary>
    /// <param name="playerId">
    /// 球員識別碼
    /// </param>
    /// <returns>
    /// 指定投手的資料集合
    /// </returns>
     public async Task<Pitcher?> GetPitcherAsync(string playerId)
    {
        try
        {
            return await _context.Pitchers
                .Include(p => p.PlayerTeams)
                .Where(p => p.PlayerId == playerId)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取投手資料時發生錯誤，playerId={PlayerId}", playerId);
            return null;
        }
    }

    public async Task<IEnumerable<Team>> GetAllTeamsAsync(string? seasonId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(seasonId) || seasonId == "ALL")
            {
                return await _context.Teams.ToListAsync();
            }

            // 依賽季篩選：取得該賽季有出賽的球隊
            var teamIds = await _context.PlayerTeams
                .Where(pt => pt.SeasonId == seasonId)
                .Select(pt => pt.TeamId)
                .Distinct()
                .ToListAsync();

            return await _context.Teams
                .Where(t => teamIds.Contains(t.TeamId))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取團隊資料時發生錯誤");
            return Enumerable.Empty<Team>();
        }
    }

    public async Task<IEnumerable<Stadium>> GetAllStadiumsAsync()
    {
        try
        {
            return await _context.Stadiums.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀?�場?��??��??��??�誤");
            return Enumerable.Empty<Stadium>();
        }
    }

    public async Task<IEnumerable<Season>> GetAllSeasonsAsync(string? playerId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(playerId))
            {
                return await _context.Seasons
                    .OrderBy(s => s.SeasonId)
                    .ToListAsync();
            }

            // 依據球員篩選：取得該球員所參與的賽季
            var seasonIds = await _context.PlayerTeams
                .Where(pt => pt.PlayerId == playerId)
                .Select(pt => pt.SeasonId)
                .Distinct()
                .ToListAsync();

            return await _context.Seasons
                .Where(s => seasonIds.Contains(s.SeasonId))
                .OrderBy(s => s.SeasonId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取賽季資料時發生錯誤");
            return Enumerable.Empty<Season>();
        }
    }

    public async Task<Dictionary<string, string>> GetBatterNameMapAsync()
    {
        try
        {
            return await _context.Batters
                .ToDictionaryAsync(b => b.PlayerId, b => b.PlayerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建�??�員?�稱對照表�??��??�誤");
            return new Dictionary<string, string>();
        }
    }

    public async Task<Dictionary<string, string>> GetPitcherNameMapAsync()
    {
        try
        {
            return await _context.Pitchers
                .ToDictionaryAsync(p => p.PlayerId, p => p.PlayerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建�??��??�稱對照表�??��??�誤");
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// 取得指定賽季的前 N 名打者
    /// </summary>
    /// <param name="seasonId">
    /// 賽季編號，可為 null，表示計算整個職業生涯的數據
    /// </param>
    /// <param name="topN">
    /// 要取得的前 N 名打者數量
    /// </param>
    /// <returns>
    /// 打者打擊數據集合
    /// </returns>
    public async Task<IEnumerable<BattingStats>> GetTopBattersAsync(string? seasonId = null, int topN = 10)
    {
        try
        {
            // 儲存所有打者的打擊數據
            List<BattingStats> allStats = [];

            // 取得所有打者
            var batters = await GetAllBattersAsync(seasonId);

            // 計算每位打者的打擊數據
            foreach (var batter in batters)
            {
                var stats = await CalculateBattingStatsAsync(batter.PlayerId, seasonId);
                allStats.Add(stats);
            }

            // 依安打數排序並取前 N 名
            return allStats.OrderByDescending(s => s.Hits).Take(topN);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"取得前 {topN} 名打者資料時發生錯誤");
            return Enumerable.Empty<BattingStats>();
        }
    }

    public async Task<IEnumerable<PA>> GetPAsByGameAsync(string seasonId, int gameSeq)
    {
        try
        {
            return await _context.PAs
                .Where(pa => pa.SeasonId == seasonId && pa.GameSeq == gameSeq)
                .OrderBy(pa => pa.Inning)
                .ThenBy(pa => pa.PaSeq)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀?��?賽�?席�??��??��??�誤，seasonId={SeasonId}, gameSeq={GameSeq}", seasonId, gameSeq);
            return Enumerable.Empty<PA>();
        }
    }

    /// <summary>
    /// 計算指定打者在指定賽季的打擊數據
    /// </summary>
    /// <param name="playerId">
    /// 打者編號
    /// </param>
    /// <param name="seasonId">
    /// 賽季編號，可為 null，表示計算整個職業生涯的數據
    /// </param>
    /// <returns>
    /// 打擊數據物件
    /// </returns>
    public async Task<BattingStats> CalculateBattingStatsAsync(string playerId, string? seasonId = null)
    {
        // 取得基本資料（改為分段查詢避免複雜 LEFT JOIN 造成 SQLite 翻譯異常）
        var batter = await _context.Batters
            .Where(b => b.PlayerId == playerId)
            .FirstOrDefaultAsync();

        var playerTeam = await _context.PlayerTeams
            .Where(pt => pt.PlayerId == playerId && pt.IsActive && (seasonId == null || seasonId == "ALL" || pt.SeasonId == seasonId))
            .OrderByDescending(pt => pt.StartDate)
            .FirstOrDefaultAsync();

        string? teamName = null;
        string? seasonName = null;
        if (playerTeam != null)
        {
            teamName = await _context.Teams
                .Where(t => t.TeamId == playerTeam.TeamId)
                .Select(t => t.TeamName)
                .FirstOrDefaultAsync();

            seasonName = await _context.Seasons
                .Where(s => s.SeasonId == playerTeam.SeasonId)
                .Select(s => s.SeasonName)
                .FirstOrDefaultAsync();
        }

        // 取得打擊數據
        List<BatterBox> batterBoxes = await _context.BatterBoxes
            .Where(bb => bb.PlayerId == playerId && (seasonId == null || seasonId == "ALL" || bb.SeasonId == seasonId))
            .ToListAsync();

        // 初始化打擊數據
        BattingStats stats = new BattingStats
        {
            PlayerName =  batter?.PlayerName ?? "未知球員",
            Team = teamName ?? "未知球隊",
            Games = 0,
            PlateAppearances = 0,
            AtBats = 0,
            Hits = 0,
            Doubles = 0,
            Triples = 0,
            HomeRuns = 0,
            RBIs = 0,
            Runs = 0,
            StolenBases = 0,
            CaughtStealing = 0,
            Walks = 0,
            Strikeouts = 0,
            Season = seasonId ?? seasonName ?? "生涯"
        };

        // 累計打擊數據
        foreach (BatterBox box in batterBoxes)
        {
            stats.PlateAppearances += box.PA;
            stats.AtBats += box.AB;
            stats.Hits += box.H;
            stats.Doubles += box.TwoB;
            stats.Triples += box.ThreeB;
            stats.HomeRuns += box.HR;
            stats.RBIs += box.RBI;
            stats.Runs += box.R;
            stats.StolenBases += box.SB;
            stats.CaughtStealing += box.CS;
            stats.Walks += box.BB;
            stats.Strikeouts += box.SO;
        }

        // 以打擊盒資料計算出賽（不同比賽場次數）
        stats.Games = batterBoxes
            .Select(bb => bb.GameSeq)
            .Distinct()
            .Count();

        return stats;
    }
}
