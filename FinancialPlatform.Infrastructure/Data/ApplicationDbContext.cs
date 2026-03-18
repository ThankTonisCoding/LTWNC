using FinancialPlatform.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinancialPlatform.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<MarketTick> MarketTicks { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Portfolio> Portfolios { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // This is absolutely required for Identity

            // Configure Portfolio -> User one-to-one relationship
            builder.Entity<Portfolio>()
                .HasOne(p => p.User)
                .WithOne(u => u.Portfolio)
                .HasForeignKey<Portfolio>(p => p.UserId);

            // You can add additional configuration here, e.g. decimal precision if annotations are not enough
            builder.Entity<Order>()
                .Property(o => o.Amount)
                .HasPrecision(18, 8);
            
            builder.Entity<Order>()
                .Property(o => o.Price)
                .HasPrecision(18, 4);
                
            builder.Entity<Portfolio>()
                .Property(p => p.CashBalance)
                .HasPrecision(18, 4);
                
            builder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 4);
        }
    }
}
