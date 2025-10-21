namespace BaseballApp.Models;

/// <summary>
/// 跑者資料
/// </summary>
public class Runner
{
    /// <summary>
    /// 流水號 (自動增加)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 事件ID
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// 跑壘型態
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 跑者ID
    /// </summary>
    public string RunnerId { get; set; } = string.Empty;

    /// <summary>
    /// 是出局
    /// </summary>
    public bool IsOut { get; set; }

    /// <summary>
    /// 是得分
    /// </summary>
    public bool Scored { get; set; }

    /// <summary>
    /// 是打者的打點
    /// </summary>
    public bool IsRBI { get; set; }

    /// <summary>
    /// 是投手責失
    /// </summary>
    public bool IsER { get; set; }

    /// <summary>
    /// 被算責失的投手 (非責失為 null)
    /// </summary>
    public string? ERPitcherId { get; set; }

    // Navigation properties
    public Event? Event { get; set; }
    public Batter? RunnerPlayer { get; set; }
    public Pitcher? ERPitcher { get; set; }
}
