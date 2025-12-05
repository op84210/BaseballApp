using Microsoft.AspNetCore.Mvc.Rendering;

namespace BaseballApp.Models;

public class PlayersViewModel
{
    /// <summary>
    /// 選中的賽季識別碼
    /// </summary>
    public string? SeasonId { get; set; }

    /// <summary>
    /// 選中的球隊識別碼
    /// </summary>
    public string? TeamId { get; set; }

    /// <summary>
    /// 選中的球員類型 (batter/pitcher)
    /// </summary>
    public string? PlayerType { get; set; }

    /// <summary>
    /// 選手列表
    /// </summary>
    public List<Player> Players { get; set; } = [];

    /// <summary>
    /// 賽季下拉選單項目
    /// </summary>
    public List<SelectListItem> SeasonOptions { get; set; } = [];

    /// <summary>
    /// 球隊下拉選單項目
    /// </summary>
    public List<SelectListItem> TeamOptions { get; set; } = [];

    /// <summary>
    /// 球員類型下拉選單項目
    /// </summary>
    public List<SelectListItem> PlayerTypeOptions { get; set; } =
    [
        new SelectListItem { Value = "", Text = "全部" },
        new SelectListItem { Value = "batter", Text = "野手" },
        new SelectListItem { Value = "pitcher", Text = "投手" }
    ];
}
