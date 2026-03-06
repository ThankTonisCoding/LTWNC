using FinancialPlatform.Infrastructure.Services;
using FinancialPlatform.WebUI.Workers;
using FinancialPlatform.WebUI.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// 3. Đăng ký SignalR
builder.Services.AddSignalR();

// 1. Đăng ký HttpClient cho BinanceService
builder.Services.AddHttpClient<BinanceService>();

// 2. Đăng ký Worker Service chạy ngầm
builder.Services.AddHostedService<MarketDataWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapHub<MarketHub>("/marketHub"); // Endpoint cho SignalR

app.Run();
// Test update 2026