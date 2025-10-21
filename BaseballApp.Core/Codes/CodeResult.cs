namespace BaseballApp.Models.Codes;

/// <summary>
/// 打席結果代碼
/// </summary>
public class CodeResult
{
    /// <summary>
    /// 代碼值 (1B, 2B, 3B, HR, IHR, SO, uBB, IBB, HBP, GO, FO, FC, E, SH, SF, GIDP, DP, TP, IH, IR, ID, IGNORE)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 代碼名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
