using System.Collections.Generic;
using BaseballApp.Data;
using BaseballApp.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace BaseballApp.Models;

public class PlayerDetailViewModel
{
    public PlayerDetailViewModel()
    {
        
    }
    public string SeasonId { get; set; }
    public Batter Player { get; set; }
    public Stats Stats { get; set; }
    public List<SelectListItem> SeriesList { get; set; }
}

public class Stats
{
    /// <summary>
    /// 總打席數
    /// </summary>
    public int TotalPAs { get; set; }

    /// <summary>
    /// 全壘打
    /// </summary>
    public int HomeRuns { get; set; }

    public List<GameStat> GameStats { get; set; } = new List<GameStat>();
}

public class GameStat
{
    public DateTime Date { get; set; }
    public int Seq { get; set; }
    public int PA { get; set; }
    public int H { get; set; }
    public int HR { get; set; }
    public int RBI { get; set; }
    public int SO { get; set; }
    public int BB { get; set; }
}