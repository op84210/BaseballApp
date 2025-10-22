using BaseballApp.Models;
using Microsoft.Data.Sqlite;
using System.Data;

namespace BaseballApp.Services;

public interface IBaseballDbService
{
    Task<IEnumerable<Game>> GetGamesAsync(string? seasonId = null);
    Task<IEnumerable<BatterBox>> GetBatterBoxAsync(string? playerId = null, string? seasonId = null);
    Task<IEnumerable<PitcherBox>> GetPitcherBoxAsync(string? playerId = null, string? seasonId = null);
    Task<IEnumerable<PA>> GetPAAsync(string? batterId = null, int? gameSeq = null);
    Task<IEnumerable<Event>> GetEventsAsync(int paId);
    Task<BattingStats> CalculateBattingStatsAsync(string playerId, string? seasonId = null);
    Task<IEnumerable<Batter>> GetAllBattersAsync(string? seasonId = null);
    Task<IEnumerable<Pitcher>> GetAllPitchersAsync(string? seasonId = null);
    Task<IEnumerable<Team>> GetAllTeamsAsync();
    Task<IEnumerable<Stadium>> GetAllStadiumsAsync();
    Task<Dictionary<string, string>> GetBatterNameMapAsync();
    Task<Dictionary<string, string>> GetPitcherNameMapAsync();
    Task<IEnumerable<BattingStats>> GetTopBattersAsync(string? seasonId = null, int topN = 10);
    Task<IEnumerable<PA>> GetPAsByGameAsync(string seasonId, int gameSeq);
}

public class BaseballDbService : IBaseballDbService
{
    private readonly string _connectionString;
    private readonly ILogger<BaseballDbService> _logger;

    public BaseballDbService(IConfiguration configuration, ILogger<BaseballDbService> logger)
    {
        var dbPath = configuration.GetValue<string>("DatabasePath") 
                     ?? Path.Combine(Directory.GetCurrentDirectory(), "data", "baseball.db");
        _connectionString = $"Data Source={dbPath}";
        _logger = logger;
    }

