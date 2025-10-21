namespace BaseballApp.Models;

/// <summary>
/// 投手成績
/// </summary>
public class PitcherBox
{
    /// <summary>
    /// 流水號 (自動增加)
    /// </summary>
    public int Id { get; set; }

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
    /// 上場順序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 球員ID
    /// </summary>
    public string? PlayerId { get; set; }

    /// <summary>
    /// 出局數 (非常見IP局數，是出局數)
    /// </summary>
    public int? IPOuts { get; set; }

    /// <summary>
    /// 用球數
    /// </summary>
    public int? NP { get; set; }

    /// <summary>
    /// 面對打席
    /// </summary>
    public int? BF { get; set; }

    /// <summary>
    /// 被安打
    /// </summary>
    public int? H { get; set; }

    /// <summary>
    /// 被全壘打
    /// </summary>
    public int? HR { get; set; }

    /// <summary>
    /// 四壞 (包含故意四壞，不包含觸身球)
    /// </summary>
    public int? BB { get; set; }

    /// <summary>
    /// 故意四壞
    /// </summary>
    public int? IBB { get; set; }

    /// <summary>
    /// 觸身球
    /// </summary>
    public int? HB { get; set; }

    /// <summary>
    /// 三振
    /// </summary>
    public int? SO { get; set; }

    /// <summary>
    /// 失分
    /// </summary>
    public int? R { get; set; }

    /// <summary>
    /// 責失
    /// </summary>
    public int? ER { get; set; }

    // Navigation properties
    public Game? Game { get; set; }
    public Pitcher? Player { get; set; }
}
