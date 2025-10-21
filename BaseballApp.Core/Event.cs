namespace BaseballApp.Models;

/// <summary>
/// 打席內事件
/// </summary>
public class Event
{
    /// <summary>
    /// 事件ID (自動增加)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 打席ID
    /// </summary>
    public int PaId { get; set; }

    /// <summary>
    /// 事件順序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 事件型態
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 球進場
    /// </summary>
    public bool InPlay { get; set; }

    /// <summary>
    /// 是好球 (判斷 pitchCode 是否應為好球，無論是否兩好球)
    /// </summary>
    public bool IsStrike { get; set; }

    /// <summary>
    /// 是壞球 (判斷 pitchCode 是否應為壞球，無論是否三壞球)
    /// </summary>
    public bool IsBall { get; set; }

    /// <summary>
    /// 投手ID
    /// </summary>
    public string? PitcherId { get; set; }

    /// <summary>
    /// 捕手ID
    /// </summary>
    public string? CatcherId { get; set; }

    /// <summary>
    /// 打者ID
    /// </summary>
    public string? BatterId { get; set; }

    /// <summary>
    /// 投球結果
    /// </summary>
    public string? PitchCode { get; set; }

    /// <summary>
    /// 球種
    /// </summary>
    public string? PitchType { get; set; }

    /// <summary>
    /// 球速 (NULL 表示無資料)
    /// </summary>
    public int? Velocity { get; set; }

    /// <summary>
    /// COORD X (NULL 表示無資料)
    /// </summary>
    public int? CoordX { get; set; }

    /// <summary>
    /// COORD Y (NULL 表示無資料)
    /// </summary>
    public int? CoordY { get; set; }

    // Navigation properties
    public PA? PA { get; set; }
    public Pitcher? Pitcher { get; set; }
    public Batter? Catcher { get; set; }
    public Batter? Batter { get; set; }
}
