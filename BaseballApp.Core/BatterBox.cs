namespace BaseballApp.Models;

/// <summary>
/// 打者成績
/// </summary>
public class BatterBox
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
    /// 打序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 替補順序 (0=先發, 1=第一個替補, 2=第二個替補...)
    /// </summary>
    public int SubOrder { get; set; }

    /// <summary>
    /// 球員ID
    /// </summary>
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>
    /// 打席
    /// </summary>
    public int PA { get; set; } = 0;

    /// <summary>
    /// 打數
    /// </summary>
    public int AB { get; set; } = 0;

    /// <summary>
    /// 得分
    /// </summary>
    public int R { get; set; } = 0;

    /// <summary>
    /// 安打
    /// </summary>
    public int H { get; set; } = 0;

    /// <summary>
    /// 打點
    /// </summary>
    public int RBI { get; set; } = 0;

    /// <summary>
    /// 二壘安打
    /// </summary>
    public int TwoB { get; set; } = 0;

    /// <summary>
    /// 三壘安打
    /// </summary>
    public int ThreeB { get; set; } = 0;

    /// <summary>
    /// 全壘打
    /// </summary>
    public int HR { get; set; } = 0;

    /// <summary>
    /// 滾地雙殺
    /// </summary>
    public int GIDP { get; set; } = 0;

    /// <summary>
    /// 雙殺打 (包含滾地雙殺)
    /// </summary>
    public int DP { get; set; } = 0;

    /// <summary>
    /// 三殺打
    /// </summary>
    public int TP { get; set; } = 0;

    /// <summary>
    /// 四壞 (包含故意四壞，不包含觸身球)
    /// </summary>
    public int BB { get; set; } = 0;

    /// <summary>
    /// 故意四壞
    /// </summary>
    public int IBB { get; set; } = 0;

    /// <summary>
    /// 觸身球
    /// </summary>
    public int HBP { get; set; } = 0;

    /// <summary>
    /// 三振
    /// </summary>
    public int SO { get; set; } = 0;

    /// <summary>
    /// 犧牲觸擊
    /// </summary>
    public int SH { get; set; } = 0;

    /// <summary>
    /// 犧牲飛球
    /// </summary>
    public int SF { get; set; } = 0;

    /// <summary>
    /// 失誤上壘
    /// </summary>
    public int E { get; set; } = 0;

    /// <summary>
    /// 盜壘成功
    /// </summary>
    public int SB { get; set; } = 0;

    /// <summary>
    /// 盜壘失敗
    /// </summary>
    public int CS { get; set; } = 0;

    // Navigation properties
    public Game? Game { get; set; }
    public Batter? Player { get; set; }
}
