using System.Collections.Generic;

namespace FinancialPlatform.Core.Interfaces
{
    public interface IMarketPredictorService
    {
        string PredictSignal(IEnumerable<double> recentRsiValues);
    }
}
