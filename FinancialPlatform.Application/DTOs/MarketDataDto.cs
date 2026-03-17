using System;

namespace FinancialPlatform.Application.DTOs
{
    public class MarketDataDto
    {
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public double? RSI { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
