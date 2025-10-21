namespace BaseballApp.Models.Codes;

/// <summary>
/// 投球結果代碼
/// </summary>
public class CodePitchCode
{
    /// <summary>
    /// 代碼值 (S, SW, B, F, FT, FOUL_BUNT, TRY_BUNT, BUNT, H)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 代碼名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
