using BaseballApp.Models;
using BaseballApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BaseballApp.Controllers;

public class BaseballController : Controller
{
    private readonly IBaseballDbService _baseballDbService;
    private readonly IRankingCacheService _rankingCacheService;
    private readonly ILogger<BaseballController> _logger;

    public BaseballController(
        IBaseballDbService baseballDbService,
        IRankingCacheService rankingCacheService,
        ILogger<BaseballController> logger)
    {
        _baseballDbService = baseballDbService;
        _rankingCacheService = rankingCacheService;
        _logger = logger;
    }

    /// <summary>
    /// 團隊列表頁面
    /// </summary>
    /// <param name="seasonId">
    /// 賽季識別碼，格式例如 "CPBL-2024-HE"
    /// </param>
    /// <returns>
    /// 團隊列表頁面
    /// </returns>
    public async Task<IActionResult> Teams(string? seasonId = null)
    {
        try
        {
            seasonId ??= "CPBL-2024-HE";

            var teams = await _baseballDbService.GetAllTeamsAsync();
            var games = await _baseballDbService.GetGamesAsync(seasonId);

            var teamStats = teams.Select(team => new
            {
                Team = team,
                Games = games.Count(g => g.AwayTeamId == team.TeamId || g.HomeTeamId == team.TeamId)
            }).ToList();

            ViewBag.SeasonId = seasonId;
            return View(teamStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入團隊頁面時發生錯誤");
            return View("Error");
        }
    }

    /// <summary>
    /// 團隊詳細資訊頁面
    /// </summary>
    /// <param name="teamId">
    /// 球隊識別碼
    /// </param>
    /// <param name="seasonId">
    /// 賽季識別碼，格式例如 "CPBL-2024-HE"
    /// </param>
    /// <returns>
    /// 團隊詳細資訊頁面
    /// </returns>
    public async Task<IActionResult> TeamDetail(string teamId, string? seasonId = null)
    {
        try
        {
            seasonId ??= "CPBL-2024-HE";

            var teams = await _baseballDbService.GetAllTeamsAsync();
            var team = teams.FirstOrDefault(t => t.TeamId == teamId);

            if (team == null)
            {
                return NotFound();
            }

            // 取得球隊打者成績
            var batterBoxes = await _baseballDbService.GetBatterBoxAsync(seasonId: seasonId);
            var batters = await _baseballDbService.GetAllBattersAsync(seasonId);

            var teamBatters = batterBoxes
                .GroupBy(bb => bb.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key,
                    PlayerName = batters.FirstOrDefault(b => b.PlayerId == g.Key)?.PlayerName ?? "Unknown",
                    PlayerNumber = batters.FirstOrDefault(b => b.PlayerId == g.Key)?.PlayerNumber,
                    Games = g.Select(x => x.GameSeq).Distinct().Count(),
                    PA = g.Sum(x => x.PA),
                    AB = g.Sum(x => x.AB),
                    H = g.Sum(x => x.H),
                    HR = g.Sum(x => x.HR),
                    RBI = g.Sum(x => x.RBI),
                    AVG = g.Sum(x => x.AB) > 0 ?
                        Math.Round((decimal)g.Sum(x => x.H) / g.Sum(x => x.AB), 3) : 0
                })
                .OrderByDescending(x => x.H)
                .ToList();

            // 取得球隊投手成績
            var pitcherBoxes = await _baseballDbService.GetPitcherBoxAsync(seasonId: seasonId);
            var pitchers = await _baseballDbService.GetAllPitchersAsync(seasonId);

            var teamPitchers = pitcherBoxes
                .GroupBy(pb => pb.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key,
                    PlayerName = pitchers.FirstOrDefault(p => p.PlayerId == g.Key)?.PlayerName ?? "Unknown",
                    PlayerNumber = pitchers.FirstOrDefault(p => p.PlayerId == g.Key)?.PlayerNumber,
                    Games = g.Select(x => x.GameSeq).Distinct().Count(),
                    IPOuts = g.Sum(x => x.IPOuts ?? 0),
                    IP = (decimal)(g.Sum(x => x.IPOuts ?? 0) / 3) + (decimal)(g.Sum(x => x.IPOuts ?? 0) % 3) / 10m,
                    H = g.Sum(x => x.H ?? 0),
                    HR = g.Sum(x => x.HR ?? 0),
                    BB = g.Sum(x => x.BB ?? 0),
                    SO = g.Sum(x => x.SO ?? 0),
                    ER = g.Sum(x => x.ER ?? 0),
                    ERA = g.Sum(x => x.IPOuts ?? 0) > 0 ?
                        Math.Round((decimal)g.Sum(x => x.ER ?? 0) * 27 / g.Sum(x => x.IPOuts ?? 0), 2) : 0
                })
                .OrderByDescending(x => x.IP)
                .ToList();

            ViewBag.Team = team;
            ViewBag.SeasonId = seasonId;
            ViewBag.TeamBatters = teamBatters;
            ViewBag.TeamPitchers = teamPitchers;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"載入團隊 {teamId} 詳細資訊時發生錯誤");
            return View("Error");
        }
    }

    /// <summary>
    /// 球員列表頁面
    /// </summary>
    /// <param name="seasonId">
    /// 賽季識別碼，格式例如 "CPBL-2024-HE"
    /// </param>
    /// <param name="teamId">
    /// 球隊識別碼
    /// </param>
    /// <returns>
    /// 球員列表頁面
    /// </returns>
    public async Task<IActionResult> Players(string? seasonId = null, string? teamId = null)
    {
        try
        {
            seasonId ??= "CPBL-2024-HE";

            var batters = await _baseballDbService.GetAllBattersAsync(seasonId);
            var teams = await _baseballDbService.GetAllTeamsAsync();

            ViewBag.SeasonId = seasonId;
            ViewBag.TeamId = teamId;
            ViewBag.Batters = batters.OrderBy(b => b.PlayerNumber).ToList();
            ViewBag.Teams = teams.ToList();

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入球員頁面時發生錯誤");
            return View("Error");
        }
    }

    /// <summary>
    /// 球員詳細資訊頁面
    /// </summary>
    /// <param name="playerId">
    /// 球員識別碼
    /// </param>
    /// <param name="seasonId">
    /// 賽季識別碼，格式例如 "CPBL-2024-HE"
    /// </param>
    /// <returns>
    /// 球員詳細資訊頁面
    /// </returns>
    public async Task<IActionResult> PlayerDetail(string playerId, string? seasonId = null)
    {
        try
        {
            // 支援全部賽季：傳入 ALL 代表生涯統計
            if (seasonId == "ALL")
            {
                seasonId = null; // 使用 null 表示生涯
            }

            // 取得賽季資料
            var seasons = await _baseballDbService.GetAllSeasonsAsync();

            // 取得球員資料
            var player = await _baseballDbService.GetBatterAsync(playerId);
            if (player == null)
            {
                return NotFound();
            }

            // 取得球員打席記錄（ALL 代表使用全部賽季資料）
            var paList = await _baseballDbService.GetPAAsync(batterId: playerId, seasonId: seasonId);
            
            // 打擊數據 (按比賽統計)
            var gameStats = paList
                .GroupBy(pa => new { pa.SeasonId, pa.GameSeq })
                .Select(g => {
                    return new GameStat
                    {
                        Date = g.FirstOrDefault()?.Game?.Date ?? DateTime.MinValue,
                        SeasonName = seasons.FirstOrDefault(s => s.SeasonId == g.Key.SeasonId)?.SeasonName ?? "Unknown",
                        Seq = g.Key.GameSeq,
                        PA = g.Count(),
                        _1B = g.Count(pa => pa.Result == "1B"),
                        _2B = g.Count(pa => pa.Result == "2B"),
                        _3B = g.Count(pa => pa.Result == "3B"),
                        HR = g.Count(pa => pa.Result == "HR"),
                        IHR = g.Count(pa => pa.Result == "IHR"),
                        SO = g.Count(pa => pa.Result == "SO"),
                        uBB = g.Count(pa => pa.Result == "uBB"),
                        IBB = g.Count(pa => pa.Result == "IBB"),
                        HBP = g.Count(pa => pa.Result == "HBP"),
                        GO = g.Count(pa => pa.Result == "GO"),
                        FO = g.Count(pa => pa.Result == "FO"),
                        FC = g.Count(pa => pa.Result == "FC"),
                        E = g.Count(pa => pa.Result == "E"),
                        SH = g.Count(pa => pa.Result == "SH"),
                        SF = g.Count(pa => pa.Result == "SF"),
                        GIDP = g.Count(pa => pa.Result == "GIDP"),
                        DP = g.Count(pa => pa.Result == "DP"),
                        TP = g.Count(pa => pa.Result == "TP"),
                        IH = g.Count(pa => pa.Result == "IH"),
                        IR = g.Count(pa => pa.Result == "IR"),
                        ID = g.Count(pa => pa.Result == "ID"),
                        IGNORE = g.Count(pa => pa.Result == "IGNORE"),
                        RBI = g.Sum(pa => pa.RBI ?? 0),
                        Opponent = (g.FirstOrDefault()?.Game?.HomeTeam == player.PlayerTeams ?
                            g.FirstOrDefault()?.Game?.AwayTeam?.TeamName : 
                            g.FirstOrDefault()?.Game?.HomeTeam?.TeamName) ?? 
                            "Unknown",
                        IsHome = g.FirstOrDefault()?.Game?.HomeTeam == player.PlayerTeams
                    };
                })
                .OrderBy(x => x.Date)
                .ToList();

            // 最佳打席
            var bestPAs = paList
                .Where(pa => pa.WPA.HasValue)
                .OrderByDescending(pa => pa.WPA)
                .Take(5)
                .Select(pa => new BestPA
                {
                    Date = pa?.Game?.Date ?? DateTime.MinValue,
                    SeasonName = seasons.FirstOrDefault(s => s.SeasonId == pa?.SeasonId)?.SeasonName ?? "Unknown",
                    Seq = pa?.GameSeq ?? 0,
                    Inning = pa?.Inning ?? 0,
                    PASeq = pa?.PaSeq ?? 0,
                    PAResult = pa?.Result ?? string.Empty,
                    WPA = pa?.WPA
                })
                .ToList();

            // 計算百分位排名和平均值
            var (percentileRanks, seasonAverages) = await CalculatePercentileRanksAsync(playerId, seasonId, gameStats);

            // 建立 ViewModel
            var model = new PlayerDetailViewModel
            {
                SeasonId = seasonId ?? "ALL",
                Player = player,
                Stats = new Stats
                {
                    GameStats = gameStats,
                    BestPAs = bestPAs,
                    PercentileRanks = percentileRanks,
                    SeasonAverages = seasonAverages
                },
                SeriesList = await GetAllSeasonsAsync()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"載入球員 {playerId} 詳細資訊時發生錯誤");
            return View("Error");
        }
    }

    /// <summary>
    /// 計算球員各項指標的百分位排名和賽季平均值
    /// </summary>
    /// <param name="playerId">球員ID</param>
    /// <param name="seasonId">賽季ID</param>
    /// <param name="gameStats">球員比賽統計</param>
    /// <returns>百分位排名字典和平均值字典</returns>
    private async Task<(Dictionary<string, decimal>, Dictionary<string, decimal>)> CalculatePercentileRanksAsync(
        string playerId, string? seasonId, List<GameStat> gameStats)
    {
        try
        {
            // 先檢查快取是否存在
            var effectiveSeasonId = seasonId ?? "ALL";
            var isCacheStale = await _rankingCacheService.IsCacheStaleAsync(effectiveSeasonId, hoursThreshold: 24);
            
            if (isCacheStale)
            {
                // 快取過期或不存在,背景更新快取
                _logger.LogWarning($"打者快取過期或不存在,將在背景更新: {effectiveSeasonId}");
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _rankingCacheService.UpdateBattingRankingsAsync(effectiveSeasonId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"背景更新打者快取失敗: {effectiveSeasonId}");
                    }
                });
            }

            // 從快取讀取所有球員的統計資料
            var allPlayerStats = await _rankingCacheService.GetBattingStatsFromCacheAsync(effectiveSeasonId);
            
            if (!allPlayerStats.Any())
            {
                _logger.LogWarning($"無法從快取讀取打者數據: {effectiveSeasonId}");
                return (new Dictionary<string, decimal>(), new Dictionary<string, decimal>());
            }

            // 計算當前球員的數據
            var totalABPlayer = gameStats.Sum(g => g.AB);
            var totalHPlayer = gameStats.Sum(g => g.H);
            var totalBBPlayer = gameStats.Sum(g => g.BB);
            var totalHBPPlayer = gameStats.Sum(g => g.HBP);
            var totalSFPlayer = gameStats.Sum(g => g.SF);
            var totalHRPlayer = gameStats.Sum(g => g.HR + g.IHR);
            var totalRBIPlayer = gameStats.Sum(g => g.RBI);
            var totalSOPlayer = gameStats.Sum(g => g.SO);
            var totalBasesPlayer = gameStats.Sum(g => g._1B + g._2B * 2 + g._3B * 3 + (g.HR + g.IHR) * 4);

            var avgPlayer = totalABPlayer > 0 ? (decimal)totalHPlayer / totalABPlayer : 0;
            var obpPlayer = (totalABPlayer + totalBBPlayer + totalHBPPlayer + totalSFPlayer) > 0
                ? (decimal)(totalHPlayer + totalBBPlayer + totalHBPPlayer) / (totalABPlayer + totalBBPlayer + totalHBPPlayer + totalSFPlayer)
                : 0;
            var slgPlayer = totalABPlayer > 0 ? (decimal)totalBasesPlayer / totalABPlayer : 0;
            var opsPlayer = obpPlayer + slgPlayer;

            // 計算百分位排名 (PR值)
            var percentileRanks = new Dictionary<string, decimal>();
            percentileRanks["AVG"] = CalculatePercentile(allPlayerStats.Select(p => p.AVG).ToList(), avgPlayer);
            percentileRanks["OBP"] = CalculatePercentile(allPlayerStats.Select(p => p.OBP).ToList(), obpPlayer);
            percentileRanks["SLG"] = CalculatePercentile(allPlayerStats.Select(p => p.SLG).ToList(), slgPlayer);
            percentileRanks["OPS"] = CalculatePercentile(allPlayerStats.Select(p => p.OPS).ToList(), opsPlayer);
            percentileRanks["HR"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.HR).ToList(), totalHRPlayer);
            percentileRanks["RBI"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.RBI).ToList(), totalRBIPlayer);
            percentileRanks["SO"] = 100 - CalculatePercentile(allPlayerStats.Select(p => (decimal)p.SO).ToList(), totalSOPlayer); // SO越少越好
            percentileRanks["BB"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.BB).ToList(), totalBBPlayer);

            // 計算賽季平均值
            var seasonAverages = new Dictionary<string, decimal>();
            seasonAverages["AVG"] = allPlayerStats.Average(p => p.AVG);
            seasonAverages["OBP"] = allPlayerStats.Average(p => p.OBP);
            seasonAverages["SLG"] = allPlayerStats.Average(p => p.SLG);
            seasonAverages["OPS"] = allPlayerStats.Average(p => p.OPS);
            seasonAverages["HR"] = (decimal)allPlayerStats.Average(p => p.HR);
            seasonAverages["RBI"] = (decimal)allPlayerStats.Average(p => p.RBI);
            seasonAverages["SO"] = (decimal)allPlayerStats.Average(p => p.SO);
            seasonAverages["BB"] = (decimal)allPlayerStats.Average(p => p.BB);

            return (percentileRanks, seasonAverages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "計算百分位排名時發生錯誤");
            return (new Dictionary<string, decimal>(), new Dictionary<string, decimal>());
        }
    }

    /// <summary>
    /// 計算百分位排名
    /// </summary>
    /// <param name="values">所有數值列表</param>
    /// <param name="targetValue">目標數值</param>
    /// <returns>百分位排名 (0-100)</returns>
    private decimal CalculatePercentile(List<decimal> values, decimal targetValue)
    {
        if (!values.Any()) return 0;

        var count = values.Count(v => v < targetValue);
        return Math.Round((decimal)count / values.Count * 100, 1);
    }

    /// <summary>
    /// 初始化排行榜 ViewModel
    /// </summary>
    /// <param name="seasonId">
    /// 賽季識別碼,格式例如 "CPBL-2024-HE"
    /// </param>
    /// <param name="category">
    /// 排行榜類別,"batting" 或 "pitching"
    /// </param>
    /// <returns>
    /// 初始化後的 RankingsViewModel
    /// </returns>
    private async Task<RankingsViewModel> initializeRankingViewModel(string seasonId, string category)
    {
        var vm = new RankingsViewModel
        {
            SeasonId = seasonId,
            Category = category == "pitching" ? RankingCategory.Pitching : RankingCategory.Batting
        };

        // 讀取賽季資料並插入 "ALL" 選項於最前
        vm.Seasons = GetAllSeasonsAsync().Result;

        // 計算門檻：單季 -> 打席 >= 120 * 3.1；投手局數 >= 120。全部賽季不設門檻。
        vm.MinQualifiedPA = seasonId != "CPBL-2024-xa" ? 0 : (int)Math.Ceiling(120 * 3.1m);
        vm.MinQualifiedIP = seasonId != "CPBL-2024-xa" ? 0 : 120; // 以整數局為門檻

        return vm;
    }

    /// <summary>
    /// 取得所有賽季列表，並在最前面加入 "全部賽季" 選項
    /// </summary>
    /// <returns>
    /// 賽季列表
    /// </returns>
    private async Task<List<SelectListItem>> GetAllSeasonsAsync()
    {
        try
        {
            List<SelectListItem> list = [];

            var seasons = await _baseballDbService.GetAllSeasonsAsync();

            // 建立 SelectListItem 列表
            list = [.. seasons
                .OrderByDescending(s => s.SeasonId)
                .Select(s => new SelectListItem
                {
                    Value = s.SeasonId,
                    Text = s.SeasonName ?? s.SeasonId
                })];

            // 插入 "全部賽季" 選項於最前面
            list.Insert(0, new SelectListItem
            {
                Value = "ALL",
                Text = "全部賽季"
            });

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入賽季列表時發生錯誤");
            return [];
        }
    }

    /// <summary>
    /// 排行榜頁面
    /// </summary>
    /// <param name="seasonId">
    /// 賽季識別碼，格式例如 "CPBL-2024-HE"
    /// </param>
    /// <param name="category">
    /// 排行榜類別，"batting" 或 "pitching"
    /// </param>
    /// <returns>
    /// 排行榜頁面
    /// </returns>
    public async Task<IActionResult> Rankings(string seasonId = "ALL", string category = "batting")
    {
        try
        {
            // 初始化 ViewModel
            var vm = await initializeRankingViewModel(seasonId, category);
    
            if (vm.Category == RankingCategory.Batting)
            {
                return await GetBattingRankings(vm);
            }
            else
            {
                return await GetPitchingRankings(vm);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入排行榜頁面時發生錯誤");
            return View("Error");
        }
    }

    /// <summary>
    /// 取得打者排行榜
    /// </summary>
    /// <param name="vm">
    /// 排行榜 ViewModel
    /// </param>
    /// <returns>
    /// 打者排行榜頁面
    /// </returns>
    public async Task<IActionResult> GetBattingRankings(RankingsViewModel vm)
    {
        try
        {
            // 優先使用快取，如果快取不存在或過期則重新計算
            var cachedRankings = await _rankingCacheService.GetBattingRankingsFromCacheAsync(vm.SeasonId, vm.MinQualifiedPA);

            // 檢查快取是否過期（超過 24 小時）
            var isCacheStale = await _rankingCacheService.IsCacheStaleAsync(vm.SeasonId, hoursThreshold: 24);
            if (cachedRankings.Any() && !isCacheStale)
            {
                // 使用快取資料
                vm.BattingRankings = cachedRankings.Take(50).ToList();
                vm.TotalQualifiedBatters = cachedRankings.Count;
                _logger.LogInformation($"使用打者排行榜快取：{vm.SeasonId}");

                return View(vm);
            }

            // 快取不存在或過期，重新計算並更新快取
            _logger.LogInformation($"打者排行榜快取不存在或過期，重新計算：{vm.SeasonId}");

            var batterEntities = await _baseballDbService.GetAllBattersAsync(vm.SeasonId);
            List<BattingStats> allStats = [];
            foreach (var batter in batterEntities)
            {
                var stats = await _baseballDbService.CalculateBattingStatsAsync(batter.PlayerId, vm.SeasonId);
                allStats.Add(stats);
            }

            var qualified = allStats
                .Where(s => s.PlateAppearances >= vm.MinQualifiedPA)
                .OrderByDescending(s => s.Hits)
                .ToList();
            vm.TotalQualifiedBatters = qualified.Count;

            var battingRankings = qualified.Select((b, index) => new BattingRankingItem
            {
                Rank = index + 1,
                PlayerId = batterEntities.FirstOrDefault(x => x.PlayerName == b.PlayerName)?.PlayerId,
                PlayerName = b.PlayerName,
                Games = b.Games,
                PA = b.PlateAppearances,
                AB = b.AtBats,
                H = b.Hits,
                HR = b.HomeRuns,
                RBI = b.RBIs,
                BB = b.Walks,
                SO = b.Strikeouts,
                AVG = b.AtBats > 0 ? Math.Round((decimal)b.Hits / b.AtBats, 3) : 0,
                OBP = (b.AtBats + b.Walks) > 0 ? Math.Round((decimal)(b.Hits + b.Walks) / (b.AtBats + b.Walks), 3) : 0,
                SLG = b.AtBats > 0 ? Math.Round((decimal)(b.Hits + b.Doubles + b.Triples * 2 + b.HomeRuns * 3) / b.AtBats, 3) : 0
            }).ToList();
            vm.BattingRankings = battingRankings;

            // 背景更新快取（不等待完成）
            _ = Task.Run(async () =>
            {
                try
                {
                    await _rankingCacheService.UpdateBattingRankingsAsync(vm.SeasonId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"背景更新打者排行榜快取失敗：{vm.SeasonId}");
                }
            });

            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入打者排行榜頁面時發生錯誤");
            return View("Error");
        }
    }

    /// <summary>
    /// 取得投手排行榜
    /// </summary>
    /// <param name="vm">
    /// 排行榜 ViewModel
    /// </param>
    /// <returns>
    /// 投手排行榜頁面
    /// </returns>
    public async Task<IActionResult> GetPitchingRankings(RankingsViewModel vm)
    {
        try
        {
            // 優先使用快取，如果快取不存在或過期則重新計算
            var cachedRankings = await _rankingCacheService.GetPitchingRankingsFromCacheAsync(vm.SeasonId, vm.MinQualifiedPA);

            // 檢查快取是否過期（超過 24 小時）
            var isCacheStale = await _rankingCacheService.IsCacheStaleAsync(vm.SeasonId, hoursThreshold: 24);
            if (cachedRankings.Any() && !isCacheStale)
            {
                // 使用快取資料
                vm.PitchingRankings = cachedRankings.Take(50).ToList();
                vm.TotalQualifiedBatters = cachedRankings.Count;
                _logger.LogInformation($"使用打者排行榜快取：{vm.SeasonId}");

                return View(vm);
            }

            // 快取不存在或過期，重新計算並更新快取
            _logger.LogInformation($"投手排行榜快取不存在或過期，重新計算：{vm.SeasonId}");

            var pitchers = await _baseballDbService.GetAllPitchersAsync(vm.SeasonId);
            var pitcherBoxes = await _baseballDbService.GetPitcherBoxAsync(seasonId: vm.SeasonId);
            var grouped = pitcherBoxes
                .GroupBy(pb => pb.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key,
                    PlayerName = pitchers.FirstOrDefault(p => p.PlayerId == g.Key)?.PlayerName ?? "Unknown",
                    Games = g.Select(x => x.GameSeq).Distinct().Count(),
                    IPOuts = g.Sum(x => x.IPOuts ?? 0),
                    IP = (decimal)(g.Sum(x => x.IPOuts ?? 0) / 3) + (decimal)(g.Sum(x => x.IPOuts ?? 0) % 3) / 10m,
                    H = g.Sum(x => x.H ?? 0),
                    HR = g.Sum(x => x.HR ?? 0),
                    BB = g.Sum(x => x.BB ?? 0),
                    SO = g.Sum(x => x.SO ?? 0),
                    R = g.Sum(x => x.R ?? 0),
                    ER = g.Sum(x => x.ER ?? 0)
                })
                .ToList();

            var qualifiedPitchers = grouped
                .Where(p => p.IP >= vm.MinQualifiedIP)
                .Select(p => new
                {
                    p.PlayerId,
                    p.PlayerName,
                    p.Games,
                    p.IPOuts,
                    p.IP,
                    p.H,
                    p.HR,
                    p.BB,
                    p.SO,
                    p.R,
                    p.ER,
                    ERA = p.IPOuts > 0 ? Math.Round((decimal)p.ER * 27 / p.IPOuts, 2) : 0,
                    WHIP = p.IPOuts > 0 ? Math.Round((decimal)(p.H + p.BB) * 3 / p.IPOuts, 2) : 0
                })
                .OrderBy(x => x.ERA)
                .Take(50)
                .ToList();
            vm.TotalQualifiedPitchers = qualifiedPitchers.Count;

            vm.PitchingRankings = qualifiedPitchers
                .Select((p, index) => new PitchingRankingItem
                {
                    Rank = index + 1,
                    PlayerId = p.PlayerId,
                    PlayerName = p.PlayerName,
                    Games = p.Games,
                    IP = p.IP,
                    H = p.H,
                    HR = p.HR,
                    BB = p.BB,
                    SO = p.SO,
                    R = p.R,
                    ER = p.ER,
                    ERA = p.ERA,
                    WHIP = p.WHIP
                })
                .ToList();

            // 背景更新快取（不等待完成）
            _ = Task.Run(async () =>
            {
                try
                {
                    await _rankingCacheService.UpdatePitchingRankingsAsync(vm.SeasonId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"背景更新投手排行榜快取失敗：{vm.SeasonId}");
                }
            });

            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入投手排行榜頁面時發生錯誤");
            return View("Error");
        }
    }
}