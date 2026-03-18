namespace FinancialPlatform.Core.Entities
{
    public class Asset
    {
        public int Id { get; set; }
        public string Symbol { get; set; } = string.Empty; // e.g. BTCUSDT
        public string Name { get; set; } = string.Empty;   // e.g. Bitcoin
        public bool IsActive { get; set; } = true;
    }
}
