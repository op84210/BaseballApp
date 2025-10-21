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
        var input = $@"{root}\data\CPBL-2024-OpenData\CPBL-2024-OpenData.json";
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

        // 建立 Code Tables
        CreateCodeTables(conn);

        // var ddl = @"

        //     -- tblBatterBox
        //     CREATE TABLE IF NOT EXISTS tblBatterBox (
        //         id INTEGER PRIMARY KEY AUTOINCREMENT,
        //         gameSeq INTEGER NOT NULL,
        //         homeOrAway TEXT NOT NULL,
        //         [order] INTEGER,
        //         playerId TEXT,
        //         PA INTEGER, AB INTEGER, R INTEGER, H INTEGER, RBI INTEGER,
        //         [2B] INTEGER, [3B] INTEGER, HR INTEGER,
        //         GIDP INTEGER, DP INTEGER, TP INTEGER,
        //         BB INTEGER, IBB INTEGER, HBP INTEGER, SO INTEGER,
        //         SH INTEGER, SF INTEGER, E INTEGER,
        //         SB INTEGER, CS INTEGER
        //     );
        //     CREATE INDEX IF NOT EXISTS idx_batterbox_game ON tblBatterBox(gameSeq, homeOrAway);
        //     CREATE INDEX IF NOT EXISTS idx_batterbox_player ON tblBatterBox(playerId);

        //     -- tblPitcherBox
        //     CREATE TABLE IF NOT EXISTS tblPitcherBox (
        //         gameSeq INTEGER NOT NULL,
        //         homeOrAway TEXT NOT NULL,
        //         [order] INTEGER NOT NULL,
        //         playerId TEXT,
        //         IPOuts INTEGER, NP INTEGER, BF INTEGER,
        //         H INTEGER, HR INTEGER,
        //         BB INTEGER, IBB INTEGER, HB INTEGER, SO INTEGER,
        //         R INTEGER, ER INTEGER,
        //         PRIMARY KEY (gameSeq, homeOrAway, [order])
        //     );
        //     CREATE INDEX IF NOT EXISTS idx_pitcherbox_player ON tblPitcherBox(playerId);

        //     -- tblPA
        //     CREATE TABLE IF NOT EXISTS tblPA (
        //         id INTEGER PRIMARY KEY AUTOINCREMENT,
        //         gameSeq INTEGER NOT NULL,
        //         homeOrAway TEXT NOT NULL,
        //         inning INTEGER,
        //         scored INTEGER,
        //         batterId TEXT,
        //         batterHand TEXT,
        //         pitcherId TEXT,
        //         pitcherHand TEXT,
        //         catcherId TEXT,
        //         paRound INTEGER,
        //         paOrder INTEGER,
        //         isPH INTEGER,
        //         awayScores INTEGER,
        //         homeScores INTEGER,
        //         strikes INTEGER,
        //         balls INTEGER,
        //         outs INTEGER,
        //         bases TEXT,
        //         homeWE TEXT,
        //         RE TEXT,
        //         result TEXT,
        //         RBI INTEGER,
        //         locationCode TEXT,
        //         trajectory TEXT,
        //         hardness TEXT,
        //         endAwayScores INTEGER,
        //         endHomeScores INTEGER,
        //         endOuts INTEGER,
        //         endBases TEXT,
        //         WPA TEXT,
        //         RE24 TEXT
        //     );
        //     CREATE INDEX IF NOT EXISTS idx_pa_game ON tblPA(gameSeq, homeOrAway, inning);
        //     CREATE INDEX IF NOT EXISTS idx_pa_batter ON tblPA(batterId);
        //     CREATE INDEX IF NOT EXISTS idx_pa_pitcher ON tblPA(pitcherId);
        // """;

        // using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        // Console.WriteLine("[OK] Tables created.");
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
                code TEXT PRIMARY KEY,
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
        InsertTblScores(conn, doc, masterData);

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
    private static void InsertTblScores(SqliteConnection conn, JsonDocument doc, MasterData masterData)
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
        var basesData = new Dictionary<string, string>
        {
            { "0", "無人在壘" },
            { "1", "一壘有人" },
            { "2", "二壘有人" },
            { "3", "一二壘有人" },
            { "4", "三壘有人" },
            { "5", "一三壘有人" },
            { "6", "二三壘有人" },
            { "7", "滿壘" }
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
    
    private class MasterData
    {
        public List<Stadium>? InsertedStadiums { get; set; } 
        public List<Season>? InsertedSeasons { get; set; }
        public List<Team>? InsertedTeams { get; set; } 
        public List<Batter>? InsertedBatters { get; set; } 
        public List<Pitcher>? InsertedPitchers { get; set; }
    }
}