using OptionsRisk.Core;
using OptionsRisk.Core.Coach;
using OptionsRisk.Core.Pricing;
using OptionsRisk.Core.Strategy;

namespace Broker.Service.Services.OptionsRisk;

// Wire DTOs for the options simulator SSE/REST surface. Kept in Broker.Service:
// the frontend consumes JSON, and no other service needs these shapes.

public sealed record OptionsConfigDto(bool Enabled, int LotSize, double StrikeStep, double WingOffset, double Capital, double DaysToExpiry, int[] Speeds, string BrokerProfile);

public sealed record LegDto(
    string Id, string Type, double Strike, string Side, int Lots,
    double EntryPrice, double LastPrice, double EntryIv, double Iv,
    double Delta, double Gamma, double ThetaPerDay, double VegaPerPoint,
    double Unrealized, double Realized, string Status);

public sealed record FuturesDto(
    string Id, string Side, int Lots, double EntryPrice, double LastPrice,
    double Unrealized, double Realized, string Status);

public sealed record TotalsDto(
    double Realized, double Unrealized, double Net, double PremiumCollected,
    double MaxLossSoFar, double DrawdownFraction,
    double NetDelta, double NetGamma, double NetThetaPerDay, double NetVegaPerPoint,
    double Gross, double Fees, double Slippage, double TransactionCosts,
    double Brokerage, double Stt, double Exchange, double Sebi, double Stamp, double Gst,
    double MarginRequired, double MarginAvailable, double MarginUtilization,
    double MarginStressLoss, bool MarginShortfall, string MarginMethod);

public sealed record PayoffPointDto(double Spot, double Pnl);
public sealed record SpotPointDto(int Tick, double Spot);
public sealed record SpikeDto(int TickIndex, int SimMinute, double FromSpot, double ToSpot, double IvJump);
public sealed record DecisionDto(
    int TickIndex, int SimMinute, string Rule, string Description, string Outcome,
    double? HoldPnl, double? ActPnl, double NetPnlAtDecision, double? OutcomePnl);

public sealed record OptionsSnapshotDto
{
    public required string Status { get; init; }
    public required int TickIndex { get; init; }
    public required int SimMinute { get; init; }
    public required double MinutesToExpiry { get; init; }
    public required double Spot { get; init; }
    public required double IvMultiplier { get; init; }
    public required double FuturesPrice { get; init; }
    public required double AtmStrike { get; init; }
    public required int Speed { get; init; }
    public required int LotSize { get; init; }
    public required IReadOnlyList<LegDto> Legs { get; init; }
    public required IReadOnlyList<FuturesDto> Futures { get; init; }
    public required TotalsDto Totals { get; init; }
    public required IReadOnlyList<double> Breakevens { get; init; }
    public required IReadOnlyList<PayoffPointDto> PayoffExpiry { get; init; }
    public required IReadOnlyList<PayoffPointDto> PayoffToday { get; init; }
    public required IReadOnlyList<SpotPointDto> SpotHistory { get; init; }
    public required IReadOnlyList<SpikeDto> Spikes { get; init; }
    public required IReadOnlyList<CoachSuggestion> Suggestions { get; init; }
    public required IReadOnlyList<DecisionDto> Decisions { get; init; }
}

