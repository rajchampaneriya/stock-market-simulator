using OptionsRisk.Core;
using OptionsRisk.Core.Pricing;
using OptionsRisk.Core.Simulation;
using OptionsRisk.Core.Strategy;
using Xunit;

namespace OptionsRisk.Core.Tests;

public sealed class MonteCarloProjectorTests
{
    private static StrategyRun Open()
    {
        var cfg = new StrategyConfig { Seed = 42 };
        var run = new StrategyRun(cfg, new BlackScholesPricer(cfg.RiskFreeRate));
        run.Open(25000);
        return run;
    }

    [Fact]
    public void SameSeedProducesIdenticalDistributions()
    {
        var run = Open();
        var settings = new MonteCarloSettings(Paths: 1000, Seed: 7);
        Assert.Equal(MonteCarloProjector.Compare(run, 1, settings),
                     MonteCarloProjector.Compare(run, 1, settings));
    }

    [Fact]
    public void ZeroActionProducesSameHoldAndActDistribution()
    {
        var result = MonteCarloProjector.Compare(Open(), 0, new MonteCarloSettings(Paths: 1000));
        Assert.Equal(result.Hold, result.Act);
        Assert.Equal(0, result.EstimatedActionCosts);
    }

    [Fact]
    public void HedgeIncludesExecutionCharges()
    {
        var result = MonteCarloProjector.Compare(Open(), 1, new MonteCarloSettings(Paths: 1000));
        Assert.True(result.EstimatedActionCosts > 0);
        Assert.InRange(result.Hold.ProbabilityOfLoss, 0, 1);
        Assert.InRange(result.Act.ProbabilityOfLoss, 0, 1);
    }
}
