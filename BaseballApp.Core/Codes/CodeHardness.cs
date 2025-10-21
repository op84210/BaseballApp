namespace BaseballApp.Models.Codes;

/// <summary>
/// 擊球力道代碼
/// </summary>
public class CodeHardness
{
    /// <summary>
    /// 代碼值 (S, M, H)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 代碼名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
