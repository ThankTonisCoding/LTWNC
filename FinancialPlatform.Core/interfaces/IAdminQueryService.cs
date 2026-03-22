using System.Threading.Tasks;

namespace FinancialPlatform.Core.Interfaces
{
    public interface IAdminQueryService
    {
        Task<object> GetDashboardMetricsAsync();
    }
}
