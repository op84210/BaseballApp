using BaseballApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using BaseballApp.Models;

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
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("PostgreSQL"),
                npgsqlOptions => npgsqlOptions.UseRelationalNulls(true)
            ));
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

// 移到獨立函式
//await DbInitializer.ApplyMigrationsAndImportAsync(app.Services);

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


// 將 Migration + 匯入封裝成獨立函式
internal static class DbInitializer
{
    public static async Task ApplyMigrationsAndImportAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = sp.GetRequiredService<BaseballDbContext>();

            // 套用 migrations
            await context.Database.MigrateAsync();
            Console.WriteLine("✓ 資料庫 Migration 完成");

            // 只在目標資料庫為 PostgreSQL 且資料為空時，從 SQLite 匯入
            if (context.Database.IsNpgsql() && !context.Batters.Any())
            {
                var sqlitePath = Path.Combine(AppContext.BaseDirectory, "data", "baseball.db");
                if (File.Exists(sqlitePath))
                {
                    using var sqlite = new SqliteConnection($"Data Source={sqlitePath}");
                    sqlite.Open();

                    using var cmd = sqlite.CreateCommand();
                    cmd.CommandText = "SELECT playerId, playerName FROM tblBatter";

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        context.Batters.Add(new Batter
                        {
                            PlayerId = reader.GetString(0),
                            PlayerName = reader.GetString(1)
                        });
                    }

                    context.SaveChanges();
                    Console.WriteLine("✓ Batters 資料自 SQLite 匯入完成");
                }
                else
                {
                    Console.WriteLine("找不到 baseball.db，未自動匯入 Batters 資料");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "資料庫 Migration 執行時發生錯誤");
            throw;
        }
    }
}
