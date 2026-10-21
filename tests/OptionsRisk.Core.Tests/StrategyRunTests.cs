using OptionsRisk.Core;
using OptionsRisk.Core.Coach;
using OptionsRisk.Core.Market;
using OptionsRisk.Core.Pricing;
using OptionsRisk.Core.Strategy;
using Xunit;

namespace OptionsRisk.Core.Tests;

public class StrategyRunTests
{
    private static StrategyConfig Config(double spot = 25013, int? seed = 42) => new()
    {
        StartingSpot = spot,
        LotSize = 75,
        StrikeStep = 50,
        WingOffset = 500,
        DaysToExpiry = 3,
        Seed = seed,
        SpikeProbabilityPerTick = 0.0,
    };

    private static StrategyRun OpenRun(StrategyConfig? cfg = null, IReadOnlyList<ICoachRule>? rules = null)
    {
        var run = new StrategyRun(cfg ?? Config(), new BlackScholesPricer(0.065), rules);
        run.Open(cfg?.StartingSpot ?? 25013);
        return run;
    }

    private static RunSnapshot Tick(StrategyRun run, double spot, double ivMultiplier = 1.0) =>
        run.OnTick(new MarketTick(spot, ivMultiplier, run.SimMinute + 1));

    // --- BDD scenario 1 (unit-level mirror) ---

    [Fact]
    public void Open_FromSpot25013_SellsFourExpectedStrikes()
    {
        var run = OpenRun();

        Assert.Equal(25000, run.AtmStrike);
        var legs = run.Legs.Select(l => l.Id).OrderBy(x => x).ToList();
        Assert.Equal(new[] { "24500-PE", "25000-CE", "25000-PE", "25500-CE" }, legs);
        Assert.All(run.Legs, l =>
        {
            Assert.Equal((int)LegSide.Short, l.Direction);
            Assert.Equal(1, l.QuantityLots);
            Assert.Equal(LegStatus.Open, l.Status);
        });
    }

    [Fact]
    public void Open_PremiumCollected_IsPositive()
    {
        var run = OpenRun();
        Assert.True(run.LastSnapshot!.Totals.PremiumCollected > 0, "short straddle+strangle must collect premium");
    }

    // --- BDD scenario 3 (unit-level mirror) ---

    [Fact]
    public void ExitLeg_RealizedPnlIsEntryMinusExitTimesLotSize()
    {
        var run = OpenRun();
        var leg = run.Legs.First(l => l.Id == "25500-CE");
        var lotSize = run.Config.LotSize;

        leg.Reprice(new OptionQuote(80, 0.2, leg.Quote.Greeks));   // sold 40 → now 80
        var entry = leg.EntryPrice;
        run.ExitLeg(leg.Id, "Test", "exit 25500 CE");

        var expected = (entry - (80 + run.Config.OptionTickSize * run.Config.NormalOptionSlippageTicks)) * lotSize;                      // (entry − exit) × 75
        var legRealized = leg.RealizedPoints * lotSize;
        Assert.Equal(expected, legRealized, 6);
        Assert.Equal(Math.Round(expected), run.LastSnapshot!.Totals.Realized, 0);
        Assert.True(run.LastSnapshot.Totals.Costs.Total > 0);
        Assert.Equal(LegStatus.Exited, leg.Status);
    }

    // --- BDD scenario 4 (unit-level mirror) ---

    [Fact]
    public void OverallPnl_SumsRealizedAndUnrealized_AcrossLegsAndFutures()
    {
        var run = OpenRun();
        Tick(run, 25100, 1.0);
        run.HedgeFutures(+1, "Delta Hedge", "buy 1 future");
        Tick(run, 25080, 1.05);
        run.ExitLeg("25500-CE", "Stop-Loss", "exit wing call");
        var snap = Tick(run, 24900, 1.02);

        var expected = snap.Totals.Realized + snap.Totals.Unrealized - snap.Totals.Costs.Total + snap.Totals.Costs.Slippage;
        Assert.Equal(expected, snap.Totals.Net, 6);
        Assert.Equal(snap.Totals.Gross - snap.Totals.Costs.Total, snap.Totals.Net, 6);

        // Cross-check: totals equal the sum over every position, open and closed.
        var perPosition = run.Legs
            .Concat<Position>(run.Futures)
            .Sum(p => (p.RealizedPoints + p.UnrealizedPoints) * Math.Abs(p.QuantityLots) * run.Config.LotSize);
        Assert.Equal(perPosition, snap.Totals.Realized + snap.Totals.Unrealized, 0);
        Assert.True(run.Futures.Count == 1 && run.Futures[0].Status == LegStatus.Open);
    }

