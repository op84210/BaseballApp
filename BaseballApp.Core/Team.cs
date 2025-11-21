namespace BaseballApp.Models;

/// <summary>
/// 球隊資料
/// </summary>
public class Team
{
    /// <summary>
    /// 球隊ID
    /// </summary>
    public string TeamId { get; set; } = string.Empty;

    /// <summary>
    /// 球隊名稱
    /// </summary>
    public string TeamName { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<PlayerTeam> PlayerTeams { get; set; } = new List<PlayerTeam>();
}
