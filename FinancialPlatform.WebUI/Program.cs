using FinancialPlatform.Core.Entities;
using FinancialPlatform.Core.Interfaces;
using FinancialPlatform.Infrastructure.Data;
using FinancialPlatform.Infrastructure.Services;
using FinancialPlatform.WebUI.Workers;
using FinancialPlatform.WebUI.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Đăng ký DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add Identity and Roles
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.AccessDeniedPath = "/AccessDenied";
});

// Add Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";
    options.InstanceName = "Finance_";
});

// Đăng ký Repository & Services
builder.Services.AddScoped<IMarketDataRepository, MarketDataRepository>();
builder.Services.AddScoped<ITradingService, TradingService>();
builder.Services.AddSingleton<IMarketPredictorService, MarketPredictorService>();

// 3. Đăng ký SignalR
builder.Services.AddSignalR();

// 1. Đăng ký HttpClient cho BinanceService
builder.Services.AddHttpClient<BinanceService>();

// 2. Đăng ký Worker Service chạy ngầm
builder.Services.AddHostedService<MarketDataWorker>();
builder.Services.AddHostedService<RiskManagementWorker>();

var app = builder.Build();

// Seed Admin Role and User
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    
    // 1. Tạo Role "Admin"
    if (!roleManager.RoleExistsAsync("Admin").GetAwaiter().GetResult())
    {
        roleManager.CreateAsync(new IdentityRole("Admin")).GetAwaiter().GetResult();
    }
    
    // 2. Tạo User "admin@wallstreet.com" nếu chưa có
    var adminEmail = "admin@wallstreet.com";
    var adminUser = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();
    if (adminUser == null)
    {
        adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var result = userManager.CreateAsync(adminUser, "Admin@123").GetAwaiter().GetResult();
        if (result.Succeeded)
        {
            userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
        }
    }
}

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
app.MapControllers();
app.MapHub<MarketHub>("/marketHub"); // Endpoint cho SignalR

app.Run();
// Test update 2026