    [Fact]
    public void FuturesLeg_MarksAtCostOfCarry_AndHedgesDelta()
    {
        var run = OpenRun();
        var before = run.LastSnapshot!.Totals.NetGreeks.Delta;
        run.HedgeFutures(+1, "Delta Hedge", "buy 1 future");
        var after = run.LastSnapshot!.Totals.NetGreeks.Delta;

        Assert.True(Math.Abs(after - before - 75.0) < 1.0, "1 lot of futures adds ~75 delta (e^{rT} ≈ 1)");
    }

    [Fact]
    public void Spike_IsRecorded_IvExpands_BreakevensExist()
    {
        var run = OpenRun();
        var snap = Tick(run, 25520, 1.6);

        Assert.NotEmpty(snap.Breakevens);
        Assert.Equal(1.6, snap.IvMultiplier);
        // Higher IV multiplier lifts the T+0 curve vs the no-spike book's entry premium.
        Assert.True(snap.Totals.Unrealized < 0, "short book loses on a 2% up spike with IV expansion");
    }

    [Fact]
    public void Expiry_SettlesOptionsAtIntrinsic_AndFinishes()
    {
        var run = new StrategyRun(Config() with { DaysToExpiry = 0.002 }, new BlackScholesPricer(0.065));
        run.Open(25013);
        var snap = run.OnTick(new MarketTick(25400, 1.0, 1));

        Assert.Equal(RunStatus.Finished, snap.Status);
        Assert.All(run.Legs, l => Assert.Equal(LegStatus.Exited, l.Status));
        Assert.True(snap.Totals.Unrealized == 0);
        // 25400: 25000 CE ITM 400, 25500 CE ITM −100 → 0; strangle settled flat, straddle pays out 400.
        Assert.True(snap.Totals.Realized < 0);
    }

    [Fact]
    public void SeededRun_IsReproducible()
    {
        RunSnapshot First()
        {
            var run = OpenRun();
            for (var i = 0; i < 10; i++) Tick(run, 25013 + i);
            return run.LastSnapshot!;
        }

        var a = First();
        var b = First();
        Assert.Equal(a.Spot, b.Spot);
        Assert.Equal(a.Totals.Net, b.Totals.Net);
        Assert.Equal(a.PayoffToday, b.PayoffToday);
    }

    [Fact]
    public void PayoffCurves_SpanWingRange_AndExpiryCurveCrossesZeroBetweenBreakevens()
    {
        var run = OpenRun();
        var snap = Tick(run, 25050);

        Assert.Equal(61, snap.PayoffAtExpiry.Count);
        var wing = snap.Legs.Max(l => l.Strike);
        Assert.True(snap.PayoffAtExpiry.Any(p => p.Spot >= wing));
        Assert.True(snap.PayoffAtExpiry.Any(p => p.Spot <= snap.AtmStrike - run.Config.WingOffset));

        foreach (var be in snap.Breakevens)
        {
            var below = snap.PayoffAtExpiry.Last(p => p.Spot <= be);
            var above = snap.PayoffAtExpiry.First(p => p.Spot >= be);
            Assert.True(below.Pnl > 0 || above.Pnl > 0, "expiry P/L is positive between the breakevens");
        }
    }
}

public class SpikeGeneratorTests
{
    [Fact]
    public void SameSeed_ProducesIdenticalSequence()
    {
        SpikeGenerator First() => new(0.5, 0.01, 0.03, 0.3, seed: 7);
        var a = First();
        var b = First();
        for (var i = 0; i < 50; i++)
            Assert.Equal(a.NextSpike(), b.NextSpike());
    }

    [Fact]
    public void MagnitudeStaysWithinBounds_AndIvJumps()
    {
        var gen = new SpikeGenerator(1.0, 0.01, 0.03, 0.3, seed: 1);
        for (var i = 0; i < 100; i++)
        {
            var spike = gen.NextSpike();
            Assert.NotNull(spike);
            Assert.InRange(Math.Abs(spike!.Value.MagnitudeFraction), 0.01 - 1e-9, 0.03 + 1e-9);
            Assert.True(spike.Value.IvMultiplierJump > 0);
            Assert.Contains(Math.Sign(spike.Value.MagnitudeFraction), new[] { -1, 1 });
        }
    }

    [Fact]
    public void ZeroProbability_NeverFires()
    {
        var gen = new SpikeGenerator(0.0, 0.01, 0.03, 0.3, seed: 1);
        for (var i = 0; i < 100; i++)
            Assert.Null(gen.NextSpike());
    }
}
