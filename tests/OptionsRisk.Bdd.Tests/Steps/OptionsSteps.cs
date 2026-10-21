using OptionsRisk.Core;
using OptionsRisk.Core.Coach;
using OptionsRisk.Core.Pricing;
using OptionsRisk.Core.Strategy;
using Reqnroll;
using Xunit;

namespace OptionsRisk.Bdd.Tests.Steps;

[Binding]
public sealed class OptionsSteps
{
    private StrategyConfig _config = new();
    private StrategyRun? _run;
    private RunSnapshot? _snapshot;
    private OptionLeg? _stopLeg;
    private CoachSuggestion? _suggestion;

    [Given(@"NIFTY spot is (\d+) and the strike step is (\d+)")]
    public void GivenSpotAndStep(double spot, double step) =>
        _config = new StrategyConfig { StartingSpot = spot, StrikeStep = step };

    [When("the user opens the strategy")]
    public void WhenOpen() => Open();

    [Then("short positions exist for 25000 CE, 25000 PE, 25500 CE and 24500 PE")]
    public void ThenStrikes()
    {
        Assert.Equal(new[] { "24500-PE", "25000-CE", "25000-PE", "25500-CE" },
            _run!.Legs.Select(l => l.Id).OrderBy(id => id).ToArray());
        Assert.All(_run.Legs, leg => Assert.Equal(-1, leg.Direction));
    }

    [Given("the strategy is open and the run uses a fixed seed")]
    public void GivenSeededRun()
    {
        _config = new StrategyConfig { StartingSpot = 25013, Seed = 42 };
        Open();
    }

    [When("NIFTY spikes up 2 percent")]
    public void WhenSpike() =>
        _snapshot = _run!.OnTick(new MarketTick(_run.Spot * 1.02, 1.7, 1));

    [Then("implied volatility rises")]
    public void ThenIvRises() => Assert.True(_snapshot!.IvMultiplier > 1);

    [Then("the coach suggests buying NIFTY futures with the lots needed to bring net delta within threshold")]
    public void ThenHedgeSuggested()
    {
        _suggestion = Assert.Single(_snapshot!.Suggestions.Where(s =>
            s.Action.Kind == CoachActionKind.BuyFutures));
        Assert.True(_suggestion.Action.Lots > 0);
        var ctx = new CoachContext(_config, _snapshot.Spot, _snapshot.IvMultiplier,
            _snapshot.FuturesPrice, _snapshot.MinutesToExpiry, _snapshot.AtmStrike,
            _run!.Legs, _run.Futures, _snapshot.Totals, _snapshot.Breakevens, _snapshot.TickIndex);
        var after = RuleMath.GreeksAfter(ctx, _suggestion.Action);
        Assert.True(Math.Abs(after.Delta) <=
            _config.DeltaHedgeThresholdLots * _config.LotSize);
    }

    [Then("it shows projected P/L with and without the hedge")]
    public void ThenProjection() =>
        Assert.True(double.IsFinite(_suggestion!.Projection.HoldPnl) &&
            double.IsFinite(_suggestion.Projection.ActPnl));

    [Given("the 25500 CE was sold at 40")]
    public void GivenShortWing()
    {
        _stopLeg = new OptionLeg {
            Id = "25500-CE", Strike = 25500, Type = OptionType.Call,
            Direction = -1, QuantityLots = 1, EntryPrice = 40, EntryIv = 0.13,
            Quote = new OptionQuote(40, 0.13, new Greeks(0.25, 0.001, -4, 3)),
        };
    }

    [When("its price reaches 80")]
    public void WhenPremiumDoubles() =>
        _stopLeg!.Reprice(new OptionQuote(80, 0.2, _stopLeg.Quote.Greeks));

    [Then("the coach recommends exiting the 25500 CE")]
    public void ThenExitSuggested()
    {
        var totals = new PortfolioTotals {
            Realized = 0, Unrealized = -40 * _config.LotSize,
            PremiumCollected = 40 * _config.LotSize, MaxLossSoFar = -40 * _config.LotSize,
            DrawdownFraction = 0, NetGreeks = _stopLeg!.Quote.Greeks
        };
        var ctx = new CoachContext(_config, 25500, 1, 25500, 300, 25000,
            [_stopLeg], [], totals, [], 1);
        _suggestion = Assert.Single(new LegStopLossRule().Evaluate(ctx));
        Assert.Equal(CoachActionKind.ExitLeg, _suggestion.Action.Kind);
    }

    [When("the user exits it")]
    public void WhenExit() => _stopLeg!.Exit(80);

    [Then(@"its realized P/L is \(40 - 80\) times lot size")]
    public void ThenRealized() =>
        Assert.Equal((40 - 80) * _config.LotSize, _stopLeg!.RealizedPoints * _config.LotSize);

    [Then("overall P/L updates")]
    public void ThenTotals() =>
        Assert.Equal(-40 * _config.LotSize,
            PortfolioMath.RealizedPnl([_stopLeg!], [], _config.LotSize));

    [Given("the strategy has open legs, closed legs and a futures hedge")]
    public void GivenMixedPortfolio()
    {
        Open();
        _run!.OnTick(new MarketTick(25100, 1, 1));
        _run.HedgeFutures(1, "BDD", "buy future");
        _run.ExitLeg("25500-CE", "BDD", "close call");
        _snapshot = _run.OnTick(new MarketTick(24900, 1.1, 2));
    }

    [Then("overall P/L equals the sum of realized and unrealized P/L less cash fees")]
    public void ThenNetPnl()
    {
        var totals = _snapshot!.Totals;
        Assert.Equal(totals.Realized + totals.Unrealized -
            (totals.Costs.Total - totals.Costs.Slippage), totals.Net, 6);
        Assert.Equal(totals.Gross - totals.Costs.Total, totals.Net, 6);
    }

    private void Open()
    {
        _run = new StrategyRun(_config, new BlackScholesPricer(_config.RiskFreeRate),
            [new DeltaHedgeRule(), new LegStopLossRule(), new StrikeBreachRule(),
             new IvSpikeRule(), new DrawdownRule()]);
        _snapshot = _run.Open(_config.StartingSpot);
    }
}
