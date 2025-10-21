namespace BaseballApp.Models.Codes;

/// <summary>
/// 球種代碼
/// </summary>
public class CodePitchType
{
    /// <summary>
    /// 代碼值 (FF, SI, FC, KN, SL, CU, CH, FO, FS, EP)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 代碼名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
