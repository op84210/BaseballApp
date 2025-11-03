using BaseballApp.Data;
using BaseballApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BaseballApp.Services;

public interface IBaseballDbService
{
    Task<IEnumerable<Game>> GetGamesAsync(string? seasonId = null);
    Task<IEnumerable<BatterBox>> GetBatterBoxAsync(string? playerId = null, string? seasonId = null);
    Task<IEnumerable<PitcherBox>> GetPitcherBoxAsync(string? playerId = null, string? seasonId = null);
    Task<IEnumerable<PA>> GetPAAsync(string? batterId = null, int? gameSeq = null);
    Task<IEnumerable<Event>> GetEventsAsync(int paId);
    Task<BattingStats> CalculateBattingStatsAsync(string playerId, string? seasonId = null);
    Task<IEnumerable<Batter>> GetAllBattersAsync(string? seasonId = null);
    Task<IEnumerable<Pitcher>> GetAllPitchersAsync(string? seasonId = null);
    Task<IEnumerable<Team>> GetAllTeamsAsync();
    Task<IEnumerable<Stadium>> GetAllStadiumsAsync();
    Task<Dictionary<string, string>> GetBatterNameMapAsync();
    Task<Dictionary<string, string>> GetPitcherNameMapAsync();
    Task<IEnumerable<BattingStats>> GetTopBattersAsync(string? seasonId = null, int topN = 10);
    Task<IEnumerable<PA>> GetPAsByGameAsync(string seasonId, int gameSeq);
}

public class BaseballDbService : IBaseballDbService
{
    private readonly BaseballDbContext _context;
    private readonly ILogger<BaseballDbService> _logger;

    public BaseballDbService(BaseballDbContext context, ILogger<BaseballDbService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Game>> GetGamesAsync(string? seasonId = null)
    {
        try
        {
            var query = _context.Games.AsQueryable();

            if (!string.IsNullOrEmpty(seasonId))
            {
                query = query.Where(g => g.SeasonId == seasonId);
            }

            return await query
                .OrderBy(g => g.Date)
                .ThenBy(g => g.Seq)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀?��?賽�??��??��??�誤");
            return Enumerable.Empty<Game>();
        }
    }

    public async Task<IEnumerable<BatterBox>> GetBatterBoxAsync(string? playerId = null, string? seasonId = null)
    {
        try
        {
            var query = _context.BatterBoxes.AsQueryable();

            if (!string.IsNullOrEmpty(playerId))
            {
                query = query.Where(bb => bb.PlayerId == playerId);
            }

            if (!string.IsNullOrEmpty(seasonId))
            {
                query = query.Where(bb => bb.SeasonId == seasonId);
            }

            return await query
                .OrderBy(bb => bb.GameSeq)
                .ThenBy(bb => bb.Order)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀?��??�Box資�??�發?�錯�?);
            return Enumerable.Empty<BatterBox>();
        }
    }

