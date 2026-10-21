using OptionsRisk.Core;
using OptionsRisk.Core.Costs;
using OptionsRisk.Core.Margin;
using OptionsRisk.Core.Pricing;
using OptionsRisk.Core.Strategy;
using Xunit;

namespace OptionsRisk.Core.Tests;

public sealed class CapitalAccountingTests
{
    private static StrategyRun Open(StrategyConfig? cfg = null)
    {
        cfg ??= new StrategyConfig();
        var run = new StrategyRun(cfg, new BlackScholesPricer(cfg.RiskFreeRate));
        run.Open(cfg.StartingSpot);
        return run;
    }

    [Fact]
    public void EntryChargesAndSlippageAppearImmediatelyInNetPnl()
    {
        var run = Open();
        var totals = run.LastSnapshot!.Totals;
        Assert.True(totals.Costs.Brokerage >= 80);
        Assert.True(totals.Costs.Stt > 0);
        Assert.True(totals.Costs.Slippage > 0);
        Assert.Equal(totals.Gross - totals.Costs.Total, totals.Net, 6);
        Assert.Equal(totals.Realized + totals.Unrealized - totals.Costs.Total +
            totals.Costs.Slippage, totals.Net, 6);
        Assert.True(totals.Net < 0);
    }

    [Fact]
    public void ClosingLegBooksAdditionalBuyFeesWithoutDoubleChargingSlippage()
    {
        var run = Open();
        var before = run.LastSnapshot!.Totals.Costs;
        run.ExitLeg(run.Legs[0].Id, "Test", "Close");
        var after = run.LastSnapshot!.Totals.Costs;
        Assert.True(after.Brokerage > before.Brokerage);
        Assert.True(after.Slippage > before.Slippage);
        Assert.Equal(run.LastSnapshot.Totals.Gross - after.Total, run.LastSnapshot.Totals.Net, 6);
    }

    [Fact]
    public void MarginEstimatorFlagsCapitalShortfall()
    {
        var estimate = PortfolioMarginEstimate.Estimate(1000, 25000, 75, 4, 0,
            [new MarginScenario(-0.03, 5, -40000), new MarginScenario(0.03, 5, -60000)]);
        Assert.True(estimate.MarginShortfall);
        Assert.True(estimate.Required > 1000);
    }

    [Fact]
    public void MarginGuardRejectsUnderfundedOpening()
    {
        var cfg = new StrategyConfig { Capital = 1000, EnforceEstimatedMargin = true };
        var run = new StrategyRun(cfg, new BlackScholesPricer(cfg.RiskFreeRate));
        Assert.Throws<InvalidOperationException>(() => run.Open(cfg.StartingSpot));
    }

    [Fact]
    public void CashFeesAreNotSlippage()
    {
        var ledger = new ExecutionAccounting();
        var fill = SimulatedFill.Price(40, TradeSide.Sell, 0.05, 2);
        var cost = IndianDerivativesCosts.Calculate(DerivativeKind.Option,
            TradeSide.Sell, fill, 40, 75);
        ledger.Record(cost);
        Assert.Equal(39.9, fill, 6);
        Assert.Equal(7.5, ledger.Slippage, 6);
        Assert.Equal(cost.Total - cost.Slippage, ledger.Fees, 6);
    }
}
