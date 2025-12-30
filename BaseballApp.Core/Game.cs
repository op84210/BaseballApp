namespace BaseballApp.Models;

/// <summary>
/// 比賽基本資料
/// </summary>
public class Game
{
    /// <summary>
    /// 賽事ID
    /// </summary>
    public string SeasonId { get; set; } = string.Empty;

    /// <summary>
    /// 比賽編號
    /// </summary>
    public int Seq { get; set; }

    /// <summary>
    /// 比賽日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 比賽場地ID
    /// </summary>
    public int? StadiumId { get; set; }

    /// <summary>
    /// 客場球隊ID
    /// </summary>
    public string? AwayTeamId { get; set; }

    /// <summary>
    /// 主場球隊ID
    /// </summary>
    public string? HomeTeamId { get; set; }

    // Navigation properties
    public required Season Season { get; set; }
    public required Stadium Stadium { get; set; }
    public required Team AwayTeam { get; set; }
    public required Team HomeTeam { get; set; }
    public ICollection<Scores> AwayScores { get; set; } = [];
    public ICollection<Scores> HomeScores { get; set; } = [];
}
