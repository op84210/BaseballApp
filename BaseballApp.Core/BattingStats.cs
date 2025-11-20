using System.ComponentModel.DataAnnotations;

namespace BaseballApp.Models;

/// <summary>
/// 打者成績
/// </summary>
public class BattingStats
{
    [Display(Name = "球員姓名")]
    /// <summary>
    /// 球員姓名
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    [Display(Name = "球隊")]
    /// <summary>
    /// 球隊
    /// </summary>
    public string Team { get; set; } = string.Empty;

    [Display(Name = "出賽數")]
    /// <summary>
    /// 出賽數
    /// </summary>
    public int Games { get; set; }

    [Display(Name = "打席數")]
    /// <summary>
    /// 打席數
    /// </summary>
    public int PlateAppearances { get; set; }

    [Display(Name = "打數")]
    /// <summary>
    /// 打數
    /// </summary>
    public int AtBats { get; set; }

    [Display(Name = "安打")]
    /// <summary>
    /// 安打
    /// </summary>
    public int Hits { get; set; }

    [Display(Name = "二安")]
    /// <summary>
    /// 二安
    /// </summary>
    public int Doubles { get; set; }

    [Display(Name = "三安")]
    /// <summary>
    /// 三安
    /// </summary>
    public int Triples { get; set; }

    [Display(Name = "全壘打")]
    /// <summary>
    /// 全壘打
    /// </summary>
    public int HomeRuns { get; set; }

    [Display(Name = "打點")]
    /// <summary>
    /// 打點
    /// </summary>
    public int RBIs { get; set; }

    [Display(Name = "得分")]
    /// <summary>
    /// 得分
    /// </summary>
    public int Runs { get; set; }

    [Display(Name = "盜壘")]
    /// <summary>
    /// 盜壘
    /// </summary>
    public int StolenBases { get; set; }

    [Display(Name = "盜壘失敗")]
    /// <summary>
    /// 盜壘失敗
    /// </summary>
    public int CaughtStealing { get; set; }

    [Display(Name = "四壞")]
    /// <summary>
    /// 四壞
    /// </summary>
    public int Walks { get; set; }

    [Display(Name = "三振")]
    /// <summary>
    /// 三振
    /// </summary>
    public int Strikeouts { get; set; }

    [Display(Name = "打擊率")]
    [DisplayFormat(DataFormatString = "{0:F3}")]
    /// <summary>
    /// 打擊率
    /// </summary>
    public double BattingAverage => AtBats > 0 ? (double)Hits / AtBats : 0;

    [Display(Name = "上壘率")]
    [DisplayFormat(DataFormatString = "{0:F3}")]
    /// <summary>
    /// 上壘率
    /// </summary>
    public double OnBasePercentage => PlateAppearances > 0 ?
        (double)(Hits + Walks) / PlateAppearances : 0;

    [Display(Name = "長打率")]
    [DisplayFormat(DataFormatString = "{0:F3}")]
    /// <summary>
    /// 長打率
    /// </summary>
    public double SluggingPercentage => AtBats > 0 ?
        (double)(Hits + Doubles + 2 * Triples + 3 * HomeRuns) / AtBats : 0;

    [Display(Name = "OPS")]
    [DisplayFormat(DataFormatString = "{0:F3}")]
    /// <summary>
    /// OPS
    /// </summary>
    public double OPS => OnBasePercentage + SluggingPercentage;

    [Display(Name = "盜壘成功率")]
    [DisplayFormat(DataFormatString = "{0:P1}")]
    /// <summary>
    /// 盜壘成功率
    /// </summary>
    public double StolenBasePercentage
    {
        get
        {
            int attempts = StolenBases + CaughtStealing;
            return attempts > 0 ? (double)StolenBases / attempts : 0;
        }
    }

    [Display(Name = "賽季")]
    /// <summary>
    /// 賽季
    /// </summary>
    public string Season { get; set; } = string.Empty;

    [Display(Name = "更新時間")]
    /// <summary>
    /// 更新時間
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}

public class MonthlyBattingStats : BattingStats
{
    [Display(Name = "月份")]
    /// <summary>
    /// 月份
    /// </summary>
    public int Month { get; set; }

    [Display(Name = "受傷")]
    /// <summary>
    /// 受傷
    /// </summary>
    public bool IsInjured { get; set; }
}