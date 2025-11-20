using BaseballApp.Models;
using BaseballApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace BaseballApp.Controllers;

public class BaseballController : Controller
{
    private readonly IBaseballDbService _baseballDbService;
    private readonly ILogger<BaseballController> _logger;

    public BaseballController(IBaseballDbService baseballDbService, ILogger<BaseballController> logger)
    {
        _baseballDbService = baseballDbService;
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
            seasonId ??= "CPBL-2024-HE";

            var batters = await _baseballDbService.GetAllBattersAsync(seasonId);
            var player = batters.FirstOrDefault(b => b.PlayerId == playerId);

            if (player == null)
            {
                return NotFound();
            }

            // 取得球員打席記錄
            var paList = await _baseballDbService.GetPAAsync(batterId: playerId);
            var seasonPAs = paList.Where(pa => pa.SeasonId == seasonId).ToList();

            // 計算統計數據
            var stats = new
            {
                TotalPAs = seasonPAs.Count,
                HomeRuns = seasonPAs.Count(pa => pa.Result == "HR"),
                Strikeouts = seasonPAs.Count(pa => pa.Result == "SO"),
                Walks = seasonPAs.Count(pa => pa.Result == "uBB" || pa.Result == "IBB"),
                TotalRBI = seasonPAs.Sum(pa => pa.RBI ?? 0),
                AvgWPA = seasonPAs.Where(pa => pa.WPA.HasValue).Any() ? 
                    seasonPAs.Where(pa => pa.WPA.HasValue).Average(pa => pa.WPA) : 0,
                
                // 按比賽統計
                GameStats = seasonPAs
                    .GroupBy(pa => new { pa.SeasonId, pa.GameSeq })
                    .Select(g => new
                    {
                        GameSeq = g.Key.GameSeq,
                        PAs = g.Count(),
                        Hits = g.Count(pa => pa.Result == "1B" || pa.Result == "2B" || 
                                            pa.Result == "3B" || pa.Result == "HR"),
                        HRs = g.Count(pa => pa.Result == "HR"),
                        RBIs = g.Sum(pa => pa.RBI ?? 0)
                    })
                    .OrderBy(x => x.GameSeq)
                    .ToList(),
                
                // 最佳打席
                BestPAs = seasonPAs
                    .Where(pa => pa.WPA.HasValue)
                    .OrderByDescending(pa => pa.WPA)
                    .Take(5)
                    .Select(pa => new
                    {
                        GameSeq = pa.GameSeq,
                        Inning = pa.Inning,
                        Result = pa.Result,
                        RBI = pa.RBI,
                        WPA = pa.WPA
                    })
                    .ToList()
            };

            ViewBag.Player = player;
            ViewBag.SeasonId = seasonId;
            ViewBag.Stats = stats;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"載入球員 {playerId} 詳細資訊時發生錯誤");
            return View("Error");
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
    public async Task<IActionResult> Rankings(string? seasonId = null, string category = "batting")
    {
        try
        {
            seasonId ??= "CPBL-2024-HE";
            var vm = new RankingsViewModel
            {
                SeasonId = seasonId,
                Category = category == "pitching" ? RankingCategory.Pitching : RankingCategory.Batting
            };

            if (vm.Category == RankingCategory.Batting)
            {
                var topBatters = await _baseballDbService.GetTopBattersAsync(seasonId, 50);
                var batters = await _baseballDbService.GetAllBattersAsync(seasonId);

                var battingRankings = topBatters.Select((b, index) =>
                {
                    var playerId = batters.FirstOrDefault(x => x.PlayerName == b.PlayerName)?.PlayerId;
                    return new BattingRankingItem
                    {
                        Rank = index + 1,
                        PlayerId = playerId,
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
                    };
                }).ToList();

                vm.BattingRankings = battingRankings;
            }
            else
            {
                var pitchers = await _baseballDbService.GetAllPitchersAsync(seasonId);
                var pitcherBoxes = await _baseballDbService.GetPitcherBoxAsync(seasonId: seasonId);

                var pitchingRankings = pitcherBoxes
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
                        ER = g.Sum(x => x.ER ?? 0),
                        ERA = g.Sum(x => x.IPOuts ?? 0) > 0 ? Math.Round((decimal)g.Sum(x => x.ER ?? 0) * 27 / g.Sum(x => x.IPOuts ?? 0), 2) : 0,
                        WHIP = g.Sum(x => x.IPOuts ?? 0) > 0 ? Math.Round((decimal)(g.Sum(x => x.H ?? 0) + g.Sum(x => x.BB ?? 0)) * 3 / g.Sum(x => x.IPOuts ?? 0), 2) : 0
                    })
                    .OrderBy(x => x.ERA)
                    .Take(50)
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

                vm.PitchingRankings = pitchingRankings;
            }

            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入排行榜頁面時發生錯誤");
            return View("Error");
        }
    }
}