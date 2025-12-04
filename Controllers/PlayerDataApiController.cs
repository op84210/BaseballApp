using BaseballApp.Models;
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
            if (!gameStats.Any())
            {
                return Ok(CreateEmptyChartResponse());
            }

            // 建立圖表數據
            var chartData = BuildBatterChartData(gameStats);
            
            // 計算雷達圖數據
            var radarStats = CalculateBatterRadarStats(gameStats);
            
            // 計算百分位排名和賽季平均值
            var allPlayerStats = await _rankingCacheService.GetBattingStatsFromCacheAsync(seasonId);
            var percentileRanks = CalculateBatterPercentiles(gameStats, allPlayerStats);
            var seasonAverages = CalculateSeasonAverages(allPlayerStats);

            // 計算球隊平均值和球隊PR值
            var playerTeam = batter.PlayerTeams
                .Where(pt => pt.IsActive)
                .OrderByDescending(pt => pt.StartDate)
                .FirstOrDefault();
            
            // 計算球隊數據
            var teamStatsResult = await CalculateTeamStats(seasonId, playerTeam, allPlayerStats);
            var teamAverages = teamStatsResult.Item1;
            var teamPercentileRanks = teamStatsResult.Item2;

            // 回傳結果
            return Ok(new
            {
                hasData = true,
                chartData,
                radarStats,
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

            var avg = cumAB > 0 ? Math.Round((decimal)cumH / cumAB, 3) : 0;
            var obpDen = cumAB + cumBB + cumHBP + cumSF;
            var obp = obpDen > 0 ? Math.Round((decimal)(cumH + cumBB + cumHBP) / obpDen, 3) : 0;
            var totalBases = cum1B + cum2B * 2 + cum3B * 3 + cumHR * 4;
            var slg = cumAB > 0 ? Math.Round((decimal)totalBases / cumAB, 3) : 0;
            var ops = obp + slg;

            chartData.Add(new
            {
                date = game.date,
                seq = game.seq,
                avgData = avg,
                opsData = ops
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
        int total1B = gameStats.Sum(g => (int)g._1b);
        int total2B = gameStats.Sum(g => (int)g._2b);
        int total3B = gameStats.Sum(g => (int)g._3b);

        var avg = totalAB > 0 ? (decimal)totalH / totalAB : 0;
        var obpDen = totalAB + totalBB + totalHBP + totalSF;
        var obp = obpDen > 0 ? (decimal)(totalH + totalBB + totalHBP) / obpDen : 0;
        var totalBases = total1B + total2B * 2 + total3B * 3 + totalHR * 4;
        var slg = totalAB > 0 ? (decimal)totalBases / totalAB : 0;
        var ops = obp + slg;

        return new
        {
            avg = avg,
            obp = obp,
            slg = slg,
            ops = ops,
            hr = totalHR,
            rbi = totalRBI,
            so = totalSO,
            bb = totalBB
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
        
        if (!allPlayerStats.Any())
            return percentileRanks;

        int totalAB = gameStats.Sum(g => (int)g.ab);
        int totalH = gameStats.Sum(g => (int)g.h);
        int totalBB = gameStats.Sum(g => (int)g.bb);
        int totalHBP = gameStats.Sum(g => (int)g.hbp);
        int totalSF = gameStats.Sum(g => (int)g.sf);
        int totalHR = gameStats.Sum(g => (int)g.hr);
        int totalRBI = gameStats.Sum(g => (int)g.rbi);
        int totalSO = gameStats.Sum(g => (int)g.so);
        int total1B = gameStats.Sum(g => (int)g._1b);
        int total2B = gameStats.Sum(g => (int)g._2b);
        int total3B = gameStats.Sum(g => (int)g._3b);

        var avg = totalAB > 0 ? (decimal)totalH / totalAB : 0;
        var obpDen = totalAB + totalBB + totalHBP + totalSF;
        var obp = obpDen > 0 ? (decimal)(totalH + totalBB + totalHBP) / obpDen : 0;
        var totalBases = total1B + total2B * 2 + total3B * 3 + totalHR * 4;
        var slg = totalAB > 0 ? (decimal)totalBases / totalAB : 0;
        var ops = obp + slg;

        percentileRanks["AVG"] = CalculatePercentile(allPlayerStats.Select(p => p.AVG).ToList(), avg);
        percentileRanks["OBP"] = CalculatePercentile(allPlayerStats.Select(p => p.OBP).ToList(), obp);
        percentileRanks["SLG"] = CalculatePercentile(allPlayerStats.Select(p => p.SLG).ToList(), slg);
        percentileRanks["OPS"] = CalculatePercentile(allPlayerStats.Select(p => p.OPS).ToList(), ops);
        percentileRanks["HR"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.HR).ToList(), totalHR);
        percentileRanks["RBI"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.RBI).ToList(), totalRBI);
        percentileRanks["SO"] = 100 - CalculatePercentile(allPlayerStats.Select(p => (decimal)p.SO).ToList(), totalSO);
        percentileRanks["BB"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.BB).ToList(), totalBB);

        return percentileRanks;
    }

    /// <summary>
    /// 計算賽季打者平均值
    /// </summary>
    /// <param name="allPlayerStats">所有球員的打擊排名緩存列表</param>
    /// <returns>賽季打者平均值字典</returns>
    private Dictionary<string, decimal> CalculateSeasonAverages(List<BattingRankingCache> allPlayerStats)
    {
        var seasonAverages = new Dictionary<string, decimal>();
        
        if (!allPlayerStats.Any())
            return seasonAverages;

        seasonAverages["AVG"] = allPlayerStats.Average(p => p.AVG);
        seasonAverages["OBP"] = allPlayerStats.Average(p => p.OBP);
        seasonAverages["SLG"] = allPlayerStats.Average(p => p.SLG);
        seasonAverages["OPS"] = allPlayerStats.Average(p => p.OPS);
        seasonAverages["HR"] = (decimal)allPlayerStats.Average(p => p.HR);
        seasonAverages["RBI"] = (decimal)allPlayerStats.Average(p => p.RBI);
        seasonAverages["SO"] = (decimal)allPlayerStats.Average(p => p.SO);
        seasonAverages["BB"] = (decimal)allPlayerStats.Average(p => p.BB);

        return seasonAverages;
    }

    /// <summary>
    /// 計算球隊統計和球隊百分位排名
    /// </summary>
    /// <param name="seasonId">賽季識別碼</param>
    /// <param name="playerTeam">球隊資訊</param>
    /// <param name="allPlayerStats">所有球員的打擊排名緩存列表</param>
    /// <returns>球隊統計和球隊百分位排名字典</returns>
    private async Task<(Dictionary<string, decimal>, Dictionary<string, decimal>)> CalculateTeamStats(
        string seasonId, 
        PlayerTeam? playerTeam, 
        List<BattingRankingCache> allPlayerStats)
    {
        var teamAverages = new Dictionary<string, decimal>();
        var teamPercentileRanks = new Dictionary<string, decimal>();

        if (playerTeam == null || !allPlayerStats.Any())
            return (teamAverages, teamPercentileRanks);

        var teamStats = await _rankingCacheService.GetTeamSeasonBattingStatsAsync(seasonId, playerTeam.TeamId);
        if (teamStats == null)
            return (teamAverages, teamPercentileRanks);

        teamAverages["AVG"] = (decimal)teamStats.AVG;
        teamAverages["OBP"] = (decimal)teamStats.OBP;
        teamAverages["SLG"] = (decimal)teamStats.SLG;
        teamAverages["OPS"] = (decimal)teamStats.OPS;
        teamAverages["HR"] = teamStats.HR;
        teamAverages["RBI"] = teamStats.RBI;
        teamAverages["SO"] = teamStats.SO;
        teamAverages["BB"] = teamStats.BB;

        teamPercentileRanks["AVG"] = CalculatePercentile(allPlayerStats.Select(p => p.AVG).ToList(), (decimal)teamStats.AVG);
        teamPercentileRanks["OBP"] = CalculatePercentile(allPlayerStats.Select(p => p.OBP).ToList(), (decimal)teamStats.OBP);
        teamPercentileRanks["SLG"] = CalculatePercentile(allPlayerStats.Select(p => p.SLG).ToList(), (decimal)teamStats.SLG);
        teamPercentileRanks["OPS"] = CalculatePercentile(allPlayerStats.Select(p => p.OPS).ToList(), (decimal)teamStats.OPS);
        teamPercentileRanks["HR"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.HR).ToList(), teamStats.HR);
        teamPercentileRanks["RBI"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.RBI).ToList(), teamStats.RBI);
        teamPercentileRanks["SO"] = 100 - CalculatePercentile(allPlayerStats.Select(p => (decimal)p.SO).ToList(), teamStats.SO);
        teamPercentileRanks["BB"] = CalculatePercentile(allPlayerStats.Select(p => (decimal)p.BB).ToList(), teamStats.BB);

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
            seasonAverages = new Dictionary<string, decimal>(),
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
            if (!gameStatsData.Any())
            {
                return Ok(CreateEmptyPitcherChartResponse());
            }

            // 建立圖表數據
            var chartData = BuildPitcherChartData(gameStatsData);
            
            // 計算雷達圖數據
            var radarStats = CalculatePitcherRadarStats(gameStatsData);
            
            // 計算百分位排名和賽季平均值
            var allPitcherStats = await _rankingCacheService.GetPitchingStatsFromCacheAsync(seasonId);
            var percentileRanks = CalculatePitcherPercentiles(gameStatsData, radarStats, allPitcherStats);
            var seasonAverages = CalculatePitcherSeasonAverages(allPitcherStats);

            // 計算球隊平均值和球隊PR值
            var playerTeam = pitcher.PlayerTeams
                .Where(pt => pt.IsActive)
                .OrderByDescending(pt => pt.StartDate)
                .FirstOrDefault();
            
            // 計算球隊數據
            var teamStatsResult = await CalculatePitcherTeamStats(seasonId, playerTeam, allPitcherStats, radarStats);
            var teamAverages = teamStatsResult.Item1;
            var teamPercentileRanks = teamStatsResult.Item2;

            // 回傳結果
            return Ok(new
            {
                hasData = true,
                chartData,
                radarStats,
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

            var era = cumIPOuts > 0 ? Math.Round((decimal)cumER * 27 / cumIPOuts, 2) : 0;
            var whip = cumIPOuts > 0 ? Math.Round((decimal)(cumH + cumBB) * 3 / cumIPOuts, 2) : 0;

            chartData.Add(new
            {
                date = game.date,
                era = era,
                whip = whip
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
        
        if (!allPitcherStats.Any())
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
    /// 計算賽季投手平均值
    /// </summary>
    /// <param name="allPitcherStats">所有投手的投球排名緩存列表</param>
    /// <returns>賽季投手平均值字典</returns>
    private Dictionary<string, decimal> CalculatePitcherSeasonAverages(List<PitchingRankingCache> allPitcherStats)
    {
        var seasonAverages = new Dictionary<string, decimal>();
        
        if (!allPitcherStats.Any())
            return seasonAverages;

        seasonAverages["ERA"] = allPitcherStats.Average(p => p.ERA);
        seasonAverages["WHIP"] = allPitcherStats.Average(p => p.WHIP);
        seasonAverages["K9"] = allPitcherStats.Average(p => p.K9);
        seasonAverages["BB9"] = allPitcherStats.Average(p => p.BB9);
        seasonAverages["KBB"] = allPitcherStats.Average(p => p.KBBRatio);
        seasonAverages["BAA"] = allPitcherStats.Average(p => p.BAA);
        seasonAverages["SO"] = (decimal)allPitcherStats.Average(p => p.SO);

        return seasonAverages;
    }

    /// <summary>
    /// 計算球隊投手統計和球隊百分位排名
    /// </summary>
    /// <param name="seasonId">賽季識別碼</param>
    /// <param name="playerTeam">球隊資訊</param>
    /// <param name="allPitcherStats">所有投手的投球排名緩存列表</param>
    /// <param name="radarStats">雷達圖統計數據</param>
    /// <returns>球隊統計和球隊百分位排名字典</returns>
    private async Task<(Dictionary<string, decimal>, Dictionary<string, decimal>)> CalculatePitcherTeamStats(
        string seasonId,
        PlayerTeam? playerTeam,
        List<PitchingRankingCache> allPitcherStats,
        dynamic radarStats)
    {
        var teamAverages = new Dictionary<string, decimal>();
        var teamPercentileRanks = new Dictionary<string, decimal>();

        if (playerTeam == null || !allPitcherStats.Any())
            return (teamAverages, teamPercentileRanks);

        var teamStats = await _rankingCacheService.GetTeamSeasonPitchingStatsAsync(seasonId, playerTeam.TeamId);
        if (teamStats == null)
            return (teamAverages, teamPercentileRanks);

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
            seasonAverages = new Dictionary<string, decimal>(),
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
        if (!values.Any()) return 0;
        var count = values.Count(v => v < targetValue);
        return Math.Round((decimal)count / values.Count * 100, 1);
    }
}
