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

        public async Task<IEnumerable<MarketTick>> GetHistoryAsync(string symbol, DateTime from, DateTime to)
        {
            // Truy vấn các tick trong khoảng thời gian cho trước và sắp xếp
            return await _context.MarketTicks
                .Where(t => t.Symbol == symbol && t.Timestamp >= from && t.Timestamp <= to)
                .OrderBy(t => t.Timestamp)
                .ToListAsync();
        }

        public async Task<MarketTick?> GetLatestTickAsync(string symbol)
        {
            // Lấy tick mới nhất của một mã bằng cách sắp xếp giảm dần theo thời gian và lấy cái đầu tiên
            return await _context.MarketTicks
                .Where(t => t.Symbol == symbol)
                .OrderByDescending(t => t.Timestamp)
                .FirstOrDefaultAsync();
        }
    }
}
