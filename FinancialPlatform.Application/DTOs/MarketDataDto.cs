using System;

namespace FinancialPlatform.Application.DTOs
{
    public class MarketDataDto
    {
        public required string Symbol { get; set; }
        public required string TimeFormatted { get; set; }
        public required string Color { get; set; }


    }
}
