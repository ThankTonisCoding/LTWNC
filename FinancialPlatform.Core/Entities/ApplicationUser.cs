using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace FinancialPlatform.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        
        // Navigation properties
        public virtual Portfolio? Portfolio { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
