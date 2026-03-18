using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialPlatform.Core.Entities
{
    public class Portfolio
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal CashBalance { get; set; } // Số dư USD / VNĐ (Ví ảo)
        
        // Navigation properties
        public virtual ApplicationUser? User { get; set; }
    }
}
