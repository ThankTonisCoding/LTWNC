using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialPlatform.Core.Entities;

namespace FinancialPlatform.Core.Interfaces
{
    public interface IMarketDataRepository
    {
        // Lưu một tick giá mới
        Task AddTickAsync(MarketTick tick);
        
        // Lấy lịch sử giá để vẽ biểu đồ
        Task<IEnumerable<MarketTick>> GetHistoryAsync(string symbol, DateTime from, DateTime to);
        
        // Lấy giá mới nhất
        Task<MarketTick> GetLatestTickAsync(string symbol);
    }
}
