using System.Threading.Tasks;

namespace FinancialPlatform.Core.Interfaces
{
    public interface IPortfolioQueryService
    {
        Task<object?> GetPortfolioAsync(string userId);
    }
}
