using BaseballApp.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace BaseballApp.Services;

public interface IBaseballDataService
{
    Task<IEnumerable<BattingStats>> GetPlayerStatsAsync(string playerName = null);
    Task<IEnumerable<MonthlyBattingStats>> GetMonthlyStatsAsync(string playerName, string season);
    Task<BattingStats> GetPlayerSeasonStatsAsync(string playerName, string season);
    Task<bool> UpdateStatsFromAPIAsync();
}

public class BaseballDataService : IBaseballDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BaseballDataService> _logger;
    private readonly string _cpblApiUrl = "https://api.cpbl.com.tw"; // 台灣職棒API
    private readonly string _mlbApiUrl = "https://statsapi.mlb.com/api/v1"; // MLB API

    public BaseballDataService(HttpClient httpClient, ILogger<BaseballDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<BattingStats>> GetPlayerStatsAsync(string playerName = null)
    {
        try
        {
            // 優先嘗試從台灣職棒API獲取數據
            var cpblStats = await GetCPBLStatsAsync(playerName);
            if (cpblStats.Any())
                return cpblStats;

            // 如果沒有台灣職棒數據，返回模擬數據
            return GetMockStats(playerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "獲取棒球數據時發生錯誤");
            return GetMockStats(playerName);
        }
    }

    public async Task<IEnumerable<MonthlyBattingStats>> GetMonthlyStatsAsync(string playerName, string season)
    {
        try
        {
            // 嘗試從台灣職棒API獲取月度數據
            var monthlyStats = await GetCPBLMonthlyStatsAsync(playerName, season);
            if (monthlyStats.Any())
                return monthlyStats;

            // 返回模擬月度數據
            return GetMockMonthlyStats(playerName, season);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"獲取{playerName}的月度數據時發生錯誤");
            return GetMockMonthlyStats(playerName, season);
        }
    }

    public async Task<BattingStats> GetPlayerSeasonStatsAsync(string playerName, string season)
    {
        var allStats = await GetPlayerStatsAsync(playerName);
        return allStats.FirstOrDefault(s => s.PlayerName.Contains(playerName) && s.Season == season)
               ?? GetMockStats(playerName).First();
    }

    public async Task<bool> UpdateStatsFromAPIAsync()
    {
        try
        {
            // 這裡可以實現從API更新數據的邏輯
            _logger.LogInformation("開始更新棒球數據...");
            // 實際實現會調用各個API並更新本地數據
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新棒球數據時發生錯誤");
            return false;
        }
    }

    private async Task<IEnumerable<BattingStats>> GetCPBLStatsAsync(string playerName = null)
    {
        var stats = new List<BattingStats>();

        try
        {
            // 台灣職棒打擊成績API (示例URL，實際需要確認官方API)
            var url = $"{_cpblApiUrl}/players/batting-stats";
            if (!string.IsNullOrEmpty(playerName))
            {
                url += $"?name={Uri.EscapeDataString(playerName)}";
            }

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var apiData = await response.Content.ReadFromJsonAsync<List<CpblApiResponse>>();
                if (apiData != null)
                {
                    foreach (var item in apiData)
                    {
                        stats.Add(new BattingStats
                        {
                            PlayerName = item.player_name,
                            Team = item.team_name,
                            PlateAppearances = item.pa,
                            AtBats = item.ab,
                            Hits = item.h,
                            Doubles = item.@double,
                            Triples = item.triple,
                            HomeRuns = item.hr,
                            RBIs = item.rbi,
                            Runs = item.r,
                            StolenBases = item.sb,
                            CaughtStealing = item.cs,
                            Walks = item.bb,
                            Strikeouts = item.so,
                            Season = item.season,
                            LastUpdated = DateTime.Now
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "無法從CPBL API獲取數據，將使用模擬數據");
        }

        return stats;
    }

    private async Task<IEnumerable<MonthlyBattingStats>> GetCPBLMonthlyStatsAsync(string playerName, string season)
    {
        var monthlyStats = new List<MonthlyBattingStats>();

        try
        {
            var url = $"{_cpblApiUrl}/players/{Uri.EscapeDataString(playerName)}/monthly-stats?season={season}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var apiData = await response.Content.ReadFromJsonAsync<List<CpblMonthlyApiResponse>>();
                if (apiData != null)
                {
                    foreach (var item in apiData)
                    {
                        monthlyStats.Add(new MonthlyBattingStats
                        {
                            PlayerName = playerName,
                            Month = item.month,
                            PlateAppearances = item.pa,
                            AtBats = item.ab,
                            Hits = item.h,
                            HomeRuns = item.hr,
                            RBIs = item.rbi,
                            Runs = item.r,
                            Walks = item.bb,
                            Strikeouts = item.so,
                            IsInjured = item.is_injured,
                            Season = season,
                            LastUpdated = DateTime.Now
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"無法獲取{playerName}的月度數據");
        }

        return monthlyStats;
    }

    private IEnumerable<BattingStats> GetMockStats(string playerName = null)
    {
        var mockStats = new List<BattingStats>
        {
            new BattingStats
            {
                Id = 1,
                PlayerName = "張育成",
                Team = "統一7-ELEVEn獅",
                PlateAppearances = 450,
                AtBats = 400,
                Hits = 120,
                Doubles = 25,
                Triples = 2,
                HomeRuns = 15,
                RBIs = 65,
                Runs = 70,
                StolenBases = 8,
                CaughtStealing = 3,
                Walks = 40,
                Strikeouts = 85,
                Season = "2024",
                LastUpdated = DateTime.Now
            },
            new BattingStats
            {
                Id = 2,
                PlayerName = "王柏融",
                Team = "台鋼雄鷹",
                PlateAppearances = 480,
                AtBats = 430,
                Hits = 135,
                Doubles = 28,
                Triples = 3,
                HomeRuns = 22,
                RBIs = 78,
                Runs = 85,
                StolenBases = 12,
                CaughtStealing = 5,
                Walks = 45,
                Strikeouts = 92,
                Season = "2024",
                LastUpdated = DateTime.Now
            },
            new BattingStats
            {
                Id = 3,
                PlayerName = "林智勝",
                Team = "中信兄弟",
                PlateAppearances = 380,
                AtBats = 340,
                Hits = 95,
                Doubles = 18,
                Triples = 1,
                HomeRuns = 18,
                RBIs = 62,
                Runs = 55,
                StolenBases = 2,
                CaughtStealing = 1,
                Walks = 35,
                Strikeouts = 75,
                Season = "2024",
                LastUpdated = DateTime.Now
            },
            new BattingStats
            {
                Id = 4,
                PlayerName = "陳金鋒",
                Team = "Lamigo桃猿",
                PlateAppearances = 520,
                AtBats = 470,
                Hits = 140,
                Doubles = 30,
                Triples = 4,
                HomeRuns = 28,
                RBIs = 95,
                Runs = 92,
                StolenBases = 15,
                CaughtStealing = 6,
                Walks = 42,
                Strikeouts = 88,
                Season = "2017",
                LastUpdated = DateTime.Now
            },
            new BattingStats
            {
                Id = 5,
                PlayerName = "彭政閔",
                Team = "中信兄弟",
                PlateAppearances = 490,
                AtBats = 440,
                Hits = 125,
                Doubles = 22,
                Triples = 2,
                HomeRuns = 12,
                RBIs = 68,
                Runs = 65,
                StolenBases = 6,
                CaughtStealing = 2,
                Walks = 38,
                Strikeouts = 65,
                Season = "2024",
                LastUpdated = DateTime.Now
            }
        };

        if (!string.IsNullOrEmpty(playerName))
        {
            return mockStats.Where(s => s.PlayerName.Contains(playerName));
        }

        return mockStats;
    }

    private IEnumerable<MonthlyBattingStats> GetMockMonthlyStats(string playerName, string season)
    {
        var monthlyStats = new List<MonthlyBattingStats>();

        for (int month = 1; month <= 6; month++)
        {
            // 模擬受傷情況
            bool isInjured = false;
            if (playerName == "張育成" && month == 3) isInjured = true;
            if (playerName == "林智勝" && (month == 2 || month == 5)) isInjured = true;

            monthlyStats.Add(new MonthlyBattingStats
            {
                PlayerName = playerName,
                Month = month,
                PlateAppearances = isInjured ? 0 : new Random().Next(60, 90),
                AtBats = isInjured ? 0 : new Random().Next(55, 80),
                Hits = isInjured ? 0 : new Random().Next(15, 25),
                HomeRuns = isInjured ? 0 : new Random().Next(1, 5),
                RBIs = isInjured ? 0 : new Random().Next(8, 15),
                Runs = isInjured ? 0 : new Random().Next(6, 12),
                Walks = isInjured ? 0 : new Random().Next(4, 8),
                Strikeouts = isInjured ? 0 : new Random().Next(8, 15),
                IsInjured = isInjured,
                Season = season,
                LastUpdated = DateTime.Now
            });
        }

        return monthlyStats;
    }

    // API響應模型
    private class CpblApiResponse
    {
        public string player_name { get; set; } = string.Empty;
        public string team_name { get; set; } = string.Empty;
        public int pa { get; set; }
        public int ab { get; set; }
        public int h { get; set; }
        public int @double { get; set; } // double是C#關鍵字，所以用@double
        public int triple { get; set; }
        public int hr { get; set; }
        public int rbi { get; set; }
        public int r { get; set; }
        public int sb { get; set; }
        public int cs { get; set; }
        public int bb { get; set; }
        public int so { get; set; }
        public string season { get; set; } = string.Empty;
    }

    private class CpblMonthlyApiResponse
    {
        public int month { get; set; }
        public int pa { get; set; }
        public int ab { get; set; }
        public int h { get; set; }
        public int hr { get; set; }
        public int rbi { get; set; }
        public int r { get; set; }
        public int bb { get; set; }
        public int so { get; set; }
        public bool is_injured { get; set; }
    }
}