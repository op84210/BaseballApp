using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BaseballApp.Data;

namespace BaseballApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataMigrationController : ControllerBase
    {
        private readonly BaseballDbContext _context;
        private readonly ILogger<DataMigrationController> _logger;
        private readonly IConfiguration _configuration;

        public DataMigrationController(BaseballDbContext context, ILogger<DataMigrationController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// 清空並重建資料庫（從 SQLite 遷移到 PostgreSQL）
        /// </summary>
        [HttpPost("rebuild")]
        public async Task<IActionResult> RebuildDatabase()
        {
            try
            {
                _logger.LogInformation("開始清空並重建資料庫...");
                Console.WriteLine("🔄 清空並重建資料庫...");

                var databaseType = _configuration.GetValue<string>("DatabaseType") ?? "SQLite";

                if (databaseType.ToUpper() != "POSTGRESQL")
                {
                    return BadRequest(new { message = "此功能僅適用於 PostgreSQL 資料庫" });
                }

                // 清空所有資料表（按相反順序以避免外鍵約束問題）
                await ClearAllTables();
                Console.WriteLine("✓ 已清空所有資料表");

                // 重建資料（從 SQLite 匯入）
                var sqlitePath = Path.Combine(AppContext.BaseDirectory, "data", "baseball.db");
                if (!System.IO.File.Exists(sqlitePath))
                {
                    return NotFound(new { message = $"找不到 SQLite 資料庫: {sqlitePath}" });
                }

                await MigrateDataFromSQLite(sqlitePath);
                Console.WriteLine("✓ 資料遷移完成");

                // 返回資料統計
                var stats = await GetDatabaseStats();
                return Ok(new
                {
                    message = "資料庫重建成功",
                    stats = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料庫重建失敗");
                return StatusCode(500, new { message = "資料庫重建失敗", error = ex.Message });
            }
        }

        /// <summary>
        /// 只清空資料庫（保留表結構）
        /// </summary>
        [HttpPost("clear")]
        public async Task<IActionResult> ClearDatabase()
        {
            try
            {
                _logger.LogInformation("開始清空資料庫...");
                Console.WriteLine("🗑️ 清空資料庫...");

                await ClearAllTables();
                Console.WriteLine("✓ 已清空所有資料表");

                return Ok(new { message = "資料庫已清空" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空資料庫失敗");
                return StatusCode(500, new { message = "清空資料庫失敗", error = ex.Message });
            }
        }

        /// <summary>
        /// 獲取資料庫統計
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = await GetDatabaseStats();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取統計失敗");
                return StatusCode(500, new { message = "獲取統計失敗", error = ex.Message });
            }
        }

        // 私有方法

        private async Task ClearAllTables()
        {
            // 清空順序很重要（按外鍵依賴關係）
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblEvent\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblRunner\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblScore\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblBatterBox\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblPitcherBox\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblPA\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblGame\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblBattingRankingCache\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblPitchingRankingCache\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblTeamGameStats\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblTeamSeasonRankingCache\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblPlayerTeam\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblBatter\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblPitcher\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblStadium\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblTeam\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblSeason\" CASCADE");

            // 清空代碼表
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblCodeBases\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblCodePitchCode\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblCodeEventType\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblCodePitchType\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblCodeRunnerType\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblCodeResult\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblCodeTrajectory\" CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"tblCodeHardness\" CASCADE");
        }

        private async Task MigrateDataFromSQLite(string sqlitePath)
        {
            var sqliteConnStr = $"Data Source={sqlitePath}";
            var sqliteOptions = new DbContextOptionsBuilder<BaseballDbContext>()
                .UseSqlite(sqliteConnStr)
                .Options;

            using var sqliteContext = new BaseballDbContext(sqliteOptions);

            try
            {
                // 主檔資料
                await CopyTable("Seasons", sqliteContext, _context, ctx => ctx.Seasons);
                await CopyTable("Teams", sqliteContext, _context, ctx => ctx.Teams);
                await CopyTable("Stadiums", sqliteContext, _context, ctx => ctx.Stadiums);
                await CopyTable("Batters", sqliteContext, _context, ctx => ctx.Batters);
                await CopyTable("Pitchers", sqliteContext, _context, ctx => ctx.Pitchers);
                await CopyTable("PlayerTeams", sqliteContext, _context, ctx => ctx.PlayerTeams);

                // 比賽資料（分批）
                await CopyTableBatched("Games", sqliteContext, _context, ctx => ctx.Games);
                await CopyTableBatched("Scores", sqliteContext, _context, ctx => ctx.Scores);
                await CopyTableBatched("BatterBoxes", sqliteContext, _context, ctx => ctx.BatterBoxes);
                await CopyTableBatched("PitcherBoxes", sqliteContext, _context, ctx => ctx.PitcherBoxes);
                await CopyTableBatched("PAs", sqliteContext, _context, ctx => ctx.PAs);
                await CopyTableBatched("Events", sqliteContext, _context, ctx => ctx.Events);
                await CopyTableBatched("Runners", sqliteContext, _context, ctx => ctx.Runners);

                // 快取資料
                await CopyTableBatched("BattingRankingCaches", sqliteContext, _context, ctx => ctx.BattingRankingCaches);
                await CopyTableBatched("PitchingRankingCaches", sqliteContext, _context, ctx => ctx.PitchingRankingCaches);

                // 新增的表
                await CopyTableBatched("TeamGameStats", sqliteContext, _context, ctx => ctx.TeamGameStats);
                await CopyTableBatched("TeamSeasonRankingCaches", sqliteContext, _context, ctx => ctx.TeamSeasonRankingCaches);

                // 代碼表
                await CopyTable("CodeBases", sqliteContext, _context, ctx => ctx.CodeBases);
                await CopyTable("CodePitchCodes", sqliteContext, _context, ctx => ctx.CodePitchCodes);
                await CopyTable("CodeEventTypes", sqliteContext, _context, ctx => ctx.CodeEventTypes);
                await CopyTable("CodePitchTypes", sqliteContext, _context, ctx => ctx.CodePitchTypes);
                await CopyTable("CodeRunnerTypes", sqliteContext, _context, ctx => ctx.CodeRunnerTypes);
                await CopyTable("CodeResults", sqliteContext, _context, ctx => ctx.CodeResults);
                await CopyTable("CodeTrajectories", sqliteContext, _context, ctx => ctx.CodeTrajectories);
                await CopyTable("CodeHardnesses", sqliteContext, _context, ctx => ctx.CodeHardnesses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料遷移失敗");
                throw;
            }
        }

        private async Task CopyTable<T>(string tableName, BaseballDbContext sourceCtx, BaseballDbContext destCtx,
            Func<BaseballDbContext, DbSet<T>> getDbSet) where T : class
        {
            var data = await getDbSet(sourceCtx).AsNoTracking().ToListAsync();
            if (data.Any())
            {
                ConvertDateTimesToUtc(data);
                getDbSet(destCtx).AddRange(data);
                await destCtx.SaveChangesAsync();
                _logger.LogInformation($"  ✓ {tableName}: {data.Count} rows");
            }
        }

        private async Task CopyTableBatched<T>(string tableName, BaseballDbContext sourceCtx, BaseballDbContext destCtx,
            Func<BaseballDbContext, DbSet<T>> getDbSet) where T : class
        {
            var data = await getDbSet(sourceCtx).AsNoTracking().ToListAsync();
            if (data.Any())
            {
                ConvertDateTimesToUtc(data);

                const int batchSize = 500;

                for (int i = 0; i < data.Count; i += batchSize)
                {
                    var batch = data.Skip(i).Take(batchSize).ToList();
                    getDbSet(destCtx).AddRange(batch);
                    await destCtx.SaveChangesAsync();
                    destCtx.ChangeTracker.Clear();
                }

                _logger.LogInformation($"  ✓ {tableName}: {data.Count} rows");
            }
        }

        private void ConvertDateTimesToUtc<T>(List<T> entities) where T : class
        {
            foreach (var entity in entities)
            {
                var properties = typeof(T).GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.PropertyType == typeof(DateTime))
                    {
                        var value = (DateTime?)prop.GetValue(entity);
                        if (value.HasValue && value.Value.Kind != DateTimeKind.Utc)
                        {
                            var utcValue = value.Value.Kind == DateTimeKind.Unspecified
                                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                                : value.Value.ToUniversalTime();
                            prop.SetValue(entity, utcValue);
                        }
                    }
                    else if (prop.PropertyType == typeof(DateTime?))
                    {
                        var value = (DateTime?)prop.GetValue(entity);
                        if (value.HasValue && value.Value.Kind != DateTimeKind.Utc)
                        {
                            var utcValue = value.Value.Kind == DateTimeKind.Unspecified
                                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                                : value.Value.ToUniversalTime();
                            prop.SetValue(entity, utcValue);
                        }
                    }
                }
            }
        }

        private async Task<object> GetDatabaseStats()
        {
            return new
            {
                seasons = await _context.Seasons.CountAsync(),
                teams = await _context.Teams.CountAsync(),
                stadiums = await _context.Stadiums.CountAsync(),
                batters = await _context.Batters.CountAsync(),
                pitchers = await _context.Pitchers.CountAsync(),
                playerTeams = await _context.PlayerTeams.CountAsync(),
                games = await _context.Games.CountAsync(),
                scores = await _context.Scores.CountAsync(),
                batterBoxes = await _context.BatterBoxes.CountAsync(),
                pitcherBoxes = await _context.PitcherBoxes.CountAsync(),
                pas = await _context.PAs.CountAsync(),
                events = await _context.Events.CountAsync(),
                runners = await _context.Runners.CountAsync(),
                battingRankingCaches = await _context.BattingRankingCaches.CountAsync(),
                pitchingRankingCaches = await _context.PitchingRankingCaches.CountAsync(),
                teamGameStats = await _context.TeamGameStats.CountAsync(),
                teamSeasonRankingCaches = await _context.TeamSeasonRankingCaches.CountAsync()
            };
        }
    }
}
