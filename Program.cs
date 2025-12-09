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

// 只進行資料庫 Migration（建立表結構），不自動匯入資料
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BaseballDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        // 自動套用 pending migrations（建立表結構）
        await context.Database.MigrateAsync();
        Console.WriteLine("✓ 資料庫 Migration 完成");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "資料庫 Migration 執行時發生錯誤");
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
