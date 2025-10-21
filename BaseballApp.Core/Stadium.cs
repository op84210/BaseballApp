namespace BaseballApp.Models;

/// <summary>
/// 比賽場地資料
/// </summary>
public class Stadium
{
    /// <summary>
    /// 比賽場地ID (自動增加)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 比賽場地
    /// </summary>
    public string stadium { get; set; } = string.Empty;
}