    public async Task<IEnumerable<PitcherBox>> GetPitcherBoxAsync(string? playerId = null, string? seasonId = null)
    {
        try
        {
            var query = _context.PitcherBoxes.AsQueryable();

            if (!string.IsNullOrEmpty(playerId))
            {
                query = query.Where(pb => pb.PlayerId == playerId);
            }

            if (!string.IsNullOrEmpty(seasonId))
            {
                query = query.Where(pb => pb.SeasonId == seasonId);
            }

            return await query
                .OrderBy(pb => pb.GameSeq)
                .ThenBy(pb => pb.Order)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀?��??�Box資�??�發?�錯�?);
            return Enumerable.Empty<PitcherBox>();
        }
    }

    public async Task<IEnumerable<PA>> GetPAAsync(string? batterId = null, int? gameSeq = null)
    {
        try
        {
            var query = _context.PAs.AsQueryable();

            if (!string.IsNullOrEmpty(batterId))
            {
                query = query.Where(pa => pa.BatterId == batterId);
            }

            if (gameSeq.HasValue)
            {
                query = query.Where(pa => pa.GameSeq == gameSeq.Value);
            }

            return await query
                .OrderBy(pa => pa.GameSeq)
                .ThenBy(pa => pa.Inning)
                .ThenBy(pa => pa.PaSeq)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀?��?席�??��??��??�誤");
            return Enumerable.Empty<PA>();
        }
    }

    public async Task<IEnumerable<Event>> GetEventsAsync(int paId)
    {
        try
        {
            return await _context.Events
                .Where(e => e.PaId == paId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取事件資料時發生錯誤，paId={PaId}", paId);
            return Enumerable.Empty<Event>();
        }
    }

    public async Task<BattingStats> CalculateBattingStatsAsync(string playerId, string? seasonId = null)
    {
        try
        {
            var batterBoxes = await GetBatterBoxAsync(playerId, seasonId);

            var stats = batterBoxes.Aggregate(new BattingStats
            {
                PlayerId = playerId
            }, (acc, bb) =>
            {
                acc.PA += bb.PA ?? 0;
                acc.AB += bb.AB ?? 0;
                acc.R += bb.R ?? 0;
                acc.H += bb.H ?? 0;
                acc.RBI += bb.RBI ?? 0;
                acc.TwoB += bb.TwoB ?? 0;
                acc.ThreeB += bb.ThreeB ?? 0;
                acc.HR += bb.HR ?? 0;
                acc.BB += bb.BB ?? 0;
                acc.SO += bb.SO ?? 0;
                return acc;
            });

            // 計�?衍�?統�?
            if (stats.AB > 0)
            {
                stats.AVG = (double)stats.H / stats.AB;
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "計�??��?統�??�發?�錯誤�?playerId={PlayerId}", playerId);
            return new BattingStats { PlayerId = playerId };
        }
    }

    public async Task<IEnumerable<Batter>> GetAllBattersAsync(string? seasonId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(seasonId))
            {
                return await _context.Batters.ToListAsync();
            }

            // ?��?賽季篩選：�?得該賽季?�出賽�??�員
            var batterIds = await _context.BatterBoxes
                .Where(bb => bb.SeasonId == seasonId)
                .Select(bb => bb.PlayerId)
                .Distinct()
                .ToListAsync();

            return await _context.Batters
                .Where(b => batterIds.Contains(b.PlayerId))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀?��??��??��??��??�誤");
            return Enumerable.Empty<Batter>();
        }
    }

    public async Task<IEnumerable<Pitcher>> GetAllPitchersAsync(string? seasonId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(seasonId))
            {
                return await _context.Pitchers.ToListAsync();
            }

            // ?��?賽季篩選：�?得該賽季?�出賽�??��?
            var pitcherIds = await _context.PitcherBoxes
                .Where(pb => pb.SeasonId == seasonId)
                .Select(pb => pb.PlayerId)
                .Distinct()
                .ToListAsync();

            return await _context.Pitchers
                .Where(p => pitcherIds.Contains(p.PlayerId))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀?��??��??��??��??�誤");
            return Enumerable.Empty<Pitcher>();
        }
    }

    public async Task<IEnumerable<Team>> GetAllTeamsAsync()
    {
        try
        {
            return await _context.Teams.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀?��??��??��??��??�誤");
            return Enumerable.Empty<Team>();
        }
    }

    public async Task<IEnumerable<Stadium>> GetAllStadiumsAsync()
    {
        try
        {
            return await _context.Stadiums.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀?�場?��??��??��??�誤");
            return Enumerable.Empty<Stadium>();
        }
    }

    public async Task<Dictionary<string, string>> GetBatterNameMapAsync()
    {
        try
        {
            return await _context.Batters
                .ToDictionaryAsync(b => b.PlayerId, b => b.PlayerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建�??�員?�稱對照表�??��??�誤");
            return new Dictionary<string, string>();
        }
    }

    public async Task<Dictionary<string, string>> GetPitcherNameMapAsync()
    {
        try
        {
            return await _context.Pitchers
                .ToDictionaryAsync(p => p.PlayerId, p => p.PlayerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建�??��??�稱對照表�??��??�誤");
            return new Dictionary<string, string>();
        }
    }

    public async Task<IEnumerable<BattingStats>> GetTopBattersAsync(string? seasonId = null, int topN = 10)
    {
        try
        {
            var query = _context.BatterBoxes.AsQueryable();

            if (!string.IsNullOrEmpty(seasonId))
            {
                query = query.Where(bb => bb.SeasonId == seasonId);
            }

            var topBatters = await query
                .GroupBy(bb => bb.PlayerId)
                .Select(g => new BattingStats
                {
                    PlayerId = g.Key,
                    PA = g.Sum(bb => bb.PA ?? 0),
                    AB = g.Sum(bb => bb.AB ?? 0),
                    R = g.Sum(bb => bb.R ?? 0),
                    H = g.Sum(bb => bb.H ?? 0),
                    RBI = g.Sum(bb => bb.RBI ?? 0),
                    TwoB = g.Sum(bb => bb.TwoB ?? 0),
                    ThreeB = g.Sum(bb => bb.ThreeB ?? 0),
                    HR = g.Sum(bb => bb.HR ?? 0),
                    BB = g.Sum(bb => bb.BB ?? 0),
                    SO = g.Sum(bb => bb.SO ?? 0)
                })
                .OrderByDescending(s => s.H)
                .Take(topN)
                .ToListAsync();

            // 計�??��???
            foreach (var stats in topBatters)
            {
                if (stats.AB > 0)
                {
                    stats.AVG = (double)stats.H / stats.AB;
                }
            }

            // 載入?�員?�稱
            var playerIds = topBatters.Select(s => s.PlayerId).ToList();
            var batters = await _context.Batters
                .Where(b => playerIds.Contains(b.PlayerId))
                .ToDictionaryAsync(b => b.PlayerId, b => b.PlayerName);

            foreach (var stats in topBatters)
            {
                if (batters.TryGetValue(stats.PlayerId, out var name))
                {
                    stats.PlayerName = name;
                }
            }

            return topBatters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "?��??��??�者�??��??��??�誤");
            return Enumerable.Empty<BattingStats>();
        }
    }

    public async Task<IEnumerable<PA>> GetPAsByGameAsync(string seasonId, int gameSeq)
    {
        try
        {
            return await _context.PAs
                .Where(pa => pa.SeasonId == seasonId && pa.GameSeq == gameSeq)
                .OrderBy(pa => pa.Inning)
                .ThenBy(pa => pa.PaSeq)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀?��?賽�?席�??��??��??�誤，seasonId={SeasonId}, gameSeq={GameSeq}", seasonId, gameSeq);
            return Enumerable.Empty<PA>();
        }
    }
}
