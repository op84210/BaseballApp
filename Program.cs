var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddControllers(); // 加入 API 控制器

// 註冊 HTTP 客戶端
builder.Services.AddHttpClient();

// 註冊棒球數據服務
builder.Services.AddScoped<BaseballApp.Services.IBaseballDataService, BaseballApp.Services.BaseballDataService>();

var app = builder.Build();

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
    pattern: "{controller=Home}/{action=Charts}/{id?}")
    .WithStaticAssets();

app.MapControllers(); // 映射 /api/*

app.Run();
