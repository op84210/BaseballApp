using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BaseballApp.Models;

public class PlayerDetailViewModel{
    public string SeasonId { get; set; } = "ALL";
    public BatterDetailModel? BatterDetail { get; set; } = null;
    public PitcherDetailModel? PitcherDetail { get; set; } = null;
    public List<SelectListItem> SeriesList { get; set; } = [];
}
public class BatterDetailModel
{
    public Batter Batter { get; set; } = new Batter();
    public BatterStats Stats { get; set; } = new BatterStats();
}

public class PitcherDetailModel
{
    public Pitcher Pitcher { get; set; } = new Pitcher();
    public PitcherStats Stats { get; set; } = new PitcherStats();
}

/// <summary>
/// 球員打擊數據統計
/// </summary>
public class BatterStats
{
    /// <summary>
    /// 出賽數
    /// </summary>
    public int Games
    {
        get
        {
            return GameStats.Count;
        }
    }

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

    /// <summary>
    /// 打擊率
    /// </summary>
    public decimal BattingAverage
    {
        get
        {
            int totalAB = GameStats.Sum(gs => gs.AB);
            if (totalAB == 0) return 0;

            return Math.Round((decimal)Hits / totalAB, 3);
        }
    }

    /// <summary>
    /// 上壘率
    /// </summary>
    public decimal OnBasePercentage
    {
        get
        {
            int totalAB = GameStats.Sum(gs => gs.AB);
            if (totalAB == 0) return 0;

            int totalBB = Walks;
            int totalHBP = GameStats.Sum(gs => gs.HBP);
            return Math.Round((decimal)(Hits + totalBB + totalHBP) / (totalAB + totalBB + totalHBP), 3);
        }
    }

    /// <summary>
    /// 長打率
    /// </summary>
    public decimal SluggingPercentage
    {
        get
        {
            int totalAB = GameStats.Sum(gs => gs.AB);
            if (totalAB == 0) return 0;

            int totalBases = GameStats.Sum(gs => gs._1B + 2 * gs._2B + 3 * gs._3B + 4 * gs.HR);
            return Math.Round((decimal)totalBases / totalAB, 3);
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

            return Math.Round((decimal)TotalPAs / GameStats.Count, 3);
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
            return Math.Round((decimal)Hits / GameStats.Count, 3);
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
            return Math.Round((decimal)TotalPAs / HomeRuns, 3);
        }
    }

    /// <summary>
    /// 比賽數據列表
    /// </summary>
    public List<BatterGameStat> GameStats { get; set; } = [];

    /// <summary>
    /// 最佳打席列表(依 WPA 排序)
    /// </summary>
    public List<BestPA> BestPAs { get; set; } = [];
}

