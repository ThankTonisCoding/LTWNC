# BÁO CÁO ĐỒ ÁN MÔN HỌC
## ĐỀ TÀI: XÂY DỰNG NỀN TẢNG TÀI CHÍNH (FINANCIAL PLATFORM)

---

### 1. GIỚI THIỆU CHUNG
- **Tên dự án:** Nền tảng Tài chính Cổ phiếu/Tiền điện tử (Financial Platform).
- **Mục tiêu:** Xây dựng một nền tảng cho phép người dùng theo dõi dữ liệu thị trường (chứng khoán, tiền số) theo thời gian thực (real-time), quản lý rủi ro đầu tư, và hỗ trợ các truy vấn báo cáo liên quan đến danh mục đầu tư (Portfolio).
- **Phạm vi dự án:** Ứng dụng web được phân chia theo tiêu chuẩn hiện đại, có tính khả mở cao (Scalability), chịu lỗi tốt (Resilience) và tuân thủ các nguyên tắc thiết kế mã nguồn sạch.

---

### 2. CÔNG NGHỆ SỬ DỤNG (TECH STACK)
Dự án sử dụng các công nghệ tiêu chuẩn của nền tảng .NET:
- **Framework chính:** ASP.NET Core MVC & Web API.
- **Cơ sở dữ liệu:** Microsoft SQL Server (qua Entity Framework Core).
- **Caching:** Redis Cache.
- **Bảo mật & Xác thực:** ASP.NET Core Identity (Role-based Authorization).
- **Real-time:** SignalR.
- **Xử lý tiến trình nền (Background Tasks):** .NET Hosted Services (Worker Services).
- **Khả năng phục hồi (Resilience):** Thư viện Polly (Exponential Backoff, Retry).

---

### 3. KIẾN TRÚC HỆ THỐNG (SYSTEM ARCHITECTURE)

#### 3.1. Clean Architecture (Kiến trúc Sạch)
Dự án được chia thành 3 phần (Projects) chính nhằm đảm bảo sự rành mạch giữa các tầng mã nguồn, giúp code dễ đọc, dễ bảo trì và dễ test:
- **Core (Domain Layer):** Chứa các Entities, Interface dùng chung. Không phụ thuộc vào Framework bên ngoài.
- **Infrastructure (Data Layer):** Xử lý DbContext, Database, lời gọi API thứ ba, hiện thực Interface.
- **WebUI (Presentation Layer):** Giao diện MVC, trang Razor, Controller, DI Container, chỉ làm nhiệm vụ điều phối luồng.

#### 3.2. Lựa chọn kiến trúc CQRS-lite (Phân tách Đọc / Ghi)
Để tối ưu hóa việc lấy dữ liệu (đọc mạnh) thay vì dồn `DbContext` vào Controller/View, ứng dụng tách các tác vụ truy vấn dữ liệu ra các **QueryService** riêng biệt. Controller/View chỉ giao tiếp với Database qua interface này.
```csharp
// Đăng ký tách biệt các Query Service trong Program.cs
builder.Services.AddScoped<IAdminQueryService, AdminQueryService>();
builder.Services.AddScoped<IPortfolioQueryService, PortfolioQueryService>();
```

---

### 4. CÁC TÍNH NĂNG & CƠ CHẾ KỸ THUẬT NỔI BẬT (KÈM MÃ NGUỒN)

#### 4.1. Asynchronous Data Seeding (Khởi tạo dữ liệu không đồng bộ)
Trong luồng khởi động (`Program.cs`), dự án cần phải tạo tài khoản hệ thống (Admin). Để tránh kẹt Thread khởi động (Thread Blocking) gây ra tình trạng App bị treo, dự án sử dụng `CreateScope()` và gọi hoàn toàn bằng phương thức bất đồng bộ (`await`).

