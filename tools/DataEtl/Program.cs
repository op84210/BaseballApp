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
        var inputFiles = new List<string>
        {
            $@"{root}\data\CPBL-2024-Challenge-OpenData\CPBL-2024-Challenge-OpenData.json",
            $@"{root}\data\CPBL-2024-OpenData\CPBL-2024-OpenData.json",
            $@"{root}\data\CPBL-2024-TaiwanSeries-OpenData\CPBL-2024-TaiwanSeries-OpenData.json"
        };
        var dbPath = $@"{root}\data\baseball.db";

        /// 解析命令列參數
        if (args.Length > 0 && args[0].Equals("--db", StringComparison.OrdinalIgnoreCase) && args.Length > 1)
        {
            dbPath = args[1];
        }

        // 執行 ETL
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        // 連接 SQLite
        using var conn = new SqliteConnection($"Data Source={dbPath};Cache=Shared");
        conn.Open();

        // 建立資料表
        CreateTables(conn);

        // 依序處理每個 JSON 檔案
        foreach (var inputFile in inputFiles)
        {
            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"[SKIP] File not found: {inputFile}");
                continue;
            }

            Console.WriteLine($"\n[INFO] Processing: {Path.GetFileName(inputFile)}");

            // 讀取 JSON 檔案
            await using var fs = File.OpenRead(inputFile);
            using var doc = await JsonDocument.ParseAsync(fs);
            
            // 檢查 JSON 格式（支援陣列或單一物件）
            if (doc.RootElement.ValueKind != JsonValueKind.Array && doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                Console.Error.WriteLine($"[ERROR] Expected JSON array or object at root in {inputFile}");
                continue;
            }

            using var tx = conn.BeginTransaction();

            // 插入資料到資料表
            InsertTables(conn, doc);

            tx.Commit();

            Console.WriteLine($"[OK] Completed: {Path.GetFileName(inputFile)}\n");
        }

        Console.WriteLine("[OK] All files processed successfully!");
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
        CreateTblPlayerTeam(conn);

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

        // 建立 Ranking Cache 與團隊逐場事實表（與 InitRankingCache 合併）
        CreateRankingCacheTables(conn);
    }

    /// <summary>
    /// 建立排行榜快取與球隊逐場事實表（併入 InitRankingCache 的建表職責）
    /// </summary>
    private static void CreateRankingCacheTables(SqliteConnection conn)
    {
        var ddl = @"
            -- tblBattingRankingCache
            CREATE TABLE IF NOT EXISTS tblBattingRankingCache (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                seasonId TEXT NOT NULL,
                playerId TEXT NOT NULL,
                playerName TEXT NOT NULL,
                rank INTEGER NOT NULL,
                games INTEGER NOT NULL,
                pa INTEGER NOT NULL,
                ab INTEGER NOT NULL,
                h INTEGER NOT NULL,
                twoB INTEGER NOT NULL,
                threeB INTEGER NOT NULL,
                hr INTEGER NOT NULL,
                rbi INTEGER NOT NULL,
                r INTEGER NOT NULL,
                so INTEGER NOT NULL,
                bb INTEGER NOT NULL,
                hbp INTEGER NOT NULL,
                sf INTEGER NOT NULL,
                sb INTEGER NOT NULL,
                avg REAL NOT NULL,
                obp REAL NOT NULL,
                slg REAL NOT NULL,
                ops REAL NOT NULL,
                updatedAt TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_BattingRankingCache_SeasonId_PlayerId 
            ON tblBattingRankingCache(seasonId, playerId);
            CREATE INDEX IF NOT EXISTS IX_BattingRankingCache_SeasonId_Rank 
            ON tblBattingRankingCache(seasonId, rank);

            -- tblPitchingRankingCache
            CREATE TABLE IF NOT EXISTS tblPitchingRankingCache (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                seasonId TEXT NOT NULL,
                playerId TEXT NOT NULL,
                playerName TEXT NOT NULL,
                rank INTEGER NOT NULL,
                games INTEGER NOT NULL,
                ip REAL NOT NULL,
                ipOuts INTEGER NOT NULL,
                h INTEGER NOT NULL,
                hr INTEGER NOT NULL,
                bb INTEGER NOT NULL,
                so INTEGER NOT NULL,
                r INTEGER NOT NULL,
                er INTEGER NOT NULL,
                w INTEGER NOT NULL,
                l INTEGER NOT NULL,
                era REAL NOT NULL,
                whip REAL NOT NULL,
                k9 REAL NOT NULL,
                bb9 REAL NOT NULL,
                kbbRatio REAL NOT NULL,
                baa REAL NOT NULL,
                updatedAt TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_PitchingRankingCache_SeasonId_PlayerId 
            ON tblPitchingRankingCache(seasonId, playerId);
            CREATE INDEX IF NOT EXISTS IX_PitchingRankingCache_SeasonId_Rank 
            ON tblPitchingRankingCache(seasonId, rank);

            -- tblTeamGameStats（團隊逐場事實表）
            CREATE TABLE IF NOT EXISTS tblTeamGameStats (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                seasonId TEXT NOT NULL,
                gameId TEXT NOT NULL,
                gameDate TEXT NOT NULL,
                teamId TEXT NOT NULL,
                teamName TEXT NOT NULL,
                opponentTeamId TEXT NOT NULL,
                opponentTeamName TEXT NOT NULL,
                isHome INTEGER NOT NULL,
                teamScore INTEGER NOT NULL,
                opponentScore INTEGER NOT NULL,
                pa INTEGER NOT NULL,
                ab INTEGER NOT NULL,
                h INTEGER NOT NULL,
                twoB INTEGER NOT NULL,
                threeB INTEGER NOT NULL,
                hr INTEGER NOT NULL,
                bb INTEGER NOT NULL,
                so INTEGER NOT NULL,
                hbp INTEGER NOT NULL,
                sf INTEGER NOT NULL,
                sb INTEGER NOT NULL,
                cs INTEGER NOT NULL,
                ipOuts INTEGER NOT NULL,
                er INTEGER NOT NULL,
                hitsAllowed INTEGER NOT NULL,
                bbAllowed INTEGER NOT NULL,
                soPitching INTEGER NOT NULL,
                hrAllowed INTEGER NOT NULL,
                createdAt TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_TeamGameStats_GameId_TeamId
            ON tblTeamGameStats(gameId, teamId);
            CREATE INDEX IF NOT EXISTS IX_TeamGameStats_Season_Team_Date
            ON tblTeamGameStats(seasonId, teamId, gameDate);

            -- tblTeamSeasonRankingCache（球隊賽季匯總/排行榜快取）
            CREATE TABLE IF NOT EXISTS tblTeamSeasonRankingCache (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                seasonId TEXT NOT NULL,
                teamId TEXT NOT NULL,
                teamName TEXT NOT NULL,
                rank INTEGER NOT NULL,
                gamesPlayed INTEGER NOT NULL,
                wins INTEGER NOT NULL,
                losses INTEGER NOT NULL,
                runsScored INTEGER NOT NULL,
                runsAllowed INTEGER NOT NULL,
                pa INTEGER NOT NULL,
                ab INTEGER NOT NULL,
                h INTEGER NOT NULL,
                twoB INTEGER NOT NULL,
                threeB INTEGER NOT NULL,
                hr INTEGER NOT NULL,
                bb INTEGER NOT NULL,
                so INTEGER NOT NULL,
                hbp INTEGER NOT NULL,
                sf INTEGER NOT NULL,
                sb INTEGER NOT NULL,
                cs INTEGER NOT NULL,
                ipOuts INTEGER NOT NULL,
                er INTEGER NOT NULL,
                hitsAllowed INTEGER NOT NULL,
                bbAllowed INTEGER NOT NULL,
                soPitching INTEGER NOT NULL,
                hrAllowed INTEGER NOT NULL,
                winPct REAL NOT NULL,
                avg REAL NOT NULL,
                obp REAL NOT NULL,
                slg REAL NOT NULL,
                ops REAL NOT NULL,
                era REAL NOT NULL,
                fip REAL,
                runDiff INTEGER NOT NULL,
                updatedAt TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_TeamSeasonRanking_SeasonId_TeamId
            ON tblTeamSeasonRankingCache(seasonId, teamId);
            CREATE INDEX IF NOT EXISTS IX_TeamSeasonRanking_SeasonId_Rank
            ON tblTeamSeasonRankingCache(seasonId, rank);
        ";

        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] Ranking cache & team game stats tables created.");
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
                teamName TEXT NOT NULL
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
    /// 建立 tblPlayerTeam 資料表 - 記錄球員與球隊的關係（支援轉隊）
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    private static void CreateTblPlayerTeam(SqliteConnection conn)
    {
        var ddl = @"
            -- tblPlayerTeam
            CREATE TABLE IF NOT EXISTS tblPlayerTeam (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                playerId TEXT NOT NULL,
                teamId TEXT NOT NULL,
                seasonId TEXT NOT NULL,
                playerNumber TEXT,
                startDate TEXT,
                endDate TEXT,
                isActive INTEGER DEFAULT 1,
                FOREIGN KEY (teamId) REFERENCES tblTeam(teamId),
                FOREIGN KEY (seasonId) REFERENCES tblSeason(seasonId)
            );
            CREATE INDEX IF NOT EXISTS idx_playerteam_player ON tblPlayerTeam(playerId);
            CREATE INDEX IF NOT EXISTS idx_playerteam_team ON tblPlayerTeam(teamId);
            CREATE INDEX IF NOT EXISTS idx_playerteam_season ON tblPlayerTeam(seasonId);
            CREATE INDEX IF NOT EXISTS idx_playerteam_active ON tblPlayerTeam(playerId, isActive);
        ";

        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] tblPlayerTeam created.");
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
        InsertTblPlayerTeam(conn, doc, masterData);

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

        // 依逐場資料聚合，寫入團隊逐場事實表
        InsertTblTeamGameStats(conn, doc);

        // 依逐場事實表重建球隊賽季匯總快取
        RebuildTeamSeasonRankingCache(conn);

        // 重建打者排行榜快取
        RebuildBattingRankingCache(conn);

        // 重建投手排行榜快取
        RebuildPitchingRankingCache(conn);
    }

    /// <summary>
    /// 由 JSON 逐場資料聚合並寫入 tblTeamGameStats
    /// </summary>
    private static void InsertTblTeamGameStats(SqliteConnection conn, JsonDocument doc)
    {
        foreach (var game in GetGames(doc))
        {
            var seasonId = GetString(game, "seasonId") ?? "";
            var seq = GetInt(game, "seq");
            var gameDateStr = GetString(game, "date") ?? "";
            var gameDate = DateTime.TryParse(gameDateStr, out var dt) ? dt.ToString("yyyy-MM-dd") : gameDateStr;

            var homeTeamId = GetString(game, "homeTeamId") ?? "";
            var homeTeam = GetString(game, "homeTeam") ?? "";
            var awayTeamId = GetString(game, "awayTeamId") ?? "";
            var awayTeam = GetString(game, "awayTeam") ?? "";

            var homeScoresTotal = 0;
            var awayScoresTotal = 0;
            if (game.TryGetProperty("homeScores", out var hScores))
                homeScoresTotal = ParseScoreArray(hScores).Sum();
            if (game.TryGetProperty("awayScores", out var aScores))
                awayScoresTotal = ParseScoreArray(aScores).Sum();

            // 聚合打擊箱資料
            var homeBatAgg = AggregateBatterBox(game, "homeBatterBox");
            var awayBatAgg = AggregateBatterBox(game, "awayBatterBox");

            // 聚合投手箱資料
            var homePitAgg = AggregatePitcherBox(game, "homePitcherBox");
            var awayPitAgg = AggregatePitcherBox(game, "awayPitcherBox");

            // 組裝並寫入主隊逐場紀錄
            InsertOneTeamGameRow(conn, seasonId, seq, gameDate, homeTeamId, homeTeam, awayTeamId, awayTeam,
                isHome: 1,
                teamScore: homeScoresTotal,
                opponentScore: awayScoresTotal,
                bat: homeBatAgg,
                pit: homePitAgg);

            // 組裝並寫入客隊逐場紀錄
            InsertOneTeamGameRow(conn, seasonId, seq, gameDate, awayTeamId, awayTeam, homeTeamId, homeTeam,
                isHome: 0,
                teamScore: awayScoresTotal,
                opponentScore: homeScoresTotal,
                bat: awayBatAgg,
                pit: awayPitAgg);
        }
    }

    private class BatAgg { public int pa, ab, h, twoB, threeB, hr, bb, so, hbp, sf, sb, cs; }
    private class PitAgg { public int ipOuts, er, hitsAllowed, bbAllowed, soPitching, hrAllowed; }

    private static BatAgg AggregateBatterBox(JsonElement game, string boxName)
    {
        var agg = new BatAgg();
        if (!game.TryGetProperty(boxName, out var boxEl) || boxEl.ValueKind != JsonValueKind.Array) return agg;
        foreach (var bat in boxEl.EnumerateArray())
        {
            agg.pa += GetInt(bat, "PA");
            agg.ab += GetInt(bat, "AB");
            agg.h += GetInt(bat, "H");
            agg.twoB += GetInt(bat, "2B");
            agg.threeB += GetInt(bat, "3B");
            agg.hr += GetInt(bat, "HR");
            agg.bb += GetInt(bat, "BB");
            agg.so += GetInt(bat, "SO");
            agg.hbp += GetInt(bat, "HBP");
            agg.sf += GetInt(bat, "SF");
            agg.sb += GetInt(bat, "SB");
            agg.cs += GetInt(bat, "CS");
        }
        return agg;
    }

    private static PitAgg AggregatePitcherBox(JsonElement game, string boxName)
    {
        var agg = new PitAgg();
        if (!game.TryGetProperty(boxName, out var boxEl) || boxEl.ValueKind != JsonValueKind.Array) return agg;
        foreach (var pit in boxEl.EnumerateArray())
        {
            agg.ipOuts += GetInt(pit, "IPOuts");
            agg.er += GetInt(pit, "ER");
            agg.hitsAllowed += GetInt(pit, "H");
            agg.bbAllowed += GetInt(pit, "BB");
            agg.soPitching += GetInt(pit, "SO");
            agg.hrAllowed += GetInt(pit, "HR");
        }
        return agg;
    }

    private static void InsertOneTeamGameRow(SqliteConnection conn,
        string seasonId, int seq, string gameDate,
        string teamId, string teamName,
        string opponentTeamId, string opponentTeamName,
        int isHome, int teamScore, int opponentScore,
        BatAgg bat, PitAgg pit)
    {
        var gameId = $"{seasonId}-{seq}";
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR IGNORE INTO tblTeamGameStats(
                seasonId, gameId, gameDate,
                teamId, teamName, opponentTeamId, opponentTeamName,
                isHome, teamScore, opponentScore,
                pa, ab, h, twoB, threeB, hr, bb, so, hbp, sf, sb, cs,
                ipOuts, er, hitsAllowed, bbAllowed, soPitching, hrAllowed,
                createdAt
            ) VALUES (
                @sid, @gid, @gdate,
                @tid, @tname, @otid, @otname,
                @isHome, @ts, @os,
                @pa, @ab, @h, @twoB, @threeB, @hr, @bb, @so, @hbp, @sf, @sb, @cs,
                @ipOuts, @er, @hitsAllowed, @bbAllowed, @soPitching, @hrAllowed,
                @createdAt
            );";

        cmd.Parameters.AddWithValue("@sid", seasonId);
        cmd.Parameters.AddWithValue("@gid", gameId);
        cmd.Parameters.AddWithValue("@gdate", gameDate);
        cmd.Parameters.AddWithValue("@tid", teamId);
        cmd.Parameters.AddWithValue("@tname", teamName);
        cmd.Parameters.AddWithValue("@otid", opponentTeamId);
        cmd.Parameters.AddWithValue("@otname", opponentTeamName);
        cmd.Parameters.AddWithValue("@isHome", isHome);
        cmd.Parameters.AddWithValue("@ts", teamScore);
        cmd.Parameters.AddWithValue("@os", opponentScore);
        cmd.Parameters.AddWithValue("@pa", bat.pa);
        cmd.Parameters.AddWithValue("@ab", bat.ab);
        cmd.Parameters.AddWithValue("@h", bat.h);
        cmd.Parameters.AddWithValue("@twoB", bat.twoB);
        cmd.Parameters.AddWithValue("@threeB", bat.threeB);
        cmd.Parameters.AddWithValue("@hr", bat.hr);
        cmd.Parameters.AddWithValue("@bb", bat.bb);
        cmd.Parameters.AddWithValue("@so", bat.so);
        cmd.Parameters.AddWithValue("@hbp", bat.hbp);
        cmd.Parameters.AddWithValue("@sf", bat.sf);
        cmd.Parameters.AddWithValue("@sb", bat.sb);
        cmd.Parameters.AddWithValue("@cs", bat.cs);
        cmd.Parameters.AddWithValue("@ipOuts", pit.ipOuts);
        cmd.Parameters.AddWithValue("@er", pit.er);
        cmd.Parameters.AddWithValue("@hitsAllowed", pit.hitsAllowed);
        cmd.Parameters.AddWithValue("@bbAllowed", pit.bbAllowed);
        cmd.Parameters.AddWithValue("@soPitching", pit.soPitching);
        cmd.Parameters.AddWithValue("@hrAllowed", pit.hrAllowed);
        cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 從 tblTeamGameStats 聚合重建 tblTeamSeasonRankingCache（全季粒度）
    /// </summary>
    private static void RebuildTeamSeasonRankingCache(SqliteConnection conn)
    {
        // 以 seasonId, teamId 聚合逐場資料
        // 註：勝敗計算以 teamScore vs opponentScore 判斷
        var ddl = @"
            -- 先清掉舊資料（可依需求改為僅更新特定季）
            DELETE FROM tblTeamSeasonRankingCache;

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
                0 as rank, -- 之後可依勝率/得失分差計算排行再更新
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
                -- 派生指標（避免除以零）
                CASE WHEN COUNT(*) > 0 THEN CAST(SUM(CASE WHEN tgs.teamScore > tgs.opponentScore THEN 1 ELSE 0 END) AS REAL) / COUNT(*) ELSE 0 END as winPct,
                CASE WHEN SUM(tgs.ab) > 0 THEN CAST(SUM(tgs.h) AS REAL) / SUM(tgs.ab) ELSE 0 END as avg,
                -- OBP = (H + BB + HBP) / (AB + BB + HBP + SF)
                CASE WHEN (SUM(tgs.ab) + SUM(tgs.bb) + SUM(tgs.hbp) + SUM(tgs.sf)) > 0
                     THEN CAST((SUM(tgs.h) + SUM(tgs.bb) + SUM(tgs.hbp)) AS REAL) / (SUM(tgs.ab) + SUM(tgs.bb) + SUM(tgs.hbp) + SUM(tgs.sf))
                     ELSE 0 END as obp,
                -- SLG = TotalBases / AB；TotalBases = 1B + 2*2B + 3*3B + 4*HR；1B = H - (2B+3B+HR)
                CASE WHEN SUM(tgs.ab) > 0
                     THEN CAST((SUM(tgs.h) - (SUM(tgs.twoB)+SUM(tgs.threeB)+SUM(tgs.hr)) + 2*SUM(tgs.twoB) + 3*SUM(tgs.threeB) + 4*SUM(tgs.hr)) AS REAL) / SUM(tgs.ab)
                     ELSE 0 END as slg,
                0 as ops, -- 先置 0，稍後用 UPDATE 計算 obp+slg
                -- ERA = 9 * ER / IP；IP = ipOuts / 3.0
                CASE WHEN SUM(tgs.ipOuts) > 0 THEN 9.0 * CAST(SUM(tgs.er) AS REAL) / (CAST(SUM(tgs.ipOuts) AS REAL) / 3.0) ELSE 0 END as era,
                NULL as fip,
                SUM(tgs.teamScore) - SUM(tgs.opponentScore) as runDiff,
                strftime('%Y-%m-%dT%H:%M:%SZ','now') as updatedAt
            FROM tblTeamGameStats tgs
            GROUP BY tgs.seasonId, tgs.teamId;

            -- 更新 OPS = OBP + SLG
            UPDATE tblTeamSeasonRankingCache
            SET ops = obp + slg;

            -- 依勝率排序計算 rank（同季內）
            WITH ranked AS (
                SELECT seasonId, teamId,
                       ROW_NUMBER() OVER (PARTITION BY seasonId ORDER BY winPct DESC, runDiff DESC) AS rnk
                FROM tblTeamSeasonRankingCache
            )
            UPDATE tblTeamSeasonRankingCache AS t
            SET rank = (SELECT rnk FROM ranked WHERE ranked.seasonId = t.seasonId AND ranked.teamId = t.teamId);

            -- 寫入 seasonId='ALL' 的紀錄（歷史累計統計）
            INSERT INTO tblTeamSeasonRankingCache(
                seasonId, teamId, teamName,
                rank, gamesPlayed, wins, losses,
                runsScored, runsAllowed,
                pa, ab, h, twoB, threeB, hr, bb, so, hbp, sf, sb, cs,
                ipOuts, er, hitsAllowed, bbAllowed, soPitching, hrAllowed,
                winPct, avg, obp, slg, ops, era, fip, runDiff, updatedAt
            )
            SELECT
                'ALL' as seasonId,
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
            GROUP BY tgs.teamId;

            -- 更新 ALL 季的 OPS
            UPDATE tblTeamSeasonRankingCache
            SET ops = obp + slg
            WHERE seasonId = 'ALL';

            -- 計算 ALL 季的 rank
            WITH ranked_all AS (
                SELECT teamId,
                       ROW_NUMBER() OVER (ORDER BY winPct DESC, runDiff DESC) AS rnk
                FROM tblTeamSeasonRankingCache
                WHERE seasonId = 'ALL'
            )
            UPDATE tblTeamSeasonRankingCache AS t
            SET rank = (SELECT rnk FROM ranked_all WHERE ranked_all.teamId = t.teamId)
            WHERE t.seasonId = 'ALL';
        ";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = ddl;
        cmd.ExecuteNonQuery();

        Console.WriteLine("[OK] Rebuilt tblTeamSeasonRankingCache from tblTeamGameStats with 'ALL' season records.");
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
            insTeam.CommandText = "INSERT OR IGNORE INTO tblTeam(teamId,teamName) VALUES (@tid,@tname)";
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
    /// 插入 tblPlayerTeam 初始資料 - 從 BatterBox 和 PitcherBox 中提取球員球隊關係
    /// </summary>
    /// <param name="conn">
    /// 資料庫連線
    /// </param>
    /// <param name="doc">
    /// JSON 文件
    /// </param>
    /// <param name="masterData">
    /// 主資料
    /// </param>
    private static void InsertTblPlayerTeam(SqliteConnection conn, JsonDocument doc, MasterData masterData)
    {
        var playerTeamRelations = new Dictionary<string, (string playerId, string teamId, string seasonId, string playerNumber, DateTime date)>();

        // 遍歷每場比賽
        foreach (var game in GetGames(doc))
        {
            var seasonId = GetString(game, "seasonId") ?? "";
            var dateStr = GetString(game, "date") ?? "";
            DateTime gameDate = DateTime.TryParse(dateStr, out var parsedDate) ? parsedDate : DateTime.MinValue;
            var homeTeamId = GetString(game, "homeTeamId") ?? "";
            var awayTeamId = GetString(game, "awayTeamId") ?? "";

            // 處理客隊打者
            if (game.TryGetProperty("awayBatterBox", out var awayBat) && awayBat.ValueKind == JsonValueKind.Array)
            {
                foreach (var bat in awayBat.EnumerateArray())
                {
                    var playerId = GetString(bat, "playerId");
                    var playerNumber = GetString(bat, "playerNumber") ?? "";
                    if (!string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(awayTeamId))
                    {
                        var key = $"{playerId}-{awayTeamId}-{seasonId}";
                        if (!playerTeamRelations.ContainsKey(key) || playerTeamRelations[key].date > gameDate)
                        {
                            playerTeamRelations[key] = (playerId, awayTeamId, seasonId, playerNumber, gameDate);
                        }
                    }
                }
            }

            // 處理主隊打者
            if (game.TryGetProperty("homeBatterBox", out var homeBat) && homeBat.ValueKind == JsonValueKind.Array)
            {
                foreach (var bat in homeBat.EnumerateArray())
                {
                    var playerId = GetString(bat, "playerId");
                    var playerNumber = GetString(bat, "playerNumber") ?? "";
                    if (!string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(homeTeamId))
                    {
                        var key = $"{playerId}-{homeTeamId}-{seasonId}";
                        if (!playerTeamRelations.ContainsKey(key) || playerTeamRelations[key].date > gameDate)
                        {
                            playerTeamRelations[key] = (playerId, homeTeamId, seasonId, playerNumber, gameDate);
                        }
                    }
                }
            }

            // 處理客隊投手
            if (game.TryGetProperty("awayPitcherBox", out var awayPit) && awayPit.ValueKind == JsonValueKind.Array)
            {
                foreach (var pit in awayPit.EnumerateArray())
                {
                    var playerId = GetString(pit, "playerId");
                    var playerNumber = GetString(pit, "playerNumber") ?? "";
                    if (!string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(awayTeamId))
                    {
                        var key = $"{playerId}-{awayTeamId}-{seasonId}";
                        if (!playerTeamRelations.ContainsKey(key) || playerTeamRelations[key].date > gameDate)
                        {
                            playerTeamRelations[key] = (playerId, awayTeamId, seasonId, playerNumber, gameDate);
                        }
                    }
                }
            }

            // 處理主隊投手
            if (game.TryGetProperty("homePitcherBox", out var homePit) && homePit.ValueKind == JsonValueKind.Array)
            {
                foreach (var pit in homePit.EnumerateArray())
                {
                    var playerId = GetString(pit, "playerId");
                    var playerNumber = GetString(pit, "playerNumber") ?? "";
                    if (!string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(homeTeamId))
                    {
                        var key = $"{playerId}-{homeTeamId}-{seasonId}";
                        if (!playerTeamRelations.ContainsKey(key) || playerTeamRelations[key].date > gameDate)
                        {
                            playerTeamRelations[key] = (playerId, homeTeamId, seasonId, playerNumber, gameDate);
                        }
                    }
                }
            }
        }

        // 插入球員球隊關係資料
        foreach (var relation in playerTeamRelations.Values)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR IGNORE INTO tblPlayerTeam(playerId, teamId, seasonId, playerNumber, startDate, isActive)
                VALUES(@playerId, @teamId, @seasonId, @playerNumber, @startDate, 1)";
            cmd.Parameters.AddWithValue("@playerId", relation.playerId);
            cmd.Parameters.AddWithValue("@teamId", relation.teamId);
            cmd.Parameters.AddWithValue("@seasonId", relation.seasonId);
            cmd.Parameters.AddWithValue("@playerNumber", relation.playerNumber ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@startDate", relation.date.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();
        }

        Console.WriteLine($"[OK] Inserted {playerTeamRelations.Count} records into tblPlayerTeam.");
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
            cmd.Parameters.AddWithValue("@PA", box.PA );
            cmd.Parameters.AddWithValue("@AB", box.AB );
            cmd.Parameters.AddWithValue("@R", box.R );
            cmd.Parameters.AddWithValue("@H", box.H );
            cmd.Parameters.AddWithValue("@RBI", box.RBI);
            cmd.Parameters.AddWithValue("@TwoB", box.TwoB);
            cmd.Parameters.AddWithValue("@ThreeB", box.ThreeB);
            cmd.Parameters.AddWithValue("@HR", box.HR);
            cmd.Parameters.AddWithValue("@GIDP", box.GIDP);
            cmd.Parameters.AddWithValue("@DP", box.DP);
            cmd.Parameters.AddWithValue("@TP", box.TP);
            cmd.Parameters.AddWithValue("@BB", box.BB);
            cmd.Parameters.AddWithValue("@IBB", box.IBB);
            cmd.Parameters.AddWithValue("@HBP", box.HBP);
            cmd.Parameters.AddWithValue("@SO", box.SO);
            cmd.Parameters.AddWithValue("@SH", box.SH);
            cmd.Parameters.AddWithValue("@SF", box.SF);
            cmd.Parameters.AddWithValue("@E", box.E);
            cmd.Parameters.AddWithValue("@SB", box.SB);
            cmd.Parameters.AddWithValue("@CS", box.CS);

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
                PlayerId = GetString(bat, "playerId") ?? "",
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

    /// <summary>
    /// 從 tblBatterBox 聚合重建 tblBattingRankingCache（按賽季）
    /// </summary>
    private static void RebuildBattingRankingCache(SqliteConnection conn)
    {
        var ddl = @"
            -- 先清掉舊資料
            DELETE FROM tblBattingRankingCache;

            -- 從 tblBatterBox 聚合打者數據
            INSERT INTO tblBattingRankingCache(
                seasonId, playerId, playerName, rank,
                games, pa, ab, h, twoB, threeB, hr, rbi, r, so, bb, hbp, sf, sb,
                avg, obp, slg, ops, updatedAt
            )
            SELECT
                bb.seasonId,
                bb.playerId,
                COALESCE(b.playerName, bb.playerId) as playerName,
                0 as rank, -- 稍後更新
                COUNT(DISTINCT bb.gameSeq) as games,
                SUM(bb.PA) as pa,
                SUM(bb.AB) as ab,
                SUM(bb.H) as h,
                SUM(bb.[2B]) as twoB,
                SUM(bb.[3B]) as threeB,
                SUM(bb.HR) as hr,
                SUM(bb.RBI) as rbi,
                SUM(bb.R) as r,
                SUM(bb.SO) as so,
                SUM(bb.BB) as bb,
                SUM(bb.HBP) as hbp,
                SUM(bb.SF) as sf,
                SUM(bb.SB) as sb,
                -- AVG = H / AB
                CASE WHEN SUM(bb.AB) > 0 THEN CAST(SUM(bb.H) AS REAL) / SUM(bb.AB) ELSE 0 END as avg,
                -- OBP = (H + BB + HBP) / (AB + BB + HBP + SF)
                CASE WHEN (SUM(bb.AB) + SUM(bb.BB) + SUM(bb.HBP) + SUM(bb.SF)) > 0
                     THEN CAST((SUM(bb.H) + SUM(bb.BB) + SUM(bb.HBP)) AS REAL) / (SUM(bb.AB) + SUM(bb.BB) + SUM(bb.HBP) + SUM(bb.SF))
                     ELSE 0 END as obp,
                -- SLG = TotalBases / AB; TotalBases = 1B + 2*2B + 3*3B + 4*HR; 1B = H - (2B+3B+HR)
                CASE WHEN SUM(bb.AB) > 0
                     THEN CAST((SUM(bb.H) - (SUM(bb.[2B]) + SUM(bb.[3B]) + SUM(bb.HR)) + 2*SUM(bb.[2B]) + 3*SUM(bb.[3B]) + 4*SUM(bb.HR)) AS REAL) / SUM(bb.AB)
                     ELSE 0 END as slg,
                0 as ops, -- 稍後更新
                strftime('%Y-%m-%dT%H:%M:%SZ','now') as updatedAt
            FROM tblBatterBox bb
            LEFT JOIN tblBatter b ON bb.playerId = b.playerId
            WHERE bb.playerId IS NOT NULL AND bb.playerId != ''
            GROUP BY bb.seasonId, bb.playerId;

            -- 更新 OPS = OBP + SLG
            UPDATE tblBattingRankingCache
            SET ops = obp + slg;

            -- 依 OPS 排序計算 rank（同季內）
            WITH ranked AS (
                SELECT seasonId, playerId,
                       ROW_NUMBER() OVER (PARTITION BY seasonId ORDER BY ops DESC, avg DESC) AS rnk
                FROM tblBattingRankingCache
            )
            UPDATE tblBattingRankingCache AS t
            SET rank = (SELECT rnk FROM ranked WHERE ranked.seasonId = t.seasonId AND ranked.playerId = t.playerId);

            -- 寫入 seasonId='ALL' 的歷史累計統計
            INSERT INTO tblBattingRankingCache(
                seasonId, playerId, playerName, rank,
                games, pa, ab, h, twoB, threeB, hr, rbi, r, so, bb, hbp, sf, sb,
                avg, obp, slg, ops, updatedAt
            )
            SELECT
                'ALL' as seasonId,
                bb.playerId,
                COALESCE(b.playerName, bb.playerId) as playerName,
                0 as rank,
                COUNT(DISTINCT bb.seasonId || '-' || bb.gameSeq) as games,
                SUM(bb.PA) as pa,
                SUM(bb.AB) as ab,
                SUM(bb.H) as h,
                SUM(bb.[2B]) as twoB,
                SUM(bb.[3B]) as threeB,
                SUM(bb.HR) as hr,
                SUM(bb.RBI) as rbi,
                SUM(bb.R) as r,
                SUM(bb.SO) as so,
                SUM(bb.BB) as bb,
                SUM(bb.HBP) as hbp,
                SUM(bb.SF) as sf,
                SUM(bb.SB) as sb,
                CASE WHEN SUM(bb.AB) > 0 THEN CAST(SUM(bb.H) AS REAL) / SUM(bb.AB) ELSE 0 END as avg,
                CASE WHEN (SUM(bb.AB) + SUM(bb.BB) + SUM(bb.HBP) + SUM(bb.SF)) > 0
                     THEN CAST((SUM(bb.H) + SUM(bb.BB) + SUM(bb.HBP)) AS REAL) / (SUM(bb.AB) + SUM(bb.BB) + SUM(bb.HBP) + SUM(bb.SF))
                     ELSE 0 END as obp,
                CASE WHEN SUM(bb.AB) > 0
                     THEN CAST((SUM(bb.H) - (SUM(bb.[2B]) + SUM(bb.[3B]) + SUM(bb.HR)) + 2*SUM(bb.[2B]) + 3*SUM(bb.[3B]) + 4*SUM(bb.HR)) AS REAL) / SUM(bb.AB)
                     ELSE 0 END as slg,
                0 as ops,
                strftime('%Y-%m-%dT%H:%M:%SZ','now') as updatedAt
            FROM tblBatterBox bb
            LEFT JOIN tblBatter b ON bb.playerId = b.playerId
            WHERE bb.playerId IS NOT NULL AND bb.playerId != ''
            GROUP BY bb.playerId;

            -- 更新 ALL 季的 OPS
            UPDATE tblBattingRankingCache
            SET ops = obp + slg
            WHERE seasonId = 'ALL';

            -- 計算 ALL 季的 rank
            WITH ranked_all AS (
                SELECT playerId,
                       ROW_NUMBER() OVER (ORDER BY ops DESC, avg DESC) AS rnk
                FROM tblBattingRankingCache
                WHERE seasonId = 'ALL'
            )
            UPDATE tblBattingRankingCache AS t
            SET rank = (SELECT rnk FROM ranked_all WHERE ranked_all.playerId = t.playerId)
            WHERE t.seasonId = 'ALL';
        ";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = ddl;
        cmd.ExecuteNonQuery();

        Console.WriteLine("[OK] Rebuilt tblBattingRankingCache from tblBatterBox with 'ALL' season records.");
    }

    /// <summary>
    /// 從 tblPitcherBox 聚合重建 tblPitchingRankingCache（按賽季）
    /// </summary>
    private static void RebuildPitchingRankingCache(SqliteConnection conn)
    {
        var ddl = @"
            -- 先清掉舊資料
            DELETE FROM tblPitchingRankingCache;

            -- 從 tblPitcherBox 聚合投手數據
            INSERT INTO tblPitchingRankingCache(
                seasonId, playerId, playerName, rank,
                games, ip, ipOuts, h, hr, bb, so, r, er, w, l,
                era, whip, k9, bb9, kbbRatio, baa, updatedAt
            )
            SELECT
                pb.seasonId,
                pb.playerId,
                COALESCE(p.playerName, pb.playerId) as playerName,
                0 as rank, -- 稍後更新
                COUNT(DISTINCT pb.gameSeq) as games,
                CAST(SUM(pb.IPOuts) AS REAL) / 3.0 as ip,
                SUM(pb.IPOuts) as ipOuts,
                SUM(pb.H) as h,
                SUM(pb.HR) as hr,
                SUM(pb.BB) as bb,
                SUM(pb.SO) as so,
                SUM(pb.R) as r,
                SUM(pb.ER) as er,
                0 as w, -- 勝場數需從比賽結果計算，這裡先設為 0
                0 as l, -- 敗場數需從比賽結果計算，這裡先設為 0
                -- ERA = 9 * ER / IP
                CASE WHEN SUM(pb.IPOuts) > 0 THEN 9.0 * CAST(SUM(pb.ER) AS REAL) / (CAST(SUM(pb.IPOuts) AS REAL) / 3.0) ELSE 0 END as era,
                -- WHIP = (H + BB) / IP
                CASE WHEN SUM(pb.IPOuts) > 0 THEN CAST((SUM(pb.H) + SUM(pb.BB)) AS REAL) / (CAST(SUM(pb.IPOuts) AS REAL) / 3.0) ELSE 0 END as whip,
                -- K9 = SO * 9 / IP
                CASE WHEN SUM(pb.IPOuts) > 0 THEN 9.0 * CAST(SUM(pb.SO) AS REAL) / (CAST(SUM(pb.IPOuts) AS REAL) / 3.0) ELSE 0 END as k9,
                -- BB9 = BB * 9 / IP
                CASE WHEN SUM(pb.IPOuts) > 0 THEN 9.0 * CAST(SUM(pb.BB) AS REAL) / (CAST(SUM(pb.IPOuts) AS REAL) / 3.0) ELSE 0 END as bb9,
                -- K/BB Ratio = SO / BB
                CASE WHEN SUM(pb.BB) > 0 THEN CAST(SUM(pb.SO) AS REAL) / SUM(pb.BB) ELSE 0 END as kbbRatio,
                -- BAA (被打擊率) = H / BF (面對打席)
                CASE WHEN SUM(pb.BF) > 0 THEN CAST(SUM(pb.H) AS REAL) / SUM(pb.BF) ELSE 0 END as baa,
                strftime('%Y-%m-%dT%H:%M:%SZ','now') as updatedAt
            FROM tblPitcherBox pb
            LEFT JOIN tblPitcher p ON pb.playerId = p.playerId
            WHERE pb.playerId IS NOT NULL AND pb.playerId != ''
            GROUP BY pb.seasonId, pb.playerId;

            -- 依 ERA 排序計算 rank（同季內，ERA 越低越好）
            WITH ranked AS (
                SELECT seasonId, playerId,
                       ROW_NUMBER() OVER (PARTITION BY seasonId ORDER BY era ASC, whip ASC) AS rnk
                FROM tblPitchingRankingCache
            )
            UPDATE tblPitchingRankingCache AS t
            SET rank = (SELECT rnk FROM ranked WHERE ranked.seasonId = t.seasonId AND ranked.playerId = t.playerId);

            -- 寫入 seasonId='ALL' 的歷史累計統計
            INSERT INTO tblPitchingRankingCache(
                seasonId, playerId, playerName, rank,
                games, ip, ipOuts, h, hr, bb, so, r, er, w, l,
                era, whip, k9, bb9, kbbRatio, baa, updatedAt
            )
            SELECT
                'ALL' as seasonId,
                pb.playerId,
                COALESCE(p.playerName, pb.playerId) as playerName,
                0 as rank,
                COUNT(DISTINCT pb.seasonId || '-' || pb.gameSeq) as games,
                CAST(SUM(pb.IPOuts) AS REAL) / 3.0 as ip,
                SUM(pb.IPOuts) as ipOuts,
                SUM(pb.H) as h,
                SUM(pb.HR) as hr,
                SUM(pb.BB) as bb,
                SUM(pb.SO) as so,
                SUM(pb.R) as r,
                SUM(pb.ER) as er,
                0 as w,
                0 as l,
                CASE WHEN SUM(pb.IPOuts) > 0 THEN 9.0 * CAST(SUM(pb.ER) AS REAL) / (CAST(SUM(pb.IPOuts) AS REAL) / 3.0) ELSE 0 END as era,
                CASE WHEN SUM(pb.IPOuts) > 0 THEN CAST((SUM(pb.H) + SUM(pb.BB)) AS REAL) / (CAST(SUM(pb.IPOuts) AS REAL) / 3.0) ELSE 0 END as whip,
                CASE WHEN SUM(pb.IPOuts) > 0 THEN 9.0 * CAST(SUM(pb.SO) AS REAL) / (CAST(SUM(pb.IPOuts) AS REAL) / 3.0) ELSE 0 END as k9,
                CASE WHEN SUM(pb.IPOuts) > 0 THEN 9.0 * CAST(SUM(pb.BB) AS REAL) / (CAST(SUM(pb.IPOuts) AS REAL) / 3.0) ELSE 0 END as bb9,
                CASE WHEN SUM(pb.BB) > 0 THEN CAST(SUM(pb.SO) AS REAL) / SUM(pb.BB) ELSE 0 END as kbbRatio,
                CASE WHEN SUM(pb.BF) > 0 THEN CAST(SUM(pb.H) AS REAL) / SUM(pb.BF) ELSE 0 END as baa,
                strftime('%Y-%m-%dT%H:%M:%SZ','now') as updatedAt
            FROM tblPitcherBox pb
            LEFT JOIN tblPitcher p ON pb.playerId = p.playerId
            WHERE pb.playerId IS NOT NULL AND pb.playerId != ''
            GROUP BY pb.playerId;

            -- 計算 ALL 季的 rank
            WITH ranked_all AS (
                SELECT playerId,
                       ROW_NUMBER() OVER (ORDER BY era ASC, whip ASC) AS rnk
                FROM tblPitchingRankingCache
                WHERE seasonId = 'ALL'
            )
            UPDATE tblPitchingRankingCache AS t
            SET rank = (SELECT rnk FROM ranked_all WHERE ranked_all.playerId = t.playerId)
            WHERE t.seasonId = 'ALL';
        ";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = ddl;
        cmd.ExecuteNonQuery();

        Console.WriteLine("[OK] Rebuilt tblPitchingRankingCache from tblPitcherBox with 'ALL' season records.");
    }
}