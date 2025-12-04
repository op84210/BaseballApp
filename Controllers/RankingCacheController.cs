using BaseballApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace BaseballApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RankingCacheController : ControllerBase
{
    private readonly IRankingCacheService _rankingCacheService;
    private readonly ILogger<RankingCacheController> _logger;

    public RankingCacheController(
        IRankingCacheService rankingCacheService,
        ILogger<RankingCacheController> logger)
    {
        _rankingCacheService = rankingCacheService;
        _logger = logger;
    }

    /// <summary>
    /// 手動更新指定賽季的打者排行榜快取
    /// </summary>
    /// <param name="seasonId">賽季ID，例如：CPBL-2024-HE</param>
    [HttpPost("batting/{seasonId}")]
    public async Task<IActionResult> UpdateBattingRankings(string seasonId)
    {
        try
        {
            _logger.LogInformation($"手動觸發更新打者排行榜快取：{seasonId}");
            await _rankingCacheService.UpdateBattingRankingsAsync(seasonId);
            return Ok(new { message = $"打者排行榜快取已更新：{seasonId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"更新打者排行榜快取失敗：{seasonId}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 手動更新指定賽季的投手排行榜快取
    /// </summary>
    /// <param name="seasonId">賽季ID，例如：CPBL-2024-HE</param>
    [HttpPost("pitching/{seasonId}")]
    public async Task<IActionResult> UpdatePitchingRankings(string seasonId)
    {
        try
        {
            _logger.LogInformation($"手動觸發更新投手排行榜快取：{seasonId}");
            await _rankingCacheService.UpdatePitchingRankingsAsync(seasonId);
            return Ok(new { message = $"投手排行榜快取已更新：{seasonId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"更新投手排行榜快取失敗：{seasonId}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 手動更新球隊賽季排行榜快取
    /// </summary>
    /// <param name="seasonId">賽季ID（可選，不提供則更新所有賽季）</param>
    [HttpPost("team")]
    public async Task<IActionResult> UpdateTeamRankings([FromQuery] string? seasonId = null)
    {
        try
        {
            _logger.LogInformation($"手動觸發更新球隊排行榜快取：{seasonId ?? "ALL"}");
            await _rankingCacheService.UpdateTeamRankingsAsync(seasonId);
            return Ok(new { message = $"球隊排行榜快取已更新：{seasonId ?? "ALL"}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"更新球隊排行榜快取失敗：{seasonId}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 手動更新指定賽季的球隊排行榜快取（路徑參數版本）
    /// </summary>
    /// <param name="seasonId">賽季ID，例如：CPBL-2024-HE</param>
    [HttpPost("team/{seasonId}")]
    public async Task<IActionResult> UpdateTeamRankingsBySeason(string seasonId)
    {
        try
        {
            _logger.LogInformation($"手動觸發更新球隊排行榜快取：{seasonId}");
            await _rankingCacheService.UpdateTeamRankingsAsync(seasonId);
            return Ok(new { message = $"球隊排行榜快取已更新：{seasonId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"更新球隊排行榜快取失敗：{seasonId}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 重新計算所有賽季的 tblTeamSeasonRankingCache（包括 seasonId=ALL）
    /// </summary>
    [HttpPost("team/rebuild/all")]
    public async Task<IActionResult> RebuildAllTeamRankings()
    {
        try
        {
            _logger.LogInformation("手動觸發重新計算所有球隊排行榜快取（包括 ALL 季）");
            
            // 先更新所有指定賽季的排行榜
            await _rankingCacheService.UpdateTeamRankingsAsync();
            
            return Ok(new { message = "所有球隊排行榜快取已重新計算（包括 seasonId=ALL）" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新計算所有球隊排行榜快取失敗");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 手動更新所有賽季的排行榜快取（包含球員與球隊）
    /// </summary>
    [HttpPost("all")]
    public async Task<IActionResult> UpdateAllRankings()
    {
        try
        {
            _logger.LogInformation("手動觸發更新所有賽季的排行榜快取");
            await _rankingCacheService.UpdateAllRankingsAsync();
            return Ok(new { message = "所有排行榜快取已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新所有排行榜快取失敗");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 檢查快取狀態
    /// </summary>
    /// <param name="seasonId">賽季ID</param>
    [HttpGet("status/{seasonId}")]
    public async Task<IActionResult> GetCacheStatus(string seasonId)
    {
        try
        {
            var isStale = await _rankingCacheService.IsCacheStaleAsync(seasonId, hoursThreshold: 24);
            return Ok(new 
            { 
                seasonId = seasonId,
                isStale = isStale,
                message = isStale ? "快取已過期或不存在" : "快取是最新的"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"檢查快取狀態失敗：{seasonId}");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
