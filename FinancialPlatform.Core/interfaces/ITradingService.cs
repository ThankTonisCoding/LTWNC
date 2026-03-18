using System.Threading.Tasks;
using FinancialPlatform.Core.Entities;

namespace FinancialPlatform.Core.Interfaces
{
    public interface ITradingService
    {
        // Khởi tạo ví ảo 10.000$ cho User mới
        Task InitializePaperWalletAsync(string userId);

        // Đặt lệnh Mua/Bán (Limit/Market)
        Task<Order> PlaceOrderAsync(string userId, string symbol, string side, string type, decimal amount, decimal? limitPrice = null, decimal? stopLoss = null, decimal? takeProfit = null);

        // Khớp lệnh Market ngay lập tức dựa trên giá Redis
        Task<bool> ExecuteMarketOrderAsync(Order order);
        
        // Đóng lệnh (Chốt lời/Cắt lỗ hoặc User tự đóng)
        Task<bool> ClosePositionAsync(int orderId, decimal? forceClosePrice = null);
    }
}
