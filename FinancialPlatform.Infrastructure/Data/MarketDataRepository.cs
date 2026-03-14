using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialPlatform.Core.Entities;
using FinancialPlatform.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinancialPlatform.Infrastructure.Data
{
    public class MarketDataRepository : IMarketDataRepository
    {
        private readonly ApplicationDbContext _context;

        public MarketDataRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddTickAsync(MarketTick tick)
        {
            await _context.MarketTicks.AddAsync(tick);
            await _context.SaveChangesAsync();
        }

        public Task<IEnumerable<MarketTick>> GetHistoryAsync(string symbol, DateTime from, DateTime to)
        {
            throw new NotImplementedException(); // Sẽ implement sau khi cần vẽ chart
        }

        public Task<MarketTick> GetLatestTickAsync(string symbol)
        {
            throw new NotImplementedException(); // Sẽ implement sau
        }
    }
}
