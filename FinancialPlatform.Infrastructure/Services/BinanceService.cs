using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FinancialPlatform.Core.Entities;

namespace FinancialPlatform.Infrastructure.Services
{
    public class BinanceService
    {
        private readonly HttpClient _httpClient;

        public BinanceService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<MarketTick?> GetTickerAsync(string symbol)
        {
            try
            {
                // Gọi API công khai của Binance (không cần key cho endpoint này)
                // API trả về: {"symbol":"BTCUSDT","price":"45000.00000000"}
                var response = await _httpClient.GetFromJsonAsync<BinanceTickerDto>(
                    $"https://api.binance.com/api/v3/ticker/price?symbol={symbol}");

                if (response == null) return null;

                // Chuyển đổi từ string sang decimal an toàn
                if (decimal.TryParse(response.PriceString, out var price))
                {
                    return new MarketTick
                    {
                        Symbol = response.Symbol,
                        Price = price,
                        Timestamp = DateTime.UtcNow
                    };
                }
                return null;
            }
            catch
            {
                // Trong thực tế nên log lỗi ở đây
                return null;
            }
        }

        // Class nội bộ để hứng dữ liệu JSON từ Binance
        private class BinanceTickerDto
        {
            [JsonPropertyName("symbol")]
            public string Symbol { get; set; } = string.Empty;

            [JsonPropertyName("price")]
            public string PriceString { get; set; } = string.Empty;
        }
    }
}
