using FinancialPlatform.Core.Entities;
using FinancialPlatform.Core.Interfaces;
using FinancialPlatform.Infrastructure.Data;
using FinancialPlatform.Infrastructure.Services;
using FinancialPlatform.WebUI.Workers;
using FinancialPlatform.WebUI.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

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

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "mock-id";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "mock-secret";
    });

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
builder.Services.AddScoped<IAdminQueryService, AdminQueryService>();
builder.Services.AddScoped<IPortfolioQueryService, PortfolioQueryService>();
builder.Services.AddSingleton<IMarketPredictorService, MarketPredictorService>();

// 3. Đăng ký SignalR
builder.Services.AddSignalR();

// 1. Đăng ký HttpClient cho BinanceService tích hợp Polly (Chống Rate-Limiting/Transient Errors)
builder.Services.AddHttpClient<BinanceService>()
    .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// 2. Đăng ký Worker Service chạy ngầm
builder.Services.AddHostedService<MarketDataWorker>();
builder.Services.AddHostedService<RiskManagementWorker>();

var app = builder.Build();

// Seed Admin Role and User
// Seed Admin Role and User (Giải quyết vấn đề Blocking Thread - Lỗi GetAwaiter)
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    
    // 1. Tạo Role "Admin" thông qua cơ chế Asynchronous
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
    
    // 2. Tạo User "admin@wallstreet.com" nếu chưa có
    var adminEmail = "admin@wallstreet.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var result = await userManager.CreateAsync(adminUser, "Admin@123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Global Exception Handling (Chuẩn ProblemDetails)
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var contextFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            if (contextFeature != null)
            {
                await context.Response.WriteAsJsonAsync(new 
                {
                    Instance = context.Request.Path,
                    Status = 500,
                    Title = "Internal Server Error",
                    Detail = contextFeature.Error.Message
                });
            }
        });
    });
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