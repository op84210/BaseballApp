using Microsoft.AspNetCore.Mvc;

namespace BaseballApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChartsApiController : ControllerBase
{
    // TODO: 注入你的資料服務/DB 取數邏輯

    // 打擊率趨勢（示例：回傳已聚合的日期-打擊率）
    [HttpGet("batting-trend")]
    public IActionResult GetBattingTrend([FromQuery] string playerId = "demo", [FromQuery] string? from = null, [FromQuery] string? to = null)
    {
        // TODO: 以 playerId + 日期區間查詢並計算
        var data = new {
            labels = new[] { "2024-04-01","2024-04-08","2024-04-15","2024-04-22","2024-04-29" },
            series = new[] { 0.250, 0.267, 0.280, 0.295, 0.310 }
        };
        return Ok(data);
    }

    // 全壘打統計（示例：回傳前 N 名）
    [HttpGet("homeruns")]
    public IActionResult GetHomeruns([FromQuery] string season = "2024", [FromQuery] int top = 10)
    {
        // TODO: 以 season 聚合 player -> HR 數
        var players = new[] {
            new { player="Player A", hr=28 },
            new { player="Player B", hr=25 },
            new { player="Player C", hr=22 }
        };
        return Ok(players);
    }

    // 雷達圖（示例：回傳已正規化 0~100 的指標）
    [HttpGet("radar")]
    public IActionResult GetRadar([FromQuery] string playerId = "demo", [FromQuery] string season = "2024")
    {
        // TODO: 以 playerId + season 計算並正規化
        var data = new {
            indicators = new[] { "AVG","OBP","SLG","OPS","HR","RBI" },
            values = new[] { 72, 68, 75, 78, 60, 66 }
        };
        return Ok(data);
    }
}