using System.ComponentModel.DataAnnotations;

namespace FinancialPlatform.Core.Entities
{
    public class Portfolio
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        public decimal CashBalance { get; set; }

        // Mật mã cho Optimistic Concurrency (Chống Race-Condition khi nhiều luồng cùng trừ tiền)
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public ApplicationUser? User { get; set; }
    }
}
