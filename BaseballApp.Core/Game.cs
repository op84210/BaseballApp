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
    public Season? Season { get; set; }
    public Stadium? Stadium { get; set; }
    public Team? AwayTeam { get; set; }
    public Team? HomeTeam { get; set; }
    // 所有得分記錄
    public ICollection<Scores> Scores { get; set; } = [];

    // 方便使用的衍生屬性（不參與對應）：主隊/客隊得分
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public IEnumerable<Scores> HomeScores => Scores.Where(s => s.HomeOrAway == "H");

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public IEnumerable<Scores> AwayScores => Scores.Where(s => s.HomeOrAway == "A");
}
