using OptionsRisk.Core.Pricing;

namespace OptionsRisk.Core.Strategy;

public enum LegSide { Short = -1, Long = 1 }
public enum LegStatus { Open, Exited }

public abstract class Position
{
    public required string Id { get; init; }
    public required int QuantityLots { get; init; }
    /// <summary>+1 long / −1 short — multiplies P/L and greek contribution.</summary>
    public required int Direction { get; init; }
    public required double EntryPrice { get; init; }
    public LegStatus Status { get; protected set; } = LegStatus.Open;
    public double? ExitPrice { get; protected set; }

    public abstract double LastPrice { get; }
    public abstract string Display { get; }

    public int Units(int lotSize) => QuantityLots * lotSize * Direction;

    /// <summary>Realized points when closed, otherwise 0.</summary>
    public double RealizedPoints => Status == LegStatus.Exited && ExitPrice.HasValue
        ? Direction * (ExitPrice.Value - EntryPrice)
        : 0;

    /// <summary>Unrealized points while open.</summary>
    public double UnrealizedPoints => Status == LegStatus.Open
        ? Direction * (LastPrice - EntryPrice)
        : 0;
}

public sealed class OptionLeg : Position
{
    public required OptionType Type { get; init; }
    public required double Strike { get; init; }
    public required double EntryIv { get; init; }
    public OptionQuote Quote { get; set; }

    public override double LastPrice => Quote.Price;
    public override string Display => $"{Strike:0} {(Type == OptionType.Call ? "CE" : "PE")}";

    public void Reprice(OptionQuote quote) => Quote = quote;

    public void Exit(double exitPrice)
    {
        ExitPrice = exitPrice;
        Status = LegStatus.Exited;
    }

    /// <summary>Intrinsic value at expiry for one unit.</summary>
    public double Intrinsic(double spot) => Type == OptionType.Call
        ? Math.Max(spot - Strike, 0)
        : Math.Max(Strike - spot, 0);
}

public sealed class FuturesPosition : Position
{
    private double _lastPrice;

    public override double LastPrice => _lastPrice;
    public override string Display => $"FUTURES {(Direction > 0 ? "LONG" : "SHORT")} {QuantityLots}L";

    public void Mark(double futuresPrice) => _lastPrice = futuresPrice;

    public void Exit()
    {
        ExitPrice = _lastPrice;
        Status = LegStatus.Exited;
    }
}
