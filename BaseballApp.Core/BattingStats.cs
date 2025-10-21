using System.ComponentModel.DataAnnotations;

namespace BaseballApp.Models;

public class BattingStats
{
    public int Id { get; set; }

    [Display(Name = "球員姓名")]
    public string PlayerName { get; set; } = string.Empty;

    [Display(Name = "球隊")]
    public string Team { get; set; } = string.Empty;

    [Display(Name = "打席數")]
    public int PlateAppearances { get; set; }

    [Display(Name = "打數")]
    public int AtBats { get; set; }

    [Display(Name = "安打")]
    public int Hits { get; set; }

    [Display(Name = "二安")]
    public int Doubles { get; set; }

    [Display(Name = "三安")]
    public int Triples { get; set; }

    [Display(Name = "全壘打")]
    public int HomeRuns { get; set; }

    [Display(Name = "打點")]
    public int RBIs { get; set; }

    [Display(Name = "得分")]
    public int Runs { get; set; }

    [Display(Name = "盜壘")]
    public int StolenBases { get; set; }

    [Display(Name = "盜壘失敗")]
    public int CaughtStealing { get; set; }

    [Display(Name = "四壞")]
    public int Walks { get; set; }

    [Display(Name = "三振")]
    public int Strikeouts { get; set; }

    [Display(Name = "打擊率")]
    [DisplayFormat(DataFormatString = "{0:F3}")]
    public double BattingAverage => AtBats > 0 ? (double)Hits / AtBats : 0;

    [Display(Name = "上壘率")]
    [DisplayFormat(DataFormatString = "{0:F3}")]
    public double OnBasePercentage => PlateAppearances > 0 ?
        (double)(Hits + Walks) / PlateAppearances : 0;

    [Display(Name = "長打率")]
    [DisplayFormat(DataFormatString = "{0:F3}")]
    public double SluggingPercentage => AtBats > 0 ?
        (double)(Hits + Doubles + 2 * Triples + 3 * HomeRuns) / AtBats : 0;

    [Display(Name = "OPS")]
    [DisplayFormat(DataFormatString = "{0:F3}")]
    public double OPS => OnBasePercentage + SluggingPercentage;

    [Display(Name = "盜壘成功率")]
    [DisplayFormat(DataFormatString = "{0:P1}")]
    public double StolenBasePercentage
    {
        get
        {
            int attempts = StolenBases + CaughtStealing;
            return attempts > 0 ? (double)StolenBases / attempts : 0;
        }
    }

    [Display(Name = "賽季")]
    public string Season { get; set; } = string.Empty;

    [Display(Name = "更新時間")]
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}

public class MonthlyBattingStats : BattingStats
{
    [Display(Name = "月份")]
    public int Month { get; set; }

    [Display(Name = "受傷")]
    public bool IsInjured { get; set; }
}