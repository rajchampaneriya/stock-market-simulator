namespace Shared.Contracts;

/// <summary>
/// Represents cached security information for price tracking.
/// </summary>
public class Security
{
    public string Symbol { get; set; } = string.Empty;
    public decimal LastPrice { get; set; }
    public decimal PreviousPrice { get; set; }
    public long Volume { get; set; }
    public DateTime LastUpdate { get; set; }
    
    public decimal Change => LastPrice - PreviousPrice;
    public decimal ChangePercent => PreviousPrice > 0 
        ? (Change / PreviousPrice) * 100 
        : 0;
}
