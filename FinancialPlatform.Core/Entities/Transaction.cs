using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialPlatform.Core.Entities
{
    public class Transaction
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = "Deposit"; // "Deposit", "Withdraw", "Trade"
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Description { get; set; } = string.Empty;
        
        public virtual ApplicationUser? User { get; set; }
    }
}
