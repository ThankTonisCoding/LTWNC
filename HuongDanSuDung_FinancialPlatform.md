# 📘 HƯỚNG DẪN SỬ DỤNG - FINANCIAL PLATFORM
**Đồ án môn học: Lập trình Web Nâng Cao**
**Nhóm thực hiện:** Nhóm 6

---

## 🚀 1. Hướng Dẫn Chạy Cài Đặt Ban Đầu (Run Project)

Để khởi động và chạy đồ án trơn tru nhất, bạn làm theo các bước sau:

**Yêu cầu hệ thống:**
* Máy tính đã cài đặt `.NET 8 SDK`.
* Đã cài đặt `Redis Server` (Memurai đối với Windows) đang chạy ở cổng `6379`.
* SQL Server đang khởi chạy.

**Cách khởi động:**
1. Mở cửa sổ Terminal (hoặc Command Prompt, PowerShell).
2. Di chuyển vào thư mục `FinancialPlatform.WebUI`:
   ```bash
   cd Desktop\LTWNC\FinancialPlatform.WebUI
   ```
3. Chạy lệnh:
   ```bash
   dotnet run
   ```
4. Trình duyệt sẽ mở ra tại đường dẫn: `https://localhost:5001` hoặc `http://localhost:5000`.

---

## 🛡 2. Hướng Dẫn Đăng Ký & Đăng Nhập

Hệ thống hỗ trợ 2 phương thức đăng nhập:

### Cách 1: Đăng ký/Đăng nhập Truyền thống
* Nhấn vào nút **"Mở Tài Khoản"** (Nút xanh trên Navbar).
* Nhập Email và Mật khẩu (yêu cầu trên 6 ký tự).
* Sau khi đăng ký thành công, hệ thống tự động **tặng 10,000 USD Tiền Ảo** dể mô phỏng giao dịch!

### Cách 2: Đăng nhập 1-Chạm bằng Google (OAuth2)
* Nhấn vào **Đăng nhập** -> Chọn nút **Tiếp tục với Google**.
* *(Lưu ý: Chức năng này yêu cầu người chấm/giáo viên cấu hình thêm ClientID của Google trên máy của họ hoặc bạn demo bằng máy đã cấu hình)*.
* Hệ thống sẽ tự kiểm tra, nếu bạn chưa có tài khoản, Web sẽ **Tự Động Đăng Ký** bằng email Google mà không bắt nhập mật khẩu!

---

## 📈 3. Hướng Dẫn Sử Dụng Giao Diện "Giám Sát Thị Trường" (Trang Chủ)

Khi truy cập trang chủ, bạn tham gia vào màn hình Real-time Trading Experience (Giám sát thời gian thực):

*   **Chuyển đổi Tài Sản:** Phía trên cùng có một bộ lọc (Select Box). Bạn có thể bấm để chọn chuyển qua lại giữa 6 loại coin lớn `BTC`, `ETH`, `BNB`, `SOL`, `ADA`, `XRP`. (Dữ liệu biểu đồ thay đổi ngay lập tức nhờ SignalR).
*   **Chỉ báo Kỹ thuật & AI:**
    *   `Chỉ báo RSI (14):` Hệ thống tính toán độ quá mua (>70 hiển thị đỏ) hoặc quá bán (<30 hiển thị xanh).
    *   `AI Dự Báo:` Tích hợp AI ML.NET chạy ngầm để dự đoán nên MUA MẠNH, BÁN KHỐNG, hay THEO DÕI.
*   **Cuộn chi tiết:** Bạn có thể ấn nút *"Xem Chi Tiết Biểu Đồ"* để web cuộn thẳng xuống đồ thị nến TradingView và bảng Đặt Lệnh trực tiếp.

---

## 💼 4. Hướng Dẫn Đặt Lệnh Giao Dịch Nhanh (Long/Short)

Khu vực **Thao Tác Nhanh** nằm ngay dưới biểu đồ TradingView (yêu cầu Đăng nhập mới hiển thị).

1. Hệ thống đã đồng bộ loại tiền bạn đang xem.  
2. **Khối lượng:** Tại ô nhập số, điền khối lượng bạn muốn giao dịch (VD: `0.01` BTC).
3. Ấn nút:
   * **[MUA KHỚP LỆNH]** (Màu Xanh): Đặt vị thế đánh lên (Long), có lời khi giá tăng.
   * **[BÁN KHỐNG]** (Màu Đỏ): Đặt vị thế đánh xuống (Short), có lời khi giá giảm.