    public async Task<IEnumerable<Game>> GetGamesAsync(string? seasonId = null)
    {
        var games = new List<Game>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT seasonId, seq, date, stadiumId, awayTeamId, homeTeamId FROM tblGame";
            if (!string.IsNullOrEmpty(seasonId))
            {
                sql += " WHERE seasonId = @seasonId";
            }
            sql += " ORDER BY date, seq";

            using var command = new SqliteCommand(sql, connection);
            if (!string.IsNullOrEmpty(seasonId))
            {
                command.Parameters.AddWithValue("@seasonId", seasonId);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                games.Add(new Game
                {
                    SeasonId = reader.GetString(0),
                    Seq = reader.GetInt32(1),
                    Date = DateTime.Parse(reader.GetString(2)),
                    StadiumId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    AwayTeamId = reader.IsDBNull(4) ? null : reader.GetString(4),
                    HomeTeamId = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取比賽資料時發生錯誤");
        }

        return games;
    }

    public async Task<IEnumerable<BatterBox>> GetBatterBoxAsync(string? playerId = null, string? seasonId = null)
    {
        var batterBoxes = new List<BatterBox>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT id, seasonId, gameSeq, homeOrAway, [order], subOrder, playerId,
                       PA, AB, R, H, RBI, [2B], [3B], HR, GIDP, DP, TP,
                       BB, IBB, HBP, SO, SH, SF, E, SB, CS
                FROM tblBatterBox
                WHERE 1=1";

            if (!string.IsNullOrEmpty(playerId))
            {
                sql += " AND playerId = @playerId";
            }
            if (!string.IsNullOrEmpty(seasonId))
            {
                sql += " AND seasonId = @seasonId";
            }

            using var command = new SqliteCommand(sql, connection);
            if (!string.IsNullOrEmpty(playerId))
            {
                command.Parameters.AddWithValue("@playerId", playerId);
            }
            if (!string.IsNullOrEmpty(seasonId))
            {
                command.Parameters.AddWithValue("@seasonId", seasonId);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                batterBoxes.Add(new BatterBox
                {
                    Id = reader.GetInt32(0),
                    SeasonId = reader.GetString(1),
                    GameSeq = reader.GetInt32(2),
                    HomeOrAway = reader.GetString(3),
                    Order = reader.GetInt32(4),
                    SubOrder = reader.GetInt32(5),
                    PlayerId = reader.IsDBNull(6) ? null : reader.GetString(6),
                    PA = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    AB = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                    R = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    H = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    RBI = reader.IsDBNull(11) ? null : reader.GetInt32(11),
                    TwoB = reader.IsDBNull(12) ? null : reader.GetInt32(12),
                    ThreeB = reader.IsDBNull(13) ? null : reader.GetInt32(13),
                    HR = reader.IsDBNull(14) ? null : reader.GetInt32(14),
                    GIDP = reader.IsDBNull(15) ? null : reader.GetInt32(15),
                    DP = reader.IsDBNull(16) ? null : reader.GetInt32(16),
                    TP = reader.IsDBNull(17) ? null : reader.GetInt32(17),
                    BB = reader.IsDBNull(18) ? null : reader.GetInt32(18),
                    IBB = reader.IsDBNull(19) ? null : reader.GetInt32(19),
                    HBP = reader.IsDBNull(20) ? null : reader.GetInt32(20),
                    SO = reader.IsDBNull(21) ? null : reader.GetInt32(21),
                    SH = reader.IsDBNull(22) ? null : reader.GetInt32(22),
                    SF = reader.IsDBNull(23) ? null : reader.GetInt32(23),
                    E = reader.IsDBNull(24) ? null : reader.GetInt32(24),
                    SB = reader.IsDBNull(25) ? null : reader.GetInt32(25),
                    CS = reader.IsDBNull(26) ? null : reader.GetInt32(26)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取打者成績時發生錯誤");
        }

        return batterBoxes;
    }

    public async Task<IEnumerable<PitcherBox>> GetPitcherBoxAsync(string? playerId = null, string? seasonId = null)
    {
        var pitcherBoxes = new List<PitcherBox>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT id, seasonId, gameSeq, homeOrAway, [order], playerId,
                       IPOuts, NP, BF, H, HR, BB, IBB, HB, SO, R, ER
                FROM tblPitcherBox
                WHERE 1=1";

            if (!string.IsNullOrEmpty(playerId))
            {
                sql += " AND playerId = @playerId";
            }
            if (!string.IsNullOrEmpty(seasonId))
            {
                sql += " AND seasonId = @seasonId";
            }

            using var command = new SqliteCommand(sql, connection);
            if (!string.IsNullOrEmpty(playerId))
            {
                command.Parameters.AddWithValue("@playerId", playerId);
            }
            if (!string.IsNullOrEmpty(seasonId))
            {
                command.Parameters.AddWithValue("@seasonId", seasonId);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                pitcherBoxes.Add(new PitcherBox
                {
                    Id = reader.GetInt32(0),
                    SeasonId = reader.GetString(1),
                    GameSeq = reader.GetInt32(2),
                    HomeOrAway = reader.GetString(3),
                    Order = reader.GetInt32(4),
                    PlayerId = reader.IsDBNull(5) ? null : reader.GetString(5),
                    IPOuts = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    NP = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    BF = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                    H = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    HR = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    BB = reader.IsDBNull(11) ? null : reader.GetInt32(11),
                    IBB = reader.IsDBNull(12) ? null : reader.GetInt32(12),
                    HB = reader.IsDBNull(13) ? null : reader.GetInt32(13),
                    SO = reader.IsDBNull(14) ? null : reader.GetInt32(14),
                    R = reader.IsDBNull(15) ? null : reader.GetInt32(15),
                    ER = reader.IsDBNull(16) ? null : reader.GetInt32(16)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取投手成績時發生錯誤");
        }

        return pitcherBoxes;
    }

    public async Task<IEnumerable<PA>> GetPAAsync(string? batterId = null, int? gameSeq = null)
    {
        var paList = new List<PA>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT ID, seasonId, gameSeq, homeOrAway, inning, paSeq, scored,
                       batterId, pitcherId, catcherId, strikes, balls, outs, bases,
                       homeWE, RE, result, RBI, locationCode, trajectory, hardness,
                       endAwayScores, endHomeScores, endOuts, endBases, WPA, RE24
                FROM tblPA
                WHERE 1=1";

            if (!string.IsNullOrEmpty(batterId))
            {
                sql += " AND batterId = @batterId";
            }
            if (gameSeq.HasValue)
            {
                sql += " AND gameSeq = @gameSeq";
            }

            using var command = new SqliteCommand(sql, connection);
            if (!string.IsNullOrEmpty(batterId))
            {
                command.Parameters.AddWithValue("@batterId", batterId);
            }
            if (gameSeq.HasValue)
            {
                command.Parameters.AddWithValue("@gameSeq", gameSeq.Value);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                paList.Add(new PA
                {
                    Id = reader.GetInt32(0),
                    SeasonId = reader.GetString(1),
                    GameSeq = reader.GetInt32(2),
                    HomeOrAway = reader.GetString(3),
                    Inning = reader.GetInt32(4),
                    PaSeq = reader.GetInt32(5),
                    Scored = reader.GetInt32(6) == 1,
                    BatterId = reader.IsDBNull(7) ? null : reader.GetString(7),
                    PitcherId = reader.IsDBNull(8) ? null : reader.GetString(8),
                    CatcherId = reader.IsDBNull(9) ? null : reader.GetString(9),
                    Strikes = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    Balls = reader.IsDBNull(11) ? null : reader.GetInt32(11),
                    Outs = reader.IsDBNull(12) ? null : reader.GetInt32(12),
                    Bases = reader.IsDBNull(13) ? null : reader.GetInt32(13),
                    HomeWE = reader.IsDBNull(14) ? null : reader.GetDecimal(14),
                    RE = reader.IsDBNull(15) ? null : reader.GetDecimal(15),
                    Result = reader.IsDBNull(16) ? null : reader.GetString(16),
                    RBI = reader.IsDBNull(17) ? null : reader.GetInt32(17),
                    LocationCode = reader.IsDBNull(18) ? null : reader.GetString(18),
                    Trajectory = reader.IsDBNull(19) ? null : reader.GetString(19),
                    Hardness = reader.IsDBNull(20) ? null : reader.GetString(20),
                    EndAwayScores = reader.IsDBNull(21) ? null : reader.GetInt32(21),
                    EndHomeScores = reader.IsDBNull(22) ? null : reader.GetInt32(22),
                    EndOuts = reader.IsDBNull(23) ? null : reader.GetInt32(23),
                    EndBases = reader.IsDBNull(24) ? null : reader.GetInt32(24),
                    WPA = reader.IsDBNull(25) ? null : reader.GetDecimal(25),
                    RE24 = reader.IsDBNull(26) ? null : reader.GetDecimal(26)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取打席資料時發生錯誤");
        }

        return paList;
    }

    public async Task<IEnumerable<Event>> GetEventsAsync(int paId)
    {
        var events = new List<Event>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT ID, paID, [order], type, inPlay, isStrike, isBall,
                       pitcherId, catcherId, batterId, pitchCode, pitchType
                FROM tblEvent
                WHERE paID = @paId
                ORDER BY [order]";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@paId", paId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                events.Add(new Event
                {
                    Id = reader.GetInt32(0),
                    PaId = reader.GetInt32(1),
                    Order = reader.GetInt32(2),
                    Type = reader.IsDBNull(3) ? null : reader.GetString(3),
                    InPlay = reader.GetInt32(4) == 1,
                    IsStrike = reader.GetInt32(5) == 1,
                    IsBall = reader.GetInt32(6) == 1,
                    PitcherId = reader.IsDBNull(7) ? null : reader.GetString(7),
                    CatcherId = reader.IsDBNull(8) ? null : reader.GetString(8),
                    BatterId = reader.IsDBNull(9) ? null : reader.GetString(9),
                    PitchCode = reader.IsDBNull(10) ? null : reader.GetString(10),
                    PitchType = reader.IsDBNull(11) ? null : reader.GetString(11)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取事件資料時發生錯誤");
        }

        return events;
    }

    public async Task<BattingStats> CalculateBattingStatsAsync(string playerId, string? seasonId = null)
    {
        try
        {
            var batterBoxes = await GetBatterBoxAsync(playerId, seasonId);
            
            var stats = new BattingStats
            {
                Season = seasonId ?? "All",
                PlateAppearances = batterBoxes.Sum(b => b.PA ?? 0),
                AtBats = batterBoxes.Sum(b => b.AB ?? 0),
                Hits = batterBoxes.Sum(b => b.H ?? 0),
                Doubles = batterBoxes.Sum(b => b.TwoB ?? 0),
                Triples = batterBoxes.Sum(b => b.ThreeB ?? 0),
                HomeRuns = batterBoxes.Sum(b => b.HR ?? 0),
                RBIs = batterBoxes.Sum(b => b.RBI ?? 0),
                Runs = batterBoxes.Sum(b => b.R ?? 0),
                StolenBases = batterBoxes.Sum(b => b.SB ?? 0),
                CaughtStealing = batterBoxes.Sum(b => b.CS ?? 0),
                Walks = batterBoxes.Sum(b => b.BB ?? 0),
                Strikeouts = batterBoxes.Sum(b => b.SO ?? 0),
                LastUpdated = DateTime.Now
            };

            // 從 tblBatter 取得球員名稱
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = "SELECT playerName, playerNumber FROM tblBatter WHERE playerId = @playerId";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@playerId", playerId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                stats.PlayerName = reader.GetString(0);
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"計算球員 {playerId} 成績時發生錯誤");
            throw;
        }
    }
   
    public async Task<IEnumerable<Batter>> GetAllBattersAsync(string? seasonId = null)
    {
        var batters = new List<Batter>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT playerId, playerNumber, playerName FROM tblBatter";
            
            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                batters.Add(new Batter
                {
                    PlayerId = reader.GetString(0),
                    PlayerNumber = reader.IsDBNull(1) ? null : reader.GetString(1),
                    PlayerName = reader.GetString(2)
                });
            }

            // 如果有 seasonId，用 LINQ 過濾出該季有出賽的打者
            if (!string.IsNullOrEmpty(seasonId))
            {
                var batterBoxes = await GetBatterBoxAsync(seasonId: seasonId);
                var activeBatterIds = batterBoxes.Select(b => b.PlayerId).Distinct().ToHashSet();
                
                return batters.Where(b => activeBatterIds.Contains(b.PlayerId)).ToList();
            }

            return batters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取打者資料時發生錯誤");
            return Enumerable.Empty<Batter>();
        }
    }

    public async Task<IEnumerable<Pitcher>> GetAllPitchersAsync(string? seasonId = null)
    {
        var pitchers = new List<Pitcher>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT playerId, playerNumber, playerName FROM tblPitcher";
            
            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                pitchers.Add(new Pitcher
                {
                    PlayerId = reader.GetString(0),
                    PlayerNumber = reader.IsDBNull(1) ? null : reader.GetString(1),
                    PlayerName = reader.GetString(2)
                });
            }

            // 如果有 seasonId，用 LINQ 過濾出該季有出賽的投手
            if (!string.IsNullOrEmpty(seasonId))
            {
                var pitcherBoxes = await GetPitcherBoxAsync(seasonId: seasonId);
                var activePitcherIds = pitcherBoxes.Select(p => p.PlayerId).Distinct().ToHashSet();
                
                return pitchers.Where(p => activePitcherIds.Contains(p.PlayerId)).ToList();
            }

            return pitchers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取投手資料時發生錯誤");
            return Enumerable.Empty<Pitcher>();
        }
    }

    public async Task<IEnumerable<Team>> GetAllTeamsAsync()
    {
        var teams = new List<Team>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT teamId, team FROM tblTeam";
            
            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                teams.Add(new Team
                {
                    TeamId = reader.GetString(0),
                    TeamName = reader.GetString(1)
                });
            }

            return teams;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取球隊資料時發生錯誤");
            return Enumerable.Empty<Team>();
        }
    }

    public async Task<IEnumerable<Stadium>> GetAllStadiumsAsync()
    {
        var stadiums = new List<Stadium>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT id, stadium FROM tblStadium";
            
            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                stadiums.Add(new Stadium
                {
                    Id = reader.GetInt32(0),
                    stadium = reader.GetString(1)
                });
            }

            return stadiums;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取球場資料時發生錯誤");
            return Enumerable.Empty<Stadium>();
        }
    }

