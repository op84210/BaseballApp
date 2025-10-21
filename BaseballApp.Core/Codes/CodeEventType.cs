namespace BaseballApp.Models.Codes;

/// <summary>
/// 事件型態代碼
/// </summary>
public class CodeEventType
{
    /// <summary>
    /// 代碼值 (PITCH, BASE, NO_PITCH, SUB)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 代碼名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