4. Một thông báo thành công hoặc thất bại xanh/đỏ (do không đủ tiền) sẽ xuất hiện phía dưới.

---

## 🏦 5. Hướng Dẫn Theo Dõi Danh Mục Đầu Tư (Portfolio)

Để xem lời/lõ lãi suất ròng của mình:
*   Bấm vào **"Ví của tôi" (Portfolio)** trên thanh Navbar.
*   **Tổng Quan Tài Sản:** Bảng mạch sẽ hiển thị TỔNG TÀI SẢN (Tính bằng Yên Nhật JPY) và Số Dư USD Khả Dụng.
*   **Positions Table (Lịch Sử Vị Thế):**
    * Tại đây liệt kê tất cả các lệnh bạn từng đánh.
    * Xem cột P&L (Profit & Loss). Nếu dương sẽ có màu xanh, nếu đánh sai xu hướng sẽ bị gánh màu Đỏ. Mọi thông số được Real-time.

---

## 🛠️ 6. Công Nghệ Được Sử Dụng (Tech Stack)

Dự án được xây dựng theo chuẩn phần mềm **Doanh nghiệp (Enterprise-level)** với Kiến trúc Sạch (Clean Architecture) và Kiến trúc Microservices quy mô nhỏ:

**1. Backend & Hệ thống Cốt lõi (Core Engine):**
* **.NET 8 (C#)**: Nền tảng cốt lõi sử dụng ASP.NET Core MVC & Web API.
* **Entity Framework Core 8**: Truy vấn cơ sở dữ liệu (ORM) với mô hình Code-First Migration.
* **SQL Server**: Lưu trữ dữ liệu giao dịch mạnh mẽ, đảm bảo tính ACID.

**2. Tối ưu Hiệu năng & Real-time:**
* **Redis Cache (StackExchange.Redis)**: Lưu trữ tạm thời (Caching) dữ liệu biến động nhanh từ Binance API giúp giảm tải Database.
* **SignalR**: Công nghệ WebSocket để đẩy dữ liệu giá (Tick Data) trực tiếp từ Server xuống Client (Trình duyệt) theo thời gian thực (Real-time).
* **Hosted Services (Background Workers)**: Các tác vụ chạy ngầm độc lập (`MarketDataWorker`, `RiskManagementWorker`) dể quét dữ liệu thị trường và quét cắt lỗ (Stop-loss) liên tục 24/7.
* **Polly**: Tích hợp cơ chế Wait-and-Retry để chống Rate-Limiting khi gọi API tài chính bên thứ 3.

**3. Trí Tuệ Nhân Tạo (AI / ML.NET):**
* **Microsoft ML.NET**: Sử dụng thuật toán Hồi quy tuyến tính (SDCA Regression Trainer) để học hỏi các khung giá chuẩn hóa (RSI Time-series) và dự báo xu hướng tương lai dể đưa lệnh Khuyến nghị (BUY/SELL/HOLD).

**4. Bảo Mật & Xác Thực (Security):**
* **ASP.NET Core Identity**: Cung cấp Cookie/Token Login, xác thực bảo mật chuẩn.
* **Google OAuth 2.0**: Cho phép người dùng đăng nhập SSO qua Google.
* **Optimistic Concurrency Control**: Sử dụng `[Timestamp] RowVersion` để chống Race-Condition (trùng lặp giao dịch khi nhiều người dùng thao tác cùng milisecond).
* **Role-based Access Control (RBAC)**: Tách quyền hạn bảo vệ API của Admin khỏi Users thường.

**5. Frontend & UI/UX:**
* **Razor Pages & Bootstrap 5**: Code Giao diện Server-side rendering (SSR) tối ưu SEO và tốc độ.
* **Chart.js & TradingView Widget**: Cung cấp biểu đồ trực quan.

**6. Testing (Kiểm thử):**
* **xUnit & Moq**: Dựng Test case cô lập (Mocking) và InMemoryDatabase để kiểm thử tự động các luồng Trading Logic quan trọng.

---

*Tài liệu này được biên soạn bởi Nhóm 6. Cảm ơn bạn đã trải nghiệm hệ thống!*
