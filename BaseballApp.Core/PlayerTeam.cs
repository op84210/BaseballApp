namespace BaseballApp.Models;

/// <summary>
/// 球員所屬球隊資料
/// </summary>
public class PlayerTeam
{
    /// <summary>
    /// 主鍵
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 球員編號
    /// </summary>
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>
    /// 所屬球隊編號
    /// </summary>
    public string TeamId { get; set; } = string.Empty;

    /// <summary>
    /// 所屬賽季編號
    /// </summary>
    public string SeasonId { get; set; } = string.Empty;

    /// <summary>
    /// 球員背號
    /// </summary>
    public string PlayerNumber { get; set; } = string.Empty;

    /// <summary>
    /// 加入日期
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 離隊日期
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 是否為現役球員
    /// </summary>
    public bool IsActive { get; set; }
}