**Minh chứng mã nguồn:**
```csharp
// Program.cs: Seed Admin Role and User
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    
    // 1. Tạo Role "Admin" không gây block thread
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
    
    // 2. Tạo User Admin nếu chưa có
    var adminEmail = "admin@wallstreet.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var result = await userManager.CreateAsync(adminUser, "Admin@123");
        if (result.Succeeded) await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}
```

#### 4.2. Xử lý dữ liệu thời gian thực (Real-time) với SignalR
Các biểu đồ và danh mục mã chứng khoán/tiền ảo được truyền tải liên tục không độ trễ xuống giao diện (Client) thông qua két nối WebSocket nhờ có SignalR. Client không cần tốn hiệu năng để tải lại (Refresh) dữ liệu thủ công.

**Minh chứng mã nguồn:**
```csharp
// Program.cs: Cấu hình đưa luồng Market qua SignalR hub
builder.Services.AddSignalR();
app.MapHub<MarketHub>("/marketHub"); // Cấu hình endpoint cho client
```

#### 4.3. Các tiến trình chạy ngầm phân tích thị trường (Background Workers)
Để đảm bảo giá cả được quét tự động 24/7 và cảnh báo mức độ rủi ro (Risk limit), hệ thống chạy độc lập các Hosted Service (Background tasks ngầm dưới OS) mà không làm lân la tác động đến hiệu suất xử lý HTTP request của trang web.

**Minh chứng mã nguồn:**
```csharp
// Program.cs: Đăng ký Worker Services 
builder.Services.AddHostedService<MarketDataWorker>();
builder.Services.AddHostedService<RiskManagementWorker>();
```

#### 4.4. Tính bền bỉ (Resilience & Fault Tolerance) thư viện Polly
Khi nền tảng kết nối với các nguồn giá trị lớn ở bên thứ 3 (ví dụ: Binance API), thường xuyên bị hiện tượng "Từ chối phục vụ - 429 Too Many Requests". Thay vì ném lỗi văng màn hình, hệ thống sẽ sử dụng cơ chế Retry ( Exponential backoff: Dừng chờ và nhân đôi thời gian mỗi lần thử lại) giúp bảo toàn luồng giao tiếp.

**Minh chứng mã nguồn:**
```csharp
// Program.cs: Cấu hình chèn Polly Retry vào HttpClient
builder.Services.AddHttpClient<BinanceService>()
    .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError()
        // Xử lý bắt mã 429 nếu bên thứ 3 ngắt kêt nối vì quá tải
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        // Thử lại 3 lần, delay thời gian đợi tăng lên theo cấp số nhân
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

#### 4.5. Global Exception Handling (Bắt lỗi toàn cục chuẩn REST API)
Chấm dứt việc dùng `try-catch` lặp đi lặp lại ở mọi ngóc ngách của Controllers. Một Middleware lỗi toàn cục bao trọn 100% Request. Các lỗi hệ thống trả về đều được chuẩn hoá thành cấu trúc Model `ProblemDetails`, mang lại vẻ xịn xò cho Rest API.

**Minh chứng mã nguồn:**
```csharp
// Program.cs: Xử lý Exception Middleware 
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        // Gán cứng trả về lỗi Server 500 định dạng JSON 
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var contextFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (contextFeature != null)
        {
            // Trả về JSON theo đúng form ProblemDetails
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

---

### 5. KẾT LUẬN & HƯỚNG PHÁT TRIỂN
- **Kết luận:** Đồ án đã áp dụng thành công các mô hình kiến trúc chuẩn mực và được đánh giá cao nhất trong hệ sinh thái ASP.NET Core (.NET). Hoàn toàn đáp ứng được tính bền vững, hiệu suất cao (Caching/Async worker) và sạch sẽ về mã nguồn (Clean Code).
- **Hạn chế & Hướng phát triển:** Tiến tới tích hợp Docker/Kubernetes để linh hoạt cấu hình triển khai, sử dụng Machine Learning để tính dự báo giá tương lai vào các đợt phát triển tiếp theo.
