using OptionsRisk.Core.Costs;
using OptionsRisk.Core.Pricing;
using OptionsRisk.Core.Strategy;

namespace OptionsRisk.Core.Simulation;

public sealed record MonteCarloSettings(int Paths = 5000, int Steps = 30, int Seed = 42,
    double HorizonTradingMinutes = 30, double AnnualVolatility = 0.13,
    double JumpProbabilityPerStep = 0.004, double JumpMin = 0.01, double JumpMax = 0.03,
    double IvJumpPerPercent = 0.30, double IvReversion = 0.97);

public sealed record MonteCarloSummary(double Mean, double Median, double ProbabilityOfLoss,
    double Var95, double ExpectedShortfall95, double P05, double P95);
public sealed record MonteCarloComparison(MonteCarloSummary Hold, MonteCarloSummary Act,
    double EstimatedActionCosts, int Paths, int Seed, string Disclaimer);

/// <summary>Common-random-number counterfactual simulator; future paths are shared between hold and act.</summary>
public static class MonteCarloProjector
{
    public static MonteCarloComparison Compare(StrategyRun run, int signedFuturesHedgeLots,
        MonteCarloSettings? settings = null, IndianFeeSchedule? fees = null)
    {
        var s = settings ?? new MonteCarloSettings();
        if (s.Paths < 100 || s.Paths > 100_000 || s.Steps < 1 || s.Steps > 10_000 ||
            s.HorizonTradingMinutes <= 0 || s.AnnualVolatility < 0 ||
            s.JumpProbabilityPerStep is < 0 or > 1 || s.JumpMin < 0 ||
            s.JumpMax < s.JumpMin || s.IvReversion is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(settings));

        var cfg = run.Config;
        var rng = new Random(s.Seed);
        var hold = new double[s.Paths];
        var act = new double[s.Paths];
        var spot0 = run.Spot;
        var initialFutures = FuturesMath.Price(spot0, cfg.RiskFreeRate,
            run.MinutesToExpiry / (cfg.TradingMinutesPerDay * 252.0));
        var actionCosts = signedFuturesHedgeLots == 0 ? 0 :
            IndianDerivativesCosts.Calculate(DerivativeKind.Future,
                signedFuturesHedgeLots > 0 ? TradeSide.Buy : TradeSide.Sell,
                initialFutures, initialFutures, Math.Abs(signedFuturesHedgeLots) * cfg.LotSize, fees).Total;
        var dt = s.HorizonTradingMinutes / s.Steps / (cfg.TradingMinutesPerDay * 252.0);
        var remaining = Math.Max(0, run.MinutesToExpiry - s.HorizonTradingMinutes);
        var t = remaining / (cfg.TradingMinutesPerDay * 252.0);
        var pricer = new BlackScholesPricer(cfg.RiskFreeRate);
        var surface = new VolatilitySurface(cfg.BaseIv, cfg.SkewSlope);
        for (var path = 0; path < s.Paths; path++)
        {
            var spot = spot0;
            var ivMult = run.IvMultiplier;
            for (var step = 0; step < s.Steps; step++)
            {
                var u1 = Math.Max(1e-15, rng.NextDouble());
                var z = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * rng.NextDouble());
                spot *= Math.Exp(-0.5 * s.AnnualVolatility * s.AnnualVolatility * dt +
                    s.AnnualVolatility * Math.Sqrt(dt) * z);
                if (rng.NextDouble() < s.JumpProbabilityPerStep)
                {
                    var magnitude = s.JumpMin + rng.NextDouble() * (s.JumpMax - s.JumpMin);
                    spot *= 1 + (rng.NextDouble() < 0.5 ? -magnitude : magnitude);
                    ivMult = Math.Min(cfg.IvMultiplierCap, ivMult + magnitude / 0.01 * s.IvJumpPerPercent);
                }
                ivMult = 1 + (ivMult - 1) * s.IvReversion;
            }
            var pnl = 0.0;
            foreach (var leg in run.Legs)
            {
                var units = cfg.LotSize * leg.QuantityLots;
                if (leg.Status == LegStatus.Exited) pnl += leg.RealizedPoints * units;
                else
                {
                    var iv = surface.IvFor(leg.Type, spot, leg.Strike, ivMult);
                    var optionPrice = t <= 0 ? leg.Intrinsic(spot) : pricer.Price(leg.Type, spot, leg.Strike, t, iv).Price;
                    pnl += leg.Direction * (optionPrice - leg.EntryPrice) * units;
                }
            }
            var futurePrice = FuturesMath.Price(spot, cfg.RiskFreeRate, t);
            foreach (var f in run.Futures)
                pnl += (f.Status == LegStatus.Exited ? f.RealizedPoints :
                    f.Direction * (futurePrice - f.EntryPrice)) * cfg.LotSize * f.QuantityLots;
            hold[path] = pnl;
            act[path] = pnl + signedFuturesHedgeLots * cfg.LotSize *
                (futurePrice - initialFutures) - actionCosts;
        }
        return new MonteCarloComparison(Summarize(hold), Summarize(act), actionCosts, s.Paths,
            s.Seed, "Educational model-conditioned scenarios, not market forecasts; no broker-calibrated margin or order-book impact.");
    }

    private static MonteCarloSummary Summarize(double[] pnl)
    {
        Array.Sort(pnl);
        var tail = Math.Max(1, (int)Math.Ceiling(pnl.Length * 0.05));
        var mean = pnl.Average();
        return new MonteCarloSummary(mean, pnl[pnl.Length / 2],
            pnl.Count(x => x < 0) / (double)pnl.Length,
            -pnl[tail - 1], -pnl.Take(tail).Average(),
            pnl[tail - 1], pnl[Math.Min(pnl.Length - 1, (int)(pnl.Length * 0.95))]);
    }
}
