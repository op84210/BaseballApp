using System.Collections.Generic;

namespace BaseballApp.Models;

public enum RankingCategory
{
    /// <summary>
    /// 打者排名
    /// </summary>
    Batting,

    /// <summary>
    /// 投手排名
    /// </summary>
    Pitching
}

public class BattingRankingItem
{
    /// <summary>
    /// 排名
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// 球員ID
    /// </summary>
    public string? PlayerId { get; set; }

    /// <summary>
    /// 球員名稱
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

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
    /// 全壘打數
    /// </summary>
    public int HR { get; set; }

    /// <summary>
    /// 打點數
    /// </summary>
    public int RBI { get; set; }

    /// <summary>
    /// 三振數
    /// </summary>
    public int SO { get; set; }

    /// <summary>
    /// 保送數
    /// </summary>
    public int BB { get; set; }

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
}

public class PitchingRankingItem
{
    /// <summary>
    /// 排名
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// 球員ID
    /// </summary>
    public string? PlayerId { get; set; }

    /// <summary>
    /// 球員名稱
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// 出賽場數
    /// </summary>
    public int Games { get; set; }

    /// <summary>
    /// 先發場數
    /// </summary>
    public decimal IP { get; set; }

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
    /// 防禦率
    /// </summary>
    public decimal ERA { get; set; }

    /// <summary>
    /// 每九局被上壘率
    /// </summary>
    public decimal WHIP { get; set; }
}

public class RankingsViewModel
{
    public RankingsViewModel()
    {
        
    }
    /// <summary>
    /// 賽季代碼，ALL 代表全部賽季
    /// </summary>
    public string SeasonId { get; set; } = string.Empty;

    /// <summary>
    /// 排名類別
    /// </summary>
    public RankingCategory Category { get; set; }

    /// <summary>
    /// 打者排名列表
    /// </summary>
    public List<BattingRankingItem> BattingRankings { get; set; } = new();

    /// <summary>
    /// 投手排名列表
    /// </summary>
    public List<PitchingRankingItem> PitchingRankings { get; set; } = new();

    /// <summary>
    /// 可選賽季列表
    /// </summary>
    public List<Season> Seasons { get; set; } = new();

    /// <summary>
    /// 最低合格打席數
    /// </summary>
    public int MinQualifiedPA { get; set; }

    /// <summary>
    /// 最低合格投球局數
    /// </summary>
    public decimal MinQualifiedIP { get; set; }

    /// <summary>
    /// 合格打者總數
    /// </summary>
    public int TotalQualifiedBatters { get; set; }

    /// <summary>
    /// 合格投手總數
    /// </summary>
    public int TotalQualifiedPitchers { get; set; }
}