public static class OptionsDtoMapper
{
    public static OptionsSnapshotDto ToDto(RunSnapshot s, int speed, int lotSize) => new()
    {
        Status = s.Status.ToString(),
        TickIndex = s.TickIndex,
        SimMinute = s.SimMinute,
        MinutesToExpiry = s.MinutesToExpiry,
        Spot = s.Spot,
        IvMultiplier = s.IvMultiplier,
        FuturesPrice = s.FuturesPrice,
        AtmStrike = s.AtmStrike,
        Speed = speed,
        LotSize = lotSize,
        Legs = s.Legs.Select(l => new LegDto(
            l.Id, l.Type == OptionType.Call ? "CE" : "PE", l.Strike,
            l.Direction < 0 ? "SHORT" : "LONG", l.QuantityLots,
            Math.Round(l.EntryPrice, 2), Math.Round(l.LastPrice, 2),
            Math.Round(l.EntryIv, 4), Math.Round(l.Quote.Iv, 4),
            Math.Round(l.Quote.Greeks.Delta, 4), Math.Round(l.Quote.Greeks.Gamma, 6),
            Math.Round(l.Quote.Greeks.ThetaPerDay, 2), Math.Round(l.Quote.Greeks.VegaPerPoint, 2),
            Math.Round(l.UnrealizedPoints * Math.Abs(l.QuantityLots) * lotSize, 0),
            Math.Round(l.RealizedPoints * Math.Abs(l.QuantityLots) * lotSize, 0),
            l.Status.ToString())).ToList(),
        Futures = s.Futures.Select(f => new FuturesDto(
            f.Id, f.Direction > 0 ? "LONG" : "SHORT", f.QuantityLots,
            Math.Round(f.EntryPrice, 2), Math.Round(f.LastPrice, 2),
            Math.Round(f.UnrealizedPoints * Math.Abs(f.QuantityLots) * lotSize, 0),
            Math.Round(f.RealizedPoints * Math.Abs(f.QuantityLots) * lotSize, 0),
            f.Status.ToString())).ToList(),
        Totals = new TotalsDto(
            s.Totals.Realized, s.Totals.Unrealized, s.Totals.Net,
            s.Totals.PremiumCollected, s.Totals.MaxLossSoFar, s.Totals.DrawdownFraction,
            Math.Round(s.Totals.NetGreeks.Delta, 2), Math.Round(s.Totals.NetGreeks.Gamma, 4),
            Math.Round(s.Totals.NetGreeks.ThetaPerDay, 2), Math.Round(s.Totals.NetGreeks.VegaPerPoint, 2),
            Math.Round(s.Totals.Gross, 2),
            Math.Round(s.Totals.Costs.Total - s.Totals.Costs.Slippage, 2),
            Math.Round(s.Totals.Costs.Slippage, 2),
            Math.Round(s.Totals.Costs.Total, 2),
            Math.Round(s.Totals.Costs.Brokerage, 2), Math.Round(s.Totals.Costs.Stt, 2),
            Math.Round(s.Totals.Costs.Exchange, 2), Math.Round(s.Totals.Costs.Sebi, 2),
            Math.Round(s.Totals.Costs.Stamp, 2), Math.Round(s.Totals.Costs.Gst, 2),
            Math.Round(s.Totals.Margin.Required, 2), Math.Round(s.Totals.Margin.FreeCapital, 2),
            Math.Round(s.Totals.Margin.Required / Math.Max(1, s.Totals.Margin.Required + s.Totals.Margin.FreeCapital), 4),
            Math.Round(s.Totals.Margin.StressLoss, 2), s.Totals.Margin.MarginShortfall,
            s.Totals.Margin.Method),
        Breakevens = s.Breakevens,
        PayoffExpiry = s.PayoffAtExpiry.Select(p => new PayoffPointDto(p.Spot, p.Pnl)).ToList(),
        PayoffToday = s.PayoffToday.Select(p => new PayoffPointDto(p.Spot, p.Pnl)).ToList(),
        SpotHistory = s.SpotHistory.Select(h => new SpotPointDto(h.Tick, h.Spot)).ToList(),
        Spikes = s.Spikes.Select(sp => new SpikeDto(sp.TickIndex, sp.SimMinute, Math.Round(sp.FromSpot, 2), Math.Round(sp.ToSpot, 2), Math.Round(sp.IvMultiplierJump, 3))).ToList(),
        Suggestions = s.Suggestions,
        Decisions = s.Decisions.Select(d => new DecisionDto(
            d.TickIndex, d.SimMinute, d.Rule, d.Description, d.Outcome,
            d.HoldPnl, d.ActPnl, Math.Round(d.NetPnlAtDecision), d.OutcomePnl)).ToList(),
    };
}
