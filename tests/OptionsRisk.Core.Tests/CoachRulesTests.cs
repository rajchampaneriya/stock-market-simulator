using OptionsRisk.Core;
using OptionsRisk.Core.Coach;
using OptionsRisk.Core.Pricing;
using OptionsRisk.Core.Strategy;
using Xunit;

namespace OptionsRisk.Core.Tests;

public class CoachRulesTests
{
    private static readonly StrategyConfig Cfg = new() { LotSize = 75, StrikeStep = 50, WingOffset = 500, Capital = 500_000 };

    private static OptionLeg ShortLeg(string id, OptionType type, double strike, double entry, double last,
        double delta = 0.5, double iv = 0.13)
    {
        var leg = new OptionLeg
        {
            Id = id, Type = type, Strike = strike, Direction = -1, QuantityLots = 1,
            EntryPrice = entry, EntryIv = iv,
            Quote = new OptionQuote(entry, iv, new Greeks(delta, 0.001, -50, 60)),
        };
        leg.Reprice(new OptionQuote(last, iv, new Greeks(delta, 0.001, -55, 65)));
        return leg;
    }

    /// <summary>The four-leg short book after a ~2% rally: calls gain delta, puts shed it.</summary>
    private static List<OptionLeg> BookAfterRally() => new()
    {
        ShortLeg("25000-CE", OptionType.Call, 25000, 60, 150, delta: 0.88),
        ShortLeg("25000-PE", OptionType.Put, 25000, 60, 20, delta: -0.12),
        ShortLeg("25500-CE", OptionType.Call, 25500, 40, 110, delta: 0.55),
        ShortLeg("24500-PE", OptionType.Put, 24500, 40, 8, delta: -0.02),
    };

    /// <summary>Mirror image after a ~2% fall.</summary>
    private static List<OptionLeg> BookAfterFall() => new()
    {
        ShortLeg("25000-CE", OptionType.Call, 25000, 60, 20, delta: 0.12),
        ShortLeg("25000-PE", OptionType.Put, 25000, 60, 150, delta: -0.88),
        ShortLeg("25500-CE", OptionType.Call, 25500, 40, 8, delta: 0.02),
        ShortLeg("24500-PE", OptionType.Put, 24500, 40, 110, delta: -0.55),
    };

    private static CoachContext Ctx(
        double spot,
        IReadOnlyList<OptionLeg> legs,
        double ivMultiplier = 1.0,
        double drawdownFraction = 0.0,
        double maxLoss = 0,
        IReadOnlyList<FuturesPosition>? futures = null)
    {
        var totals = new PortfolioTotals
        {
            Realized = 0,
            Unrealized = legs.Sum(l => l.UnrealizedPoints * 75),
            PremiumCollected = legs.Sum(l => l.EntryPrice * 75),
            MaxLossSoFar = maxLoss,
            DrawdownFraction = drawdownFraction,
            NetGreeks = PortfolioMath.NetGreeks(legs, futures ?? [], 75, 0.065, 0.02),
        };
        return new CoachContext(Cfg, spot, ivMultiplier, spot * 1.001, 300, 25000,
            legs, futures ?? [], totals, [24400, 25600], 10);
    }

    // --- BDD scenario 2 (unit-level mirror): upward spike → buy-futures hedge suggestion ---

    [Fact]
    public void DeltaHedgeRule_AfterRally_SuggestsBuyingFutures_WithProjections()
    {
        var ctx = Ctx(25400, BookAfterRally(), ivMultiplier: 1.6);

        var suggestion = new DeltaHedgeRule().Evaluate(ctx).Single();

        Assert.Equal(CoachActionKind.BuyFutures, suggestion.Action.Kind);
        Assert.True(suggestion.Action.Lots >= 1);
        Assert.Contains("buy", suggestion.Title, StringComparison.OrdinalIgnoreCase);
        Assert.True(suggestion.Projection.HoldPnl < 0, "adverse shock loses money when unhedged");
        Assert.True(suggestion.Projection.ActPnl > suggestion.Projection.HoldPnl,
            "hedging must project better than holding under the adverse move");
    }

    [Fact]
    public void DeltaHedgeRule_AfterFall_SuggestsSellingFutures()
    {
        var ctx = Ctx(24600, BookAfterFall(), ivMultiplier: 1.5);

        var suggestion = new DeltaHedgeRule().Evaluate(ctx).Single();
        Assert.Equal(CoachActionKind.SellFutures, suggestion.Action.Kind);
    }

    [Fact]
    public void DeltaHedgeRule_QuietBook_NoSuggestion()
    {
        var legs = new List<OptionLeg>
        {
            ShortLeg("25000-CE", OptionType.Call, 25000, 60, 62, delta: 0.5),
            ShortLeg("25000-PE", OptionType.Put, 25000, 60, 58, delta: -0.5),
        };
        // Near-ATM book: deltas roughly cancel → craft explicitly neutral totals.
        var totals = new PortfolioTotals
        {
            Realized = 0, Unrealized = 0, PremiumCollected = 9000, MaxLossSoFar = 0,
            DrawdownFraction = 0, NetGreeks = new Greeks(10, -0.01, 200, -240),
        };
        var neutralCtx = new CoachContext(Cfg, 25010, 1.0, 25100, 300, 25000, legs, [], totals, [], 10);

        Assert.Empty(new DeltaHedgeRule().Evaluate(neutralCtx));
        Assert.Empty(new IvSpikeRule().Evaluate(neutralCtx));
        Assert.Empty(new DrawdownRule().Evaluate(neutralCtx));
    }

