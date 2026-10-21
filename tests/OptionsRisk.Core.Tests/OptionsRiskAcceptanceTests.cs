using OptionsRisk.Core;
using OptionsRisk.Core.Coach;
using OptionsRisk.Core.Pricing;
using OptionsRisk.Core.Strategy;
using Xunit;

namespace OptionsRisk.Core.Tests;

/// <summary>Executable Given/When/Then acceptance scenarios at the pure-domain boundary.</summary>
public sealed class OptionsRiskAcceptanceTests
{
    private static StrategyRun GivenOpenBook() {
        var config = new StrategyConfig { StartingSpot = 25013, Seed = 42, SpikeProbabilityPerTick = 0 };
        var run = new StrategyRun(config, new BlackScholesPricer(config.RiskFreeRate),
            [new DeltaHedgeRule(), new LegStopLossRule(), new StrikeBreachRule(), new IvSpikeRule(), new DrawdownRule()]);
        run.Open(config.StartingSpot);
        return run;
    }

    [Fact]
    public void GivenOpeningSpot_WhenOpeningStrategy_ThenFourShortOptionsAreCreated() {
        var run = GivenOpenBook();
        Assert.Equal(4, run.Legs.Count);
        Assert.All(run.Legs, l => Assert.Equal(-1, l.Direction));
        Assert.Equal(25000, run.AtmStrike);
    }

    [Fact]
    public void GivenSeededRun_WhenApplyingSameMarketPath_ThenSnapshotsMatch() {
        static RunSnapshot Scenario() {
            var run = GivenOpenBook();
            for (var i = 1; i <= 12; i++)
                run.OnTick(new MarketTick(25013 + i * 3, 1.0, i));
            return run.LastSnapshot!;
        }
        var first = Scenario();
        var second = Scenario();
        Assert.Equal(first.Totals.Net, second.Totals.Net);
        Assert.Equal(first.PayoffToday, second.PayoffToday);
    }

    [Fact]
    public void GivenVolatilitySpike_WhenRepricing_ThenShortBookLosesValue() {
        var normal = GivenOpenBook();
        var spiked = GivenOpenBook();
        var baseline = normal.OnTick(new MarketTick(25013, 1.0, 1));
        var stressed = spiked.OnTick(new MarketTick(25013, 1.8, 1));
        Assert.True(stressed.Totals.Net < baseline.Totals.Net);
    }

    [Fact]
    public void GivenAnOpenLeg_WhenClosingIt_ThenItsStatusIsExited() {
        var run = GivenOpenBook();
        var id = run.Legs[0].Id;
        run.ExitLeg(id, "Acceptance", "Close threatened leg");
        Assert.Equal(LegStatus.Exited, run.Legs.Single(l => l.Id == id).Status);
    }
}
