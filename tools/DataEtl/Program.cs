using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace DataEtl;

class Program
{
    static async Task Main(string[] args)
    {
        string root = @"c:\Users\kwlin\Desktop\ideas\BaseballApp";
        var input = $@"{root}\data\CPBL-2024-OpenData\CPBL-2024-OpenData.json";
        var dbPath = $@"{root}\data\baseball.db";

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--input", StringComparison.OrdinalIgnoreCase)) input = args[i + 1];
            if (args[i].Equals("--db", StringComparison.OrdinalIgnoreCase)) dbPath = args[i + 1];
        }

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        if (!File.Exists(input)) { Console.Error.WriteLine($"Input not found: {input}"); return; }

        using var conn = new SqliteConnection($"Data Source={dbPath};Cache=Shared");
        conn.Open();

        var ddl = """
-- Master Data Tables

-- tblStadium
CREATE TABLE IF NOT EXISTS tblStadium (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  stadium TEXT NOT NULL UNIQUE
);

-- tblSeason
CREATE TABLE IF NOT EXISTS tblSeason (
  seasonId TEXT PRIMARY KEY,
  season TEXT NOT NULL
);

-- tblTeam
CREATE TABLE IF NOT EXISTS tblTeam (
  teamId TEXT PRIMARY KEY,
  team TEXT NOT NULL
);

-- tblPlayer
CREATE TABLE IF NOT EXISTS tblPlayer (
  playerId TEXT PRIMARY KEY,
  playerNumber TEXT,
  playerName TEXT NOT NULL
);

-- Game Tables

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

-- tblBatterBox
CREATE TABLE IF NOT EXISTS tblBatterBox (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  gameSeq INTEGER NOT NULL,
  homeOrAway TEXT NOT NULL,
  [order] INTEGER,
  playerId TEXT,
  PA INTEGER, AB INTEGER, R INTEGER, H INTEGER, RBI INTEGER,
  [2B] INTEGER, [3B] INTEGER, HR INTEGER,
  GIDP INTEGER, DP INTEGER, TP INTEGER,
  BB INTEGER, IBB INTEGER, HBP INTEGER, SO INTEGER,
  SH INTEGER, SF INTEGER, E INTEGER,
  SB INTEGER, CS INTEGER
);
CREATE INDEX IF NOT EXISTS idx_batterbox_game ON tblBatterBox(gameSeq, homeOrAway);
CREATE INDEX IF NOT EXISTS idx_batterbox_player ON tblBatterBox(playerId);

-- tblPitcherBox
CREATE TABLE IF NOT EXISTS tblPitcherBox (
  gameSeq INTEGER NOT NULL,
  homeOrAway TEXT NOT NULL,
  [order] INTEGER NOT NULL,
  playerId TEXT,
  IPOuts INTEGER, NP INTEGER, BF INTEGER,
  H INTEGER, HR INTEGER,
  BB INTEGER, IBB INTEGER, HB INTEGER, SO INTEGER,
  R INTEGER, ER INTEGER,
  PRIMARY KEY (gameSeq, homeOrAway, [order])
);
CREATE INDEX IF NOT EXISTS idx_pitcherbox_player ON tblPitcherBox(playerId);

-- tblPA
CREATE TABLE IF NOT EXISTS tblPA (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  gameSeq INTEGER NOT NULL,
  homeOrAway TEXT NOT NULL,
  inning INTEGER,
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
  bases TEXT,
  homeWE TEXT,
  RE TEXT,
  result TEXT,
  RBI INTEGER,
  locationCode TEXT,
  trajectory TEXT,
  hardness TEXT,
  endAwayScores INTEGER,
  endHomeScores INTEGER,
  endOuts INTEGER,
  endBases TEXT,
  WPA TEXT,
  RE24 TEXT
);
CREATE INDEX IF NOT EXISTS idx_pa_game ON tblPA(gameSeq, homeOrAway, inning);
CREATE INDEX IF NOT EXISTS idx_pa_batter ON tblPA(batterId);
CREATE INDEX IF NOT EXISTS idx_pa_pitcher ON tblPA(pitcherId);
""";
        using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }
        Console.WriteLine("[OK] Tables created.");

        await using var fs = File.OpenRead(input);
        using var doc = await JsonDocument.ParseAsync(fs);

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            Console.Error.WriteLine("Expected JSON array at root");
            return;
        }

        using var tx = conn.BeginTransaction();

        // Prepare INSERT commands for master data
        var insStadium = conn.CreateCommand();
        insStadium.CommandText = "INSERT OR IGNORE INTO tblStadium(stadium) VALUES(@stad)";
        insStadium.Parameters.AddWithValue("@stad", "");

        var insSeason = conn.CreateCommand();
        insSeason.CommandText = "INSERT OR IGNORE INTO tblSeason(seasonId,season) VALUES(@sid,@sname)";
        insSeason.Parameters.AddWithValue("@sid", "");
        insSeason.Parameters.AddWithValue("@sname", "");

        var insTeam = conn.CreateCommand();
        insTeam.CommandText = "INSERT OR IGNORE INTO tblTeam(teamId,team) VALUES(@tid,@tname)";
        insTeam.Parameters.AddWithValue("@tid", "");
        insTeam.Parameters.AddWithValue("@tname", "");

        var insPlayer = conn.CreateCommand();
        insPlayer.CommandText = "INSERT OR IGNORE INTO tblPlayer(playerId,playerNumber,playerName) VALUES(@pid,@pnum,@pname)";
        insPlayer.Parameters.AddWithValue("@pid", "");
        insPlayer.Parameters.AddWithValue("@pnum", "");
        insPlayer.Parameters.AddWithValue("@pname", "");

        // Prepare INSERT commands for game data
        var insGame = conn.CreateCommand();
        insGame.CommandText = "INSERT OR REPLACE INTO tblGame(seasonId,seq,date,stadiumId,awayTeamId,homeTeamId) VALUES(@sid,@seq,@date,@stadId,@away,@home)";
        insGame.Parameters.AddWithValue("@sid", "");
        insGame.Parameters.AddWithValue("@seq", 0);
        insGame.Parameters.AddWithValue("@date", "");
        insGame.Parameters.AddWithValue("@stadId", 0);
        insGame.Parameters.AddWithValue("@away", "");
        insGame.Parameters.AddWithValue("@home", "");

        var insBatter = conn.CreateCommand();
        insBatter.CommandText = """
INSERT INTO tblBatterBox(gameSeq,homeOrAway,[order],playerId,PA,AB,R,H,RBI,[2B],[3B],HR,GIDP,DP,TP,BB,IBB,HBP,SO,SH,SF,E,SB,CS)
VALUES(@seq,@hoa,@ord,@pid,@pa,@ab,@r,@h,@rbi,@b2,@b3,@hr,@gidp,@dp,@tp,@bb,@ibb,@hbp,@so,@sh,@sf,@e,@sb,@cs)
""";
        foreach (var p in new[] { "@seq", "@hoa", "@ord", "@pid", "@pa", "@ab", "@r", "@h", "@rbi", "@b2", "@b3", "@hr", "@gidp", "@dp", "@tp", "@bb", "@ibb", "@hbp", "@so", "@sh", "@sf", "@e", "@sb", "@cs" })
            insBatter.Parameters.AddWithValue(p, 0);

        var insPitcher = conn.CreateCommand();
        insPitcher.CommandText = """
INSERT OR REPLACE INTO tblPitcherBox(gameSeq,homeOrAway,[order],playerId,IPOuts,NP,BF,H,HR,BB,IBB,HB,SO,R,ER)
VALUES(@seq,@hoa,@ord,@pid,@ipo,@np,@bf,@h,@hr,@bb,@ibb,@hb,@so,@r,@er)
""";
        foreach (var p in new[] { "@seq", "@hoa", "@ord", "@pid", "@ipo", "@np", "@bf", "@h", "@hr", "@bb", "@ibb", "@hb", "@so", "@r", "@er" })
            insPitcher.Parameters.AddWithValue(p, 0);

        var insPA = conn.CreateCommand();
        insPA.CommandText = """
INSERT INTO tblPA(gameSeq,homeOrAway,inning,scored,batterId,batterHand,pitcherId,pitcherHand,catcherId,paRound,paOrder,isPH,
  awayScores,homeScores,strikes,balls,outs,bases,homeWE,RE,result,RBI,locationCode,trajectory,hardness,
  endAwayScores,endHomeScores,endOuts,endBases,WPA,RE24)
VALUES(@seq,@hoa,@inn,@sc,@bid,@bh,@pid,@ph,@cid,@pr,@po,@isph,@aws,@hms,@str,@bal,@out,@bas,@hwe,@re,@res,@rbi,@loc,@trj,@hrd,@eaws,@ehms,@eout,@ebas,@wpa,@re24)
""";
        foreach (var p in new[] { "@seq", "@hoa", "@inn", "@sc", "@bid", "@bh", "@pid", "@ph", "@cid", "@pr", "@po", "@isph", "@aws", "@hms", "@str", "@bal", "@out", "@bas", "@hwe", "@re", "@res", "@rbi", "@loc", "@trj", "@hrd", "@eaws", "@ehms", "@eout", "@ebas", "@wpa", "@re24" })
            insPA.Parameters.AddWithValue(p, 0);

        int gameCount = 0, batterCount = 0, pitcherCount = 0, paCount = 0;
        int stadiumCount = 0, seasonCount = 0, teamCount = 0, playerCount = 0;

        // First pass: collect and insert master data
        foreach (var game in doc.RootElement.EnumerateArray())
        {
            if (game.ValueKind != JsonValueKind.Object) continue;

            // Insert Stadium
            string stadium = GetString(game, "stadium") ?? "";
            if (!string.IsNullOrEmpty(stadium))
            {
                insStadium.Parameters["@stad"].Value = stadium;
                if (insStadium.ExecuteNonQuery() > 0) stadiumCount++;
            }

            // Insert Season
            string seasonId = GetString(game, "seasonId") ?? "";
            string season = GetString(game, "season") ?? "";
            if (!string.IsNullOrEmpty(seasonId))
            {
                insSeason.Parameters["@sid"].Value = seasonId;
                insSeason.Parameters["@sname"].Value = season;
                if (insSeason.ExecuteNonQuery() > 0) seasonCount++;
            }

            // Insert Teams
            string awayTeamId = GetString(game, "awayTeamId") ?? "";
            string awayTeam = GetString(game, "awayTeam") ?? "";
            if (!string.IsNullOrEmpty(awayTeamId))
            {
                insTeam.Parameters["@tid"].Value = awayTeamId;
                insTeam.Parameters["@tname"].Value = awayTeam;
                if (insTeam.ExecuteNonQuery() > 0) teamCount++;
            }

            string homeTeamId = GetString(game, "homeTeamId") ?? "";
            string homeTeam = GetString(game, "homeTeam") ?? "";
            if (!string.IsNullOrEmpty(homeTeamId))
            {
                insTeam.Parameters["@tid"].Value = homeTeamId;
                insTeam.Parameters["@tname"].Value = homeTeam;
                if (insTeam.ExecuteNonQuery() > 0) teamCount++;
            }

            // Insert Players from BatterBox
            if (game.TryGetProperty("awayBatterBox", out var awayBat) && awayBat.ValueKind == JsonValueKind.Array)
            {
                foreach (var b in awayBat.EnumerateArray())
                {
                    string playerId = GetString(b, "playerId") ?? "";
                    string playerNumber = GetString(b, "playerNumber") ?? "";
                    string playerName = GetString(b, "playerName") ?? "";
                    if (!string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(playerName))
                    {
                        insPlayer.Parameters["@pid"].Value = playerId;
                        insPlayer.Parameters["@pnum"].Value = playerNumber;
                        insPlayer.Parameters["@pname"].Value = playerName;
                        if (insPlayer.ExecuteNonQuery() > 0) playerCount++;
                    }
                }
            }

            if (game.TryGetProperty("homeBatterBox", out var homeBat) && homeBat.ValueKind == JsonValueKind.Array)
            {
                foreach (var b in homeBat.EnumerateArray())
                {
                    string playerId = GetString(b, "playerId") ?? "";
                    string playerNumber = GetString(b, "playerNumber") ?? "";
                    string playerName = GetString(b, "playerName") ?? "";
                    if (!string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(playerName))
                    {
                        insPlayer.Parameters["@pid"].Value = playerId;
                        insPlayer.Parameters["@pnum"].Value = playerNumber;
                        insPlayer.Parameters["@pname"].Value = playerName;
                        if (insPlayer.ExecuteNonQuery() > 0) playerCount++;
                    }
                }
            }

            // Insert Players from PitcherBox
            if (game.TryGetProperty("awayPitcherBox", out var awayPit) && awayPit.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in awayPit.EnumerateArray())
                {
                    string playerId = GetString(p, "playerId") ?? "";
                    string playerNumber = GetString(p, "playerNumber") ?? "";
                    string playerName = GetString(p, "playerName") ?? "";
                    if (!string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(playerName))
                    {
                        insPlayer.Parameters["@pid"].Value = playerId;
                        insPlayer.Parameters["@pnum"].Value = playerNumber;
                        insPlayer.Parameters["@pname"].Value = playerName;
                        if (insPlayer.ExecuteNonQuery() > 0) playerCount++;
                    }
                }
            }

            if (game.TryGetProperty("homePitcherBox", out var homePit) && homePit.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in homePit.EnumerateArray())
                {
                    string playerId = GetString(p, "playerId") ?? "";
                    string playerNumber = GetString(p, "playerNumber") ?? "";
                    string playerName = GetString(p, "playerName") ?? "";
                    if (!string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(playerName))
                    {
                        insPlayer.Parameters["@pid"].Value = playerId;
                        insPlayer.Parameters["@pnum"].Value = playerNumber;
                        insPlayer.Parameters["@pname"].Value = playerName;
                        if (insPlayer.ExecuteNonQuery() > 0) playerCount++;
                    }
                }
            }
        }

        Console.WriteLine($"[Master Data] Stadiums: {stadiumCount}, Seasons: {seasonCount}, Teams: {teamCount}, Players: {playerCount}");

        // Second pass: insert game data
        var getStadiumId = conn.CreateCommand();
        getStadiumId.CommandText = "SELECT id FROM tblStadium WHERE stadium = @stad";
        getStadiumId.Parameters.AddWithValue("@stad", "");

        foreach (var game in doc.RootElement.EnumerateArray())
        {
            if (game.ValueKind != JsonValueKind.Object) continue;

            string seasonId = GetString(game, "seasonId") ?? "";
            int seq = GetInt(game, "seq");
            string date = GetString(game, "date") ?? "";
            string stadium = GetString(game, "stadium") ?? "";
            string awayTeamId = GetString(game, "awayTeamId") ?? "";
            string homeTeamId = GetString(game, "homeTeamId") ?? "";

            // Get stadiumId from tblStadium
            int stadiumId = 0;
            if (!string.IsNullOrEmpty(stadium))
            {
                getStadiumId.Parameters["@stad"].Value = stadium;
                var result = getStadiumId.ExecuteScalar();
                if (result != null) stadiumId = Convert.ToInt32(result);
            }

            insGame.Parameters["@sid"].Value = seasonId;
            insGame.Parameters["@seq"].Value = seq;
            insGame.Parameters["@date"].Value = date;
            insGame.Parameters["@stadId"].Value = stadiumId;
            insGame.Parameters["@away"].Value = awayTeamId;
            insGame.Parameters["@home"].Value = homeTeamId;
            insGame.ExecuteNonQuery();
            gameCount++;

            if (game.TryGetProperty("awayBatterBox", out var awayBat) && awayBat.ValueKind == JsonValueKind.Array)
                foreach (var b in awayBat.EnumerateArray()) { InsertBatter(insBatter, seq, "away", b); batterCount++; }
            if (game.TryGetProperty("homeBatterBox", out var homeBat) && homeBat.ValueKind == JsonValueKind.Array)
                foreach (var b in homeBat.EnumerateArray()) { InsertBatter(insBatter, seq, "home", b); batterCount++; }

            if (game.TryGetProperty("awayPitcherBox", out var awayPit) && awayPit.ValueKind == JsonValueKind.Array)
                foreach (var p in awayPit.EnumerateArray()) { InsertPitcher(insPitcher, seq, "away", p); pitcherCount++; }
            if (game.TryGetProperty("homePitcherBox", out var homePit) && homePit.ValueKind == JsonValueKind.Array)
                foreach (var p in homePit.EnumerateArray()) { InsertPitcher(insPitcher, seq, "home", p); pitcherCount++; }

            if (game.TryGetProperty("awayPAList", out var awayPA) && awayPA.ValueKind == JsonValueKind.Array)
                foreach (var pa in awayPA.EnumerateArray()) { InsertPA(insPA, seq, "away", pa); paCount++; }
            if (game.TryGetProperty("homePAList", out var homePA) && homePA.ValueKind == JsonValueKind.Array)
                foreach (var pa in homePA.EnumerateArray()) { InsertPA(insPA, seq, "home", pa); paCount++; }
        }

        tx.Commit();

        Console.WriteLine($"[OK] SQLite ready: {dbPath}");
        Console.WriteLine($"  Master Data:");
        Console.WriteLine($"    - Stadiums: {stadiumCount}");
        Console.WriteLine($"    - Seasons: {seasonCount}");
        Console.WriteLine($"    - Teams: {teamCount}");
        Console.WriteLine($"    - Players: {playerCount}");
        Console.WriteLine($"  Game Data:");
        Console.WriteLine($"    - Games: {gameCount}");
        Console.WriteLine($"    - Batters: {batterCount}");
        Console.WriteLine($"    - Pitchers: {pitcherCount}");
        Console.WriteLine($"    - PA: {paCount}");

        void InsertBatter(SqliteCommand cmd, int seq, string hoa, JsonElement b)
        {
            cmd.Parameters["@seq"].Value = seq;
            cmd.Parameters["@hoa"].Value = hoa;
            cmd.Parameters["@ord"].Value = GetInt(b, "order");
            cmd.Parameters["@pid"].Value = GetString(b, "playerId") ?? "";
            cmd.Parameters["@pa"].Value = GetInt(b, "PA");
            cmd.Parameters["@ab"].Value = GetInt(b, "AB");
            cmd.Parameters["@r"].Value = GetInt(b, "R");
            cmd.Parameters["@h"].Value = GetInt(b, "H");
            cmd.Parameters["@rbi"].Value = GetInt(b, "RBI");
            cmd.Parameters["@b2"].Value = GetInt(b, "2B");
            cmd.Parameters["@b3"].Value = GetInt(b, "3B");
            cmd.Parameters["@hr"].Value = GetInt(b, "HR");
            cmd.Parameters["@gidp"].Value = GetInt(b, "GIDP");
            cmd.Parameters["@dp"].Value = GetInt(b, "DP");
            cmd.Parameters["@tp"].Value = GetInt(b, "TP");
            cmd.Parameters["@bb"].Value = GetInt(b, "BB");
            cmd.Parameters["@ibb"].Value = GetInt(b, "IBB");
            cmd.Parameters["@hbp"].Value = GetInt(b, "HBP");
            cmd.Parameters["@so"].Value = GetInt(b, "SO");
            cmd.Parameters["@sh"].Value = GetInt(b, "SH");
            cmd.Parameters["@sf"].Value = GetInt(b, "SF");
            cmd.Parameters["@e"].Value = GetInt(b, "E");
            cmd.Parameters["@sb"].Value = GetInt(b, "SB");
            cmd.Parameters["@cs"].Value = GetInt(b, "CS");
            cmd.ExecuteNonQuery();
        }

        void InsertPitcher(SqliteCommand cmd, int seq, string hoa, JsonElement p)
        {
            cmd.Parameters["@seq"].Value = seq;
            cmd.Parameters["@hoa"].Value = hoa;
            cmd.Parameters["@ord"].Value = GetInt(p, "order");
            cmd.Parameters["@pid"].Value = GetString(p, "playerId") ?? "";
            cmd.Parameters["@ipo"].Value = GetInt(p, "IPOuts");
            cmd.Parameters["@np"].Value = GetInt(p, "NP");
            cmd.Parameters["@bf"].Value = GetInt(p, "BF");
            cmd.Parameters["@h"].Value = GetInt(p, "H");
            cmd.Parameters["@hr"].Value = GetInt(p, "HR");
            cmd.Parameters["@bb"].Value = GetInt(p, "BB");
            cmd.Parameters["@ibb"].Value = GetInt(p, "IBB");
            cmd.Parameters["@hb"].Value = GetInt(p, "HB");
            cmd.Parameters["@so"].Value = GetInt(p, "SO");
            cmd.Parameters["@r"].Value = GetInt(p, "R");
            cmd.Parameters["@er"].Value = GetInt(p, "ER");
            cmd.ExecuteNonQuery();
        }

        void InsertPA(SqliteCommand cmd, int seq, string hoa, JsonElement pa)
        {
            cmd.Parameters["@seq"].Value = seq;
            cmd.Parameters["@hoa"].Value = hoa;
            cmd.Parameters["@inn"].Value = GetInt(pa, "inning");
            cmd.Parameters["@sc"].Value = GetBool(pa, "scored") ? 1 : 0;
            cmd.Parameters["@bid"].Value = GetString(pa, "batterId") ?? "";
            cmd.Parameters["@bh"].Value = GetString(pa, "batterHand") ?? "";
            cmd.Parameters["@pid"].Value = GetString(pa, "pitcherId") ?? "";
            cmd.Parameters["@ph"].Value = GetString(pa, "pitcherHand") ?? "";
            cmd.Parameters["@cid"].Value = GetString(pa, "catcherId") ?? "";
            cmd.Parameters["@pr"].Value = GetInt(pa, "paRound");
            cmd.Parameters["@po"].Value = GetInt(pa, "paOrder");
            cmd.Parameters["@isph"].Value = GetBool(pa, "isPH") ? 1 : 0;
            cmd.Parameters["@aws"].Value = GetInt(pa, "awayScores");
            cmd.Parameters["@hms"].Value = GetInt(pa, "homeScores");
            cmd.Parameters["@str"].Value = GetInt(pa, "strikes");
            cmd.Parameters["@bal"].Value = GetInt(pa, "balls");
            cmd.Parameters["@out"].Value = GetInt(pa, "outs");
            cmd.Parameters["@bas"].Value = GetString(pa, "bases") ?? "";
            cmd.Parameters["@hwe"].Value = GetString(pa, "homeWE") ?? "";
            cmd.Parameters["@re"].Value = GetString(pa, "RE") ?? "";
            cmd.Parameters["@res"].Value = GetString(pa, "result") ?? "";
            cmd.Parameters["@rbi"].Value = GetInt(pa, "RBI");
            cmd.Parameters["@loc"].Value = GetString(pa, "locationCode") ?? "";
            cmd.Parameters["@trj"].Value = GetString(pa, "trajectory") ?? "";
            cmd.Parameters["@hrd"].Value = GetString(pa, "hardness") ?? "";
            cmd.Parameters["@eaws"].Value = GetInt(pa, "endAwayScores");
            cmd.Parameters["@ehms"].Value = GetInt(pa, "endHomeScores");
            cmd.Parameters["@eout"].Value = GetInt(pa, "endOuts");
            cmd.Parameters["@ebas"].Value = GetString(pa, "endBases") ?? "";
            cmd.Parameters["@wpa"].Value = GetString(pa, "WPA") ?? "";
            cmd.Parameters["@re24"].Value = GetString(pa, "RE24") ?? "";
            cmd.ExecuteNonQuery();
        }

        static string? GetString(JsonElement obj, params string[] names)
        {
            foreach (var n in names)
                if (obj.TryGetProperty(n, out var el) && el.ValueKind == JsonValueKind.String)
                    return el.GetString();
            return null;
        }

        static bool GetBool(JsonElement obj, params string[] names)
        {
            foreach (var n in names)
                if (obj.TryGetProperty(n, out var el))
                    return el.ValueKind == JsonValueKind.True;
            return false;
        }

        static int GetInt(JsonElement obj, params string[] names)
        {
            foreach (var n in names)
                if (obj.TryGetProperty(n, out var el) && el.ValueKind == JsonValueKind.Number)
                    return el.GetInt32();
            return 0;
        }
    }
}