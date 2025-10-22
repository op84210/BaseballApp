using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using BaseballApp.Models;
using Microsoft.Data.Sqlite;

namespace DataEtl;

class Program
{
    static async Task Main(string[] args)
    {
        /// 設定預設路徑與參數
        string root = @"c:\Users\kwlin\Desktop\ideas\BaseballApp";
        var input = $@"{root}\data\CPBL-2024-Challenge-OpenData\CPBL-2024-Challenge-OpenData.json";
        var dbPath = $@"{root}\data\baseball.db";

        /// 解析命令列參數
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--input", StringComparison.OrdinalIgnoreCase)) input = args[i + 1];
            if (args[i].Equals("--db", StringComparison.OrdinalIgnoreCase)) dbPath = args[i + 1];
        }

        // 執行 ETL
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        if (!File.Exists(input)) { Console.Error.WriteLine($"Input not found: {input}"); return; }

        // 連接 SQLite
        using var conn = new SqliteConnection($"Data Source={dbPath};Cache=Shared");
        conn.Open();

        // 建立資料表
        CreateTables(conn);

        // 讀取 JSON 檔案
        await using var fs = File.OpenRead(input);
        using var doc = await JsonDocument.ParseAsync(fs);
        
        // 檢查 JSON 格式（支援陣列或單一物件）
        if (doc.RootElement.ValueKind != JsonValueKind.Array && doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            Console.Error.WriteLine("Expected JSON array or object at root");
            return;
        }

        using var tx = conn.BeginTransaction();

        // 插入資料到資料表
        InsertTables(conn, doc);