    // --- BDD scenario 3 (rule-level mirror) ---

    [Fact]
    public void LegStopLossRule_PremiumAtDoubleEntry_SuggestsExit()
    {
        var leg = ShortLeg("25500-CE", OptionType.Call, 25500, 40, 80);
        var ctx = Ctx(25200, new List<OptionLeg> { leg });

        var suggestion = new LegStopLossRule().Evaluate(ctx).Single();
        Assert.Equal(CoachActionKind.ExitLeg, suggestion.Action.Kind);
        Assert.Equal("25500-CE", suggestion.Action.LegId);
        Assert.Contains("2", suggestion.Explanation); // mentions the multiple
    }

    [Fact]
    public void LegStopLossRule_BelowMultiple_NoSuggestion()
    {
        var leg = ShortLeg("25500-CE", OptionType.Call, 25500, 40, 79.9);
        Assert.Empty(new LegStopLossRule().Evaluate(Ctx(25200, new List<OptionLeg> { leg })));
    }

    [Fact]
    public void StrikeBreachRule_SpotAboveWing_SuggestsClosingThreatenedCall()
    {
        var wing = ShortLeg("25500-CE", OptionType.Call, 25500, 40, 90, delta: 0.7);
        var wingPut = ShortLeg("24500-PE", OptionType.Put, 24500, 40, 8, delta: -0.05);
        var ctx = Ctx(25600, new List<OptionLeg> { wing, wingPut });

        var suggestion = new StrikeBreachRule().Evaluate(ctx).Single();
        Assert.Equal("25500-CE", suggestion.Action.LegId);
    }

    [Fact]
    public void StrikeBreachRule_SpotInsideWings_NoSuggestion()
    {
        var wing = ShortLeg("25500-CE", OptionType.Call, 25500, 40, 60, delta: 0.5);
        var wingPut = ShortLeg("24500-PE", OptionType.Put, 24500, 40, 30, delta: -0.4);
        Assert.Empty(new StrikeBreachRule().Evaluate(Ctx(25100, new List<OptionLeg> { wing, wingPut })));
    }

    [Fact]
    public void IvSpikeRule_HighIvMultiplier_WarnsWithVegaContext()
    {
        var leg = ShortLeg("25000-CE", OptionType.Call, 25000, 60, 90, delta: 0.5, iv: 0.2);
        var suggestion = new IvSpikeRule().Evaluate(Ctx(25010, new List<OptionLeg> { leg }, ivMultiplier: 1.6)).Single();

        Assert.Contains("IV", suggestion.Title);
        Assert.Equal(CoachActionKind.ExitLeg, suggestion.Action.Kind);
        Assert.Contains("vega", suggestion.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DrawdownRule_BeyondThreshold_SuggestsReducingSize()
    {
        var losing = ShortLeg("24500-PE", OptionType.Put, 24500, 40, 90, delta: -0.8);
        var ok = ShortLeg("25500-CE", OptionType.Call, 25500, 40, 25, delta: 0.1);
        var suggestion = new DrawdownRule()
            .Evaluate(Ctx(24650, new List<OptionLeg> { losing, ok }, drawdownFraction: 0.15, maxLoss: -75_000))
            .Single();

        Assert.Equal("24500-PE", suggestion.Action.LegId);   // worst leg first
        Assert.Contains("Drawdown", suggestion.Title);
    }

    [Fact]
    public void ProjectionEstimator_ActBeatsHold_UnderAdverseShock()
    {
        // Net short delta −60 (unhedged): adverse shock is up.
        var totals = new PortfolioTotals
        {
            Realized = 0, Unrealized = -5000, PremiumCollected = 9000, MaxLossSoFar = -5000,
            DrawdownFraction = 0.01, NetGreeks = new Greeks(-60, -0.0005, 150, -200),
        };
        var legs = new List<OptionLeg> { ShortLeg("25500-CE", OptionType.Call, 25500, 40, 80, delta: 0.6) };
        var ctx = new CoachContext(Cfg, 25400, 1.0, 25425, 300, 25000, legs, [], totals, [], 5);

        var (projection, after) = ProjectionEstimator.Estimate(
            ctx, new CoachAction(CoachActionKind.BuyFutures, 1), a => RuleMath.GreeksAfter(ctx, a));

        Assert.True(projection.ShockMove > 0, "adverse direction for net-short delta is up");
        // GreeksAfter derives from legs (one short call, delta 0.6 → −45) plus 1 futures lot (75·e^{rT} ≈ 75.015).
        Assert.Equal(30.015, after.Delta, 2);
        Assert.True(projection.ActPnl > projection.HoldPnl);
    }
}
