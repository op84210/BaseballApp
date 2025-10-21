namespace BaseballApp.Models;

/// <summary>
/// 打席資料
/// </summary>
public class PA
{
    /// <summary>
    /// 打席ID (自動增加)
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
    /// 局數
    /// </summary>
    public int Inning { get; set; }

    /// <summary>
    /// 打席序號 (該局第幾個打席)
    /// </summary>
    public int PaSeq { get; set; }

    /// <summary>
    /// 有得分
    /// </summary>
    public bool Scored { get; set; }

    /// <summary>
    /// 打者ID
    /// </summary>
    public string? BatterId { get; set; }

    /// <summary>
    /// 打者用手 (R=右, L=左)
    /// </summary>
    public string? BatterHand { get; set; }

    /// <summary>
    /// 投手ID
    /// </summary>
    public string? PitcherId { get; set; }

    /// <summary>
    /// 投手用手 (R=右, L=左)
    /// </summary>
    public string? PitcherHand { get; set; }

    /// <summary>
    /// 捕手ID
    /// </summary>
    public string? CatcherId { get; set; }

    /// <summary>
    /// 進攻輪次
    /// </summary>
    public int? PaRound { get; set; }

    /// <summary>
    /// 打序
    /// </summary>
    public int? PaOrder { get; set; }

    /// <summary>
    /// 是代打
    /// </summary>
    public bool IsPH { get; set; }

    /// <summary>
    /// 結束打席前客場分
    /// </summary>
    public int? AwayScores { get; set; }

    /// <summary>
    /// 結束打席前主場分
    /// </summary>
    public int? HomeScores { get; set; }

    /// <summary>
    /// 結束打席前好球數
    /// </summary>
    public int? Strikes { get; set; }

    /// <summary>
    /// 結束打席前壞球數
    /// </summary>
    public int? Balls { get; set; }

    /// <summary>
    /// 結束打席前出局數
    /// </summary>
    public int? Outs { get; set; }

    /// <summary>
    /// 結束打席前壘包狀況
    /// </summary>
    public string? Bases { get; set; }

    /// <summary>
    /// 結束打席前主場勝率
    /// </summary>
    public decimal? HomeWE { get; set; }

    /// <summary>
    /// 結束打席前得分期望值
    /// </summary>
    public decimal? RE { get; set; }

    /// <summary>
    /// 打席結果
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// 打點
    /// </summary>
    public int? RBI { get; set; }

    /// <summary>
    /// 擊球落點
    /// </summary>
    public string? LocationCode { get; set; }

    /// <summary>
    /// 擊球彈道
    /// </summary>
    public string? Trajectory { get; set; }

    /// <summary>
    /// 擊球力道 (非全部擊球結果都有)
    /// </summary>
    public string? Hardness { get; set; }

    /// <summary>
    /// 打席結束後客場分
    /// </summary>
    public int? EndAwayScores { get; set; }

    /// <summary>
    /// 打席結束後主場分
    /// </summary>
    public int? EndHomeScores { get; set; }

    /// <summary>
    /// 打席結束後出局數
    /// </summary>
    public int? EndOuts { get; set; }

    /// <summary>
    /// 打席結束後壘包狀況
    /// </summary>
    public string? EndBases { get; set; }

    /// <summary>
    /// 勝率增加
    /// </summary>
    public decimal? WPA { get; set; }

    /// <summary>
    /// 得分期望值增加
    /// </summary>
    public decimal? RE24 { get; set; }

    // Navigation properties
    public Game? Game { get; set; }
    public Batter? Batter { get; set; }
    public Pitcher? Pitcher { get; set; }
    public Batter? Catcher { get; set; }
}
