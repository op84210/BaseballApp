using Microsoft.Data.Sqlite;

partial class Program
{
    static void Main(string[] args)
    {
        var dbPath = "../../data/baseball.db";
        var connString = $"Data Source={dbPath}";

        Console.WriteLine("=== 檢查 baseball.db 內容 ===\n");

        using var conn = new SqliteConnection(connString);
        conn.Open();

        // 列出所有表格及其結構
        Console.WriteLine("--- 資料表列表及結構 ---");
        var tables = new List<string>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var tableName = reader.GetString(0);
                tables.Add(tableName);
                Console.WriteLine($"\n  {tableName}");
            }
        }

        // 顯示每個表的欄位結構
        foreach (var table in tables)
        {
            if (table == "sqlite_sequence") continue;
            
            Console.WriteLine($"\n  {table} 欄位:");
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({table})";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine($"    - {reader.GetString(1),-20} {reader.GetString(2)}");
            }
        }

        // 查詢每個表的資料筆數
        Console.WriteLine("\n--- 各表資料筆數 ---");
        foreach (var table in tables)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
            var count = cmd.ExecuteScalar();
            Console.WriteLine($"  {table,-20} : {count,8:N0} 筆");
        }

        // 查看 tblGame 的所有資料
        Console.WriteLine("\n--- tblGame 所有資料 ---");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM tblGame";
            using var reader = cmd.ExecuteReader();
            int count = 0;
            while (reader.Read())
            {
                count++;
                Console.WriteLine($"  筆 {count}: seasonId={reader[0]}, seq={reader[1]}");
            }
            Console.WriteLine($"  總計: {count} 筆");
        }

        // 查看 tblBatterBox 的前 5 筆
        Console.WriteLine("\n--- tblBatterBox 前 5 筆 ---");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT gameSeq, homeOrAway, playerId, PA, AB, H, HR, RBI, SO FROM tblBatterBox LIMIT 5";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine($"  G{reader[0],-3} {reader[1]} | {reader[2],-15} | PA:{reader[3],2} AB:{reader[4],2} H:{reader[5],2} HR:{reader[6],2} RBI:{reader[7],2} SO:{reader[8],2}");
            }
        }

        // 查看 tblPitcherBox 的前 5 筆
        Console.WriteLine("\n--- tblPitcherBox 前 5 筆 ---");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT gameSeq, homeOrAway, playerId, IPOuts, NP, H, HR, BB, SO, ER FROM tblPitcherBox LIMIT 5";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var ipOuts = reader.GetInt32(3);
                var ip = ipOuts / 3.0;
                Console.WriteLine($"  G{reader[0],-3} {reader[1]} | {reader[2],-15} | IP:{ip:F1} NP:{reader[4],3} H:{reader[5],2} HR:{reader[6],2} BB:{reader[7],2} SO:{reader[8],2} ER:{reader[9],2}");
            }
        }

        // 查看 tblPA 的前 5 筆
        Console.WriteLine("\n--- tblPA 前 5 筆 ---");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT gameSeq, inning, batterId, pitcherId, result, strikes, balls, outs FROM tblPA LIMIT 5";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine($"  G{reader[0],-3} T{reader[1]} | Batter:{reader[2],-15} Pitcher:{reader[3],-15} | 結果:{reader[4],-6} | {reader[5]}S {reader[6]}B {reader[7]}O");
            }
        }

        // 統計資訊
        Console.WriteLine("\n--- 統計資訊 ---");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(DISTINCT seasonId) FROM tblGame";
            var seasonCount = cmd.ExecuteScalar();
            Console.WriteLine($"  賽事數: {seasonCount}");
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT MIN(date), MAX(date) FROM tblGame";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                Console.WriteLine($"  比賽日期範圍: {reader[0]} ~ {reader[1]}");
            }
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(DISTINCT playerId) FROM tblBatterBox";
            var batterCount = cmd.ExecuteScalar();
            Console.WriteLine($"  不重複打者 ID: {batterCount}");
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(DISTINCT playerId) FROM tblPitcherBox";
            var pitcherCount = cmd.ExecuteScalar();
            Console.WriteLine($"  不重複投手 ID: {pitcherCount}");
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT SUM(PA) FROM tblBatterBox";
            var totalPA = cmd.ExecuteScalar();
            Console.WriteLine($"  總打席數 (從 BatterBox): {totalPA:N0}");
        }

        Console.WriteLine("\n=== 檢查完成 ===");
    }
}
