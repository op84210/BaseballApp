namespace BaseballApp.Models.Codes;

/// <summary>
/// 跑壘型態代碼
/// </summary>
public class CodeRunnerType
{
    /// <summary>
    /// 代碼值 (PA, ADVANCE, SB, CS, CS_E, PO)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 代碼名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
