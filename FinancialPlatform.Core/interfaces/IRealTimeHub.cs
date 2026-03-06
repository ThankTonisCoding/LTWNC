using System.Threading.Tasks;
using FinancialPlatform.Core.Entities;

namespace FinancialPlatform.Core.Interfaces
{
    public interface IRealTimeHub
    {
        Task BroadcastMarketData(MarketTick data);
    }
}
