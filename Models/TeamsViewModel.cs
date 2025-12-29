using BaseballApp.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BaseballApp.Models;

/// <summary>
/// 球隊列表頁面 ViewModel
/// </summary>
public class TeamsViewModel
{
    /// <summary>
    /// 賽季識別碼
    /// </summary>
    public string SeasonId { get; set; } = "ALL";

    /// <summary>
    /// 賽季下拉選單項目
    /// </summary>
    public List<SelectListItem> SeasonOptions { get; set; } = [];

    /// <summary>
    /// 球隊卡片列表
    /// </summary>
    public List<TeamCardViewModel> Teams { get; set; } = new();

    /// <summary>
    /// 勝率與勝場變化圖表資料
    /// </summary>
    public WinRateChartData ChartData { get; set; } = new();

    /// <summary>
    /// 全聯盟戰績表格
    /// </summary>
    public List<TeamStandingViewModel> Standings { get; set; } = new();
}

/// <summary>
/// 球隊卡片 ViewModel
/// </summary>
public class TeamCardViewModel
{
    /// <summary>
    /// 球隊資訊
    /// </summary>
    public Team Team { get; set; } = new();

    /// <summary>
    /// 比賽場次
    /// </summary>
    public int Games { get; set; }

    /// <summary>
    /// 勝場
    /// </summary>
    public int Wins { get; set; }

    /// <summary>
    /// 敗場
    /// </summary>
    public int Losses { get; set; }

    /// <summary>
    /// 和局
    /// </summary>
    public int Ties { get; set; }

    /// <summary>
    /// 勝率
    /// </summary>
    public decimal WinRate { get; set; }

    /// <summary>
    /// 排名
    /// </summary>
    public int Rank { get; set; }
}

/// <summary>
/// 勝率與勝場變化圖表資料
/// </summary>
public class WinRateChartData
{
    /// <summary>
    /// 日期列表 (X軸)
    /// </summary>
    public List<string> Dates { get; set; } = new();

    /// <summary>
    /// 各球隊資料
    /// </summary>
    public List<TeamChartData> Teams { get; set; } = new();
}

/// <summary>
/// 單隊圖表資料
/// </summary>
public class TeamChartData
{
    /// <summary>
    /// 球隊名稱
    /// </summary>
    public string TeamName { get; set; } = string.Empty;

    /// <summary>
    /// 球隊ID
    /// </summary>
    public string TeamId { get; set; } = string.Empty;

    /// <summary>
    /// 累積勝場數序列
    /// </summary>
    public List<int> Wins { get; set; } = new();

    /// <summary>
    /// 勝率序列
    /// </summary>
    public List<decimal> WinRates { get; set; } = new();
}

/// <summary>
/// 球隊戰績 ViewModel
/// </summary>
public class TeamStandingViewModel
{
    /// <summary>
    /// 排名
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// 球隊名稱
    /// </summary>
    public string TeamName { get; set; } = string.Empty;

    /// <summary>
    /// 球隊ID
    /// </summary>
    public string TeamId { get; set; } = string.Empty;

    /// <summary>
    /// 出賽數
    /// </summary>
    public int Games { get; set; }

    /// <summary>
    /// 勝場
    /// </summary>
    public int Wins { get; set; }

    /// <summary>
    /// 敗場
    /// </summary>
    public int Losses { get; set; }

    /// <summary>
    /// 和局
    /// </summary>
    public int Ties { get; set; }

    /// <summary>
    /// 勝率
    /// </summary>
    public decimal WinRate { get; set; }

    /// <summary>
    /// 勝差
    /// </summary>
    public decimal GamesBehind { get; set; }
}