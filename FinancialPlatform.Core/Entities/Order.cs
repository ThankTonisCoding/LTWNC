using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialPlatform.Core.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int AssetId { get; set; }
        public string Side { get; set; } = "Buy"; // "Buy" or "Sell"
        public string Type { get; set; } = "Market"; // "Market" or "Limit"
        
        [Column(TypeName = "decimal(18,8)")]
        public decimal Amount { get; set; } // Số lượng coin
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal Price { get; set; } // Giá đặt lệnh
        
        public string Status { get; set; } = "Pending"; // "Pending", "Filled", "Cancelled", "Closed"
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal? StopLoss { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal? TakeProfit { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
        
        // Navigation properties
        public virtual ApplicationUser? User { get; set; }
        public virtual Asset? Asset { get; set; }
    }
}