    public async Task<Dictionary<string, string>> GetBatterNameMapAsync()
    {
        try
        {
            var batters = await GetAllBattersAsync();
            
            // 使用 LINQ 建立 ID -> Name 的字典
            return batters.ToDictionary(b => b.PlayerId, b => b.PlayerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立打者名稱對照表時發生錯誤");
            return new Dictionary<string, string>();
        }
    }

    public async Task<Dictionary<string, string>> GetPitcherNameMapAsync()
    {
        try
        {
            var pitchers = await GetAllPitchersAsync();
            
            // 使用 LINQ 建立 ID -> Name 的字典
            return pitchers.ToDictionary(p => p.PlayerId, p => p.PlayerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立投手名稱對照表時發生錯誤");
            return new Dictionary<string, string>();
        }
    }

    public async Task<IEnumerable<BattingStats>> GetTopBattersAsync(string? seasonId = null, int topN = 10)
    {
        try
        {
            var batterBoxes = await GetBatterBoxAsync(seasonId: seasonId);
            var batterNames = await GetBatterNameMapAsync();

            // 使用 LINQ 進行分組、聚合、排序
            var topBatters = batterBoxes
                .Where(b => !string.IsNullOrEmpty(b.PlayerId))
                .GroupBy(b => b.PlayerId)
                .Select(g => new BattingStats
                {
                    PlayerName = batterNames.GetValueOrDefault(g.Key ?? "", "未知"),
                    Season = seasonId ?? "All",
                    PlateAppearances = g.Sum(b => b.PA ?? 0),
                    AtBats = g.Sum(b => b.AB ?? 0),
                    Hits = g.Sum(b => b.H ?? 0),
                    Doubles = g.Sum(b => b.TwoB ?? 0),
                    Triples = g.Sum(b => b.ThreeB ?? 0),
                    HomeRuns = g.Sum(b => b.HR ?? 0),
                    RBIs = g.Sum(b => b.RBI ?? 0),
                    Runs = g.Sum(b => b.R ?? 0),
                    StolenBases = g.Sum(b => b.SB ?? 0),
                    CaughtStealing = g.Sum(b => b.CS ?? 0),
                    Walks = g.Sum(b => b.BB ?? 0),
                    Strikeouts = g.Sum(b => b.SO ?? 0),
                    LastUpdated = DateTime.Now
                })
                .OrderByDescending(s => s.Hits)  // 按安打數排序
                .Take(topN)
                .ToList();

            return topBatters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得打擊排行榜時發生錯誤");
            return Enumerable.Empty<BattingStats>();
        }
    }

    public async Task<IEnumerable<PA>> GetPAsByGameAsync(string seasonId, int gameSeq)
    {
        var paList = new List<PA>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT ID, seasonId, gameSeq, homeOrAway, inning, paSeq, scored,
                       batterId, pitcherId, catcherId, strikes, balls, outs, bases,
                       homeWE, RE, result, RBI, locationCode, trajectory, hardness,
                       endAwayScores, endHomeScores, endOuts, endBases, WPA, RE24
                FROM tblPA
                WHERE seasonId = @seasonId AND gameSeq = @gameSeq
                ORDER BY inning, paSeq";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@seasonId", seasonId);
            command.Parameters.AddWithValue("@gameSeq", gameSeq);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                paList.Add(new PA
                {
                    Id = reader.GetInt32(0),
                    SeasonId = reader.GetString(1),
                    GameSeq = reader.GetInt32(2),
                    HomeOrAway = reader.GetString(3),
                    Inning = reader.GetInt32(4),
                    PaSeq = reader.GetInt32(5),
                    Scored = reader.GetInt32(6) == 1,
                    BatterId = reader.IsDBNull(7) ? null : reader.GetString(7),
                    PitcherId = reader.IsDBNull(8) ? null : reader.GetString(8),
                    CatcherId = reader.IsDBNull(9) ? null : reader.GetString(9),
                    Strikes = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    Balls = reader.IsDBNull(11) ? null : reader.GetInt32(11),
                    Outs = reader.IsDBNull(12) ? null : reader.GetInt32(12),
                    Bases = reader.IsDBNull(13) ? null : reader.GetInt32(13),
                    HomeWE = reader.IsDBNull(14) ? null : reader.GetDecimal(14),
                    RE = reader.IsDBNull(15) ? null : reader.GetDecimal(15),
                    Result = reader.IsDBNull(16) ? null : reader.GetString(16),
                    RBI = reader.IsDBNull(17) ? null : reader.GetInt32(17),
                    LocationCode = reader.IsDBNull(18) ? null : reader.GetString(18),
                    Trajectory = reader.IsDBNull(19) ? null : reader.GetString(19),
                    Hardness = reader.IsDBNull(20) ? null : reader.GetString(20),
                    EndAwayScores = reader.IsDBNull(21) ? null : reader.GetInt32(21),
                    EndHomeScores = reader.IsDBNull(22) ? null : reader.GetInt32(22),
                    EndOuts = reader.IsDBNull(23) ? null : reader.GetInt32(23),
                    EndBases = reader.IsDBNull(24) ? null : reader.GetInt32(24),
                    WPA = reader.IsDBNull(25) ? null : reader.GetDecimal(25),
                    RE24 = reader.IsDBNull(26) ? null : reader.GetDecimal(26)
                });
            }

            return paList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取比賽打席資料時發生錯誤");
            return Enumerable.Empty<PA>();
        }
    }
}