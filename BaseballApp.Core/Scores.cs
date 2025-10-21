namespace BaseballApp.Models;

/// <summary>
/// 得分資料
/// </summary>
public class Scores
{
    /// <summary>
    /// 賽事ID
    /// </summary>
    public string SeasonId { get; set; } = string.Empty;

    /// <summary>
    /// 比賽編號
    /// </summary>
    public int GameSeq { get; set; }

    /// <summary>
    /// 主/客場 (A=客場, H=主場)
    /// </summary>
    public string HomeOrAway { get; set; } = string.Empty;

    /// <summary>
    /// 局數
    /// </summary>
    public int Inning { get; set; }

    /// <summary>
    /// 得分
    /// </summary>
    public int Score { get; set; }

    // Navigation property
    public Game? Game { get; set; }
}