/// <summary>
/// 打擊數據統計
/// </summary>
public class BatterGameStat
{
    /// <summary>
    /// 比賽日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 賽季名稱
    /// </summary>
    public string SeasonName { get; set; } = string.Empty;

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
            return _1B + _2B + _3B + HR + IHR;
        }
    }

    /// <summary>
    /// 打數
    /// </summary>
    public int AB 
    {
        get
        {
            // 打數不包含：保送、觸身球、犧牲觸擊、高飛犧牲打、妨礙打擊 (IH)、不算打席 (IGNORE)
            return PA - BB - HBP - SH - SF - IH - IGNORE;
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
    /// 賽季名稱
    /// </summary>
    public string SeasonName { get; set; } = string.Empty;

    /// <summary>
    /// 比賽序號
    /// </summary>
    public int Seq { get; set; }

    /// <summary>
    /// 對手
    /// </summary>
    public string Opponent { get; set; } = string.Empty;

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

/// <summary>
/// 投手數據統計
/// </summary>
public class PitcherStats
{
    /// <summary>
    /// 出賽數
    /// </summary>
    public int Games
    {
        get
        {
            return GameStats.Count;
        }
    }

    /// <summary>
    /// 總投球局數（出局數 / 3）
    /// </summary>
    public decimal TotalIP
    {
        get
        {
            int totalOuts = GameStats.Sum(gs => gs.IPOuts);
            return (decimal)(totalOuts / 3) + (decimal)(totalOuts % 3) / 10m;
        }
    }

    /// <summary>
    /// 總投球出局數
    /// </summary>
    public int TotalIPOuts
    {
        get
        {
            return GameStats.Sum(gs => gs.IPOuts);
        }
    }

    /// <summary>
    /// 面對打席數
    /// </summary>
    public int TotalBF
    {
        get
        {
            return GameStats.Sum(gs => gs.BF);
        }
    }

    /// <summary>
    /// 被安打數
    /// </summary>
    public int HitsAllowed
    {
        get
        {
            return GameStats.Sum(gs => gs.H);
        }
    }

    /// <summary>
    /// 被全壘打數
    /// </summary>
    public int HomeRunsAllowed
    {
        get
        {
            return GameStats.Sum(gs => gs.HR);
        }
    }

    /// <summary>
    /// 四壞球數
    /// </summary>
    public int Walks
    {
        get
        {
            return GameStats.Sum(gs => gs.BB);
        }
    }

    /// <summary>
    /// 故意四壞
    /// </summary>
    public int IntentionalWalks
    {
        get
        {
            return GameStats.Sum(gs => gs.IBB);
        }
    }

    /// <summary>
    /// 觸身球數
    /// </summary>
    public int HitBatters
    {
        get
        {
            return GameStats.Sum(gs => gs.HBP);
        }
    }

    /// <summary>
    /// 三振數
    /// </summary>
    public int Strikeouts
    {
        get
        {
            return GameStats.Sum(gs => gs.SO);
        }
    }

    /// <summary>
    /// 失分數
    /// </summary>
    public int RunsAllowed
    {
        get
        {
            return GameStats.Sum(gs => gs.R);
        }
    }

    /// <summary>
    /// 自責分數
    /// </summary>
    public int EarnedRuns
    {
        get
        {
            return GameStats.Sum(gs => gs.ER);
        }
    }

    /// <summary>
    /// 用球數
    /// </summary>
    public int TotalPitches
    {
        get
        {
            return GameStats.Sum(gs => gs.NP);
        }
    }

    /// <summary>
    /// 防禦率 (ERA)
    /// </summary>
    public decimal ERA
    {
        get
        {
            if (TotalIPOuts == 0) return 0;
            return Math.Round((decimal)EarnedRuns * 27 / TotalIPOuts, 2);
        }
    }

    /// <summary>
    /// 每局被上壘率 (WHIP)
    /// </summary>
    public decimal WHIP
    {
        get
        {
            if (TotalIPOuts == 0) return 0;
            return Math.Round((decimal)(HitsAllowed + Walks) * 3 / TotalIPOuts, 2);
        }
    }

    /// <summary>
    /// 每九局三振率 (K/9)
    /// </summary>
    public decimal K9
    {
        get
        {
            if (TotalIPOuts == 0) return 0;
            return Math.Round((decimal)Strikeouts * 27 / TotalIPOuts, 2);
        }
    }

    /// <summary>
    /// 每九局保送率 (BB/9)
    /// </summary>
    public decimal BB9
    {
        get
        {
            if (TotalIPOuts == 0) return 0;
            return Math.Round((decimal)Walks * 27 / TotalIPOuts, 2);
        }
    }

    /// <summary>
    /// 三振保送比 (K/BB)
    /// </summary>
    public decimal KBBRatio
    {
        get
        {
            if (Walks == 0) return Strikeouts;
            return Math.Round((decimal)Strikeouts / Walks, 2);
        }
    }

    /// <summary>
    /// 平均每場投球局數
    /// </summary>
    public decimal AverageIPPerGame
    {
        get
        {
            if (Games == 0) return 0;
            return Math.Round(TotalIP / Games, 2);
        }
    }

    /// <summary>
    /// 平均每局用球數
    /// </summary>
    public decimal AveragePitchesPerInning
    {
        get
        {
            if (TotalIPOuts == 0) return 0;
            var innings = (decimal)TotalIPOuts / 3m;
            return Math.Round((decimal)TotalPitches / innings, 1);
        }
    }

    /// <summary>
    /// 被打擊率 (對手打擊率)
    /// </summary>
    public decimal OpponentBAA
    {
        get
        {
            // AB = BF - BB - HBP - SF (簡化計算，假設SF很少)
            int opponentAB = TotalBF - Walks - HitBatters;
            if (opponentAB <= 0) return 0;
            return Math.Round((decimal)HitsAllowed / opponentAB, 3);
        }
    }

    /// <summary>
    /// 比賽數據列表
    /// </summary>
    public List<PitcherGameStat> GameStats { get; set; } = [];

    /// <summary>
    /// 最佳投球表現列表
    /// </summary>
    public List<BestPitchingPerformance> BestPerformances { get; set; } = [];
}

/// <summary>
/// 投手單場比賽數據
/// </summary>
public class PitcherGameStat
{
    /// <summary>
    /// 比賽日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 賽季名稱
    /// </summary>
    public string SeasonName { get; set; } = string.Empty;

    /// <summary>
    /// 比賽序號
    /// </summary>
    public int Seq { get; set; }

    /// <summary>
    /// 投球局數（出局數）
    /// </summary>
    public int IPOuts { get; set; }

    /// <summary>
    /// 投球局數（格式化：例如 6.1 = 6又1/3局）
    /// </summary>
    public decimal IP
    {
        get
        {
            return (decimal)(IPOuts / 3) + (decimal)(IPOuts % 3) / 10m;
        }
    }

    /// <summary>
    /// 用球數
    /// </summary>
    public int NP { get; set; }

    /// <summary>
    /// 面對打席數
    /// </summary>
    public int BF { get; set; }

    /// <summary>
    /// 被安打數
    /// </summary>
    public int H { get; set; }

    /// <summary>
    /// 被全壘打數
    /// </summary>
    public int HR { get; set; }

    /// <summary>
    /// 四壞球數
    /// </summary>
    public int BB { get; set; }

    /// <summary>
    /// 故意四壞
    /// </summary>
    public int IBB { get; set; }

    /// <summary>
    /// 觸身球數
    /// </summary>
    public int HBP { get; set; }

    /// <summary>
    /// 三振數
    /// </summary>
    public int SO { get; set; }

    /// <summary>
    /// 失分數
    /// </summary>
    public int R { get; set; }

    /// <summary>
    /// 自責分數
    /// </summary>
    public int ER { get; set; }

    /// <summary>
    /// 防禦率
    /// </summary>
    public decimal ERA
    {
        get
        {
            if (IPOuts == 0) return 0;
            return Math.Round((decimal)ER * 27 / IPOuts, 2);
        }
    }

    /// <summary>
    /// WHIP
    /// </summary>
    public decimal WHIP
    {
        get
        {
            if (IPOuts == 0) return 0;
            return Math.Round((decimal)(H + BB) * 3 / IPOuts, 2);
        }
    }

    /// <summary>
    /// 對手
    /// </summary>
    public required string Opponent { get; set; }

    /// <summary>
    /// 是否主場
    /// </summary>
    public bool IsHome { get; set; }

    /// <summary>
    /// 是否先發
    /// </summary>
    public bool IsStarter { get; set; }
}

/// <summary>
/// 最佳投球表現
/// </summary>
public class BestPitchingPerformance
{
    /// <summary>
    /// 比賽日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 賽季名稱
    /// </summary>
    public string SeasonName { get; set; } = string.Empty;

    /// <summary>
    /// 比賽序號
    /// </summary>
    public int Seq { get; set; }

    /// <summary>
    /// 投球局數
    /// </summary>
    public decimal IP { get; set; }

    /// <summary>
    /// 三振數
    /// </summary>
    public int SO { get; set; }

    /// <summary>
    /// 防禦率
    /// </summary>
    public decimal ERA { get; set; }

    /// <summary>
    /// 對手
    /// </summary>
    public string Opponent { get; set; } = string.Empty;

    /// <summary>
    /// 評分指標（用於排序）
    /// </summary>
    public decimal Score { get; set; }
}