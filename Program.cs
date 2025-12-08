using BaseballApp.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 禁用檔案系統監視（解決 Render 部署的 inotify 限制問題）
builder.Configuration.Sources
    .OfType<Microsoft.Extensions.Configuration.Json.JsonConfigurationSource>()
    .ToList()
    .ForEach(s => s.ReloadOnChange = false);

// 禁用物理檔案提供者的檔案監視
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<Microsoft.Extensions.FileProviders.IFileProvider>(
        new Microsoft.Extensions.FileProviders.NullFileProvider());
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddControllers(); // 加入 API 控制器

// 註冊 HTTP 客戶端
builder.Services.AddHttpClient();

// 配置資料庫 - 支援多種資料庫
var databaseType = builder.Configuration.GetValue<string>("DatabaseType") ?? "SQLite";

switch (databaseType.ToUpper())
{
    case "SQLITE":
        builder.Services.AddDbContext<BaseballDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("SQLite")));
        break;
    
    case "SQLSERVER":
        builder.Services.AddDbContext<BaseballDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
        break;
    
    case "POSTGRESQL":
        builder.Services.AddDbContext<BaseballDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));
        break;
    
    default:
        throw new InvalidOperationException($"不支援的資料庫類型: {databaseType}");
}

// 註冊棒球數據服務
builder.Services.AddScoped<BaseballApp.Services.IBaseballDbService, BaseballApp.Services.BaseballDbService>();

// 註冊排行榜快取服務
builder.Services.AddScoped<BaseballApp.Services.IRankingCacheService, BaseballApp.Services.RankingCacheService>();

// 註冊背景服務：定期更新排行榜快取
builder.Services.AddHostedService<BaseballApp.BackgroundServices.RankingCacheUpdateService>();

var app = builder.Build();

// 自動執行資料庫 Migration（部署到雲端時自動建表）
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BaseballDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        // 自動套用 pending migrations（如果資料表不存在會自動建立）
        await context.Database.MigrateAsync();
        Console.WriteLine("✓ 資料庫 Migration 完成");
        
        // 如果是 PostgreSQL 且資料庫為空，自動從 SQLite 匯入資料
        if (databaseType.ToUpper() == "POSTGRESQL")
        {
            var hasData = await context.Seasons.AnyAsync();
            if (!hasData)
            {
                logger.LogInformation("PostgreSQL 資料庫為空，開始從 SQLite 匯入資料...");
                Console.WriteLine("📦 開始資料遷移...");
                
                var sqlitePath = Path.Combine(AppContext.BaseDirectory, "data", "baseball.db");
                if (File.Exists(sqlitePath))
                {
                    await MigrateDataFromSQLite(context, sqlitePath, logger);
                    Console.WriteLine("✓ 資料遷移完成");
                }
                else
                {
                    logger.LogWarning($"找不到 SQLite 資料庫: {sqlitePath}");
                }
            }
            else
            {
                Console.WriteLine("✓ PostgreSQL 已有資料，跳過遷移");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "資料庫 Migration 執行時發生錯誤");
    }
}

// 資料遷移函式
static async Task MigrateDataFromSQLite(BaseballDbContext pgContext, string sqlitePath, ILogger logger)
{
    var sqliteConnStr = $"Data Source={sqlitePath}";
    var sqliteOptions = new DbContextOptionsBuilder<BaseballDbContext>()
        .UseSqlite(sqliteConnStr)
        .Options;
    
    using var sqliteContext = new BaseballDbContext(sqliteOptions);
    
    try
    {
        // 主檔資料
        await CopyTable("Seasons", sqliteContext, pgContext, ctx => ctx.Seasons, logger);
        await CopyTable("Teams", sqliteContext, pgContext, ctx => ctx.Teams, logger);
        await CopyTable("Stadiums", sqliteContext, pgContext, ctx => ctx.Stadiums, logger);
        await CopyTable("Batters", sqliteContext, pgContext, ctx => ctx.Batters, logger);
        await CopyTable("Pitchers", sqliteContext, pgContext, ctx => ctx.Pitchers, logger);
        
        // 比賽資料（分批）
        await CopyTableBatched("Games", sqliteContext, pgContext, ctx => ctx.Games, logger);
        await CopyTableBatched("Scores", sqliteContext, pgContext, ctx => ctx.Scores, logger);
        await CopyTableBatched("BatterBoxes", sqliteContext, pgContext, ctx => ctx.BatterBoxes, logger);
        await CopyTableBatched("PitcherBoxes", sqliteContext, pgContext, ctx => ctx.PitcherBoxes, logger);
        await CopyTableBatched("PAs", sqliteContext, pgContext, ctx => ctx.PAs, logger);
        await CopyTableBatched("Events", sqliteContext, pgContext, ctx => ctx.Events, logger);
        await CopyTableBatched("Runners", sqliteContext, pgContext, ctx => ctx.Runners, logger);
        
        // 快取資料
        await CopyTableBatched("BattingRankingCaches", sqliteContext, pgContext, ctx => ctx.BattingRankingCaches, logger);
        await CopyTableBatched("PitchingRankingCaches", sqliteContext, pgContext, ctx => ctx.PitchingRankingCaches, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "資料遷移失敗");
        throw;
    }
}

static async Task CopyTable<T>(string tableName, BaseballDbContext sourceCtx, BaseballDbContext destCtx, 
    Func<BaseballDbContext, DbSet<T>> getDbSet, ILogger logger) where T : class
{
    var data = await getDbSet(sourceCtx).AsNoTracking().ToListAsync();
    if (data.Any())
    {
        getDbSet(destCtx).AddRange(data);
        await destCtx.SaveChangesAsync();
        logger.LogInformation($"  ✓ {tableName}: {data.Count} rows");
    }
}

static async Task CopyTableBatched<T>(string tableName, BaseballDbContext sourceCtx, BaseballDbContext destCtx,
    Func<BaseballDbContext, DbSet<T>> getDbSet, ILogger logger) where T : class
{
    var data = await getDbSet(sourceCtx).AsNoTracking().ToListAsync();
    if (data.Any())
    {
        const int batchSize = 500;
        
        for (int i = 0; i < data.Count; i += batchSize)
        {
            var batch = data.Skip(i).Take(batchSize).ToList();
            getDbSet(destCtx).AddRange(batch);
            await destCtx.SaveChangesAsync();
            destCtx.ChangeTracker.Clear();
        }
        
        logger.LogInformation($"  ✓ {tableName}: {data.Count} rows");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    //pattern: "{controller=Baseball}/{action=Rankings}/{id?}")
    pattern: "{controller=Baseball}/{action=Players}/{id?}")
    //pattern: "{controller=Baseball}/{action=PlayerDetail}/{playerId=3zbEo}&{seasonId=CPBL-2024-HE}")
    .WithStaticAssets();

app.MapControllers(); // 映射 /api/*

app.Run();
