# Task 02: Implement Shared Contracts

## Objective
Create the shared data models used across all services for market data events and security information.

## Prerequisites
- Task 01 completed (solution structure exists)
- Shared.Contracts project created

## Acceptance Criteria
- [ ] `MarketDataEvent.cs` created with proper properties and documentation
- [ ] `Security.cs` created with price tracking logic
- [ ] Both classes are `public` and properly namespaced
- [ ] Computed properties (`Change`, `ChangePercent`) implemented correctly
- [ ] XML documentation comments included
- [ ] Project compiles without warnings

## Implementation Details

### MarketDataEvent.cs
```csharp
namespace Shared.Contracts;

/// <summary>
/// Represents a market data tick event.
/// Simple model for MVP - symbol, price, volume, timestamp only.
/// </summary>
public record MarketDataEvent
{
    public string Symbol { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public long Volume { get; init; }
    public DateTime Timestamp { get; init; }
}
```

### Security.cs
```csharp
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
```

## Verification
```bash
dotnet build src/Shared.Contracts/
```

## Related Sections
- Technical Approach Section 5.1 (Shared Contracts)

## Estimated Effort
30 minutes
