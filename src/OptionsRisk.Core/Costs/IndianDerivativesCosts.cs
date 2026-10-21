namespace OptionsRisk.Core.Costs;

/// <summary>Effective-dated, illustrative NSE F&O fee profile. Verify against current broker contract note.</summary>
public sealed record IndianFeeSchedule(
    double OptionsBrokeragePerOrder = 20,
    double FuturesBrokeragePerOrder = 20,
    double FuturesBrokerageRate = 0.0003,
    double OptionsSttSell = 0.0015,
    double OptionsExerciseStt = 0.0015,
    double FuturesSttSell = 0.0005,
    double OptionsExchangeRate = 0.0003553,
    double FuturesExchangeRate = 0.0000183,
    double SebiRate = 0.000001,
    double OptionsStampBuy = 0.00003,
    double FuturesStampBuy = 0.00002,
    double GstRate = 0.18);

public enum DerivativeKind { Option, Future }
public enum TradeSide { Buy, Sell }

public sealed record ExecutionCost(double Brokerage, double Stt, double Exchange, double Sebi, double Stamp, double Gst, double Slippage) {
    public double Total => Brokerage + Stt + Exchange + Sebi + Stamp + Gst + Slippage;
}

public static class IndianDerivativesCosts
{
    /// <param name="executionPrice">Actual expected fill price, including slippage.</param>
    /// <param name="referencePrice">Mid/reference price, used only to report slippage cost.</param>
    public static ExecutionCost Calculate(DerivativeKind kind, TradeSide side, double executionPrice,
        double referencePrice, int quantity, IndianFeeSchedule? schedule = null)
    {
        var f = schedule ?? new IndianFeeSchedule();
        if (quantity <= 0 || !double.IsFinite(executionPrice) || executionPrice < 0 ||
            !double.IsFinite(referencePrice) || referencePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(executionPrice));
        var turnover = executionPrice * quantity;
        var brokerage = kind == DerivativeKind.Option ? f.OptionsBrokeragePerOrder
            : Math.Min(f.FuturesBrokeragePerOrder, turnover * f.FuturesBrokerageRate);
        var stt = side == TradeSide.Sell ? turnover * (kind == DerivativeKind.Option ? f.OptionsSttSell : f.FuturesSttSell) : 0;
        var exchange = turnover * (kind == DerivativeKind.Option ? f.OptionsExchangeRate : f.FuturesExchangeRate);
        var sebi = turnover * f.SebiRate;
        var stamp = side == TradeSide.Buy ? turnover * (kind == DerivativeKind.Option ? f.OptionsStampBuy : f.FuturesStampBuy) : 0;
        var gst = (brokerage + exchange + sebi) * f.GstRate;
        var slippage = Math.Max(0, (side == TradeSide.Buy ? executionPrice - referencePrice : referencePrice - executionPrice) * quantity);
        return new ExecutionCost(brokerage, stt, exchange, sebi, stamp, gst, slippage);
    }
}
