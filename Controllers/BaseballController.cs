using BaseballApp.Models;
using BaseballApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace BaseballApp.Controllers;

public class BaseballController : Controller
{
    private readonly IBaseballDataService _baseballDataService;
    private readonly ILogger<BaseballController> _logger;

    public BaseballController(IBaseballDataService baseballDataService, ILogger<BaseballController> logger)
    {
        _baseballDataService = baseballDataService;
        _logger = logger;
    }

    // GET: /Baseball/Stats
    public async Task<IActionResult> Stats(string playerName = null)
    {
        try
        {
            var stats = await _baseballDataService.GetPlayerStatsAsync(playerName);
            ViewBag.PlayerName = playerName;
            return View(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "獲取球員統計數據時發生錯誤");
            ViewBag.ErrorMessage = "無法獲取數據，請稍後再試";
            return View(Enumerable.Empty<BattingStats>());
        }
    }

    // GET: /Baseball/PlayerStats/{playerName}/{season}
    public async Task<IActionResult> PlayerStats(string playerName, string season = "2024")
    {
        try
        {
            var stats = await _baseballDataService.GetPlayerSeasonStatsAsync(playerName, season);
            if (stats == null)
            {
                return NotFound($"找不到球員 {playerName} 在 {season} 賽季的數據");
            }

            ViewBag.PlayerName = playerName;
            ViewBag.Season = season;
            return View(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"獲取球員 {playerName} 的數據時發生錯誤");
            ViewBag.ErrorMessage = $"無法獲取球員 {playerName} 的數據";
            return View(new BattingStats { PlayerName = playerName, Season = season });
        }
    }

    // GET: /Baseball/MonthlyStats/{playerName}/{season}
    public async Task<IActionResult> MonthlyStats(string playerName, string season = "2024")
    {
        try
        {
            var monthlyStats = await _baseballDataService.GetMonthlyStatsAsync(playerName, season);
            ViewBag.PlayerName = playerName;
            ViewBag.Season = season;
            return View(monthlyStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"獲取球員 {playerName} 的月度數據時發生錯誤");
            ViewBag.ErrorMessage = $"無法獲取球員 {playerName} 的月度數據";
            return View(Enumerable.Empty<MonthlyBattingStats>());
        }
    }

    // POST: /Baseball/UpdateStats
    [HttpPost]
    public async Task<IActionResult> UpdateStats()
    {
        try
        {
            var success = await _baseballDataService.UpdateStatsFromAPIAsync();
            if (success)
            {
                TempData["SuccessMessage"] = "數據更新成功！";
            }
            else
            {
                TempData["ErrorMessage"] = "數據更新失敗，請稍後再試";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新數據時發生錯誤");
            TempData["ErrorMessage"] = "數據更新過程中發生錯誤";
        }

        return RedirectToAction("Stats");
    }

    // API端點 - 返回JSON數據供圖表使用
    [HttpGet]
    public async Task<IActionResult> GetChartData(string playerName, string season = "2024")
    {
        try
        {
            var monthlyStats = await _baseballDataService.GetMonthlyStatsAsync(playerName, season);

            var chartData = new
            {
                labels = monthlyStats.Select(m => $"{m.Month}月").ToArray(),
                battingAverage = monthlyStats.Select(m => m.IsInjured ? (double?)null : m.BattingAverage).ToArray(),
                homeRuns = monthlyStats.Select(m => m.IsInjured ? (int?)null : m.HomeRuns).ToArray(),
                rbis = monthlyStats.Select(m => m.IsInjured ? (int?)null : m.RBIs).ToArray(),
                injuredMonths = monthlyStats.Where(m => m.IsInjured).Select(m => m.Month).ToArray()
            };

            return Json(chartData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"獲取圖表數據時發生錯誤: {playerName}");
            return Json(new { error = "無法獲取數據" });
        }
    }
}