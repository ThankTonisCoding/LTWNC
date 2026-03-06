using System;

namespace FinancialPlatform.Core.Entities
{
    public class MarketTick
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Symbol { get; set; } = string.Empty; // Ví dụ: BTCUSDT, AAPL
        public decimal Price { get; set; } // Dùng decimal cho tiền tệ để tránh sai số
        public decimal Volume { get; set; }
        public DateTime Timestamp { get; set; } // Thời gian ghi nhận
        
        // Các chỉ số kỹ thuật (có thể tính sau hoặc lưu luôn nếu muốn cache)
        public double? RSI { get; set; }
        public double? MACD { get; set; }
    }
}
