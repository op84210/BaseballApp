namespace BaseballApp.Models;

/// <summary>
/// 球隊賽季統計資料 DTO
/// </summary>
public class TeamSeasonStatsDto
{
    public decimal AVG { get; set; }
    public decimal OBP { get; set; }
    public decimal SLG { get; set; }
    public decimal OPS { get; set; }
    public decimal HR { get; set; }
    public decimal RBI { get; set; }
    public decimal SO { get; set; }
    public decimal BB { get; set; }
}

/// <summary>
/// 球隊賽季統計資料查詢結果（用於 SQL 查詢）
/// </summary>
public class TeamSeasonStatsQueryResult
{
    public double? Avg { get; set; }
    public double? Obp { get; set; }
    public double? Slg { get; set; }
    public double? Ops { get; set; }
    public double? Hr { get; set; }
    public double? Rbi { get; set; }
    public double? So { get; set; }
    public double? Bb { get; set; }
}

/// <summary>
/// 球隊賽季投手統計資料 DTO
/// </summary>
public class TeamSeasonPitchingStatsDto
{
    public decimal ERA { get; set; }
    public decimal WHIP { get; set; }
    public decimal K9 { get; set; }
    public decimal BB9 { get; set; }
    public decimal KBBRatio { get; set; }
    public decimal BAA { get; set; }
    public decimal SO { get; set; }
}

/// <summary>
/// 球隊賽季投手統計資料查詢結果（用於 SQL 查詢）
/// </summary>
public class TeamSeasonPitchingStatsQueryResult
{
    public double? Era { get; set; }
    public double? Whip { get; set; }
    public double? K9 { get; set; }
    public double? Bb9 { get; set; }
    public double? KbbRatio { get; set; }
    public double? Baa { get; set; }
    public double? So { get; set; }
}

