namespace BaseballApp.Models;

/// <summary>
/// 球隊賽季匯總/排行榜快取 (tblTeamSeasonRankingCache)
/// </summary>
public class TeamSeasonRankingCache
{
    public int Id { get; set; }
    public string SeasonId { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int RunsScored { get; set; }
    public int RunsAllowed { get; set; }
    public int PA { get; set; }
    public int AB { get; set; }
    public int H { get; set; }
    public int TwoB { get; set; }
    public int ThreeB { get; set; }
    public int HR { get; set; }
    public int BB { get; set; }
    public int SO { get; set; }
    public int HBP { get; set; }
    public int SF { get; set; }
    public int SB { get; set; }
    public int CS { get; set; }
    public decimal AVG { get; set; }
    public decimal OBP { get; set; }
    public decimal SLG { get; set; }
    public decimal OPS { get; set; }
}