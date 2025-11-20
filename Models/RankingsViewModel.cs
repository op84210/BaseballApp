using System.Collections.Generic;

namespace BaseballApp.Models;

public enum RankingCategory
{
    Batting,
    Pitching
}

public class BattingRankingItem
{
    public int Rank { get; set; }
    public string? PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int Games { get; set; }
    public int PA { get; set; }
    public int AB { get; set; }
    public int H { get; set; }
    public int HR { get; set; }
    public int RBI { get; set; }
    public int SO { get; set; }
    public int BB { get; set; }
    public decimal AVG { get; set; }
    public decimal OBP { get; set; }
    public decimal SLG { get; set; }
}

public class PitchingRankingItem
{
    public int Rank { get; set; }
    public string? PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int Games { get; set; }
    public decimal IP { get; set; }
    public int H { get; set; }
    public int HR { get; set; }
    public int BB { get; set; }
    public int SO { get; set; }
    public int R { get; set; }
    public int ER { get; set; }
    public decimal ERA { get; set; }
    public decimal WHIP { get; set; }
}

public class RankingsViewModel
{
    public string SeasonId { get; set; } = string.Empty;
    public RankingCategory Category { get; set; }
    public List<BattingRankingItem> BattingRankings { get; set; } = new();
    public List<PitchingRankingItem> PitchingRankings { get; set; } = new();
}
