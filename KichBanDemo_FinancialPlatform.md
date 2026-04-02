# KỊCH BẢN DEMO ĐỒ ÁN: FINANCIAL PLATFORM

## Phần 1: Mở đầu & Khởi động dự án (1-2 phút)
- **Hành động Demo**: Mở Visual Studio / IDE, chạy lệnh Run project.
- **Lời thuyết trình**: "Chào thầy/cô và các bạn, hôm nay nhóm em xin demo dự án Nền tảng Tài chính (Financial Platform). Khi ứng dụng vừa khởi động, dự án của em đã áp dụng cơ chế **Asynchronous Data Seeding** (Khởi tạo dữ liệu bất đồng bộ) để tự động tạo tài khoản Admin mặc định mà không làm block (nghẽn) luồng khởi động chính của ứng dụng."
- **Show Code (Chỉ vào `Program.cs` dòng 64-89)**:
```csharp
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
    // ... code tạo user ...
}
```

## Phần 2: Kiến trúc hệ thống & Tách biệt truy vấn (CQRS-lite) (2-3 phút)
- **Hành động Demo**: Mở cấu trúc thư mục của dự án (Core, Infrastructure, WebUI). Mở trang Danh mục đầu tư (Portfolio) trên trình duyệt cho thấy dữ liệu load nhanh.
- **Lời thuyết trình**: "Về mặt kiến trúc, dự án áp dụng mô hình **Clean Architecture** nghiêm ngặt. Để tăng hiệu suất đọc dữ liệu (Read) và tránh EntityFramework gắn chặt vào Controller, em đã sử dụng các **Query Services**. Các Service này chỉ chuyên trách việc truy vấn dữ liệu từ Database ra View, giúp code tối ưu hơn và dễ test."
- **Show Code (Chỉ vào `Program.cs` phần đăng ký DI và `IAdminQueryService.cs` hoặc `IPortfolioQueryService.cs`)**:
```csharp
// Program.cs - Tách biệt Command và Query
builder.Services.AddScoped<IAdminQueryService, AdminQueryService>();
builder.Services.AddScoped<IPortfolioQueryService, PortfolioQueryService>();
```

## Phần 3: Real-time & Các tiến trình chạy ngầm (Background Workers) (2-3 phút)
- **Hành động Demo**: Mở màn hình hiển thị giá thị trường (Market). Giải thích làm sao giá thay đổi mà không cần reload trang.
- **Lời thuyết trình**: "Thị trường tài chính luôn biến động, nên em sử dụng **SignalR** để push dữ liệu real-time từ Server về Client. Các dữ liệu này được thu thập thông qua các **Hosted Services (Worker)** chạy ngầm liên tục phía dưới."
- **Show Code (Chỉ vào `Program.cs` dòng 49-60 và 126)**:
```csharp
// Đăng ký SignalR để update giao diện real-time
builder.Services.AddSignalR();
app.MapHub<MarketHub>("/marketHub");

// Đăng ký Worker Service chạy ngầm
builder.Services.AddHostedService<MarketDataWorker>();
builder.Services.AddHostedService<RiskManagementWorker>();
```

## Phần 4: Tính bền bỉ của hệ thống (Resilience) chống Rate-Limit (1-2 phút)
- **Hành động Demo**: Giải thích tính năng lấy dữ liệu từ bên thứ 3 (ví dụ Binance API).
- **Lời thuyết trình**: "Khi gọi API ra bên ngoài để lấy dữ liệu coin/chứng khoán, việc gọi quá nhiều làm Server nước ngoài chặn (Rate-Limiting). Để giải quyết, em áp dụng thư viện **Polly** tích hợp xử lý chống lỗi gián đoạn và Retry tự động với cơ chế Exponential Backoff (chờ lâu dần ra sau mỗi lần thử lại)."
- **Show Code (Chỉ vào `Program.cs` dòng 52-57)**:
```csharp
// Đăng ký HttpClient cho BinanceService tích hợp Polly (Chống Rate-Limiting/Transient Errors)
builder.Services.AddHttpClient<BinanceService>()
    .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

## Phần 5: Bắt lỗi toàn cục chuẩn REST API (Global Exception Handling) (1-2 phút)
- **Hành động Demo**: Giả lập một lỗi (nếu có thể) hoặc trình bày hướng xử lý lỗi của team.
- **Lời thuyết trình**: "Thay vì để lọt các Exception văng màn hình vàng/lỗi ứng dụng (YSoD) hoặc try catch ở mọi nơi, em đã xây dựng **Global Exception Handling Middleware** đạt chuẩn `ProblemDetails` của HTTP. Mọi lỗi chưa kiểm soát (500) sẽ được gom lại và trả về JSON thống nhất."
- **Show Code (Chỉ vào `Program.cs` dòng 94-113)**:
```csharp
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
```
