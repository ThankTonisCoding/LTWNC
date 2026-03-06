using System;

namespace FinancialPlatform.Application.DTOs
{
    public class MarketDataDto
    {
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public string TimeFormatted { get; set; } // Dạng string để hiển thị UI dễ hơn
        public string Color { get; set; } // "green" hoặc "red" để UI hiển thị
    }
}