        tx.Commit();
    }

    private static string? GetString(JsonElement obj, params string[] names)
    {
        foreach (var n in names)
            if (obj.TryGetProperty(n, out var el) && el.ValueKind == JsonValueKind.String)
                return el.GetString();
        return null;
    }

    private static bool GetBool(JsonElement obj, params string[] names)
    {
        foreach (var n in names)
            if (obj.TryGetProperty(n, out var el))
                return el.ValueKind == JsonValueKind.True;
        return false;
    }

    private static int GetInt(JsonElement obj, params string[] names)
    {
        foreach (var n in names)
            if (obj.TryGetProperty(n, out var el) && el.ValueKind == JsonValueKind.Number)
                return el.GetInt32();
        return 0;
    }

    private static int? GetIntNullable(JsonElement obj, params string[] names)
    {
        foreach (var n in names)
            if (obj.TryGetProperty(n, out var el) && el.ValueKind == JsonValueKind.Number)
                return el.GetInt32();
        return null;
    }

    private static decimal? GetDecimal(JsonElement obj, params string[] names)
    {
        foreach (var n in names)
        {
            if (obj.TryGetProperty(n, out var el))
            {
                if (el.ValueKind == JsonValueKind.Number)
                    return el.GetDecimal();
                if (el.ValueKind == JsonValueKind.String)
                {
                    var str = el.GetString();
                    if (!string.IsNullOrEmpty(str) && decimal.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value))
                        return value;
                }
            }
        }
        return null;
    }
    
    /// <summary>
    /// 取得比賽資料列舉（支援陣列或單一物件）
    /// </summary>
    private static IEnumerable<JsonElement> GetGames(JsonDocument doc)
    {
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            // JSON 陣列格式
            foreach (var game in doc.RootElement.EnumerateArray())
            {
                if (game.ValueKind == JsonValueKind.Object)
                    yield return game;
            }
        }
        else if (doc.RootElement.ValueKind == JsonValueKind.Object)
        {
            // 單一物件格式
            yield return doc.RootElement;
        }
    }
    
    /// <summary>
    /// 建立資料表
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    private static void CreateTables(SqliteConnection conn)
    {
        // 建立 Master Data Tables
        CreateTblStadium(conn);
        CreateTblSeason(conn);
        CreateTblTeam(conn);
        CreateTblBatter(conn);
        CreateTblPitcher(conn);

        // 建立 Game Tables
        CreateTblGame(conn);
        CreateTblScores(conn);

        // 建立 Stats Tables
        CreateTblBatterBox(conn);
        CreateTblPitcherBox(conn);

        // 建立 PA Tables
        CreateTblPA(conn);
        CreateTblEvent(conn);
        CreateTblRunner(conn);

        // 建立 Code Tables
        CreateCodeTables(conn);
    }

    /// <summary>
    /// 建立 tblStadium 資料表
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    private static void CreateTblStadium(SqliteConnection conn)
    {
        var ddl = @"
            -- tblStadium
            CREATE TABLE IF NOT EXISTS tblStadium (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                stadium TEXT NOT NULL UNIQUE
            );
        ";

        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] tblStadium created.");
    }

    /// <summary>
    /// 建立 tblSeason 資料表
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    private static void CreateTblSeason(SqliteConnection conn)
    {
        var ddl = @"
            -- tblSeason
            CREATE TABLE IF NOT EXISTS tblSeason (
                seasonId TEXT PRIMARY KEY,
                season TEXT NOT NULL
            );
        ";

        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] tblSeason created.");
    }

    /// <summary>
    /// 建立 tblTeam 資料表
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    private static void CreateTblTeam(SqliteConnection conn)
    {
        var ddl = @"
            -- tblTeam
            CREATE TABLE IF NOT EXISTS tblTeam (
                teamId TEXT PRIMARY KEY,
                team TEXT NOT NULL
            );
        ";

        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] tblTeam created.");
    }

    /// <summary>
    /// 建立 tblBatter 資料表
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    private static void CreateTblBatter(SqliteConnection conn)
    {
        var ddl = @"
            -- tblBatter
            CREATE TABLE IF NOT EXISTS tblBatter (
                playerId TEXT PRIMARY KEY,
                playerNumber TEXT,
                playerName TEXT NOT NULL
            );
        ";

        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] tblBatter created.");
    }

    /// <summary>
    /// 建立 tblPitcher 資料表
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    private static void CreateTblPitcher(SqliteConnection conn)
    {
        var ddl = @"
            -- tblPitcher
            CREATE TABLE IF NOT EXISTS tblPitcher (
                playerId TEXT PRIMARY KEY,
                playerNumber TEXT,
                playerName TEXT NOT NULL
            );
        ";

        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] tblPitcher created.");
    }

    /// <summary>
    /// 建立 tblGame 資料表
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    private static void CreateTblGame(SqliteConnection conn)
    {
        var ddl = @"
            -- tblGame
            CREATE TABLE IF NOT EXISTS tblGame (
                seasonId TEXT NOT NULL,
                seq INTEGER NOT NULL,
                date TEXT,
                stadiumId INTEGER,
                awayTeamId TEXT,
                homeTeamId TEXT,
                PRIMARY KEY (seasonId, seq)
            );
        ";

        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] tblGame created.");
    }

    /// <summary>
    /// 建立 tblScores 資料表
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    private static void CreateTblScores(SqliteConnection conn)
    {
        var ddl = @"
            -- tblScores
            CREATE TABLE IF NOT EXISTS tblScores (
                seasonId TEXT NOT NULL,
                gameSeq INTEGER NOT NULL,
                homeOrAway TEXT NOT NULL,
                inning INTEGER NOT NULL,
                score INTEGER,
                PRIMARY KEY (seasonId, gameSeq, homeOrAway, inning)
            );
        ";

        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] tblScores created.");
    }

    /// <summary>
    /// 建立 tblBatterBox 資料表
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    private static void CreateTblBatterBox(SqliteConnection conn)
    {
        var ddl = @"
            -- tblBatterBox
            CREATE TABLE IF NOT EXISTS tblBatterBox (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                seasonId TEXT NOT NULL,
                gameSeq INTEGER NOT NULL,
                homeOrAway TEXT NOT NULL,
                [order] INTEGER NOT NULL,
                subOrder INTEGER NOT NULL,
                playerId TEXT,
                PA INTEGER, AB INTEGER, R INTEGER, H INTEGER, RBI INTEGER,
                [2B] INTEGER, [3B] INTEGER, HR INTEGER,
                GIDP INTEGER, DP INTEGER, TP INTEGER,
                BB INTEGER, IBB INTEGER, HBP INTEGER, SO INTEGER,
                SH INTEGER, SF INTEGER, E INTEGER,
                SB INTEGER, CS INTEGER,
                UNIQUE(seasonId, gameSeq, homeOrAway, [order], subOrder)
            );
            CREATE INDEX IF NOT EXISTS idx_batterbox_game ON tblBatterBox(seasonId, gameSeq, homeOrAway);
            CREATE INDEX IF NOT EXISTS idx_batterbox_player ON tblBatterBox(playerId);
        ";

        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] tblBatterBox created.");
    }

    /// <summary>
    /// 建立 tblPitcherBox 資料表
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    private static void CreateTblPitcherBox(SqliteConnection conn)
    {
        var ddl = @"
            -- tblPitcherBox
            CREATE TABLE IF NOT EXISTS tblPitcherBox (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                seasonId TEXT NOT NULL,
                gameSeq INTEGER NOT NULL,
                homeOrAway TEXT NOT NULL,
                [order] INTEGER NOT NULL,
                playerId TEXT,
                IPOuts INTEGER, NP INTEGER, BF INTEGER,
                H INTEGER, HR INTEGER,
                BB INTEGER, IBB INTEGER, HB INTEGER, SO INTEGER,
                R INTEGER, ER INTEGER,
                UNIQUE(seasonId, gameSeq, homeOrAway, [order])
            );
            CREATE INDEX IF NOT EXISTS idx_pitcherbox_game ON tblPitcherBox(seasonId, gameSeq, homeOrAway);
            CREATE INDEX IF NOT EXISTS idx_pitcherbox_player ON tblPitcherBox(playerId);
        ";

        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] tblPitcherBox created.");
    }

    private static void CreateTblPA(SqliteConnection conn)
    {
        var ddl = @"
            -- tblPA
            CREATE TABLE IF NOT EXISTS tblPA (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                seasonId TEXT NOT NULL,
                gameSeq INTEGER NOT NULL,
                homeOrAway TEXT NOT NULL,
                inning INTEGER NOT NULL,
                paSeq INTEGER NOT NULL,
                scored INTEGER,
                batterId TEXT,
                batterHand TEXT,
                pitcherId TEXT,
                pitcherHand TEXT,
                catcherId TEXT,
                paRound INTEGER,
                paOrder INTEGER,
                isPH INTEGER,
                awayScores INTEGER,
                homeScores INTEGER,
                strikes INTEGER,
                balls INTEGER,
                outs INTEGER,
                bases INTEGER,
                homeWE REAL,
                RE REAL,
                result TEXT,
                RBI INTEGER,
                locationCode TEXT,
                trajectory TEXT,
                hardness TEXT,
                endAwayScores INTEGER,
                endHomeScores INTEGER,
                endOuts INTEGER,
                endBases INTEGER,
                WPA REAL,
                RE24 REAL,
                UNIQUE(seasonId, gameSeq, homeOrAway, inning, paSeq)
            );
            CREATE INDEX IF NOT EXISTS idx_pa_game ON tblPA(seasonId, gameSeq);
            CREATE INDEX IF NOT EXISTS idx_pa_batter ON tblPA(batterId);
            CREATE INDEX IF NOT EXISTS idx_pa_pitcher ON tblPA(pitcherId);
        ";

        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] tblPA created.");
    }

    private static void CreateTblEvent(SqliteConnection conn)
    {
        var ddl = @"
            -- tblEvent
            CREATE TABLE IF NOT EXISTS tblEvent (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                paID INTEGER NOT NULL,
                [order] INTEGER NOT NULL,
                type TEXT,
                inPlay INTEGER,
                isStrike INTEGER,
                isBall INTEGER,
                pitcherId TEXT,
                catcherId TEXT,
                batterId TEXT,
                pitchCode TEXT,
                pitchType TEXT,
                velocity REAL,
                coordX REAL,
                coordY REAL,
                UNIQUE(paID, [order]),
                FOREIGN KEY (paID) REFERENCES tblPA(id)
            );
            CREATE INDEX IF NOT EXISTS idx_event_pa ON tblEvent(paID);
        ";

        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] tblEvent created.");
    }

    private static void CreateTblRunner(SqliteConnection conn)
    {
        var ddl = @"
            -- tblRunner
            CREATE TABLE IF NOT EXISTS tblRunner (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                eventID INTEGER NOT NULL,
                type TEXT,
                runnerID TEXT,
                isOut INTEGER,
                scored INTEGER,
                isRBI INTEGER,
                isER INTEGER,
                ERPitcherID TEXT,
                FOREIGN KEY (eventID) REFERENCES tblEvent(id)
            );
            CREATE INDEX IF NOT EXISTS idx_runner_event ON tblRunner(eventID);
            CREATE INDEX IF NOT EXISTS idx_runner_runner ON tblRunner(runnerID);
        ";

        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] tblRunner created.");
    }

    /// <summary>
    /// 建立所有代碼資料表
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    private static void CreateCodeTables(SqliteConnection conn)
    {
        var ddl = @"
            -- tblCodeBases - 壘包狀況代碼
            CREATE TABLE IF NOT EXISTS tblCodeBases (
                code INTEGER PRIMARY KEY,
                name TEXT NOT NULL
            );

            -- tblCodePitchCode - 投球結果代碼
            CREATE TABLE IF NOT EXISTS tblCodePitchCode (
                code TEXT PRIMARY KEY,
                name TEXT NOT NULL
            );

            -- tblCodeEventType - 事件型態代碼
            CREATE TABLE IF NOT EXISTS tblCodeEventType (
                code TEXT PRIMARY KEY,
                name TEXT NOT NULL
            );

            -- tblCodePitchType - 球種代碼
            CREATE TABLE IF NOT EXISTS tblCodePitchType (
                code TEXT PRIMARY KEY,
                name TEXT NOT NULL
            );

            -- tblCodeRunnerType - 跑壘型態代碼
            CREATE TABLE IF NOT EXISTS tblCodeRunnerType (
                code TEXT PRIMARY KEY,
                name TEXT NOT NULL
            );

            -- tblCodeResult - 打席結果代碼
            CREATE TABLE IF NOT EXISTS tblCodeResult (
                code TEXT PRIMARY KEY,
                name TEXT NOT NULL
            );

            -- tblCodeTrajectory - 擊球彈道代碼
            CREATE TABLE IF NOT EXISTS tblCodeTrajectory (
                code TEXT PRIMARY KEY,
                name TEXT NOT NULL
            );

            -- tblCodeHardness - 擊球力道代碼
            CREATE TABLE IF NOT EXISTS tblCodeHardness (
                code TEXT PRIMARY KEY,
                name TEXT NOT NULL
            );
        ";

        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] Code tables created.");
    }

    /// <summary>
    /// 插入初始資料到資料表
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    private static void InsertTables(SqliteConnection conn, JsonDocument doc)
    {
        // 插入 Master Data
        MasterData masterData = new MasterData();
        masterData.InsertedStadiums = InsertTblStadium(conn, doc);
        masterData.InsertedSeasons = InsertTblSeason(conn, doc);
        masterData.InsertedTeams = InsertTblTeam(conn, doc);
        masterData.InsertedBatters = InsertTblBatter(conn, doc);
        masterData.InsertedPitchers = InsertTblPitcher(conn, doc);

        // 建立 Game Tables
        InsertTblGame(conn, doc, masterData);
        InsertTblScores(conn, doc);

        // 插入 Stats Tables
        InsertTblBatterBox(conn, doc);
        InsertTblPitcherBox(conn, doc);

        // 插入 PA Tables (依序執行,傳遞 ID Map)
        var paIdMap = InsertTblPA(conn, doc, masterData);
        var eventIdMap = InsertTblEvent(conn, doc, paIdMap, masterData);
        InsertTblRunner(conn, doc, eventIdMap, masterData);

        // 插入 Code Tables
        InsertCodeTables(conn);
    }

    /// <summary>
    /// 插入 tblStadium 初始資料
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    /// <param name="doc">
    /// JSON 文件
    /// </param>
    /// <returns>
    /// 比賽場地列表
    /// </returns>
    private static List<Stadium> InsertTblStadium(SqliteConnection conn, JsonDocument doc)
    {
        List<string> stadiums = new List<string>();

        // 遍歷每場比賽（支援陣列或單一物件）
        foreach (var game in GetGames(doc))
        {
            var stadium = GetString(game, "stadium");
            if (!string.IsNullOrEmpty(stadium) && !stadiums.Contains(stadium))
            {
                stadiums.Add(stadium);
            }
        }

        // 插入場地資料到 tblStadium 資料表
        foreach (var stadium in stadiums.Distinct())
        {
            var insStadium = conn.CreateCommand();
            insStadium.CommandText = "INSERT OR IGNORE INTO tblStadium(stadium) VALUES(@stad)";
            insStadium.Parameters.AddWithValue("@stad", stadium);
            insStadium.ExecuteNonQuery();
        }

        Console.WriteLine($"[OK] Inserted {stadiums.Count} records into tblStadium.");

        // 回傳插入的場地列表
        List<Stadium> insertedStadiums = new List<Stadium>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, stadium FROM tblStadium";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int id = reader.GetInt32(0);
            string name = reader.GetString(1);
            insertedStadiums.Add(new Stadium { Id = id, stadium = name });
        }

        return insertedStadiums;
    }

    /// <summary>
    /// 插入 tblSeason 初始資料
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    /// <param name="doc">
    /// JSON 文件
    /// </param>
    /// <returns>
    /// 比賽季別列表
    /// </returns>
    private static List<Season> InsertTblSeason(SqliteConnection conn, JsonDocument doc)
    {
        List<Season> seasons = new List<Season>();

        foreach (var game in GetGames(doc))
        {
            string seasonId = GetString(game, "seasonId") ?? "";
            string season = GetString(game, "season") ?? "";
            if (!string.IsNullOrEmpty(seasonId))
            {
                if (!seasons.Any(s => s.SeasonId == seasonId))
                    seasons.Add(new Season { SeasonId = seasonId, SeasonName = season });
            }
        }

        foreach (var season in seasons.Distinct())
        {
            var insSeason = conn.CreateCommand();
            insSeason.CommandText = "INSERT OR IGNORE INTO tblSeason(seasonId,season) VALUES(@sid,@sname)";
            insSeason.Parameters.AddWithValue("@sid", season.SeasonId);
            insSeason.Parameters.AddWithValue("@sname", season.SeasonName);
            insSeason.ExecuteNonQuery();
        }

        Console.WriteLine($"[OK] Inserted {seasons.Count} records into tblSeason.");
        return seasons;
    }

    /// <summary>
    /// 插入 tblTeam 初始資料
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    /// <param name="doc">
    /// JSON 文件
    /// </param>
    /// <returns>
    /// 球隊列表
    /// </returns>
    private static List<Team> InsertTblTeam(SqliteConnection conn, JsonDocument doc)
    {
        List<Team> teams = new List<Team>();

        foreach (var game in GetGames(doc))
        {
            string awayTeamId = GetString(game, "awayTeamId") ?? "";
            string awayTeam = GetString(game, "awayTeam") ?? "";
            if (!string.IsNullOrEmpty(awayTeamId))
            {
                if (!teams.Any(t => t.TeamId == awayTeamId))
                    teams.Add(new Team { TeamId = awayTeamId, TeamName = awayTeam });
            }

            string homeTeamId = GetString(game, "homeTeamId") ?? "";
            string homeTeam = GetString(game, "homeTeam") ?? "";
            if (!string.IsNullOrEmpty(homeTeamId))
            {
                if (!teams.Any(t => t.TeamId == homeTeamId))
                    teams.Add(new Team { TeamId = homeTeamId, TeamName = homeTeam });
            }
        }

        foreach (var team in teams.Distinct())
        {
            var insTeam = conn.CreateCommand();
            insTeam.CommandText = "INSERT OR IGNORE INTO tblTeam(teamId,team) VALUES (@tid,@tname)";
            insTeam.Parameters.AddWithValue("@tid", team.TeamId);
            insTeam.Parameters.AddWithValue("@tname", team.TeamName);
            insTeam.ExecuteNonQuery();
        }

        Console.WriteLine($"[OK] Inserted {teams.Count} records into tblTeam.");
        return teams;
    }

    /// <summary>
    /// 插入 tblBatter 初始資料
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    /// <param name="doc">
    /// JSON 文件
    /// </param>
    /// <returns>
    /// 打者列表
    /// </returns>
    private static List<Batter> InsertTblBatter(SqliteConnection conn, JsonDocument doc)
    {
        // 儲存所有打者的資料
        // 包含 playerId playerName playerNumber
        List<Batter> batters = new List<Batter>();

        // 遍歷每場比賽
        foreach (var game in GetGames(doc))
        {
            // 從 awayBatterBox 中取得打者資料
            if (game.TryGetProperty("awayBatterBox", out var awayBat) && awayBat.ValueKind == JsonValueKind.Array)
            {
                foreach (var bat in awayBat.EnumerateArray())
                {
                    string playerId = GetString(bat, "playerId") ?? "";
                    string playerName = GetString(bat, "playerName") ?? "";
                    string playerNumber = GetString(bat, "playerNumber") ?? "";

                    if (!string.IsNullOrEmpty(playerId))
                    {
                        // 檢查打者是否已存在
                        if (!batters.Any(b => b.PlayerId == playerId))
                            batters.Add(new Batter { PlayerId = playerId, PlayerName = playerName, PlayerNumber = playerNumber });
                    }
                }
            }

            // 從 homeBatterBox 中取得打者資料
            if (game.TryGetProperty("homeBatterBox", out var homeBat) && homeBat.ValueKind == JsonValueKind.Array)
            {
                foreach (var bat in homeBat.EnumerateArray())
                {
                    string playerId = GetString(bat, "playerId") ?? "";
                    string playerName = GetString(bat, "playerName") ?? "";
                    string playerNumber = GetString(bat, "playerNumber") ?? "";

                    if (!string.IsNullOrEmpty(playerId))
                    {
                        // 檢查打者是否已存在
                        if (!batters.Any(b => b.PlayerId == playerId))
                            batters.Add(new Batter { PlayerId = playerId, PlayerName = playerName, PlayerNumber = playerNumber });
                    }
                }
            }
        }

        // 插入打者資料到 tblBatter 資料表
        foreach (var batter in batters.Distinct())
        {
            var insBatter = conn.CreateCommand();
            insBatter.CommandText = "INSERT OR IGNORE INTO tblBatter(playerId,playerNumber,playerName) VALUES(@bid,@bnumber,@bname)";
            insBatter.Parameters.AddWithValue("@bid", batter.PlayerId);
            insBatter.Parameters.AddWithValue("@bnumber", batter.PlayerNumber);
            insBatter.Parameters.AddWithValue("@bname", batter.PlayerName);
            insBatter.ExecuteNonQuery();
        }

        Console.WriteLine($"[OK] Inserted {batters.Count} records into tblBatter.");
        return batters;
    }

    /// <summary>
    /// 插入 tblPitcher 初始資料
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    /// <param name="doc">
    /// JSON 文件
    /// </param>
    /// <returns>
    /// 投手列表
    /// </returns>
    private static List<Pitcher> InsertTblPitcher(SqliteConnection conn, JsonDocument doc)
    {
        // 儲存所有投手的資料
        // 包含 playerId playerName playerNumber
        List<Pitcher> pitchers = new List<Pitcher>();

        // 遍歷每場比賽
        foreach (var game in GetGames(doc))
        {
            // 從 awayPitcherBox 中取得投手資料
            if (game.TryGetProperty("awayPitcherBox", out var awayPit) && awayPit.ValueKind == JsonValueKind.Array)
            {
                foreach (var pit in awayPit.EnumerateArray())
                {
                    string playerId = GetString(pit, "playerId") ?? "";
                    string playerName = GetString(pit, "playerName") ?? "";
                    string playerNumber = GetString(pit, "playerNumber") ?? "";

                    if (!string.IsNullOrEmpty(playerId))
                    {
                        if (!pitchers.Any(p => p.PlayerId == playerId))
                            pitchers.Add(new Pitcher { PlayerId = playerId, PlayerName = playerName, PlayerNumber = playerNumber });
                    }
                }
            }

            // 從 homePitcherBox 中取得投手資料
            if (game.TryGetProperty("homePitcherBox", out var homePit) && homePit.ValueKind == JsonValueKind.Array)
            {
                foreach (var pit in homePit.EnumerateArray())
                {
                    string playerId = GetString(pit, "playerId") ?? "";
                    string playerName = GetString(pit, "playerName") ?? "";
                    string playerNumber = GetString(pit, "playerNumber") ?? "";

                    if (!string.IsNullOrEmpty(playerId))
                    {
                        if (!pitchers.Any(p => p.PlayerId == playerId))
                            pitchers.Add(new Pitcher { PlayerId = playerId, PlayerName = playerName, PlayerNumber = playerNumber });
                    }
                }
            }
        }

        // 插入投手資料到 tblPitcher 資料表
        foreach (var pitcher in pitchers.Distinct())
        {
            var insPitcher = conn.CreateCommand();
            insPitcher.CommandText = "INSERT OR IGNORE INTO tblPitcher(playerId,playerNumber,playerName) VALUES(@pid,@pnumber,@pname)";
            insPitcher.Parameters.AddWithValue("@pid", pitcher.PlayerId);
            insPitcher.Parameters.AddWithValue("@pnumber", pitcher.PlayerNumber);
            insPitcher.Parameters.AddWithValue("@pname", pitcher.PlayerName);
            insPitcher.ExecuteNonQuery();
        }

        Console.WriteLine($"[OK] Inserted {pitchers.Count} records into tblPitcher.");
        return pitchers;
    }

    /// <summary>
    /// 插入 tblGame 初始資料
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    /// <param name="doc">
    /// JSON 文件
    /// </param>
    private static void InsertTblGame(SqliteConnection conn, JsonDocument doc, MasterData masterData)
    {
        List<Game> games = new List<Game>();

        // 儲存所有比賽的資料
        foreach (var game in GetGames(doc))
        {
            var seasonId = GetString(game, "seasonId") ?? "";
            var seq = GetInt(game, "seq");
            DateTime gameDate = DateTime.Parse(GetString(game, "date") ?? "");
            var stadiumId = masterData.InsertedStadiums!.FirstOrDefault(s => s.stadium == GetString(game, "stadium"))?.Id ?? 0;
            var homeTeamId = GetString(game, "homeTeamId") ?? "";
            var awayTeamId = GetString(game, "awayTeamId") ?? "";

            games.Add(new Game
            {
                SeasonId = seasonId,
                Seq = seq,
                Date = gameDate,
                StadiumId = stadiumId,
                HomeTeamId = homeTeamId,
                AwayTeamId = awayTeamId
            });
        }

        foreach (var game in games.Distinct())
        {
            var insGame = conn.CreateCommand();
            insGame.CommandText = @"
                INSERT OR IGNORE INTO tblGame(seasonId,seq,date,stadiumId,awayTeamId,homeTeamId)
                VALUES(@sid,@seq,@date,@stadid,@atid,@htid)";
            insGame.Parameters.AddWithValue("@sid", game.SeasonId);
            insGame.Parameters.AddWithValue("@seq", game.Seq);
            insGame.Parameters.AddWithValue("@date", game.Date.ToString("yyyy-MM-dd"));
            insGame.Parameters.AddWithValue("@stadid", game.StadiumId);
            insGame.Parameters.AddWithValue("@atid", game.AwayTeamId);
            insGame.Parameters.AddWithValue("@htid", game.HomeTeamId);
            insGame.ExecuteNonQuery();
        }

        Console.WriteLine($"[OK] Inserted {games.Count} records into tblGame.");
    }

    /// <summary>
    /// 插入 tblScores 初始資料
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    /// <param name="doc">
    /// JSON 文件
    /// </param>
    private static void InsertTblScores(SqliteConnection conn, JsonDocument doc)
    {
        // 使用 LINQ 收集所有得分資料
        var scores = GetGames(doc)
            .Where(game => game.TryGetProperty("homeScores", out _) && game.TryGetProperty("awayScores", out _))
            .SelectMany(game =>
            {
                var seasonId = GetString(game, "seasonId") ?? "";
                var seq = GetInt(game, "seq");
                
                // 解析主隊得分
                var homeScores = ParseScoreArray(game.GetProperty("homeScores"))
                    .Select((score, index) => new Scores
                    {
                        SeasonId = seasonId,
                        GameSeq = seq,
                        HomeOrAway = "H",
                        Inning = index + 1,
                        Score = score
                    });

                // 解析客隊得分
                var awayScores = ParseScoreArray(game.GetProperty("awayScores"))
                    .Select((score, index) => new Scores
                    {
                        SeasonId = seasonId,
                        GameSeq = seq,
                        HomeOrAway = "A",
                        Inning = index + 1,
                        Score = score
                    });

                return homeScores.Concat(awayScores);
            })
            .ToList();

        Console.WriteLine($"[INFO] Collected {scores.Count} score records");

        // 批次插入得分資料
        foreach (var score in scores)
        {
            var insScore = conn.CreateCommand();
            insScore.CommandText = @"
                INSERT OR IGNORE INTO tblScores(seasonId,gameSeq,homeOrAway,inning,score)
                VALUES(@sid,@gseq,@hoa,@inning,@score)";
            insScore.Parameters.AddWithValue("@sid", score.SeasonId);
            insScore.Parameters.AddWithValue("@gseq", score.GameSeq);
            insScore.Parameters.AddWithValue("@hoa", score.HomeOrAway);
            insScore.Parameters.AddWithValue("@inning", score.Inning);
            insScore.Parameters.AddWithValue("@score", score.Score);
            insScore.ExecuteNonQuery();
        }

        Console.WriteLine($"[OK] Inserted {scores.Count} records into tblScores.");
    }

    /// <summary>
    /// 插入 tblBatterBox 初始資料
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    /// <param name="doc">
    /// JSON 文件
    /// </param>
    private static void InsertTblBatterBox(SqliteConnection conn, JsonDocument doc)
    {
        // 收集客隊打者成績資料
        var awayBatterBoxes = GetGames(doc)
            .SelectMany(game =>
            {
                var seasonId = GetString(game, "seasonId") ?? "";
                var seq = GetInt(game, "seq");
                return ParseBatterBox(game, "awayBatterBox", seasonId, seq, "A");
            })
            .ToList();

        // 插入客隊打者成績資料
        InsertTblBatterBox(conn, awayBatterBoxes);

        Console.WriteLine($"[OK] Inserted {awayBatterBoxes.Count} away batter box records into tblBatterBox.");

        // 收集主隊打者成績資料
        var homeBatterBoxes = GetGames(doc)
            .SelectMany(game =>
            {
                var seasonId = GetString(game, "seasonId") ?? "";
                var seq = GetInt(game, "seq");
                return ParseBatterBox(game, "homeBatterBox", seasonId, seq, "H");
            })
            .ToList();

        // 插入主隊打者成績資料
        InsertTblBatterBox(conn, homeBatterBoxes);

        Console.WriteLine($"[OK] Inserted {homeBatterBoxes.Count} home batter box records into tblBatterBox.");
    }

    /// <summary>
    /// 插入 tblPitcherBox 初始資料
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    /// <param name="doc">
    /// JSON 文件
    /// </param>
    private static void InsertTblPitcherBox(SqliteConnection conn, JsonDocument doc)
    {
        // 收集客隊投手成績資料
        var awayPitcherBoxes = GetGames(doc)
            .SelectMany(game =>
            {
                var seasonId = GetString(game, "seasonId") ?? "";
                var seq = GetInt(game, "seq");
                return ParsePitcherBox(game, "awayPitcherBox", seasonId, seq, "A");
            })
            .ToList();

        // 插入客隊投手成績資料
        InsertTblPitcherBox(conn, awayPitcherBoxes);

        Console.WriteLine($"[OK] Inserted {awayPitcherBoxes.Count} away pitcher box records into tblPitcherBox.");

        // 收集主隊投手成績資料
        var homePitcherBoxes = GetGames(doc)
            .SelectMany(game =>
            {
                var seasonId = GetString(game, "seasonId") ?? "";
                var seq = GetInt(game, "seq");
                return ParsePitcherBox(game, "homePitcherBox", seasonId, seq, "H");
            })
            .ToList();

        // 插入主隊投手成績資料
        InsertTblPitcherBox(conn, homePitcherBoxes);

        Console.WriteLine($"[OK] Inserted {homePitcherBoxes.Count} home pitcher box records into tblPitcherBox.");
    }

    private static void InsertTblPitcherBox(SqliteConnection conn, IEnumerable<PitcherBox> pitcherBoxes)
    {
        foreach (var box in pitcherBoxes)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR IGNORE INTO tblPitcherBox(
                    seasonId, gameSeq, homeOrAway, [order], playerId,
                    IPOuts, NP, BF, H, HR, BB, IBB, HB, SO, R, ER
                ) VALUES(
                    @sid, @gseq, @hoa, @order, @pid,
                    @IPOuts, @NP, @BF, @H, @HR, @BB, @IBB, @HB, @SO, @R, @ER
                )";

            cmd.Parameters.AddWithValue("@sid", box.SeasonId);
            cmd.Parameters.AddWithValue("@gseq", box.GameSeq);
            cmd.Parameters.AddWithValue("@hoa", box.HomeOrAway);
            cmd.Parameters.AddWithValue("@order", box.Order);
            cmd.Parameters.AddWithValue("@pid", box.PlayerId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IPOuts", box.IPOuts ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@NP", box.NP ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BF", box.BF ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@H", box.H ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@HR", box.HR ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BB", box.BB ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IBB", box.IBB ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@HB", box.HB ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SO", box.SO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@R", box.R ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ER", box.ER ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 插入 tblPA 資料，並回傳 paIdMap (paKey -> paID)
    /// </summary>
    private static Dictionary<string, int> InsertTblPA(SqliteConnection conn, JsonDocument doc, MasterData masterData )
    {
        var allPaIdMap = new Dictionary<string, int>();
        
        // 處理所有遊戲
        foreach (var game in GetGames(doc))
        {
            var seasonId = GetString(game, "seasonId") ?? "";
            var gameSeq = GetInt(game, "seq");

            // 解析客隊 PA
            var awayPAList = ParsePA(game, "awayPAList", seasonId, gameSeq, "A", masterData);
            var paIdMap = InsertTblPA(conn, awayPAList);

            // 解析主隊 PA
            var homePAList = ParsePA(game, "homePAList", seasonId, gameSeq, "H", masterData);
            var homePaIdMap = InsertTblPA(conn, homePAList);

            // 合併到總 Map
            foreach (var kv in paIdMap)
                allPaIdMap[kv.Key] = kv.Value;
            foreach (var kv in homePaIdMap)
                allPaIdMap[kv.Key] = kv.Value;

            Console.WriteLine($"[OK] Game {seasonId}-{gameSeq} tblPA inserted: Away={awayPAList.Count()}, Home={homePAList.Count()}");
        }
        
        return allPaIdMap;
    }

    private static Dictionary<string, int> InsertTblPA(SqliteConnection conn, IEnumerable<(PA pa, string paKey)> paList)
    {
        var paIdMap = new Dictionary<string, int>();

        foreach (var (pa, paKey) in paList)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR IGNORE INTO tblPA(
                    seasonId, gameSeq, homeOrAway, inning, paSeq, scored,
                    batterId, batterHand, pitcherId, pitcherHand, catcherId,
                    paRound, paOrder, isPH,
                    awayScores, homeScores, strikes, balls, outs, bases,
                    homeWE, RE, result, RBI,
                    locationCode, trajectory, hardness,
                    endAwayScores, endHomeScores, endOuts, endBases,
                    WPA, RE24
                ) VALUES(
                    @sid, @gseq, @hoa, @inning, @paSeq, @scored,
                    @batterId, @batterHand, @pitcherId, @pitcherHand, @catcherId,
                    @paRound, @paOrder, @isPH,
                    @awayScores, @homeScores, @strikes, @balls, @outs, @bases,
                    @homeWE, @RE, @result, @RBI,
                    @locationCode, @trajectory, @hardness,
                    @endAwayScores, @endHomeScores, @endOuts, @endBases,
                    @WPA, @RE24
                );
                SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("@sid", pa.SeasonId);
            cmd.Parameters.AddWithValue("@gseq", pa.GameSeq);
            cmd.Parameters.AddWithValue("@hoa", pa.HomeOrAway);
            cmd.Parameters.AddWithValue("@inning", pa.Inning);
            cmd.Parameters.AddWithValue("@paSeq", pa.PaSeq);
            cmd.Parameters.AddWithValue("@scored", pa.Scored ? 1 : 0);
            cmd.Parameters.AddWithValue("@batterId", pa.BatterId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@batterHand", pa.BatterHand ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@pitcherId", pa.PitcherId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@pitcherHand", pa.PitcherHand ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@catcherId", pa.CatcherId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@paRound", pa.PaRound ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@paOrder", pa.PaOrder ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@isPH", pa.IsPH ? 1 : 0);
            cmd.Parameters.AddWithValue("@awayScores", pa.AwayScores ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@homeScores", pa.HomeScores ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@strikes", pa.Strikes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@balls", pa.Balls ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@outs", pa.Outs ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@bases", pa.Bases ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@homeWE", pa.HomeWE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RE", pa.RE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@result", pa.Result ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RBI", pa.RBI ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@locationCode", pa.LocationCode ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@trajectory", pa.Trajectory ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@hardness", pa.Hardness ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@endAwayScores", pa.EndAwayScores ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@endHomeScores", pa.EndHomeScores ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@endOuts", pa.EndOuts ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@endBases", pa.EndBases ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@WPA", pa.WPA ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RE24", pa.RE24 ?? (object)DBNull.Value);

            var result = cmd.ExecuteScalar();
            var paID = result != null ? Convert.ToInt32(result) : 0;
            
            // 只有成功插入（rowid > 0）才加入 map；若為 0 表示 INSERT OR IGNORE 衝突
            if (paID > 0)
            {
                paIdMap[paKey] = paID;
            }
            else
            {
                // INSERT OR IGNORE 衝突，需查詢已存在的 ID
                var queryCmd = conn.CreateCommand();
                queryCmd.CommandText = @"
                    SELECT id FROM tblPA 
                    WHERE seasonId=@sid AND gameSeq=@gseq AND homeOrAway=@hoa AND inning=@inning AND paSeq=@paSeq";
                queryCmd.Parameters.AddWithValue("@sid", pa.SeasonId);
                queryCmd.Parameters.AddWithValue("@gseq", pa.GameSeq);
                queryCmd.Parameters.AddWithValue("@hoa", pa.HomeOrAway);
                queryCmd.Parameters.AddWithValue("@inning", pa.Inning);
                queryCmd.Parameters.AddWithValue("@paSeq", pa.PaSeq);
                var existingID = queryCmd.ExecuteScalar();
                if (existingID != null)
                {
                    paIdMap[paKey] = Convert.ToInt32(existingID);
                }
            }
        }

        return paIdMap;
    }

    /// <summary>
    /// 插入 tblEvent 資料，並回傳 eventIdMap (eventKey -> eventID)
    /// </summary>
    private static Dictionary<string, int> InsertTblEvent(SqliteConnection conn, JsonDocument doc, Dictionary<string, int> paIdMap, MasterData masterData)
    {
        var allEventIdMap = new Dictionary<string, int>();
        
        // 處理所有遊戲
        foreach (var game in GetGames(doc))
        {
            var seasonId = GetString(game, "seasonId") ?? "";
            var gameSeq = GetInt(game, "seq");

            // 解析客隊 Event
            var awayEventList = ParseEvent(game, "awayPAList", seasonId, gameSeq, "A", paIdMap, masterData);
            var awayEventIdMap = InsertTblEvent(conn, awayEventList);

            // 解析主隊 Event
            var homeEventList = ParseEvent(game, "homePAList", seasonId, gameSeq, "H", paIdMap, masterData);
            var homeEventIdMap = InsertTblEvent(conn, homeEventList);

            // 合併到總 Map
            foreach (var kv in awayEventIdMap)
                allEventIdMap[kv.Key] = kv.Value;
            foreach (var kv in homeEventIdMap)
                allEventIdMap[kv.Key] = kv.Value;

            Console.WriteLine($"[OK] Game {seasonId}-{gameSeq} tblEvent inserted: Away={awayEventList.Count()}, Home={homeEventList.Count()}");
        }
        
        return allEventIdMap;
    }

    private static Dictionary<string, int> InsertTblEvent(SqliteConnection conn, IEnumerable<(Event evt, string eventKey)> eventList)
    {
        var eventIdMap = new Dictionary<string, int>();

        foreach (var (evt, eventKey) in eventList)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR IGNORE INTO tblEvent(
                    paID, [order], type, inPlay, isStrike, isBall,
                    pitcherId, catcherId, batterId,
                    pitchCode, pitchType, velocity, coordX, coordY
                ) VALUES(
                    @paID, @order, @type, @inPlay, @isStrike, @isBall,
                    @pitcherId, @catcherId, @batterId,
                    @pitchCode, @pitchType, @velocity, @coordX, @coordY
                );
                SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("@paID", evt.PaId);
            cmd.Parameters.AddWithValue("@order", evt.Order);
            cmd.Parameters.AddWithValue("@type", evt.Type ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@inPlay", evt.InPlay ? 1 : 0);
            cmd.Parameters.AddWithValue("@isStrike", evt.IsStrike ? 1 : 0);
            cmd.Parameters.AddWithValue("@isBall", evt.IsBall ? 1 : 0);
            cmd.Parameters.AddWithValue("@pitcherId", evt.PitcherId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@catcherId", evt.CatcherId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@batterId", evt.BatterId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@pitchCode", evt.PitchCode ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@pitchType", evt.PitchType ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@velocity", evt.Velocity ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@coordX", evt.CoordX ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@coordY", evt.CoordY ?? (object)DBNull.Value);

            var result = cmd.ExecuteScalar();
            var eventID = result != null ? Convert.ToInt32(result) : 0;
            
            // 只有成功插入（rowid > 0）才加入 map
            if (eventID > 0)
            {
                eventIdMap[eventKey] = eventID;
            }
            else
            {
                // INSERT OR IGNORE 衝突，需查詢已存在的 ID
                var queryCmd = conn.CreateCommand();
                queryCmd.CommandText = @"SELECT id FROM tblEvent WHERE paID=@paID AND [order]=@order";
                queryCmd.Parameters.AddWithValue("@paID", evt.PaId);
                queryCmd.Parameters.AddWithValue("@order", evt.Order);
                var existingID = queryCmd.ExecuteScalar();
                if (existingID != null)
                {
                    eventIdMap[eventKey] = Convert.ToInt32(existingID);
                }
            }
        }

        return eventIdMap;
    }

    /// <summary>
    /// 插入 tblRunner 資料
    /// </summary>
    private static void InsertTblRunner(SqliteConnection conn, JsonDocument doc, Dictionary<string, int> eventIdMap, MasterData masterData)
    {
        int totalAwayRunners = 0, totalHomeRunners = 0;

        // 處理所有遊戲
        foreach (var game in GetGames(doc))
        {
            var seasonId = GetString(game, "seasonId") ?? "";
            var gameSeq = GetInt(game, "seq");

            // 解析客隊 Runner
            var awayRunnerList = ParseRunner(game, "awayPAList", seasonId, gameSeq, "A", eventIdMap, masterData);
            InsertTblRunner(conn, awayRunnerList);

            // 解析主隊 Runner
            var homeRunnerList = ParseRunner(game, "homePAList", seasonId, gameSeq, "H", eventIdMap, masterData);
            InsertTblRunner(conn, homeRunnerList);

            totalAwayRunners += awayRunnerList.Count();
            totalHomeRunners += homeRunnerList.Count();

            Console.WriteLine($"[OK] Game {seasonId}-{gameSeq} tblRunner inserted: Away={awayRunnerList.Count()}, Home={homeRunnerList.Count()}");
        }
        
        Console.WriteLine($"[OK] Total tblRunner inserted: Away={totalAwayRunners}, Home={totalHomeRunners}");
    }

    private static void InsertTblRunner(SqliteConnection conn, IEnumerable<Runner> runnerList)
    {
        foreach (var runner in runnerList)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR IGNORE INTO tblRunner(
                    eventID, type, runnerID, isOut, scored, isRBI, isER, ERPitcherID
                ) VALUES(
                    @eventID, @type, @runnerID, @isOut, @scored, @isRBI, @isER, @ERPitcherID
                )";

            cmd.Parameters.AddWithValue("@eventID", runner.EventId);
            cmd.Parameters.AddWithValue("@type", runner.Type ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@runnerID", runner.RunnerId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@isOut", runner.IsOut ? 1 : 0);
            cmd.Parameters.AddWithValue("@scored", runner.Scored ? 1 : 0);
            cmd.Parameters.AddWithValue("@isRBI", runner.IsRBI ? 1 : 0);
            cmd.Parameters.AddWithValue("@isER", runner.IsER ? 1 : 0);
            cmd.Parameters.AddWithValue("@ERPitcherID", runner.ERPitcherId ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }
    }

    private static void InsertTblBatterBox(SqliteConnection conn, IEnumerable<BatterBox> BatterBoxes){

         foreach (var box in BatterBoxes)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR IGNORE INTO tblBatterBox(
                    seasonId, gameSeq, homeOrAway, [order], subOrder, playerId,
                    PA, AB, R, H, RBI, [2B], [3B], HR,
                    GIDP, DP, TP, BB, IBB, HBP, SO, SH, SF, E, SB, CS
                ) VALUES(
                    @sid, @gseq, @hoa, @order, @subOrder, @pid,
                    @PA, @AB, @R, @H, @RBI, @TwoB, @ThreeB, @HR,
                    @GIDP, @DP, @TP, @BB, @IBB, @HBP, @SO, @SH, @SF, @E, @SB, @CS
                )";

            cmd.Parameters.AddWithValue("@sid", box.SeasonId);
            cmd.Parameters.AddWithValue("@gseq", box.GameSeq);
            cmd.Parameters.AddWithValue("@hoa", box.HomeOrAway);
            cmd.Parameters.AddWithValue("@order", box.Order);
            cmd.Parameters.AddWithValue("@subOrder", box.SubOrder);
            cmd.Parameters.AddWithValue("@pid", box.PlayerId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@PA", box.PA ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@AB", box.AB ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@R", box.R ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@H", box.H ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RBI", box.RBI ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TwoB", box.TwoB ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ThreeB", box.ThreeB ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@HR", box.HR ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@GIDP", box.GIDP ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DP", box.DP ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TP", box.TP ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BB", box.BB ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IBB", box.IBB ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@HBP", box.HBP ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SO", box.SO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SH", box.SH ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SF", box.SF ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@E", box.E ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SB", box.SB ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CS", box.CS ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 解析 PA 資料
    /// </summary>
    private static IEnumerable<(PA pa, string paKey)> ParsePA(JsonElement root, string listName, string seasonId, int gameSeq, string homeOrAway, MasterData masterData)
    {
        if (!root.TryGetProperty(listName, out var paListElement) || paListElement.ValueKind != JsonValueKind.Array)
            return Enumerable.Empty<(PA, string)>();

        var paList = new List<(PA, string)>();
        int paSeq = 0;

        foreach (var paElement in paListElement.EnumerateArray())
        {
            paSeq++;
            var inning = GetInt(paElement, "inning");

            // 建立 paKey 用於 eventIdMap
            var paKey = $"{seasonId}-{gameSeq}-{homeOrAway}-{inning}-{paSeq}";

            // 解析 batterId, pitcherId, catcherId
            var batterName = GetString(paElement, "batterName");
            var batterId = batterName != null ? masterData.GetPlayerIdByName(batterName) : null;
            var pitcherName = GetString(paElement, "pitcherName");
            var pitcherId = pitcherName != null ? masterData.GetPlayerIdByName(pitcherName) : null;
            var catcherName = GetString(paElement, "catcherName");
            var catcherId = catcherName != null ? masterData.GetPlayerIdByName(catcherName) : null;

            var pa = new PA
            {
                SeasonId = seasonId,
                GameSeq = gameSeq,
                HomeOrAway = homeOrAway,
                Inning = inning,
                PaSeq = paSeq,
                Scored = GetBool(paElement, "scored"),
                BatterId = batterId,
                BatterHand = GetString(paElement, "batterHand"),
                PitcherId = pitcherId,
                PitcherHand = GetString(paElement, "pitcherHand"),
                CatcherId = catcherId,
                PaRound = GetIntNullable(paElement, "paRound"),
                PaOrder = GetIntNullable(paElement, "paOrder"),
                IsPH = GetBool(paElement, "isPH"),
                AwayScores = GetIntNullable(paElement, "awayScores"),
                HomeScores = GetIntNullable(paElement, "homeScores"),
                Strikes = GetIntNullable(paElement, "strikes"),
                Balls = GetIntNullable(paElement, "balls"),
                Outs = GetIntNullable(paElement, "outs"),
                Bases = GetIntNullable(paElement, "bases"),
                HomeWE = GetDecimal(paElement, "homeWE"),
                RE = GetDecimal(paElement, "RE"),
                Result = GetString(paElement, "result"),
                RBI = GetIntNullable(paElement, "RBI"),
                LocationCode = GetString(paElement, "locationCode"),
                Trajectory = GetString(paElement, "trajectory"),
                Hardness = GetString(paElement, "hardness"),
                EndAwayScores = GetIntNullable(paElement, "endAwayScores"),
                EndHomeScores = GetIntNullable(paElement, "endHomeScores"),
                EndOuts = GetIntNullable(paElement, "endOuts"),
                EndBases = GetIntNullable(paElement, "endBases"),
                WPA = GetDecimal(paElement, "WPA"),
                RE24 = GetDecimal(paElement, "RE24")
            };

            paList.Add((pa, paKey));
        }

        return paList;
    }

    /// <summary>
    /// 解析 Event 資料
    /// </summary>
    private static IEnumerable<(Event evt, string eventKey)> ParseEvent(JsonElement root, string listName, string seasonId, int gameSeq, string homeOrAway, Dictionary<string, int> paIdMap, MasterData masterData)
    {
        if (!root.TryGetProperty(listName, out var paListElement) || paListElement.ValueKind != JsonValueKind.Array)
            return Enumerable.Empty<(Event, string)>();

        var eventList = new List<(Event, string)>();
        int paSeq = 0;

        foreach (var paElement in paListElement.EnumerateArray())
        {
            paSeq++;
            var inning = GetInt(paElement, "inning");
            var paKey = $"{seasonId}-{gameSeq}-{homeOrAway}-{inning}-{paSeq}";

            if (!paIdMap.TryGetValue(paKey, out var paID))
                continue; // 找不到對應的 PA

            if (!paElement.TryGetProperty("events", out var eventsElement) || eventsElement.ValueKind != JsonValueKind.Array)
                continue;

            int eventOrder = 0;
            foreach (var eventElement in eventsElement.EnumerateArray())
            {
                eventOrder++;
                var eventKey = $"{paKey}-{eventOrder}";

                // 解析 velocity, coordX, coordY (支援數字或字串，允許正負小數)
                var velocity = GetDecimal(eventElement, "velocity");
                var coordX = GetDecimal(eventElement, "coordX");
                var coordY = GetDecimal(eventElement, "coordY");

                // 解析 pitcherId, catcherId, batterId
                var pitcherName = GetString(eventElement, "pitcherName");
                var pitcherId = pitcherName != null ? masterData.GetPlayerIdByName(pitcherName) : null;
                var catcherName = GetString(eventElement, "catcherName");
                var catcherId = catcherName != null ? masterData.GetPlayerIdByName(catcherName) : null;
                var batterName = GetString(eventElement, "batterName");
                var batterId = batterName != null ? masterData.GetPlayerIdByName(batterName) : null;

                var evt = new Event
                {
                    PaId = paID,
                    Order = eventOrder,
                    Type = GetString(eventElement, "type"),
                    InPlay = GetBool(eventElement, "inPlay"),
                    IsStrike = GetBool(eventElement, "isStrike"),
                    IsBall = GetBool(eventElement, "isBall"),
                    PitcherId = pitcherId,
                    CatcherId = catcherId,
                    BatterId = batterId,
                    PitchCode = GetString(eventElement, "pitchCode"),
                    PitchType = GetString(eventElement, "pitchType"),
                    Velocity = velocity,
                    CoordX = coordX,
                    CoordY = coordY
                };

                eventList.Add((evt, eventKey));
            }
        }

        return eventList;
    }

    /// <summary>
    /// 解析 Runner 資料
    /// </summary>
    private static IEnumerable<Runner> ParseRunner(JsonElement root, string listName, string seasonId, int gameSeq, string homeOrAway, Dictionary<string, int> eventIdMap, MasterData masterData)
    {
        if (!root.TryGetProperty(listName, out var paListElement) || paListElement.ValueKind != JsonValueKind.Array)
            return Enumerable.Empty<Runner>();

        var runnerList = new List<Runner>();
        int paSeq = 0;

        foreach (var paElement in paListElement.EnumerateArray())
        {
            paSeq++;
            var inning = GetInt(paElement, "inning");
            var paKey = $"{seasonId}-{gameSeq}-{homeOrAway}-{inning}-{paSeq}";

            if (!paElement.TryGetProperty("events", out var eventsElement) || eventsElement.ValueKind != JsonValueKind.Array)
                continue;

            int eventOrder = 0;
            foreach (var eventElement in eventsElement.EnumerateArray())
            {
                eventOrder++;
                var eventKey = $"{paKey}-{eventOrder}";

                if (!eventIdMap.TryGetValue(eventKey, out var eventID))
                    continue; // 找不到對應的 Event

                if (!eventElement.TryGetProperty("runners", out var runnersElement) || runnersElement.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var runnerElement in runnersElement.EnumerateArray())
                {
                    // 解析 runnerId 和 ERPitcherId
                    var runnerName = GetString(runnerElement, "runnerName");
                    var runnerId = runnerName != null ? masterData.GetPlayerIdByName(runnerName) : null;
                    var ERPitcherName = GetString(runnerElement, "ERPitcherName");
                    var ERPitcherId = ERPitcherName != null ? masterData.GetPlayerIdByName(ERPitcherName) : null;

                    var runner = new Runner
                    {
                        EventId = eventID,
                        Type = GetString(runnerElement, "type") ?? "",
                        RunnerId = runnerId ?? "",
                        IsOut = GetBool(runnerElement, "isOut"),
                        Scored = GetBool(runnerElement, "scored"),
                        IsRBI = GetBool(runnerElement, "isRBI"),
                        IsER = GetBool(runnerElement, "isER"),
                        ERPitcherId = ERPitcherId
                    };

                    runnerList.Add(runner);
                }
            }
        }

        return runnerList;
    }

    /// <summary>
    /// 解析打者成績資料
    /// </summary>
    private static IEnumerable<BatterBox> ParseBatterBox(JsonElement game, string boxName, string seasonId, int gameSeq, string homeOrAway)
    {
        if (!game.TryGetProperty(boxName, out var boxElement) || boxElement.ValueKind != JsonValueKind.Array)
            return Enumerable.Empty<BatterBox>();

        // 解析打者成績資料
        var batterBoxes = new List<BatterBox>();
        int TempOrder = 0;
        int subOrder = 0;

        foreach (var bat in boxElement.EnumerateArray())
        {
            int order = GetInt(bat, "order");

            // 同隊第二個相同打序即為替補上場
            subOrder = (order != TempOrder) ? 0 : subOrder + 1;
            if (order != TempOrder) TempOrder = order;

            var batterBox = new BatterBox
            {
                SeasonId = seasonId,
                GameSeq = gameSeq,
                HomeOrAway = homeOrAway,
                Order = order,
                SubOrder = subOrder,
                PlayerId = GetString(bat, "playerId"),
                PA = GetInt(bat, "PA"),
                AB = GetInt(bat, "AB"),
                R = GetInt(bat, "R"),
                H = GetInt(bat, "H"),
                RBI = GetInt(bat, "RBI"),
                TwoB = GetInt(bat, "2B"),
                ThreeB = GetInt(bat, "3B"),
                HR = GetInt(bat, "HR"),
                GIDP = GetInt(bat, "GIDP"),
                DP = GetInt(bat, "DP"),
                TP = GetInt(bat, "TP"),
                BB = GetInt(bat, "BB"),
                IBB = GetInt(bat, "IBB"),
                HBP = GetInt(bat, "HBP"),
                SO = GetInt(bat, "SO"),
                SH = GetInt(bat, "SH"),
                SF = GetInt(bat, "SF"),
                E = GetInt(bat, "E"),
                SB = GetInt(bat, "SB"),
                CS = GetInt(bat, "CS")
            };

            batterBoxes.Add(batterBox);
        }

        return batterBoxes;
    }

    /// <summary>
    /// 解析投手成績資料
    /// </summary>
    private static IEnumerable<PitcherBox> ParsePitcherBox(JsonElement game, string boxName, string seasonId, int gameSeq, string homeOrAway)
    {
        if (!game.TryGetProperty(boxName, out var boxElement) || boxElement.ValueKind != JsonValueKind.Array)
            return Enumerable.Empty<PitcherBox>();

        // 解析投手成績資料
        var pitcherBoxes = new List<PitcherBox>();

        foreach (var pit in boxElement.EnumerateArray())
        {
            var pitcherBox = new PitcherBox
            {
                SeasonId = seasonId,
                GameSeq = gameSeq,
                HomeOrAway = homeOrAway,
                Order = GetInt(pit, "order"),
                PlayerId = GetString(pit, "playerId"),
                IPOuts = GetInt(pit, "IPOuts"),
                NP = GetInt(pit, "NP"),
                BF = GetInt(pit, "BF"),
                H = GetInt(pit, "H"),
                HR = GetInt(pit, "HR"),
                BB = GetInt(pit, "BB"),
                IBB = GetInt(pit, "IBB"),
                HB = GetInt(pit, "HB"),
                SO = GetInt(pit, "SO"),
                R = GetInt(pit, "R"),
                ER = GetInt(pit, "ER")
            };

            pitcherBoxes.Add(pitcherBox);
        }

        return pitcherBoxes;
    }

    /// <summary>
    /// 解析得分陣列（支援數字或字串格式）
    /// </summary>
    private static IEnumerable<int> ParseScoreArray(JsonElement scoresElement)
    {
        if (scoresElement.ValueKind != JsonValueKind.Array)
            return Enumerable.Empty<int>();

        return scoresElement.EnumerateArray()
            .Select(element =>
            {
                if (element.ValueKind == JsonValueKind.Number)
                    return element.GetInt32();
                
                if (element.ValueKind == JsonValueKind.String)
                {
                    var scoreStr = element.GetString();
                    if (!string.IsNullOrEmpty(scoreStr) && int.TryParse(scoreStr, out int score))
                        return score;
                }
                
                return 0;
            });
    }

    /// <summary>
    /// 插入代碼資料到所有代碼表
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    private static void InsertCodeTables(SqliteConnection conn)
    {
        // tblCodeBases - 壘包狀況代碼
        var basesData = new Dictionary<int, string>
        {
            { 0, "無人在壘" },
            { 1, "一壘有人" },
            { 2, "二壘有人" },
            { 3, "一二壘有人" },
            { 4, "三壘有人" },
            { 5, "一三壘有人" },
            { 6, "二三壘有人" },
            { 7, "滿壘" }
        };
        InsertCodeTable(conn, "tblCodeBases", basesData);

        // tblCodePitchCode - 投球結果代碼
        var pitchCodeData = new Dictionary<string, string>
        {
            { "S", "無揮棒好球" },
            { "SW", "揮棒落空" },
            { "B", "壞球" },
            { "F", "界外" },
            { "FT", "擦棒被捕" },
            { "FOUL_BUNT", "觸擊界外" },
            { "TRY_BUNT", "觸擊落空" },
            { "BUNT", "觸擊" },
            { "H", "擊球進場" }
        };
        InsertCodeTable(conn, "tblCodePitchCode", pitchCodeData);

        // tblCodeEventType - 事件型態代碼
        var eventTypeData = new Dictionary<string, string>
        {
            { "PITCH", "投球事件，可能包含跑壘" },
            { "BASE", "無投球跑壘事件" },
            { "NO_PITCH", "無投球事件" },
            { "SUB", "換人事件" }
        };
        InsertCodeTable(conn, "tblCodeEventType", eventTypeData);

        // tblCodePitchType - 球種代碼
        var pitchTypeData = new Dictionary<string, string>
        {
            { "FF", "四縫" },
            { "SI", "伸卡/二縫" },
            { "FC", "卡特" },
            { "KN", "蝴蝶" },
            { "SL", "滑球" },
            { "CU", "曲球" },
            { "CH", "變速" },
            { "FO", "指叉" },
            { "FS", "快指" },
            { "EP", "小便" }
        };
        InsertCodeTable(conn, "tblCodePitchType", pitchTypeData);

        // tblCodeRunnerType - 跑壘型態代碼
        var runnerTypeData = new Dictionary<string, string>
        {
            { "PA", "打者" },
            { "ADVANCE", "推進" },
            { "SB", "盜壘成功" },
            { "CS", "盜壘失敗" },
            { "CS_E", "盜壘失敗但野手失誤上壘" },
            { "PO", "牽制出局" }
        };
        InsertCodeTable(conn, "tblCodeRunnerType", runnerTypeData);

        // tblCodeResult - 打席結果代碼
        var resultData = new Dictionary<string, string>
        {
            { "1B", "一安" },
            { "2B", "二安" },
            { "3B", "三安" },
            { "HR", "全壘打" },
            { "IHR", "場內全壘打" },
            { "SO", "三振" },
            { "uBB", "非故意四壞" },
            { "IBB", "故意四壞" },
            { "HBP", "觸身保送" },
            { "GO", "滾地出局" },
            { "FO", "飛球出局" },
            { "FC", "野手選擇" },
            { "E", "失誤" },
            { "SH", "犧牲觸擊" },
            { "SF", "犧牲飛球" },
            { "GIDP", "滾地雙殺" },
            { "DP", "雙殺" },
            { "TP", "三殺" },
            { "IH", "妨礙打擊" },
            { "IR", "妨礙跑壘" },
            { "ID", "妨礙守備" },
            { "IGNORE", "不算打席（壘包跑者出局導致半局結束等）" }
        };
        InsertCodeTable(conn, "tblCodeResult", resultData);

        // tblCodeTrajectory - 擊球彈道代碼
        var trajectoryData = new Dictionary<string, string>
        {
            { "G", "滾地" },
            { "L", "平飛" },
            { "F", "高飛" },
            { "P", "內野高飛" }
        };
        InsertCodeTable(conn, "tblCodeTrajectory", trajectoryData);

        // tblCodeHardness - 擊球力道代碼
        var hardnessData = new Dictionary<string, string>
        {
            { "S", "弱" },
            { "M", "中" },
            { "H", "強" }
        };
        InsertCodeTable(conn, "tblCodeHardness", hardnessData);
    }

    /// <summary>
    /// 插入代碼資料到指定代碼表
    /// </summary>
    /// <param name="conn">資料庫連線</param>
    /// <param name="tableName">資料表名稱</param>
    /// <param name="data">代碼資料 (code, name)</param>
    private static void InsertCodeTable(SqliteConnection conn, string tableName, Dictionary<string, string> data)
    {
        int count = 0;
        foreach (var item in data)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"INSERT OR IGNORE INTO {tableName}(code, name) VALUES(@code, @name)";
            cmd.Parameters.AddWithValue("@code", item.Key);
            cmd.Parameters.AddWithValue("@name", item.Value);
            cmd.ExecuteNonQuery();
            count++;
        }
        Console.WriteLine($"[OK] Inserted {count} records into {tableName}.");
    }

    // Overload for integer code tables (e.g., tblCodeBases)
    private static void InsertCodeTable(SqliteConnection conn, string tableName, Dictionary<int, string> data)
    {
        int count = 0;
        foreach (var item in data)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"INSERT OR IGNORE INTO {tableName}(code, name) VALUES(@code, @name)";
            cmd.Parameters.AddWithValue("@code", item.Key);
            cmd.Parameters.AddWithValue("@name", item.Value);
            cmd.ExecuteNonQuery();
            count++;
        }
        Console.WriteLine($"[OK] Inserted {count} records into {tableName}.");
    }

    private class MasterData
    {
        public List<Stadium>? InsertedStadiums { get; set; } 
        public List<Season>? InsertedSeasons { get; set; }
        public List<Team>? InsertedTeams { get; set; } 
        public List<Batter>? InsertedBatters { get; set; } 
        public List<Pitcher>? InsertedPitchers { get; set; }

        public string? GetPlayerIdByName(string playerName)
        {
            var batter = InsertedBatters?.FirstOrDefault(b => b.PlayerName == playerName);
            if (batter != null)
                return batter.PlayerId;

            var pitcher = InsertedPitchers?.FirstOrDefault(p => p.PlayerName == playerName);
            if (pitcher != null)
                return pitcher.PlayerId;

            return null;
        }
    }
}