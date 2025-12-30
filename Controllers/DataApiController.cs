using System.Linq;
using BaseballApp.Models;
using BaseballApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace BaseballApp.Controllers;

[Route("api/data")]
[ApiController]
public class DataApiController : ControllerBase
{
    private readonly IBaseballDbService _baseballDbService;
    private readonly IRankingCacheService _rankingCacheService;
    private readonly ILogger<DataApiController> _logger;

    public DataApiController(
        IBaseballDbService baseballDbService,
        IRankingCacheService rankingCacheService,
        ILogger<DataApiController> logger)
    {
        _baseballDbService = baseballDbService;
        _rankingCacheService = rankingCacheService;
        _logger = logger;
    }

    /// <summary>
    /// 取得打者圖表數據
    /// </summary>
    /// <param name="playerId">球員識別碼</param>
    /// <param name="seasonId">賽季識別碼，預設為 null 表示所有賽季</param>
    /// <returns>圖表數據</returns>
    [HttpGet("batter/{playerId}/chart")]
    public async Task<IActionResult> GetBatterChartData(
        string playerId,
        [FromQuery] string seasonId = "ALL")
    {
        try
        {
            // 取得打者資料
            var batter = await _baseballDbService.GetBatterAsync(playerId);
            if (batter == null)
            {
                return NotFound(new { error = "球員不存在" });
            }

            // 取得打者打席資料
            var paList = await _baseballDbService.GetPAAsync(batterId: playerId, seasonId: seasonId);
            var seasons = await _baseballDbService.GetAllSeasonsAsync();
            var seasonsDict = seasons.ToDictionary(s => s.SeasonId, s => s.SeasonName ?? s.SeasonId);

            // 建立打者比賽統計
            var gameStats = BuildBatterGameStats(paList.ToList(), seasonsDict);
            if (gameStats.Count == 0)
            {
                return NotFound(new { error = "該球員在此賽季沒有打擊數據" });
            }

            // 建立圖表數據
            var chartData = BuildBatterChartData(gameStats);

            // 計算雷達圖數據
            var radarStats = CalculateBatterRadarStats(gameStats);

            // 計算百分位排名和聯盟中位數
            var allPlayerStats = await _rankingCacheService.GetBattingStatsFromCacheAsync(seasonId);
            var percentileRanks = CalculateBatterPercentiles(gameStats, allPlayerStats);
            var leagueMedianStats = CalculateLeagueMedianStats(allPlayerStats);

            // 計算球隊平均值和球隊PR值
            var playerTeam = batter.PlayerTeams
                .Where(pt => pt.IsActive)
                .OrderByDescending(pt => pt.StartDate)
                .FirstOrDefault();

            // 如果沒有球隊資料，顯示錯誤訊息
            if (playerTeam == null)
            {
                return NotFound(new { error = "球員沒有有效的球隊資料" });
            }

            // 計算球隊數據
            var teamStatsResult = await CalculateTeamStats(seasonId, playerTeam.TeamId, allPlayerStats);
            var teamAverages = teamStatsResult.Item1;
            var teamPercentileRanks = teamStatsResult.Item2;

            // 回傳結果
            return Ok(new
            {
                hasData = true,
                chartData,
                radarStats,
                percentileRanks,
                leagueMedianStats,
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
    /// 建立打者比賽統計
    /// </summary>
    /// <param name="paList">打席列表</param>
    /// <param name="seasonsDict">賽季字典</param>
    /// <returns>打者比賽統計列表</returns>
    private List<dynamic> BuildBatterGameStats(List<PA> paList, Dictionary<string, string> seasonsDict)
    {
        return paList
            .GroupBy(pa => new { pa.SeasonId, pa.GameSeq })
            .Select(g => new
            {
                date = g.FirstOrDefault()?.Game?.Date ?? DateTime.MinValue,
                seasonName = seasonsDict.TryGetValue(g.Key.SeasonId!, out var sn) ? sn : "Unknown",
                seq = g.Key.GameSeq,
                pa = g.Count(),
                ab = g.Count(pa => new[] { "1B", "2B", "3B", "HR", "IHR", "SO", "GO", "FO", "FC", "E", "GIDP", "DP", "TP" }.Contains(pa.Result)),
                h = g.Count(pa => new[] { "1B", "2B", "3B", "HR", "IHR" }.Contains(pa.Result)),
                _1b = g.Count(pa => pa.Result == "1B"),
                _2b = g.Count(pa => pa.Result == "2B"),
                _3b = g.Count(pa => pa.Result == "3B"),
                hr = g.Count(pa => pa.Result == "HR" || pa.Result == "IHR"),
                so = g.Count(pa => pa.Result == "SO"),
                bb = g.Count(pa => pa.Result == "uBB" || pa.Result == "IBB"),
                hbp = g.Count(pa => pa.Result == "HBP"),
                sf = g.Count(pa => pa.Result == "SF"),
                r = g.Count(pa => pa.Scored),
                rbi = g.Sum(pa => pa.RBI ?? 0)
            } as dynamic)
            .OrderBy(x => x.date)
            .ToList();
    }

    /// <summary>
    /// 建立打者折線圖數據 (累積AVG和OPS)
    /// </summary>
    /// <param name="gameStats">打者比賽統計列表</param>
    /// <returns>折線圖數據</returns>
    private List<object> BuildBatterChartData(List<dynamic> gameStats)
    {
        var chartData = new List<object>();
        int cumAB = 0, cumH = 0, cumBB = 0, cumHBP = 0, cumSF = 0, cum1B = 0, cum2B = 0, cum3B = 0, cumHR = 0;

        foreach (var game in gameStats)
        {
            cumAB += game.ab;
            cumH += game.h;
            cumBB += game.bb;
            cumHBP += game.hbp;
            cumSF += game.sf;
            cum1B += game._1b;
            cum2B += game._2b;
            cum3B += game._3b;
            cumHR += game.hr;

            // 累積數據
            var cumulativeAVG = cumAB > 0 ? Math.Round((decimal)cumH / cumAB, 3) : 0;
            var cumulativeOBPDen = cumAB + cumBB + cumHBP + cumSF;
            var cumulativeOBP = cumulativeOBPDen > 0 ? Math.Round((decimal)(cumH + cumBB + cumHBP) / cumulativeOBPDen, 3) : 0;
            var cumulativeTotalBases = cum1B + cum2B * 2 + cum3B * 3 + cumHR * 4;
            var cumulativeSLG = cumAB > 0 ? Math.Round((decimal)cumulativeTotalBases / cumAB, 3) : 0;
            var cumulativeOPS = cumulativeOBP + cumulativeSLG;

            // 單場數據
            var gameAB = (int)game.ab;
            var gameH = (int)game.h;
            var gameBB = (int)game.bb;
            var gameHBP = (int)game.hbp;
            var gameSF = (int)game.sf;
            var game1B = (int)game._1b;
            var game2B = (int)game._2b;
            var game3B = (int)game._3b;
            var gameHR = (int)game.hr;

            var gameAVG = gameAB > 0 ? Math.Round((decimal)gameH / gameAB, 3) : 0;
            var gameOBPDen = gameAB + gameBB + gameHBP + gameSF;
            var gameOBP = gameOBPDen > 0 ? Math.Round((decimal)(gameH + gameBB + gameHBP) / gameOBPDen, 3) : 0;
            var gameTotalBases = game1B + game2B * 2 + game3B * 3 + gameHR * 4;
            var gameSLG = gameAB > 0 ? Math.Round((decimal)gameTotalBases / gameAB, 3) : 0;
            var gameOPS = gameOBP + gameSLG;

            chartData.Add(new
            {
                date = game.date,
                seq = game.seq,
                avgData = cumulativeAVG,
                opsData = cumulativeOPS,
                gameAVG = gameAVG,
                gameOPS = gameOPS,
                ab = gameAB
            });
        }

        return chartData;
    }

    /// <summary>
    /// 計算打者雷達圖數據
    /// </summary>
    /// <param name="gameStats">打者比賽統計列表</param>
    /// <returns>雷達圖數據</returns>
    private dynamic CalculateBatterRadarStats(List<dynamic> gameStats)
    {
        int totalAB = gameStats.Sum(g => (int)g.ab);
        int totalH = gameStats.Sum(g => (int)g.h);
        int totalBB = gameStats.Sum(g => (int)g.bb);
        int totalHBP = gameStats.Sum(g => (int)g.hbp);
        int totalSF = gameStats.Sum(g => (int)g.sf);
        int totalHR = gameStats.Sum(g => (int)g.hr);
        int totalRBI = gameStats.Sum(g => (int)g.rbi);
        int totalSO = gameStats.Sum(g => (int)g.so);
        int totalPA = gameStats.Sum(g => (int)g.pa);
        int totalR = gameStats.Sum(g => (int)g.r);
        int total1B = gameStats.Sum(g => (int)g._1b);
        int total2B = gameStats.Sum(g => (int)g._2b);
        int total3B = gameStats.Sum(g => (int)g._3b);

        var avg = totalAB > 0 ? (decimal)totalH / totalAB : 0;
        var obpDen = totalAB + totalBB + totalHBP + totalSF;
        var obp = obpDen > 0 ? (decimal)(totalH + totalBB + totalHBP) / obpDen : 0;
        var totalBases = total1B + total2B * 2 + total3B * 3 + totalHR * 4;
        var slg = totalAB > 0 ? (decimal)totalBases / totalAB : 0;
        var ops = obp + slg;

        // 計算三振率、保送率和得分率（以打席數為分母）
        var kPct = totalPA > 0 ? Math.Round((decimal)totalSO / totalPA * 100, 1) : 0;
        var bbPct = totalPA > 0 ? Math.Round((decimal)totalBB / totalPA * 100, 1) : 0;
        var rPct = totalPA > 0 ? Math.Round((decimal)totalR / totalPA * 100, 1) : 0;

        return new
        {
            avg = avg,
            obp = obp,
            slg = slg,
            ops = ops,
            rbi = totalRBI,
            r = rPct,
            so = kPct,
            bb = bbPct
        };
    }

    /// <summary>
    /// 計算打者百分位排名
    /// </summary>
    /// <param name="gameStats">打者比賽統計列表</param>
    /// <param name="allPlayerStats">所有球員的打擊排名緩存列表</param>
    /// <returns>打者百分位排名字典</returns>
    private Dictionary<string, decimal> CalculateBatterPercentiles(List<dynamic> gameStats, List<BattingRankingCache> allPlayerStats)
    {
        var percentileRanks = new Dictionary<string, decimal>();

        if (allPlayerStats.Count == 0)
            return percentileRanks;

        int totalAB = gameStats.Sum(g => (int)g.ab);
        int totalH = gameStats.Sum(g => (int)g.h);
        int totalBB = gameStats.Sum(g => (int)g.bb);
        int totalHBP = gameStats.Sum(g => (int)g.hbp);
        int totalSF = gameStats.Sum(g => (int)g.sf);
        int totalHR = gameStats.Sum(g => (int)g.hr);
        int totalRBI = gameStats.Sum(g => (int)g.rbi);
        int totalSO = gameStats.Sum(g => (int)g.so);
        int totalPA = gameStats.Sum(g => (int)g.pa);
        int totalR = gameStats.Sum(g => (int)g.r);
        int total1B = gameStats.Sum(g => (int)g._1b);
        int total2B = gameStats.Sum(g => (int)g._2b);
        int total3B = gameStats.Sum(g => (int)g._3b);

        var avg = totalAB > 0 ? (decimal)totalH / totalAB : 0;
        var obpDen = totalAB + totalBB + totalHBP + totalSF;
        var obp = obpDen > 0 ? (decimal)(totalH + totalBB + totalHBP) / obpDen : 0;
        var totalBases = total1B + total2B * 2 + total3B * 3 + totalHR * 4;
        var slg = totalAB > 0 ? (decimal)totalBases / totalAB : 0;
        var ops = obp + slg;

        // 計算三振率、保送率和得分率（以打席數為分母）
        var kPct = totalPA > 0 ? (decimal)totalSO / totalPA : 0;
        var bbPct = totalPA > 0 ? (decimal)totalBB / totalPA : 0;
        var rPct = totalPA > 0 ? (decimal)totalR / totalPA : 0;

        percentileRanks["AVG"] = CalculatePercentile(allPlayerStats.Select(p => p.AVG).ToList(), avg);
        percentileRanks["OBP"] = CalculatePercentile(allPlayerStats.Select(p => p.OBP).ToList(), obp);
        percentileRanks["SLG"] = CalculatePercentile(allPlayerStats.Select(p => p.SLG).ToList(), slg);
        percentileRanks["OPS"] = CalculatePercentile(allPlayerStats.Select(p => p.OPS).ToList(), ops);
        percentileRanks["RBI"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.RBI).ToList(), totalRBI);
        percentileRanks["R"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.R / (p.PA > 0 ? p.PA : 1)).ToList(), rPct);
        percentileRanks["SO"] = 100 - CalculatePercentile(allPlayerStats.Select(p => (decimal)p.SO / (p.PA > 0 ? p.PA : 1)).ToList(), kPct);
        percentileRanks["BB"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.BB / (p.PA > 0 ? p.PA : 1)).ToList(), bbPct);

        return percentileRanks;
    }

    /// <summary>
    /// 計算聯盟中位數打者統計
    /// </summary>
    /// <param name="allPlayerStats">所有球員的打擊排名緩存列表</param>
    /// <returns>聯盟中位數打者統計字典</returns>
    private Dictionary<string, decimal> CalculateLeagueMedianStats(List<BattingRankingCache> allPlayerStats)
    {
        var leagueMedianStats = new Dictionary<string, decimal>();

        if (allPlayerStats.Count == 0)
            return leagueMedianStats;

        leagueMedianStats["AVG"] = CalculateMedian(allPlayerStats.Select(p => p.AVG).ToList());
        leagueMedianStats["OBP"] = CalculateMedian(allPlayerStats.Select(p => p.OBP).ToList());
        leagueMedianStats["SLG"] = CalculateMedian(allPlayerStats.Select(p => p.SLG).ToList());
        leagueMedianStats["OPS"] = CalculateMedian(allPlayerStats.Select(p => p.OPS).ToList());
        leagueMedianStats["RBI"] = CalculateMedian(allPlayerStats.Select(p => (decimal)p.RBI).ToList());
        leagueMedianStats["R"] = CalculateMedian(allPlayerStats.Select(p => p.PA > 0 ? (decimal)p.R / p.PA * 100 : 0).ToList());
        leagueMedianStats["SO"] = CalculateMedian(allPlayerStats.Select(p => p.PA > 0 ? (decimal)p.SO / p.PA * 100 : 0).ToList());
        leagueMedianStats["BB"] = CalculateMedian(allPlayerStats.Select(p => p.PA > 0 ? (decimal)p.BB / p.PA * 100 : 0).ToList());

        return leagueMedianStats;
    }

    /// <summary>
    /// 計算球隊統計和球隊百分位排名
    /// </summary>
    /// <param name="seasonId">賽季識別碼</param>
    /// <param name="playerTeam">球隊資訊</param>
    /// <param name="allPlayerStats">所有球員的打擊排名緩存列表</param>
    /// <returns>球隊統計和球隊百分位排名字典</returns>
    private async Task<(Dictionary<string, decimal>, Dictionary<string, decimal>)> CalculateTeamStats(
        string seasonId, string teamId,
        List<BattingRankingCache> allPlayerStats)
    {
        var teamAverages = new Dictionary<string, decimal>();
        var teamPercentileRanks = new Dictionary<string, decimal>();

        if (allPlayerStats.Count == 0)
            return (teamAverages, teamPercentileRanks);

        var teamStats = await _rankingCacheService.GetTeamSeasonBattingStatsAsync(seasonId, teamId);
        if (teamStats == null)
            return (teamAverages, teamPercentileRanks);

        // 計算球隊的三振率、保送率和得分率
        var teamKPct = teamStats.PA > 0 ? (decimal)teamStats.SO / teamStats.PA * 100 : 0;
        var teamBBPct = teamStats.PA > 0 ? (decimal)teamStats.BB / teamStats.PA * 100 : 0;
        var teamRPct = teamStats.PA > 0 ? (decimal)teamStats.R / teamStats.PA * 100 : 0;

        teamAverages["AVG"] = (decimal)teamStats.AVG;
        teamAverages["OBP"] = (decimal)teamStats.OBP;
        teamAverages["SLG"] = (decimal)teamStats.SLG;
        teamAverages["OPS"] = (decimal)teamStats.OPS;
        teamAverages["RBI"] = teamStats.RBI;
        teamAverages["R"] = teamRPct;
        teamAverages["SO"] = teamKPct;
        teamAverages["BB"] = teamBBPct;

        teamPercentileRanks["AVG"] = CalculatePercentile(allPlayerStats.Select(p => p.AVG).ToList(), (decimal)teamStats.AVG);
        teamPercentileRanks["OBP"] = CalculatePercentile(allPlayerStats.Select(p => p.OBP).ToList(), (decimal)teamStats.OBP);
        teamPercentileRanks["SLG"] = CalculatePercentile(allPlayerStats.Select(p => p.SLG).ToList(), (decimal)teamStats.SLG);
        teamPercentileRanks["OPS"] = CalculatePercentile(allPlayerStats.Select(p => p.OPS).ToList(), (decimal)teamStats.OPS);
        teamPercentileRanks["RBI"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.RBI).ToList(), teamStats.RBI);
        teamPercentileRanks["R"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.R / (p.PA > 0 ? p.PA : 1)).ToList(), teamRPct / 100);
        teamPercentileRanks["SO"] = 100 - CalculatePercentile(allPlayerStats.Select(p => (decimal)p.SO / (p.PA > 0 ? p.PA : 1)).ToList(), teamKPct / 100);
        teamPercentileRanks["BB"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.BB / (p.PA > 0 ? p.PA : 1)).ToList(), teamBBPct / 100);

        return (teamAverages, teamPercentileRanks);
    }

    /// <summary>
    /// 創建空白圖表回應
    /// </summary>
    /// <returns>空白圖表回應物件</returns>
    private object CreateEmptyChartResponse()
    {
        return new
        {
            hasData = false,
            message = "該球員在此賽季沒有打擊數據",
            chartData = new List<object>(),
            radarStats = new { },
            percentileRanks = new Dictionary<string, decimal>(),
            leagueMedianStats = new Dictionary<string, decimal>(),
            teamAverages = new Dictionary<string, decimal>(),
            teamPercentileRanks = new Dictionary<string, decimal>()
        };
    }

    /// <summary>
    /// 取得投手圖表數據
    /// </summary>
    /// <param name="playerId">球員識別碼</param>
    /// <param name="seasonId">賽季識別碼，預設為 "ALL" 表示所有賽季</param>
    /// <returns>圖表數據</returns>
    [HttpGet("pitcher/{playerId}/chart")]
    public async Task<IActionResult> GetPitcherChartData(
        string playerId,
        [FromQuery] string seasonId = "ALL")
    {
        try
        {
            // 取得投手資料
            var pitcher = await _baseballDbService.GetPitcherAsync(playerId);
            if (pitcher == null)
            {
                return NotFound(new { error = "投手不存在" });
            }

            // 取得投手投球盒資料
            var pitcherBoxes = await _baseballDbService.GetPitcherBoxAsync(seasonId: seasonId);
            var seasons = await _baseballDbService.GetAllSeasonsAsync();
            var seasonsDict = seasons.ToDictionary(s => s.SeasonId, s => s.SeasonName ?? s.SeasonId);

            // 建立投手比賽統計
            var gameStatsData = BuildPitcherGameStats(pitcherBoxes.ToList(), playerId);
            if (gameStatsData.Count == 0)
            {
                return NotFound(new { error = "該投手在此賽季沒有投球數據" });
            }

            // 建立圖表數據
            var chartData = BuildPitcherChartData(gameStatsData);

            // 計算雷達圖數據
            var radarStats = CalculatePitcherRadarStats(gameStatsData);

            // 計算百分位排名和聯盟中位數
            var allPitcherStats = await _rankingCacheService.GetPitchingStatsFromCacheAsync(seasonId);
            var percentileRanks = CalculatePitcherPercentiles(gameStatsData, radarStats, allPitcherStats);
            var leagueMedianStats = CalculatePitcherLeagueMedianStats(allPitcherStats);

            // 計算球隊平均值和球隊PR值
            var playerTeam = pitcher.PlayerTeams
                .Where(pt => pt.IsActive)
                .OrderByDescending(pt => pt.StartDate)
                .FirstOrDefault();

            if (playerTeam == null)
            {
                return NotFound(new { error = "投手沒有有效的球隊資料" });
            }

            // 計算球隊數據
            var teamStatsResult = await CalculatePitcherTeamStats(seasonId, playerTeam.TeamId, allPitcherStats);
            var teamAverages = teamStatsResult.Item1;
            var teamPercentileRanks = teamStatsResult.Item2;

            // 回傳結果
            return Ok(new
            {
                hasData = true,
                chartData,
                radarStats,
                percentileRanks,
                leagueMedianStats,
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

    /// <summary>
    /// 建立投手比賽統計
    /// </summary>
    /// <param name="pitcherBoxes">投手盒列表</param>
    /// <param name="playerId">球員識別碼</param>
    /// <returns>投手比賽統計列表</returns>
    private List<dynamic> BuildPitcherGameStats(List<PitcherBox> pitcherBoxes, string playerId)
    {
        return pitcherBoxes
            .Where(pb => pb.PlayerId == playerId)
            .GroupBy(pb => new { pb.SeasonId, pb.GameSeq })
            .Select(g =>
            {
                var first = g.FirstOrDefault();
                return new
                {
                    date = first?.Game?.Date ?? DateTime.MinValue,
                    ipOuts = g.Sum(x => x.IPOuts ?? 0),
                    er = g.Sum(x => x.ER ?? 0),
                    h = g.Sum(x => x.H ?? 0),
                    bb = g.Sum(x => x.BB ?? 0),
                    so = g.Sum(x => x.SO ?? 0),
                    hr = g.Sum(x => x.HR ?? 0),
                    hbp = g.Sum(x => x.HB ?? 0),
                    bf = g.Sum(x => x.BF ?? 0)
                } as dynamic;
            })
            .OrderBy(x => x.date)
            .ToList();
    }

    /// <summary>
    /// 建立投手折線圖數據 (累積ERA和WHIP)
    /// </summary>
    /// <param name="gameStatsData">投手比賽統計列表</param>
    /// <returns>折線圖數據</returns>
    private List<object> BuildPitcherChartData(List<dynamic> gameStatsData)
    {
        var chartData = new List<object>();
        int cumIPOuts = 0, cumER = 0, cumH = 0, cumBB = 0;

        foreach (var game in gameStatsData)
        {
            cumIPOuts += game.ipOuts;
            cumER += game.er;
            cumH += game.h;
            cumBB += game.bb;

            // 累積數據
            var cumulativeERA = cumIPOuts > 0 ? Math.Round((decimal)cumER * 27 / cumIPOuts, 2) : 0;
            var cumulativeWHIP = cumIPOuts > 0 ? Math.Round((decimal)(cumH + cumBB) * 3 / cumIPOuts, 2) : 0;

            // 單場數據
            var gameIPOuts = (int)game.ipOuts;
            var gameER = (int)game.er;
            var gameH = (int)game.h;
            var gameBB = (int)game.bb;

            var gameERA = gameIPOuts > 0 ? Math.Round((decimal)gameER * 27 / gameIPOuts, 2) : 0;
            var gameWHIP = gameIPOuts > 0 ? Math.Round((decimal)(gameH + gameBB) * 3 / gameIPOuts, 2) : 0;

            chartData.Add(new
            {
                date = game.date,
                era = cumulativeERA,
                whip = cumulativeWHIP,
                gameERA = gameERA,
                gameWHIP = gameWHIP,
                ipOuts = gameIPOuts
            });
        }

        return chartData;
    }

    /// <summary>
    /// 計算投手雷達圖數據
    /// </summary>
    /// <param name="gameStatsData">投手比賽統計列表</param>
    /// <returns>雷達圖數據</returns>
    private dynamic CalculatePitcherRadarStats(List<dynamic> gameStatsData)
    {
        var totalHRPlayer = gameStatsData.Sum(g => (int)g.hr);
        var totalSOPlayer = gameStatsData.Sum(g => (int)g.so);
        var totalBBPlayer = gameStatsData.Sum(g => (int)g.bb);
        var totalHPlayer = gameStatsData.Sum(g => (int)g.h);
        var totalERPlayer = gameStatsData.Sum(g => (int)g.er);
        var totalIPOutsPlayer = gameStatsData.Sum(g => (int)g.ipOuts);
        var totalHBPPlayer = gameStatsData.Sum(g => (int)g.hbp);
        var totalBFPlayer = gameStatsData.Sum(g => (int)g.bf);

        var eraPlayer = totalIPOutsPlayer > 0 ? (decimal)totalERPlayer * 27 / totalIPOutsPlayer : 0;
        var whipPlayer = totalIPOutsPlayer > 0 ? (decimal)(totalHPlayer + totalBBPlayer) * 3 / totalIPOutsPlayer : 0;
        var k9Player = totalIPOutsPlayer > 0 ? (decimal)totalSOPlayer * 27 / totalIPOutsPlayer : 0;
        var bb9Player = totalIPOutsPlayer > 0 ? (decimal)totalBBPlayer * 27 / totalIPOutsPlayer : 0;
        var kbbPlayer = totalBBPlayer > 0 ? (decimal)totalSOPlayer / totalBBPlayer : totalSOPlayer;
        var opponentAB = totalBFPlayer - totalBBPlayer - totalHBPPlayer;
        var baaPlayer = opponentAB > 0 ? (decimal)totalHPlayer / opponentAB : 0;

        return new
        {
            era = eraPlayer,
            whip = whipPlayer,
            k9 = k9Player,
            bb9 = bb9Player,
            kbb = kbbPlayer,
            baa = baaPlayer,
            so = totalSOPlayer,
            bb = totalBBPlayer
        };
    }

    /// <summary>
    /// 計算投手百分位排名
    /// </summary>
    /// <param name="gameStatsData">投手比賽統計列表</param>
    /// <param name="radarStats">雷達圖統計數據</param>
    /// <param name="allPitcherStats">所有投手的投球排名緩存列表</param>
    /// <returns>投手百分位排名字典</returns>
    private Dictionary<string, decimal> CalculatePitcherPercentiles(List<dynamic> gameStatsData, dynamic radarStats, List<PitchingRankingCache> allPitcherStats)
    {
        var percentileRanks = new Dictionary<string, decimal>();

        if (allPitcherStats.Count == 0)
            return percentileRanks;

        var eraPlayer = (decimal)radarStats.era;
        var whipPlayer = (decimal)radarStats.whip;
        var k9Player = (decimal)radarStats.k9;
        var bb9Player = (decimal)radarStats.bb9;
        var kbbPlayer = (decimal)radarStats.kbb;
        var baaPlayer = (decimal)radarStats.baa;
        var totalSOPlayer = (int)radarStats.so;

        // ERA, WHIP, BB9, BAA 越低越好，需要反轉
        percentileRanks["ERA"] = 100 - CalculatePercentile(allPitcherStats.Select(p => p.ERA).ToList(), eraPlayer);
        percentileRanks["WHIP"] = 100 - CalculatePercentile(allPitcherStats.Select(p => p.WHIP).ToList(), whipPlayer);
        percentileRanks["K9"] = CalculatePercentile(allPitcherStats.Select(p => p.K9).ToList(), k9Player);
        percentileRanks["BB9"] = 100 - CalculatePercentile(allPitcherStats.Select(p => p.BB9).ToList(), bb9Player);
        percentileRanks["KBBRatio"] = CalculatePercentile(allPitcherStats.Select(p => p.KBBRatio).ToList(), kbbPlayer);
        percentileRanks["BAA"] = 100 - CalculatePercentile(allPitcherStats.Select(p => p.BAA).ToList(), baaPlayer);
        percentileRanks["SO"] = CalculatePercentile(allPitcherStats.Select(p => (decimal)p.SO).ToList(), totalSOPlayer);

        return percentileRanks;
    }

    /// <summary>
    /// 計算聯盟中位數投手統計
    /// </summary>
    /// <param name="allPitcherStats">所有投手的投球排名緩存列表</param>
    /// <returns>聯盟中位數投手統計字典</returns>
    private Dictionary<string, decimal> CalculatePitcherLeagueMedianStats(List<PitchingRankingCache> allPitcherStats)
    {
        var leagueMedianStats = new Dictionary<string, decimal>();

        if (allPitcherStats.Count == 0)
            return leagueMedianStats;

        leagueMedianStats["ERA"] = CalculateMedian(allPitcherStats.Select(p => p.ERA).ToList());
        leagueMedianStats["WHIP"] = CalculateMedian(allPitcherStats.Select(p => p.WHIP).ToList());
        leagueMedianStats["K9"] = CalculateMedian(allPitcherStats.Select(p => p.K9).ToList());
        leagueMedianStats["BB9"] = CalculateMedian(allPitcherStats.Select(p => p.BB9).ToList());
        leagueMedianStats["KBB"] = CalculateMedian(allPitcherStats.Select(p => p.KBBRatio).ToList());
        leagueMedianStats["BAA"] = CalculateMedian(allPitcherStats.Select(p => p.BAA).ToList());
        leagueMedianStats["SO"] = CalculateMedian(allPitcherStats.Select(p => (decimal)p.SO).ToList());

        return leagueMedianStats;
    }

    /// <summary>
    /// 計算球隊投手統計和球隊百分位排名
    /// </summary>
    /// <param name="seasonId">賽季識別碼</param>
    /// <param name="playerTeam">球隊資訊</param>
    /// <param name="allPitcherStats">所有投手的投球排名緩存列表</param>
    /// <returns>球隊統計和球隊百分位排名字典</returns>
    private async Task<(Dictionary<string, decimal>, Dictionary<string, decimal>)> CalculatePitcherTeamStats(
        string seasonId,
        string teamId,
        List<PitchingRankingCache> allPitcherStats)
    {
        var teamAverages = new Dictionary<string, decimal>();
        var teamPercentileRanks = new Dictionary<string, decimal>();

        if (allPitcherStats.Count == 0)
            return (teamAverages, teamPercentileRanks);

        var teamStats = await _rankingCacheService.GetTeamSeasonPitchingStatsAsync(seasonId, teamId);
        if (teamStats == null)
            return (teamAverages, teamPercentileRanks);

        // 計算球隊平均值
        teamAverages["ERA"] = teamStats.ERA;
        teamAverages["WHIP"] = teamStats.WHIP;
        teamAverages["K9"] = teamStats.K9;
        teamAverages["BB9"] = teamStats.BB9;
        teamAverages["KBB"] = teamStats.KBBRatio;
        teamAverages["BAA"] = teamStats.BAA;
        teamAverages["SO"] = teamStats.SO;

        // 計算球隊平均在所有球員中的PR值
        teamPercentileRanks["ERA"] = 100 - CalculatePercentile(allPitcherStats.Select(p => p.ERA).ToList(), teamStats.ERA);
        teamPercentileRanks["WHIP"] = 100 - CalculatePercentile(allPitcherStats.Select(p => p.WHIP).ToList(), teamStats.WHIP);
        teamPercentileRanks["K9"] = CalculatePercentile(allPitcherStats.Select(p => p.K9).ToList(), teamStats.K9);
        teamPercentileRanks["BB9"] = 100 - CalculatePercentile(allPitcherStats.Select(p => p.BB9).ToList(), teamStats.BB9);
        teamPercentileRanks["KBB"] = CalculatePercentile(allPitcherStats.Select(p => p.KBBRatio).ToList(), teamStats.KBBRatio);
        teamPercentileRanks["BAA"] = 100 - CalculatePercentile(allPitcherStats.Select(p => p.BAA).ToList(), teamStats.BAA);
        teamPercentileRanks["SO"] = CalculatePercentile(allPitcherStats.Select(p => (decimal)p.SO).ToList(), teamStats.SO);

        return (teamAverages, teamPercentileRanks);
    }

    /// <summary>
    /// 創建空白投手圖表回應
    /// </summary>
    /// <returns>空白圖表回應物件</returns>
    private object CreateEmptyPitcherChartResponse()
    {
        return new
        {
            hasData = false,
            message = "該球員在此賽季沒有投球數據",
            chartData = new List<object>(),
            radarStats = new { },
            percentileRanks = new Dictionary<string, decimal>(),
            leagueMedianStats = new Dictionary<string, decimal>(),
            teamAverages = new Dictionary<string, decimal>(),
            teamPercentileRanks = new Dictionary<string, decimal>()
        };
    }

    /// <summary>
    /// 計算百分位排名
    /// </summary>
    /// <param name="values">數值列表</param>
    /// <param name="targetValue">目標值</param>
    /// <returns>百分位排名</returns>
    private decimal CalculatePercentile(List<decimal> values, decimal targetValue)
    {
        if (values.Count == 0) return 0;
        var count = values.Count(v => v < targetValue);
        return Math.Round((decimal)count / values.Count * 100, 1);
    }

    /// <summary>
    /// 計算中位數
    /// </summary>
    /// <param name="values">數值列表</param>
    /// <returns>中位數</returns>
    private decimal CalculateMedian(List<decimal> values)
    {
        if (values.Count == 0) return 0;

        var sorted = values.OrderBy(v => v).ToList();
        int count = sorted.Count;

        if (count % 2 == 1)
        {
            // 奇數個：取中間值
            return sorted[count / 2];
        }
        else
        {
            // 偶數個：取中間兩個的平均
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
        }
    }

    [HttpGet("winRate/chart")]
    public async Task<IActionResult> GetWinRateChartData([FromQuery] string seasonId = "ALL")
    {
        try
        {
            // 取得比賽資料
            var games = await _baseballDbService.GetGamesAsync(seasonId: seasonId);

            string[] dates = games
                .Select(g => g.Date.ToString("yyyy-MM-dd"))
                .Distinct()
                .OrderBy(d => d)
                .ToArray();

            var teams = await _baseballDbService.GetAllTeamsAsync();
            var teamsList = new List<Team>();
            teamsList.AddRange(teams.Select(t => new Team
            {
                name = t.TeamName,
                gameCount = 0,
                wins = new List<int>(),
                losses = new List<int>(),
                winRates = new List<decimal>()
            }));

            foreach (var g in games)
            {
                var homeScore = g.HomeScores.Sum(s => s.Score);
                var awayScore = g.AwayScores.Sum(s => s.Score);

                // 更新主隊數據
                var HomeTeam = g.HomeTeam;
                var homeTeam = teamsList.FirstOrDefault(t => t.name == HomeTeam.TeamName);
                if (homeTeam != null)
                {
                    homeTeam.gameCount += 1;
                    if (homeScore > awayScore)
                    {
                        homeTeam.wins.Add(1);
                        homeTeam.losses.Add(0);
                    }
                    else if (awayScore > homeScore)
                    {
                        homeTeam.wins.Add(0);
                        homeTeam.losses.Add(1);
                    }
                    else
                    {
                        // 平局
                        homeTeam.wins.Add(0);
                        homeTeam.losses.Add(0);
                    }
                    var totalGames = homeTeam.wins.Sum() + homeTeam.losses.Sum();
                    var winRate = totalGames > 0 ? Math.Round((decimal)homeTeam.wins.Sum() / totalGames * 100, 2) : 0;
                    homeTeam.winRates.Add(winRate);
                }

                // 更新客隊數據
                var AwayTeam = g.AwayTeam;
                var awayTeam = teamsList.FirstOrDefault(t => t.name == AwayTeam.TeamName);
                if (awayTeam != null)
                {
                    awayTeam.gameCount += 1;
                    if (awayScore > homeScore)
                    {
                        awayTeam.wins.Add(1);
                        awayTeam.losses.Add(0);
                    }
                    else if (homeScore > awayScore)
                    {
                        awayTeam.wins.Add(0);
                        awayTeam.losses.Add(1);
                    }
                    else
                    {
                        // 平局
                        awayTeam.wins.Add(0);
                        awayTeam.losses.Add(0);
                    }
                    var totalGames = awayTeam.wins.Sum() + awayTeam.losses.Sum();
                    var winRate = totalGames > 0 ? Math.Round((decimal)awayTeam.wins.Sum() / totalGames * 100, 2) : 0;
                    awayTeam.winRates.Add(winRate);
                }

                // 處理沒有比賽的球隊
                foreach (var team in teamsList)
                {
                    if (team.name != HomeTeam.TeamName && team.name != AwayTeam.TeamName)
                    {
                        team.wins.Add(team.wins.Count > 0 ? team.wins.Last() : 0);
                        team.losses.Add(team.losses.Count > 0 ? team.losses.Last() : 0);
                        team.winRates.Add(team.winRates.Count > 0 ? team.winRates.Last() : 0);
                    }
                }
            }

            // 回傳結果
            return Ok(new
            {
                hasData = true,
                chartData = new TeamsWinRate
                {
                    dates = dates,
                    teams = teamsList.ToArray()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"取得勝率圖表數據失敗: seasonId={seasonId}");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    private class TeamsWinRate
    {
        public required string[] dates { get; set; }
        public required Team[] teams { get; set; }

    }
    private class Team
    {
        public required string name { get; set; }
        public int gameCount { get; set; } = 0;
        public required List<int> wins { get; set; } = new List<int>();
        public required List<int> losses { get; set; } = new List<int>();
        public required List<decimal> winRates { get; set; } = new List<decimal>();
    }
}
