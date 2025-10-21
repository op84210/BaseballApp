namespace BaseballApp.Models.Codes;

/// <summary>
/// 擊球彈道代碼
/// </summary>
public class CodeTrajectory
{
    /// <summary>
    /// 代碼值 (G, L, F, P)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 代碼名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
