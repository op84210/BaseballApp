namespace BaseballApp.Models;

/// <summary>
/// 球隊逐場事實表 (tblTeamGameStats)
/// </summary>
public class TeamGameStats
{
    public int Id { get; set; }
    public string SeasonId { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string GameDate { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string OpponentTeamId { get; set; } = string.Empty;
    public string OpponentTeamName { get; set; } = string.Empty;
    public int IsHome { get; set; }
    public int TeamScore { get; set; }
    public int OpponentScore { get; set; }
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
    public int IPOuts { get; set; }
    public int ER { get; set; }
    public int HitsAllowed { get; set; }
    public int BbAllowed { get; set; }
    public int SoPitching { get; set; }
    public int HrAllowed { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}