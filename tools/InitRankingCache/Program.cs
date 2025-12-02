using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace BaseballApp.Tools
{
    class Program
    {
        static void Main(string[] args)
        {
            var dbPath = @"c:\Users\kwlin\Desktop\ideas\BaseballApp\data\baseball.db";
            var connectionString = $"Data Source={dbPath}";
            
            Console.WriteLine($"資料庫路徑：{dbPath}");
            
            var sql = @"
-- 建立打者排行榜快取資料表
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

-- 建立索引
CREATE UNIQUE INDEX IF NOT EXISTS IX_BattingRankingCache_SeasonId_PlayerId 
ON tblBattingRankingCache(seasonId, playerId);

CREATE INDEX IF NOT EXISTS IX_BattingRankingCache_SeasonId_Rank 
ON tblBattingRankingCache(seasonId, rank);

-- 建立投手排行榜快取資料表
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
    updatedAt TEXT NOT NULL
);

-- 建立索引
CREATE UNIQUE INDEX IF NOT EXISTS IX_PitchingRankingCache_SeasonId_PlayerId 
ON tblPitchingRankingCache(seasonId, playerId);

CREATE INDEX IF NOT EXISTS IX_PitchingRankingCache_SeasonId_Rank 
ON tblPitchingRankingCache(seasonId, rank);
";

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            try
            {
                command.ExecuteNonQuery();
                Console.WriteLine("✓ 排行榜快取資料表建立成功！");
                
                // 驗證資料表是否存在
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%RankingCache%'";
                using var reader = command.ExecuteReader();
                
                Console.WriteLine("\n已建立的資料表：");
                while (reader.Read())
                {
                    Console.WriteLine($"  - {reader.GetString(0)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 建立資料表時發生錯誤：{ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            
            Console.WriteLine("\n按任意鍵結束...");
            Console.ReadKey();
        }
    }
}
