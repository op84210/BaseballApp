using BaseballApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace BaseballApp.Controllers;

[Route("api/playerdata")]
[ApiController]
public class PlayerDataApiController : ControllerBase
{
    private readonly IBaseballDbService _baseballDbService;
    private readonly IRankingCacheService _rankingCacheService;
    private readonly ILogger<PlayerDataApiController> _logger;

    public PlayerDataApiController(
        IBaseballDbService baseballDbService,
        IRankingCacheService rankingCacheService,
        ILogger<PlayerDataApiController> logger)
    {
        _baseballDbService = baseballDbService;
        _rankingCacheService = rankingCacheService;
        _logger = logger;
    }

    /// <summary>
    /// 取得打者圖表數據
    /// </summary>
    [HttpGet("batter/{playerId}/chart")]
    public async Task<IActionResult> GetBatterChartData(
        string playerId, 
        [FromQuery] string? seasonId = null)
    {
        try
        {
            var batter = await _baseballDbService.GetBatterAsync(playerId);
            if (batter == null)
            {
                return NotFound(new { error = "球員不存在" });
            }

            var paList = await _baseballDbService.GetPAAsync(batterId: playerId, seasonId: seasonId);
            var seasons = await _baseballDbService.GetAllSeasonsAsync();

            // 打擊數據 (按比賽統計)
            var gameStats = paList
                .GroupBy(pa => new { pa.SeasonId, pa.GameSeq })
                .Select(g => new
                {
                    Date = g.FirstOrDefault()?.Game?.Date ?? DateTime.MinValue,
                    SeasonName = seasons.FirstOrDefault(s => s.SeasonId == g.Key.SeasonId)?.SeasonName ?? "Unknown",
                    Seq = g.Key.GameSeq,
                    PA = g.Count(),
                    AB = g.Count(pa => new[] { "1B", "2B", "3B", "HR", "IHR", "SO", "GO", "FO", "FC", "E", "GIDP", "DP", "TP" }.Contains(pa.Result)),
                    H = g.Count(pa => new[] { "1B", "2B", "3B", "HR", "IHR" }.Contains(pa.Result)),
                    _1B = g.Count(pa => pa.Result == "1B"),
                    _2B = g.Count(pa => pa.Result == "2B"),
                    _3B = g.Count(pa => pa.Result == "3B"),
                    HR = g.Count(pa => pa.Result == "HR" || pa.Result == "IHR"),
                    SO = g.Count(pa => pa.Result == "SO"),
                    BB = g.Count(pa => pa.Result == "uBB" || pa.Result == "IBB"),
                    HBP = g.Count(pa => pa.Result == "HBP"),
                    SF = g.Count(pa => pa.Result == "SF"),
                    RBI = g.Sum(pa => pa.RBI ?? 0)
                })
                .OrderBy(x => x.Date)
                .ToList();

            // 計算累計數據和率值
            var chartData = new List<object>();
            int totalAB = 0, totalH = 0, totalBB = 0, totalHBP = 0, totalSF = 0, totalBases = 0;

            foreach (var game in gameStats)
            {
                totalAB += game.AB;
                totalH += game.H;
                totalBB += game.BB;
                totalHBP += game.HBP;
                totalSF += game.SF;
                totalBases += game._1B + game._2B * 2 + game._3B * 3 + game.HR * 4;

                var avg = totalAB > 0 ? Math.Round((decimal)totalH / totalAB, 3) : 0;
                var obp = (totalAB + totalBB + totalHBP + totalSF) > 0
                    ? Math.Round((decimal)(totalH + totalBB + totalHBP) / (totalAB + totalBB + totalHBP + totalSF), 3)
                    : 0;
                var slg = totalAB > 0 ? Math.Round((decimal)totalBases / totalAB, 3) : 0;
                var ops = obp + slg;

                chartData.Add(new
                {
                    date = game.Date,
                    seq = game.Seq,
                    avg,
                    obp,
                    slg,
                    ops
                });
            }

            // 計算百分位排名
            var allPlayerStats = await _rankingCacheService.GetBattingStatsFromCacheAsync(seasonId ?? "ALL");
            var totalABPlayer = gameStats.Sum(g => g.AB);
            var totalHPlayer = gameStats.Sum(g => g.H);
            var totalBBPlayer = gameStats.Sum(g => g.BB);
            var totalHBPPlayer = gameStats.Sum(g => g.HBP);
            var totalSFPlayer = gameStats.Sum(g => g.SF);
            var totalHRPlayer = gameStats.Sum(g => g.HR);
            var totalRBIPlayer = gameStats.Sum(g => g.RBI);
            var totalSOPlayer = gameStats.Sum(g => g.SO);
            var totalBasesPlayer = gameStats.Sum(g => g._1B + g._2B * 2 + g._3B * 3 + g.HR * 4);

            var avgPlayer = totalABPlayer > 0 ? (decimal)totalHPlayer / totalABPlayer : 0;
            var obpPlayer = (totalABPlayer + totalBBPlayer + totalHBPPlayer + totalSFPlayer) > 0
                ? (decimal)(totalHPlayer + totalBBPlayer + totalHBPPlayer) / (totalABPlayer + totalBBPlayer + totalHBPPlayer + totalSFPlayer)
                : 0;
            var slgPlayer = totalABPlayer > 0 ? (decimal)totalBasesPlayer / totalABPlayer : 0;
            var opsPlayer = obpPlayer + slgPlayer;

            var percentileRanks = new Dictionary<string, decimal>();
            if (allPlayerStats.Any())
            {
                percentileRanks["AVG"] = CalculatePercentile(allPlayerStats.Select(p => p.AVG).ToList(), avgPlayer);
                percentileRanks["OBP"] = CalculatePercentile(allPlayerStats.Select(p => p.OBP).ToList(), obpPlayer);
                percentileRanks["SLG"] = CalculatePercentile(allPlayerStats.Select(p => p.SLG).ToList(), slgPlayer);
                percentileRanks["OPS"] = CalculatePercentile(allPlayerStats.Select(p => p.OPS).ToList(), opsPlayer);
                percentileRanks["HR"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.HR).ToList(), totalHRPlayer);
                percentileRanks["RBI"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.RBI).ToList(), totalRBIPlayer);
                percentileRanks["SO"] = 100 - CalculatePercentile(allPlayerStats.Select(p => (decimal)p.SO).ToList(), totalSOPlayer);
                percentileRanks["BB"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.BB).ToList(), totalBBPlayer);
            }

            // 計算賽季平均值
            var seasonAverages = new Dictionary<string, decimal>();
            if (allPlayerStats.Any())
            {
                seasonAverages["AVG"] = allPlayerStats.Average(p => p.AVG);
                seasonAverages["OBP"] = allPlayerStats.Average(p => p.OBP);
                seasonAverages["SLG"] = allPlayerStats.Average(p => p.SLG);
                seasonAverages["OPS"] = allPlayerStats.Average(p => p.OPS);
                seasonAverages["HR"] = (decimal)allPlayerStats.Average(p => p.HR);
                seasonAverages["RBI"] = (decimal)allPlayerStats.Average(p => p.RBI);
                seasonAverages["SO"] = (decimal)allPlayerStats.Average(p => p.SO);
                seasonAverages["BB"] = (decimal)allPlayerStats.Average(p => p.BB);
            }

            // 計算球隊平均值和球隊PR值
            var teamAverages = new Dictionary<string, decimal>();
            var teamPercentileRanks = new Dictionary<string, decimal>();
            var playerTeam = batter.PlayerTeams
                .Where(pt => pt.IsActive)
                .OrderByDescending(pt => pt.StartDate)
                .FirstOrDefault();

            if (playerTeam != null)
            {
                var teamSeasonId = (seasonId ?? "ALL") == "ALL" ? playerTeam.SeasonId : seasonId;
                var teamStats = await _rankingCacheService.GetTeamSeasonBattingStatsAsync(teamSeasonId!, playerTeam.TeamId);
                if (teamStats != null)
                {
                    teamAverages["AVG"] = teamStats.AVG;
                    teamAverages["OBP"] = teamStats.OBP;
                    teamAverages["SLG"] = teamStats.SLG;
                    teamAverages["OPS"] = teamStats.OPS;
                    teamAverages["HR"] = teamStats.HR;
                    teamAverages["RBI"] = teamStats.RBI;
                    teamAverages["SO"] = teamStats.SO;
                    teamAverages["BB"] = teamStats.BB;

                    // 計算球隊在所有球隊中的PR值
                    var allTeamStats = await _rankingCacheService.GetAllTeamSeasonBattingStatsAsync(teamSeasonId!);
                    if (allTeamStats.Any())
                    {
                        teamPercentileRanks["AVG"] = CalculatePercentile(allTeamStats.Select(t => t.AVG).ToList(), teamStats.AVG);
                        teamPercentileRanks["OBP"] = CalculatePercentile(allTeamStats.Select(t => t.OBP).ToList(), teamStats.OBP);
                        teamPercentileRanks["SLG"] = CalculatePercentile(allTeamStats.Select(t => t.SLG).ToList(), teamStats.SLG);
                        teamPercentileRanks["OPS"] = CalculatePercentile(allTeamStats.Select(t => t.OPS).ToList(), teamStats.OPS);
                        teamPercentileRanks["HR"] = CalculatePercentile(allTeamStats.Select(t => t.HR).ToList(), teamStats.HR);
                        teamPercentileRanks["RBI"] = CalculatePercentile(allTeamStats.Select(t => t.RBI).ToList(), teamStats.RBI);
                        teamPercentileRanks["SO"] = 100 - CalculatePercentile(allTeamStats.Select(t => t.SO).ToList(), teamStats.SO);
                        teamPercentileRanks["BB"] = CalculatePercentile(allTeamStats.Select(t => t.BB).ToList(), teamStats.BB);
                    }
                }
            }

            return Ok(new
            {
                chartData,
                percentileRanks,
                seasonAverages,
                teamAverages,
                teamPercentileRanks
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"取得打者圖表數據失敗: playerId={playerId}, seasonId={seasonId}");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 取得投手圖表數據
    /// </summary>
    [HttpGet("pitcher/{playerId}/chart")]
    public async Task<IActionResult> GetPitcherChartData(
        string playerId, 
        [FromQuery] string? seasonId = null)
    {
        try
        {
            var pitcher = await _baseballDbService.GetPitcherAsync(playerId);
            if (pitcher == null)
            {
                return NotFound(new { error = "投手不存在" });
            }

            var pitcherBoxes = await _baseballDbService.GetPitcherBoxAsync(seasonId: seasonId);
            var seasons = await _baseballDbService.GetAllSeasonsAsync();
            var seasonsDict = seasons.ToDictionary(s => s.SeasonId, s => s.SeasonName ?? s.SeasonId);

            // 投球數據 (按比賽統計)
            var gameStats = pitcherBoxes
                .Where(pb => pb.PlayerId == playerId)
                .GroupBy(pb => new { pb.SeasonId, pb.GameSeq })
                .Select(g =>
                {
                    var first = g.FirstOrDefault();
                    return new
                    {
                        Date = first?.Game?.Date ?? DateTime.MinValue,
                        SeasonName = seasonsDict.TryGetValue(g.Key.SeasonId!, out var sn) ? sn : (g.Key.SeasonId ?? "Unknown"),
                        Seq = g.Key.GameSeq,
                        IPOuts = g.Sum(x => x.IPOuts ?? 0),
                        ER = g.Sum(x => x.ER ?? 0),
                        H = g.Sum(x => x.H ?? 0),
                        BB = g.Sum(x => x.BB ?? 0),
                        SO = g.Sum(x => x.SO ?? 0),
                        HR = g.Sum(x => x.HR ?? 0),
                        HBP = g.Sum(x => x.HB ?? 0),
                        BF = g.Sum(x => x.BF ?? 0)
                    };
                })
                .OrderBy(x => x.Date)
                .ToList();

            // 計算累計數據和率值
            var chartData = new List<object>();
            int totalIPOuts = 0, totalER = 0, totalH = 0, totalBB = 0, totalSO = 0, totalHBP = 0, totalBF = 0;

            foreach (var game in gameStats)
            {
                totalIPOuts += game.IPOuts;
                totalER += game.ER;
                totalH += game.H;
                totalBB += game.BB;
                totalSO += game.SO;
                totalHBP += game.HBP;
                totalBF += game.BF;

                var era = totalIPOuts > 0 ? Math.Round((decimal)totalER * 27 / totalIPOuts, 2) : 0;
                var whip = totalIPOuts > 0 ? Math.Round((decimal)(totalH + totalBB) * 3 / totalIPOuts, 2) : 0;
                var k9 = totalIPOuts > 0 ? Math.Round((decimal)totalSO * 27 / totalIPOuts, 2) : 0;
                var bb9 = totalIPOuts > 0 ? Math.Round((decimal)totalBB * 27 / totalIPOuts, 2) : 0;

                chartData.Add(new
                {
                    date = game.Date,
                    seq = game.Seq,
                    era,
                    whip,
                    k9,
                    bb9
                });
            }

            // 計算投手統計值
            var totalHRPlayer = gameStats.Sum(g => g.HR);
            var totalSOPlayer = gameStats.Sum(g => g.SO);
            var totalBBPlayer = gameStats.Sum(g => g.BB);
            var totalHPlayer = gameStats.Sum(g => g.H);
            var totalERPlayer = gameStats.Sum(g => g.ER);
            var totalIPOutsPlayer = gameStats.Sum(g => g.IPOuts);
            var totalHBPPlayer = gameStats.Sum(g => g.HBP);
            var totalBFPlayer = gameStats.Sum(g => g.BF);

            var eraPlayer = totalIPOutsPlayer > 0 ? (decimal)totalERPlayer * 27 / totalIPOutsPlayer : 0;
            var whipPlayer = totalIPOutsPlayer > 0 ? (decimal)(totalHPlayer + totalBBPlayer) * 3 / totalIPOutsPlayer : 0;
            var k9Player = totalIPOutsPlayer > 0 ? (decimal)totalSOPlayer * 27 / totalIPOutsPlayer : 0;
            var bb9Player = totalIPOutsPlayer > 0 ? (decimal)totalBBPlayer * 27 / totalIPOutsPlayer : 0;
            var kbbPlayer = totalBBPlayer > 0 ? (decimal)totalSOPlayer / totalBBPlayer : totalSOPlayer;
            var opponentAB = totalBFPlayer - totalBBPlayer - totalHBPPlayer;
            var baaPlayer = opponentAB > 0 ? (decimal)totalHPlayer / opponentAB : 0;

            // 計算百分位排名
            var allPitcherStats = await _rankingCacheService.GetPitchingStatsFromCacheAsync(seasonId ?? "ALL");
            var percentileRanks = new Dictionary<string, decimal>();
            
            if (allPitcherStats.Any())
            {
                // ERA, WHIP, BB9, BAA 越低越好，需要反轉
                percentileRanks["ERA"] = 100 - CalculatePercentile(allPitcherStats.Select(p => p.ERA).ToList(), eraPlayer);
                percentileRanks["WHIP"] = 100 - CalculatePercentile(allPitcherStats.Select(p => p.WHIP).ToList(), whipPlayer);
                percentileRanks["K9"] = CalculatePercentile(allPitcherStats.Select(p => p.K9).ToList(), k9Player);
                percentileRanks["BB9"] = 100 - CalculatePercentile(allPitcherStats.Select(p => p.BB9).ToList(), bb9Player);
                percentileRanks["KBBRatio"] = CalculatePercentile(allPitcherStats.Select(p => p.KBBRatio).ToList(), kbbPlayer);
                percentileRanks["BAA"] = 100 - CalculatePercentile(allPitcherStats.Select(p => p.BAA).ToList(), baaPlayer);
                percentileRanks["SO"] = CalculatePercentile(allPitcherStats.Select(p => (decimal)p.SO).ToList(), totalSOPlayer);
            }

            // 計算賽季平均值
            var seasonAverages = new Dictionary<string, decimal>();
            if (allPitcherStats.Any())
            {
                seasonAverages["ERA"] = allPitcherStats.Average(p => p.ERA);
                seasonAverages["WHIP"] = allPitcherStats.Average(p => p.WHIP);
                seasonAverages["K9"] = allPitcherStats.Average(p => p.K9);
                seasonAverages["BB9"] = allPitcherStats.Average(p => p.BB9);
                seasonAverages["KBB"] = allPitcherStats.Average(p => p.KBBRatio);
                seasonAverages["BAA"] = allPitcherStats.Average(p => p.BAA);
                seasonAverages["SO"] = (decimal)allPitcherStats.Average(p => p.SO);
            }

            // 計算球隊平均值和球隊PR值
            var teamAverages = new Dictionary<string, decimal>();
            var teamPercentileRanks = new Dictionary<string, decimal>();
            var pitcherData = await _baseballDbService.GetPitcherAsync(playerId);
            
            if (pitcherData != null)
            {
                var playerTeam = pitcherData.PlayerTeams
                    .Where(pt => pt.IsActive)
                    .OrderByDescending(pt => pt.StartDate)
                    .FirstOrDefault();

                if (playerTeam != null)
                {
                    var teamSeasonId = (seasonId ?? "ALL") == "ALL" ? playerTeam.SeasonId : seasonId;
                    var teamStats = await _rankingCacheService.GetTeamSeasonPitchingStatsAsync(teamSeasonId!, playerTeam.TeamId);
                    if (teamStats != null)
                    {
                        teamAverages["ERA"] = teamStats.ERA;
                        teamAverages["WHIP"] = teamStats.WHIP;
                        teamAverages["K9"] = teamStats.K9;
                        teamAverages["BB9"] = teamStats.BB9;
                        teamAverages["KBB"] = teamStats.KBBRatio;
                        teamAverages["BAA"] = teamStats.BAA;
                        teamAverages["SO"] = teamStats.SO;

                        // 計算球隊在所有球隊中的PR值
                        var allTeamStats = await _rankingCacheService.GetAllTeamSeasonPitchingStatsAsync(teamSeasonId!);
                        if (allTeamStats.Any())
                        {
                            teamPercentileRanks["ERA"] = 100 - CalculatePercentile(allTeamStats.Select(t => t.ERA).ToList(), teamStats.ERA);
                            teamPercentileRanks["WHIP"] = 100 - CalculatePercentile(allTeamStats.Select(t => t.WHIP).ToList(), teamStats.WHIP);
                            teamPercentileRanks["K9"] = CalculatePercentile(allTeamStats.Select(t => t.K9).ToList(), teamStats.K9);
                            teamPercentileRanks["BB9"] = 100 - CalculatePercentile(allTeamStats.Select(t => t.BB9).ToList(), teamStats.BB9);
                            teamPercentileRanks["KBB"] = CalculatePercentile(allTeamStats.Select(t => t.KBBRatio).ToList(), teamStats.KBBRatio);
                            teamPercentileRanks["BAA"] = 100 - CalculatePercentile(allTeamStats.Select(t => t.BAA).ToList(), teamStats.BAA);
                            teamPercentileRanks["SO"] = CalculatePercentile(allTeamStats.Select(t => t.SO).ToList(), teamStats.SO);
                        }
                    }
                }
            }

            return Ok(new
            {
                chartData,
                percentileRanks,
                seasonAverages,
                teamAverages,
                teamPercentileRanks
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"取得投手圖表數據失敗: playerId={playerId}, seasonId={seasonId}");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    private decimal CalculatePercentile(List<decimal> values, decimal targetValue)
    {
        if (!values.Any()) return 0;
        var count = values.Count(v => v < targetValue);
        return Math.Round((decimal)count / values.Count * 100, 1);
    }
}
