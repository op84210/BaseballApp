namespace BaseballApp.Models;

/// <summary>
/// 打者排行榜快取
/// </summary>
public class BattingRankingCache
{
    /// <summary>
    /// 主鍵
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 賽季ID
    /// </summary>
    public string SeasonId { get; set; } = string.Empty;

    /// <summary>
    /// 球員ID
    /// </summary>
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>
    /// 球員名稱
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// 排名
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// 出賽場數
    /// </summary>
    public int Games { get; set; }

    /// <summary>
    /// 打席數
    /// </summary>
    public int PA { get; set; }

    /// <summary>
    /// 打數
    /// </summary>
    public int AB { get; set; }

    /// <summary>
    /// 安打數
    /// </summary>
    public int H { get; set; }

    /// <summary>
    /// 二壘安打
    /// </summary>
    public int TwoB { get; set; }

    /// <summary>
    /// 三壘安打
    /// </summary>
    public int ThreeB { get; set; }

    /// <summary>
    /// 全壘打數
    /// </summary>
    public int HR { get; set; }

    /// <summary>
    /// 打點數
    /// </summary>
    public int RBI { get; set; }

    /// <summary>
    /// 得分數
    /// </summary>
    public int R { get; set; }

    /// <summary>
    /// 三振數
    /// </summary>
    public int SO { get; set; }

    /// <summary>
    /// 保送數
    /// </summary>
    public int BB { get; set; }

    /// <summary>
    /// 盜壘數
    /// </summary>
    public int SB { get; set; }

    /// <summary>
    /// 打擊率
    /// </summary>
    public decimal AVG { get; set; }

    /// <summary>
    /// 上壘率
    /// </summary>
    public decimal OBP { get; set; }

    /// <summary>
    /// 長打率
    /// </summary>
    public decimal SLG { get; set; }

    /// <summary>
    /// OPS
    /// </summary>
    public decimal OPS { get; set; }

    /// <summary>
    /// 更新時間
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 投手排行榜快取
/// </summary>
public class PitchingRankingCache
{
    /// <summary>
    /// 主鍵
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 賽季ID
    /// </summary>
    public string SeasonId { get; set; } = string.Empty;

    /// <summary>
    /// 球員ID
    /// </summary>
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>
    /// 球員名稱
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// 排名
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// 出賽場數
    /// </summary>
    public int Games { get; set; }

    /// <summary>
    /// 投球局數（小數）
    /// </summary>
    public decimal IP { get; set; }

    /// <summary>
    /// 投球局數（出局數）
    /// </summary>
    public int IPOuts { get; set; }

    /// <summary>
    /// 被安打數
    /// </summary>
    public int H { get; set; }

    /// <summary>
    /// 被全壘打數
    /// </summary>
    public int HR { get; set; }

    /// <summary>
    /// 四壞球數
    /// </summary>
    public int BB { get; set; }

    /// <summary>
    /// 三振數
    /// </summary>
    public int SO { get; set; }

    /// <summary>
    /// 失分數
    /// </summary>
    public int R { get; set; }

    /// <summary>
    /// 自責分數
    /// </summary>
    public int ER { get; set; }

    /// <summary>
    /// 勝場數
    /// </summary>
    public int W { get; set; }

    /// <summary>
    /// 敗場數
    /// </summary>
    public int L { get; set; }

    /// <summary>
    /// 防禦率
    /// </summary>
    public decimal ERA { get; set; }

    /// <summary>
    /// 每局被上壘率
    /// </summary>
    public decimal WHIP { get; set; }

    /// <summary>
    /// 更新時間
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
