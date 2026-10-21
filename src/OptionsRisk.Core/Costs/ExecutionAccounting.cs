namespace OptionsRisk.Core.Costs;

/// <summary>
/// Tracks cash fees separately from execution slippage. The position's actual
/// fill price already includes slippage, so only fees are subtracted from
/// marked-to-market P/L; otherwise slippage would be charged twice.
/// </summary>
public sealed class ExecutionAccounting
{
    public double Brokerage { get; private set; }
    public double Stt { get; private set; }
    public double Exchange { get; private set; }
    public double Sebi { get; private set; }
    public double Stamp { get; private set; }
    public double Gst { get; private set; }
    public double Slippage { get; private set; }
    public double Fees => Brokerage + Stt + Exchange + Sebi + Stamp + Gst;
    public double TotalCosts => Fees + Slippage;

    public void Record(ExecutionCost cost)
    {
        Brokerage += cost.Brokerage;
        Stt += cost.Stt;
        Exchange += cost.Exchange;
        Sebi += cost.Sebi;
        Stamp += cost.Stamp;
        Gst += cost.Gst;
        Slippage += cost.Slippage;
    }

    public ExecutionCost Snapshot() =>
        new(Brokerage, Stt, Exchange, Sebi, Stamp, Gst, Slippage);
}

/// <summary>Configurable educational bid/ask and market-impact proxy in ticks.</summary>
public static class SimulatedFill
{
    public static double Price(double reference, TradeSide side, double tickSize, double adverseTicks)
    {
        if (!double.IsFinite(reference) || reference < 0 || tickSize <= 0 ||
            adverseTicks < 0 || !double.IsFinite(adverseTicks))
            throw new ArgumentOutOfRangeException(nameof(reference));
        var displacement = tickSize * adverseTicks;
        return Math.Max(0, side == TradeSide.Buy ? reference + displacement : reference - displacement);
    }
}
