using FinancialPlatform.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancialPlatform.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<MarketTick> MarketTicks { get; set; }

        // Trong tương lai, bạn sẽ thêm các DbSet khác ở đây
        // public DbSet<User> Users { get; set; }
        // public DbSet<Transaction> Transactions { get; set; }
    }
}