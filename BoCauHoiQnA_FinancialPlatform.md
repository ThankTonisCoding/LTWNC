# 🎯 BỘ CÂU HỎI VẤN ĐÁP BẢO VỆ ĐỒ ÁN (Q&A)
**Đề tài:** Financial Platform (Nền tảng Giao dịch Tài chính Thực tế)
**Công nghệ:** ASP.NET Core 8, SignalR, Entity Framework Core, Redis, ML.NET, OAuth2.

---

### Câu 1: Em hãy giải thích lý do tại sao nhóm chọn mô hình Clean Architecture thay vì MVC truyền thống?
**🏆 Trả lời:**
Dạ, nhóm chọn Clean Architecture vì các nền tảng tài chính đòi hỏi tính mở rộng và dễ bảo trì rất cao. Mô hình MVC truyền thống thường nhồi nhét quá nhiều logic vào Controllers. Với Clean Architecture, nhóm tách biệt hoàn toàn Giao diện (`WebUI`) khỏi Nghiệp vụ (`Application` & `Core`) và Tương tác dữ liệu (`Infrastructure`). Nhờ vậy, nếu tương lai muốn đổi từ SQL Server sang cơ sở dữ liệu khác, nhóm chỉ cần sửa lớp Infrastructure mà không làm sập toàn bộ hệ thống.

### Câu 2: Làm thế nào để đồ thị và số liệu trên web có thể nhảy liên tục giống các sàn giao dịch thật mà không cần Load lại trang?
**🏆 Trả lời:**
Dạ, nhóm sử dụng **SignalR** - công nghệ WebSockets của Microsoft. 
Thay vì trình duyệt phải liên tục F5 để hỏi máy chủ "có giá mới chưa", nhóm viết một cái `Background Worker` (1 dịch vụ chạy ngầm 24/7 trên server). Dịch vụ này cứ mỗi 2 giây sẽ kéo giá từ sàn Binance về, sau đó kết hợp với SignalR "đẩy (push)" lượng dữ liệu đó xuống trực tiếp toàn bộ các trình duyệt đang mở trang web ngay trong chớp mắt.

### Câu 3: API của Binance giới hạn số lần gọi tốn phí, hệ thống của nhóm em nếu gọi dồn dập bị Binance chặn thì trang web có bị sập luôn không?
**🏆 Trả lời:**
Dạ không ạ. Đây chính là điểm sáng của đồ án khi nhóm tích hợp thư viện **Polly (Resilience Strategy)** vào `HttpClient`. Khi Binance báo lỗi chặn (Lỗi 429 - Too Many Requests), Polly sẽ tự động "dập lửa" bằng cách bắt lỗi và tiến hành `WaitAndRetryAsync` – tự động áp dụng cơ chế chờ thời gian trễ tăng dần theo cấp số nhân (đợi 2s, 4s, 8s...) rồi mới gọi lại, giúp duy trì tuổi thọ ứng dụng và không bao giờ bị Crash (sập) màn hình của người dùng.

### Câu 4: Nhóm ứng dụng thư viện Trí tuệ Nhân tạo (Machine Learning) vào chức năng nào?
**🏆 Trả lời:**
Dạ nhóm tích hợp **ML.NET** để xây dựng tính năng "Trợ lý AI Phân tích Kỹ thuật".
Cụ thể, `MarketDataWorker` sẽ tính toán trực tiếp chỉ số RSI (Độ mạnh tương đối) của 15 phiên gần nhất. Dải dữ liệu này được đưa vào bộ Predict của ML.NET để dự đoán xem thị trường đang Quá mua (Bơm thổi giá) hay Quá bán (Rớt giá thê thảm), từ đó AI sẽ đưa tín hiệu gợi ý `MUA MẠNH`, `BÁN KHỐNG` hoặc `THEO DÕI` ngay trên màn hình để hỗ trợ nhà đầu tư ra quyết định.

### Câu 5: Hệ thống Đăng nhập bằng Google hoạt động như thế nào, làm thế nào để nó ghi nhận được tiền ảo 10.000$ lúc đăng nhập?
**🏆 Trả lời:**
Dạ, nhóm tích hợp chuẩn bảo mật **OAuth2** thông qua lớp `AddGoogle` trong hệ thống.
Khi user bấm uỷ quyền từ Google, mã Google token sẽ trả về trang `ExternalLogin` của nhóm. Nhóm lập trình logic kiểm tra: nếu email này chưa từng tồn tại trong Database, hệ thống sẽ im lặng tự động khởi tạo User mới để tránh phiền cho người dùng. Ngay trong tích tắc khởi tạo đó, nhóm gọi hàm `InitializePaperWalletAsync` kích hoạt việc tạo ví và bơm sẵn 10,000 USD giả lập vào thẳng tài khoản cho User.

### Câu 6: Ở trang Danh Mục Đầu Tư (Portfolio), bảng P&L (Lợi nhuận) được tính toán ra sao? 
**🏆 Trả lời:**
Dạ, nhóm sử dụng mẫu thiết kế CQRS (Query Service). Khi người dùng vào trang này, hệ thống sẽ lôi hết lịch sử mua/bán của họ ra. Nếu họ đánh lệnh `Khớp Lệnh (Long/Buy)`, lợi nhuận sẽ là *(Giá hiện tại - Giá mua) * Khối lượng*. Ngược lại nếu đánh `Bán Khống (Short/Sell)`, lợi nhuận sẽ là *(Giá bán - Giá hiện tại) * Khối lượng*. 

### Câu 7: Việc có một dịch vụ chạy ngầm bám sàn Binance lấy giá (MarketDataWorker) có ngốn hết CPU của máy chủ không?
**🏆 Trả lời:**
Dạ nhóm đã tính toán tối ưu hoá việc này bằng cách:
1. Chỉ quét lấy giá 1 danh sách cố định (6 tài sản lớn).
2. Tách nhỏ bài toán ra: Giá Realtime được lưu tạm vào **Redis** (Cache) để giảm độ nghẽn truy xuất, còn việc lưu vào **SQL Server** thì không lưu từng cái mà dùng cơ chế **Batching** (Gom đủ 15 cục giá mới lưu vào database một lần) để chia sẻ áp lực cho ổ cứng máy chủ. Bằng kỹ thuật này, hệ thống chạy rất tiết kiệm tài nguyên.
