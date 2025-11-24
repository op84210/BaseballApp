using System.Collections.Generic;
using BaseballApp.Data;
using BaseballApp.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace BaseballApp.Models;

public class PlayerDetailViewModel
{
    public string SeasonId { get; set; } = "ALL";
    public Batter Player { get; set; } = new Batter();
    public Stats Stats { get; set; } = new Stats();
    public List<SelectListItem> SeriesList { get; set; } = [];
}

/// <summary>
/// 球員數據統計
/// </summary>
public class Stats
{
    /// <summary>
    /// 總打席數
    /// </summary>
    public int TotalPAs
    {
        get
        {
            return GameStats.Sum(gs => gs.PA);
        }
    }

    /// <summary>
    /// 全壘打
    /// </summary>
    public int HomeRuns
    {
        get
        {
            return GameStats.Sum(gs => gs.HR);
        }
    }

    /// <summary>
    /// 安打數
    /// </summary>
    public int Hits
    {
        get
        {
            return GameStats.Sum(gs => gs.H);
        }
    }

    /// <summary>
    /// 打點數
    /// </summary>
    public int RBIs
    {
        get
        {
            return GameStats.Sum(gs => gs.RBI);
        }
    }

    /// <summary>
    /// 三振數
    /// </summary>
    public int StrikeOuts
    {
        get
        {
            return GameStats.Sum(gs => gs.SO);
        }
    }

    /// <summary>
    /// 保送數
    /// </summary>
    public int Walks
    {
        get
        {
            return GameStats.Sum(gs => gs.BB);
        }
    }

    // /// <summary>
    // /// 盜壘成功數
    // /// </summary>
    // public int StolenBases { get; set; }

    // /// <summary>
    // /// 盜壘失敗數
    // /// </summary>
    // public int CaughtStealing { get; set; }

    /// <summary>
    /// 平均每場打席數
    /// </summary>
    public decimal AveragePAsPerGame
    {
        get
        {
            if (GameStats.Count == 0) return 0;
            return (decimal)TotalPAs / GameStats.Count;
        }
    }

    /// <summary>
    /// 平均每場安打數
    /// </summary>
    public decimal AverageHitsPerGame
    {
        get
        {
            if (GameStats.Count == 0) return 0;
            return Hits / GameStats.Count;
        }
    }

    /// <summary>
    /// 平均幾個打席有一支全壘打
    /// </summary>
    public decimal PAsPerHomeRun
    {
        get
        {
            if (HomeRuns == 0) return 0;
            return (decimal)TotalPAs / HomeRuns;
        }
    }

    /// <summary>
    /// 比賽數據列表
    /// </summary>
    public List<GameStat> GameStats { get; set; } = [];

    /// <summary>
    /// 最佳打席列表（依 WPA 排序）
    /// </summary>
    public List<BestPA> BestPAs { get; set; } = [];
}

/// <summary>
/// 比賽數據
/// </summary>
public class GameStat
{
    /// <summary>
    /// 比賽日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 比賽序號
    /// </summary>
    public int Seq { get; set; }

    /// <summary>
    /// 打席數
    /// </summary>
    public int PA { get; set; }

    /// <summary>
    /// 安打數
    /// </summary>
    public int H
    {
        get
        {
            return _1B + _2B + _3B + HR;
        }
    }

    /// <summary>
    /// 打數
    /// </summary>
    public int AB 
    {
        get
        {
            // 打數不包含保送、觸身球、犧牲觸擊、高飛犧牲打、不算打席
            return PA - BB - HBP - SH - SF - IGNORE;
        }
    }

    /// <summary>
    /// 一壘安打數
    /// </summary>
    public int _1B { get; set; }

    /// <summary>
    /// 二壘安打數
    /// </summary>
    public int _2B { get; set; }

    /// <summary>
    /// 三壘安打數
    /// </summary>
    public int _3B { get; set; }

    /// <summary>
    /// 全壘打數
    /// </summary>
    public int HR { get; set; }

    /// <summary>
    /// 場內全壘打數
    /// </summary>
    public int IHR { get; set; }

    /// <summary>
    /// 三振數
    /// </summary>
    public int SO { get; set; }

    /// <summary>
    /// 非保送四壞球數
    /// </summary>
    public int uBB { get; set; }

    /// <summary>
    /// 故意四壞球數
    /// </summary>
    public int IBB { get; set; }

    /// <summary>
    /// 保送數
    /// </summary>
    public int BB
    {
        get
        {
            return uBB + IBB;
        }
    }

    /// <summary>
    /// 觸身球數
    /// </summary>
    public int HBP { get; set; }

    /// <summary>
    /// 滾地球數
    /// </summary>
    public int GO { get; set; }

    /// <summary>
    /// 飛球數
    /// </summary>
    public int FO { get; set; }

    /// <summary>
    /// 野手選擇數
    /// </summary>
    public int FC { get; set; }

    /// <summary>
    /// 失誤數
    /// </summary>
    public int E { get; set; }

    /// <summary>
    /// 犧牲觸擊數
    /// </summary>
    public int SH { get; set; }

    /// <summary>
    /// 高飛犧牲打數
    /// </summary>
    public int SF { get; set; }

    /// <summary>
    /// 滾地雙殺打數
    /// </summary>
    public int GIDP { get; set; }

    /// <summary>
    /// 雙殺打數
    /// </summary>
    public int DP { get; set; }

    /// <summary>
    /// 三殺打數
    /// </summary>
    public int TP { get; set; }

    /// <summary>
    /// 妨礙打擊數
    /// </summary>
    public int IH { get; set; }

    /// <summary>
    /// 妨礙跑壘數
    /// </summary>
    public int IR { get; set; }

    /// <summary>
    /// 妨礙守備數
    /// </summary>
    public int ID { get; set; }

    /// <summary>
    /// 不算打席（壘包跑者出局導致半局結束等）
    /// </summary>
    public int IGNORE { get; set; }

    /// <summary>
    /// 得分數
    /// </summary>
    public int R { get; set; }

    /// <summary>
    /// 打點數
    /// </summary>
    public int RBI { get; set; }

    /// <summary>
    /// 對手
    /// </summary>
    public required string Opponent { get; set; }

    /// <summary>
    /// 是否主場
    /// </summary>
    public bool IsHome { get; set; }
}

/// <summary>
/// 最佳打席數據
/// </summary>
public class BestPA
{
    /// <summary>
    /// 比賽日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 比賽序號
    /// </summary>
    public int Seq { get; set; }

    /// <summary>
    /// 局數
    /// </summary>
    public int Inning { get; set; }

    /// <summary>
    /// 打席序號
    /// </summary>
    public int PASeq { get; set; }

    /// <summary>
    /// 打席結果
    /// </summary>
    public string PAResult { get; set; } = string.Empty;

    /// <summary>
    /// 勝率貢獻值
    /// </summary>
    public decimal? WPA { get; set; }
}