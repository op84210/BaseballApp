using System.Text.Json;
using Microsoft.Data.Sqlite;
using System.Globalization;

var root = @"c:\Users\kwlin\Desktop\ideas\BaseballApp";
var input = $@"{root}\data\CPBL-2024-OpenData\CPBL-2024-OpenData.json";
var dbPath = $@"{root}\data\baseball.db";

// 可選：從參數覆寫 --input / --db
for (int i=0;i<args.Length-1;i++){
    if(args[i].Equals("--input", StringComparison.OrdinalIgnoreCase)) input = args[i+1];
    if(args[i].Equals("--db", StringComparison.OrdinalIgnoreCase)) dbPath = args[i+1];
}

Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
if (!File.Exists(input)) { Console.Error.WriteLine($"Input not found: {input}"); return; }

using var conn = new SqliteConnection($"Data Source={dbPath};Cache=Shared");
conn.Open();

// 建表
var ddl = """
CREATE TABLE IF NOT EXISTS batting_events (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  date TEXT NOT NULL,
  game_id TEXT,
  player_id TEXT,
  player_name TEXT,
  team TEXT,
  ab INTEGER,
  h INTEGER,
  hr INTEGER,
  bb INTEGER
);
CREATE INDEX IF NOT EXISTS idx_batting_events_player_date ON batting_events(player_name, date);
CREATE INDEX IF NOT EXISTS idx_batting_events_date ON batting_events(date);
""";
using (var cmd = conn.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }

// 解析 JSON -> 插入
using var tx = conn.BeginTransaction();
using var insert = conn.CreateCommand();
insert.CommandText = """
INSERT INTO batting_events(date, game_id, player_id, player_name, team, ab, h, hr, bb)
VALUES($date, $game_id, $player_id, $player_name, $team, $ab, $h, $hr, $bb)
""";
insert.Parameters.Add("$date", SqliteType.Text);
insert.Parameters.Add("$game_id", SqliteType.Text);
insert.Parameters.Add("$player_id", SqliteType.Text);
insert.Parameters.Add("$player_name", SqliteType.Text);
insert.Parameters.Add("$team", SqliteType.Text);
insert.Parameters.Add("$ab", SqliteType.Integer);
insert.Parameters.Add("$h", SqliteType.Integer);
insert.Parameters.Add("$hr", SqliteType.Integer);
insert.Parameters.Add("$bb", SqliteType.Integer);

int rows = 0;
await using var fs = File.OpenRead(input);
using var doc = await JsonDocument.ParseAsync(fs);

IEnumerable<JsonElement> EnumerateRows(JsonElement root)
{
    if (root.ValueKind == JsonValueKind.Array) return root.EnumerateArray();
    if (root.ValueKind == JsonValueKind.Object)
    {
        foreach (var k in new[] { "data", "records", "items" })
            if (root.TryGetProperty(k, out var arr) && arr.ValueKind == JsonValueKind.Array)
                return arr.EnumerateArray();
    }
    return Array.Empty<JsonElement>();
}

foreach (var row in EnumerateRows(doc.RootElement))
{
    if (row.ValueKind != JsonValueKind.Object) continue;

    string? dateStr = GetString(row, "date","gameDate","game_date");
    if (!TryParseDate(dateStr, out var date)) continue;

    string gameId   = GetString(row, "gameId","game_id","gid","gameNo","game_no") ?? "";
    string playerId = GetString(row, "playerId","player_id","pid") ?? "";
    string playerNm = GetString(row, "playerName","player_name","name","player") ?? "";
    string team     = GetString(row, "team","teamName","team_name") ?? "";

    int ab = GetInt(row, "ab","at_bats","atBats");
    int h  = GetInt(row, "h","hits");
    int hr = GetInt(row, "hr","homeRuns","home_runs");
    int bb = GetInt(row, "bb","walks");

    insert.Parameters["$date"]!.Value = date.ToString("yyyy-MM-dd");
    insert.Parameters["$game_id"]!.Value = gameId;
    insert.Parameters["$player_id"]!.Value = playerId;
    insert.Parameters["$player_name"]!.Value = playerNm;
    insert.Parameters["$team"]!.Value = team;
    insert.Parameters["$ab"]!.Value = ab;
    insert.Parameters["$h"]!.Value = h;
    insert.Parameters["$hr"]!.Value = hr;
    insert.Parameters["$bb"]!.Value = bb;

    rows += insert.ExecuteNonQuery();
}
tx.Commit();

Console.WriteLine($"SQLite ready: {dbPath}");
Console.WriteLine($"Inserted rows: {rows}");

// 小工具函式
static string? GetString(JsonElement obj, params string[] names)
{
    foreach (var n in names)
        foreach (var p in obj.EnumerateObject())
            if (p.NameEquals(n) || p.Name.Equals(n, StringComparison.OrdinalIgnoreCase))
                return p.Value.ValueKind switch {
                    JsonValueKind.String => p.Value.GetString(),
                    JsonValueKind.Number => p.Value.ToString(),
                    _ => null
                };
    return null;
}
static int GetInt(JsonElement obj, params string[] names)
{
    var s = GetString(obj, names);
    return int.TryParse(s, out var v) ? v : 0;
}
static bool TryParseDate(string? s, out DateTime d)
{
    if (!string.IsNullOrWhiteSpace(s))
    {
        if (DateTime.TryParse(s, out d)) return true;
        var formats = new[] { "yyyy-MM-dd","yyyy/MM/dd","yyyyMMdd" };
        foreach (var f in formats)
            if (DateTime.TryParseExact(s, f, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return true;
    }
    d = default; return false;
}
