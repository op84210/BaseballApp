namespace BaseballApp.Models;

/// <summary>
/// 球員資料_打者
/// </summary>
public class Player
{
    /// <summary>
    /// 球員ID
    /// </summary>
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>
    /// 球員背號
    /// </summary>
    public string? PlayerNumber { get; set; }

    /// <summary>
    /// 球員名稱
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<PlayerTeam> PlayerTeams { get; set; } = new List<PlayerTeam>();
}
