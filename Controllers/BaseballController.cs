using BaseballApp.Models;
using BaseballApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace BaseballApp.Controllers;

public class BaseballController : Controller
{
    private readonly IBaseballDbService _baseballDbService;
    private readonly IRankingCacheService _rankingCacheService;
    private readonly ILogger<BaseballController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public BaseballController(
        IBaseballDbService baseballDbService,
        IRankingCacheService rankingCacheService,
        ILogger<BaseballController> logger,
        IServiceScopeFactory scopeFactory)
    {
        _baseballDbService = baseballDbService;
        _rankingCacheService = rankingCacheService;
        _logger = logger;
        _scopeFactory = scopeFactory;
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
    public async Task<IActionResult> Teams(string seasonId = "ALL")
    {
        try
        {
            var teams = await _baseballDbService.GetAllTeamsAsync(seasonId);
            var gameResults = await _baseballDbService.GetGameResultsAsync(seasonId);
            var standings = await _baseballDbService.GetTeamStandingsAsync(seasonId);
            var seasons = await GetSeasonOptions(seasonId);

            // 計算圖表資料與時間序列
            var chartData = BuildChartData(gameResults, teams);

            var viewModel = new TeamsViewModel
            {
                SeasonId = seasonId,
                SeasonOptions = seasons,
                Teams = teams.Select(t => new TeamCardViewModel { /* ... */ }).ToList(),
                ChartData = chartData,
                Standings = standings.Select(s => new TeamStandingViewModel { /* ... */ }).ToList()
            };

            return View(viewModel);
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
    /// <param name="playerType">
    /// 球員類型，"batter" 或 "pitcher"
    /// </param>
    /// <returns>
    /// 球員列表頁面
    /// </returns>
    public async Task<IActionResult> Players(string seasonId = "ALL", string teamId = "", string playerType = "")
    {
        try
        {
            List<Player> players = new List<Player>();

            if (playerType == "batter" || string.IsNullOrEmpty(playerType))
            {
                var batters = (await _baseballDbService.GetAllBattersAsync(seasonId, teamId))
                    .Cast<Player>().ToList();
                players.AddRange(batters);
            }
            
            if (playerType == "pitcher" || string.IsNullOrEmpty(playerType))
            {
                var pitchers = (await _baseballDbService.GetAllPitchersAsync(seasonId, teamId))
                    .Cast<Player>().ToList();
                players.AddRange(pitchers);
            }
            
            var seasonOptions = await GetSeasonOptions(seasonId);
            var teamOptions = await GetTeamOptions(seasonId, teamId);

            var vm = new PlayersViewModel
            {
                SeasonId = seasonId,
                TeamId = teamId,
                PlayerType = playerType,
                SeasonOptions = seasonOptions,
                TeamOptions = teamOptions,
                Players = players.OrderBy(p => 
                    int.TryParse(p.PlayerNumber, out var num) ? num : 999).ToList()
            };

            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入球員頁面時發生錯誤");
            return View("Error");
        }
    }

    /// <summary>
    /// 取得賽季下拉選單項目
    /// </summary>
    private async Task<List<SelectListItem>> GetSeasonOptions(string seasonId)
    {
        try
        {
            var seasons = await _baseballDbService.GetAllSeasonsAsync();

            var seasonOptions = seasons
                .OrderByDescending(s => s.SeasonId)
                .Select(s => new SelectListItem
                {
                    Value = s.SeasonId,
                    Text = s.SeasonName ?? s.SeasonId,
                    Selected = (s.SeasonId == seasonId)
                })
                .ToList();

            seasonOptions.Insert(0, new SelectListItem
            {
                Value = "ALL",
                Text = "全部賽季",
                Selected = (seasonId == "ALL")
            });

            return seasonOptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得賽季下拉選單項目時發生錯誤");
            return new List<SelectListItem>();
        }
    }

    private async Task<List<SelectListItem>> GetTeamOptions(string seasonId, string teamId)
    {
        var teams = await _baseballDbService.GetAllTeamsAsync(seasonId);
        var teamOptions = teams
            .OrderBy(t => t.TeamName)
            .Select(t => new SelectListItem
            {
                Value = t.TeamId,
                Text = t.TeamName,
                Selected = (t.TeamId == teamId)
            })
            .ToList();

        teamOptions.Insert(0, new SelectListItem
        {
            Value = "",
            Text = "全部球隊",
            Selected = string.IsNullOrEmpty(teamId)
        });

        return teamOptions;
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
    public async Task<IActionResult> PlayerDetail(string playerId, string seasonId = "ALL")
    {
        try
        {
            // 讀取賽季列表（供下拉選單）
            var seriesList = await GetAllSeasonsAsync();

            // 生成打者與投手詳細資料
            var batterDetail = await BuildBatterDetail(playerId, seasonId);
            var pitcherDetail = await BuildPitcherDetail(playerId, seasonId);

            // 若皆為 null，視為不存在
            if (batterDetail == null && pitcherDetail == null)
            {
                return NotFound();
            }

            var model = new PlayerDetailViewModel
            {
                SeasonId = seasonId,
                SeriesList = seriesList,
                BatterDetail = batterDetail,
                PitcherDetail = pitcherDetail
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
    /// 建立打者詳細資料
    /// </summary>
    /// <param name="playerId">
    /// 球員識別碼
    /// </param>
    /// <param name="seasonId">
    /// 賽季識別碼，格式例如 "CPBL-2024-HE"
    /// </param>
    /// <returns>
    /// 打者詳細資料模型，若找不到則回傳 null
    /// </returns>
    private async Task<BatterDetailModel?> BuildBatterDetail(string playerId, string seasonId)
    {
        var batter = await _baseballDbService.GetBatterAsync(playerId);
        if (batter == null) return null;

        var seasonsDict = (await _baseballDbService.GetAllSeasonsAsync()).ToDictionary(s => s.SeasonId, s => s.SeasonName ?? s.SeasonId);
        var paList = await _baseballDbService.GetPAAsync(batterId: playerId, seasonId: seasonId);
        var batterTeam = batter.PlayerTeams.FirstOrDefault()?.TeamId;

        var batterGameStats = BuildBatterGameStats(paList.ToList(), seasonsDict, batterTeam);
        var bestPAs = BuildBestPAs(paList.ToList(), seasonsDict, batterTeam);

        return new BatterDetailModel
        {
            Batter = batter,
            Stats = new BatterStats
            {
                GameStats = batterGameStats,
                BestPAs = bestPAs
            }
        };
    }

    /// <summary>
    /// 建立打者每場統計
    /// </summary>
    /// <param name="paList">
    /// 打席列表
    /// </param>
    /// <param name="seasonsDict">
    /// 賽季字典
    /// </param>
    /// <param name="batterTeam">
    /// 打者所屬球隊ID
    /// </param>
    /// <returns>
    /// 打者每場統計列表
    /// </returns>
    private List<BatterGameStat> BuildBatterGameStats(List<PA> paList, Dictionary<string, string> seasonsDict, string? batterTeam)
    {
        return paList
            .GroupBy(pa => new { pa.SeasonId, pa.GameSeq })
            .Select(g =>
            {
                var first = g.FirstOrDefault();
                var game = first?.Game;
                var seasonName = seasonsDict.TryGetValue(g.Key.SeasonId!, out var sn) ? sn : "Unknown";
                var is_home = game?.HomeTeamId == batterTeam;

                return new BatterGameStat
                {
                    Date = game?.Date ?? DateTime.MinValue,
                    SeasonName = seasonName,
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
                    Opponent = GetOpponentTeamName(game, batterTeam),
                    IsHome = is_home
                };
            })
            .OrderBy(x => x.Date)
            .ToList();
    }

    /// <summary>
    /// 建立最佳打席
    /// </summary>
    /// <param name="paList">
    /// 打席列表
    /// </param>
    /// <param name="seasonsDict">
    /// 賽季字典
    /// </param>
    /// <param name="batterTeam">
    /// 打者所屬球隊ID
    /// </param>
    /// <returns>
    /// 最佳打席列表
    /// </returns>
    private List<BestPA> BuildBestPAs(List<PA> paList, Dictionary<string, string> seasonsDict, string? batterTeam)
    {
        return paList
            .Where(pa => pa.WPA.HasValue)
            .OrderByDescending(pa => pa.WPA)
            .Take(5)
            .Select(pa => new BestPA
            {
                Date = pa.Game?.Date ?? DateTime.MinValue,
                SeasonName = seasonsDict.TryGetValue(pa.SeasonId!, out var sn) ? sn : "Unknown",
                Seq = pa.GameSeq,
                Opponent = GetOpponentTeamName(pa.Game, batterTeam),
                Inning = pa.Inning,
                PASeq = pa.PaSeq,
                PAResult = pa.Result ?? string.Empty,
                WPA = pa.WPA
            })
            .ToList();
    }

    /// <summary>
    /// 取得對手隊名
    /// </summary>
    /// <param name="game">
    /// 比賽物件
    /// </param>
    /// <param name="playerTeamId">
    /// 球員所屬球隊ID
    /// </param>
    /// <returns>
    /// 對手隊名
    /// </returns>
    private string GetOpponentTeamName(Game? game, string? playerTeamId)
    {
        if (game?.HomeTeamId == null)
            return "Unknown";

        var is_home = game.HomeTeamId == playerTeamId;
        return (is_home ? game.AwayTeam?.TeamName : game.HomeTeam?.TeamName) ?? "Unknown";
    }

    /// <summary>
    /// 建立投手詳細資料
    /// </summary>
    /// <param name="playerId">
    /// 投手識別碼
    /// </param>
    /// <param name="seasonId">
    /// 賽季識別碼
    /// </param>
    /// <returns>
    /// 投手詳細資料模型
    /// </returns>
    private async Task<PitcherDetailModel?> BuildPitcherDetail(string playerId, string seasonId)
    {
        // 取得投手基本資料
        var pitcher = await _baseballDbService.GetPitcherAsync(playerId);
        if (pitcher == null) return null;

        // 取得投手成績資料
        var pitcherBoxes = await _baseballDbService.GetPitcherBoxAsync(seasonId: seasonId);
        var seasonsDict = (await _baseballDbService.GetAllSeasonsAsync()).ToDictionary(s => s.SeasonId, s => s.SeasonName ?? s.SeasonId);
        var pitcherTeam = pitcher.PlayerTeams.FirstOrDefault()?.TeamId;

        // 取得投手投球事件資料
        var pitcherEvents = await _baseballDbService.GetPitcherEventsAsync(playerId, seasonId);

        // 建立投手每場統計與最佳投手表現
        var pitcherGameStats = BuildPitcherGameStats(pitcherBoxes.ToList(), playerId, seasonsDict, pitcherTeam);
        var bestPitchingPerformances = BuildBestPitchingPerformances(pitcherGameStats);

        // 建立球種統計與球速統計
        var pitchTypeStats = BuildPitchTypeStats(pitcherEvents);
        var velocityStats = BuildVelocityStats(pitcherEvents);

        // 回傳投手詳細資料模型
        return new PitcherDetailModel
        {
            Pitcher = pitcher,
            Stats = new PitcherStats
            {
                GameStats = pitcherGameStats,
                BestPerformances = bestPitchingPerformances,
                PitchTypeStats = pitchTypeStats,
                VelocityStats = velocityStats
            }
        };
    }

    /// <summary>
    /// 建立投手每場統計
    /// </summary>
    /// <param name="pitcherBoxes">
    /// 投手 Box 列表
    /// </param>
    /// <param name="playerId">
    /// 投手識別碼
    /// </param>
    /// <param name="seasonsDict">
    /// 賽季字典
    /// </param>
    /// <param name="pitcherTeam">
    /// 投手所屬球隊ID
    /// </param>
    /// <returns>
    /// 投手每場統計列表
    /// </returns>
    private List<PitcherGameStat> BuildPitcherGameStats(List<PitcherBox> pitcherBoxes, string playerId, 
        Dictionary<string, string> seasonsDict, string? pitcherTeam)
    {
        return pitcherBoxes
            .Where(pb => pb.PlayerId == playerId)
            .GroupBy(pb => new { pb.SeasonId, pb.GameSeq })
            .Select(g =>
            {
                var first = g.FirstOrDefault();
                int ipOuts = g.Sum(x => x.IPOuts ?? 0);
                int er = g.Sum(x => x.ER ?? 0);
                int h = g.Sum(x => x.H ?? 0);
                int bb = g.Sum(x => x.BB ?? 0);
                int so = g.Sum(x => x.SO ?? 0);
                int hr = g.Sum(x => x.HR ?? 0);
                int np = g.Sum(x => x.NP ?? 0);
                int bf = g.Sum(x => x.BF ?? 0);
                int r = g.Sum(x => x.R ?? 0);

                var game = first?.Game;
                var opponentName = GetOpponentTeamName(game, pitcherTeam);

                return new PitcherGameStat
                {
                    Date = first?.Game?.Date ?? DateTime.MinValue,
                    SeasonName = seasonsDict.TryGetValue(g.Key.SeasonId!, out var sn) ? sn : (g.Key.SeasonId ?? "Unknown"),
                    Seq = g.Key.GameSeq,
                    Opponent = opponentName,
                    IsStarter = false,
                    IPOuts = ipOuts,
                    NP = np,
                    H = h,
                    HR = hr,
                    SO = so,
                    BB = bb,
                    R = r,
                    ER = er,
                    BF = bf
                };
            })
            .OrderBy(x => x.Date)
            .ToList();
    }

    /// <summary>
    /// 建立最佳投手表現
    /// </summary>
    /// <param name="pitcherGameStats">
    /// 投手每場統計列表
    /// </param>
    /// <returns>
    /// 最佳投手表現列表
    /// </returns>
    private List<BestPitchingPerformance> BuildBestPitchingPerformances(List<PitcherGameStat> pitcherGameStats)
    {
        return pitcherGameStats
            .Select(g => new BestPitchingPerformance
            {
                Date = g.Date,
                SeasonName = g.SeasonName,
                Seq = g.Seq,
                Opponent = g.Opponent,
                IP = g.IP,
                SO = g.SO,
                ERA = g.ERA,
                Score = (decimal)g.IP * 10m - (decimal)g.ERA * 2m + (decimal)g.SO * 0.5m
            })
            .OrderByDescending(x => x.Score)
            .Take(5)
            .ToList();
    }

    /// <summary>
    /// 建立球種使用統計
    /// </summary>
    /// <param name="pitcherEvents">
    /// 投手投球事件列表
    /// </param>
    /// <returns>
    /// 球種統計列表
    /// </returns>
    private List<PitchTypeStat> BuildPitchTypeStats(IEnumerable<Event> pitcherEvents)
    {
        var pitchTypeGroups = pitcherEvents
            .Where(e => !string.IsNullOrEmpty(e.PitchType))
            .GroupBy(e => e.PitchType)
            .Select(g => new
            {
                PitchType = g.Key,
                Count = g.Count(),
                AverageVelocity = g.Where(e => e.Velocity.HasValue).Any() 
                    ? g.Where(e => e.Velocity.HasValue).Average(e => e.Velocity.Value) 
                    : 0m
            })
            .ToList();

        var totalPitches = pitchTypeGroups.Sum(g => g.Count);

        return pitchTypeGroups
            .Select(g => new PitchTypeStat
            {
                PitchType = g.PitchType ?? "",
                PitchTypeName = GetPitchTypeName(g.PitchType ?? ""),
                Count = g.Count,
                UsagePercentage = totalPitches > 0 ? Math.Round((decimal)g.Count / totalPitches * 100, 1) : 0,
                AverageVelocity = Math.Round(g.AverageVelocity, 1)
            })
            .OrderByDescending(x => x.UsagePercentage)
            .ToList();
    }

    /// <summary>
    /// 建立球速統計
    /// </summary>
    /// <param name="pitcherEvents">
    /// 投手投球事件列表
    /// </param>
    /// <returns>
    /// 球速統計
    /// </returns>
    private VelocityStat BuildVelocityStats(IEnumerable<Event> pitcherEvents)
    {
        var velocities = pitcherEvents
            .Where(e => e.Velocity.HasValue)
            .Select(e => e.Velocity.Value)
            .ToList();

        if (!velocities.Any())
        {
            return new VelocityStat();
        }

        var avgVelocity = velocities.Average();
        var maxVelocity = velocities.Max();
        var minVelocity = velocities.Min();
        var variance = velocities.Sum(v => Math.Pow((double)(v - avgVelocity), 2)) / velocities.Count;
        var stdDev = Math.Sqrt(variance);

        return new VelocityStat
        {
            AverageVelocity = Math.Round(avgVelocity, 1),
            MaxVelocity = Math.Round(maxVelocity, 1),
            MinVelocity = Math.Round(minVelocity, 1),
            VelocityStdDev = Math.Round((decimal)stdDev, 1)
        };
    }

    /// <summary>
    /// 取得球種名稱
    /// </summary>
    /// <param name="pitchType">
    /// 球種代碼
    /// </param>
    /// <returns>
    /// 球種名稱
    /// </returns>
    private string GetPitchTypeName(string pitchType)
    {
        return pitchType switch
        {
            "FF" => "四縫線快速球",
            "FT" => "二縫線快速球",
            "SI" => "伸卡球",
            "FC" => "卡特球",
            "CU" => "曲球",
            "SL" => "滑球",
            "CH" => "變速球",
            "KN" => "蝴蝶球",
            "EP" => "小便球",
            "FO" => "指叉球",
            "FS" => "快指球",
            "SC" => "螺絲球",
            _ => pitchType
        };
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
        vm.Seasons = await GetAllSeasonsAsync();

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
    private async Task<List<SelectListItem>> GetAllSeasonsAsync(string playerId = "")
    {
        try
        {
            List<SelectListItem> list = [];

            var seasons = await _baseballDbService.GetAllSeasonsAsync(playerId);

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
            if (cachedRankings.Count != 0 && !isCacheStale)
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
                    using var scope = _scopeFactory.CreateScope();
                    var rankingCacheService = scope.ServiceProvider.GetRequiredService<IRankingCacheService>();
                    await rankingCacheService.UpdateBattingRankingsAsync(vm.SeasonId);
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
            if (cachedRankings.Count != 0 && !isCacheStale)
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
                    using var scope = _scopeFactory.CreateScope();
                    var rankingCacheService = scope.ServiceProvider.GetRequiredService<IRankingCacheService>();
                    await rankingCacheService.UpdatePitchingRankingsAsync(vm.SeasonId);
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