using FinancialPlatform.Core.Interfaces;
using FinancialPlatform.Infrastructure.Data;
using FinancialPlatform.Infrastructure.Services;
using FinancialPlatform.WebUI.Workers;
using FinancialPlatform.WebUI.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Đăng ký DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Đăng ký Repository
builder.Services.AddScoped<IMarketDataRepository, MarketDataRepository>();

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