using OptionsRisk.Core.Pricing;
using Xunit;

namespace OptionsRisk.Core.Tests;

public class BlackScholesPricerTests
{
    private readonly BlackScholesPricer _pricer = new(riskFreeRate: 0.05);

    // Hull, Options, Futures and Other Derivatives — Ch. 19 worked example (r = 10%).
    [Fact]
    public void Call_MatchesHullReferenceValue()
    {
        var hull = new BlackScholesPricer(0.10);
        var q = hull.Price(OptionType.Call, spot: 42, strike: 40, yearsToExpiry: 0.5, volatility: 0.20);
        Assert.InRange(q.Price, 4.70, 4.82);
    }

    [Fact]
    public void Put_MatchesHullReferenceValue()
    {
        var hull = new BlackScholesPricer(0.10);
        var q = hull.Price(OptionType.Put, spot: 42, strike: 40, yearsToExpiry: 0.5, volatility: 0.20);
        Assert.InRange(q.Price, 0.75, 0.87);
    }

    [Fact]
    public void PutCallParity_Holds()
    {
        var r = 0.065;
        var p = new BlackScholesPricer(r);
        var call = p.Price(OptionType.Call, 25013, 25000, 3.0 / 252.0, 0.13).Price;
        var put = p.Price(OptionType.Put, 25013, 25000, 3.0 / 252.0, 0.13).Price;
        var parity = 25013 - 25000 * Math.Exp(-r * 3.0 / 252.0);
        Assert.Equal(parity, call - put, 4);
    }

    [Fact]
    public void AtmStraddle_ApproximatelyFourSigSqrtT()
    {
        var t = 3.0 / 252.0;
        var p = new BlackScholesPricer(0.0);
        var call = p.Price(OptionType.Call, 25000, 25000, t, 0.13).Price;
        var put = p.Price(OptionType.Put, 25000, 25000, t, 0.13).Price;
        // ATM approximation: straddle ≈ 0.8 · S · σ · √T
        var approx = 0.8 * 25000 * 0.13 * Math.Sqrt(t);
        Assert.InRange(call + put, approx * 0.9, approx * 1.1);
    }

    [Fact]
    public void Greeks_HaveExpectedSigns()
    {
        var call = _pricer.Price(OptionType.Call, 25000, 25500, 0.05, 0.15);
        var put = _pricer.Price(OptionType.Put, 25000, 24500, 0.05, 0.15);
        var callAtm = _pricer.Price(OptionType.Call, 25000, 25000, 0.05, 0.15);
        var putAtm = _pricer.Price(OptionType.Put, 25000, 25000, 0.05, 0.15);

        Assert.InRange(call.Greeks.Delta, 0, 1);
        Assert.InRange(put.Greeks.Delta, -1, 0);
        Assert.True(call.Greeks.Gamma > 0);
        Assert.True(put.Greeks.Gamma > 0);
        Assert.True(call.Greeks.ThetaPerDay < 0, "long option theta must decay");
        Assert.True(call.Greeks.VegaPerPoint > 0);
        Assert.Equal(callAtm.Greeks.Delta - 1.0, putAtm.Greeks.Delta, 6);
    }

    [Fact]
    public void Prices_AreMonotonic()
    {
        var atm = _pricer.Price(OptionType.Call, 25000, 25000, 0.05, 0.15).Price;
        var otm = _pricer.Price(OptionType.Call, 25000, 25200, 0.05, 0.15).Price;
        Assert.True(atm > otm, "call price must fall as strike rises");

        var otmPut = _pricer.Price(OptionType.Put, 25000, 24800, 0.05, 0.15).Price;
        var atmPut = _pricer.Price(OptionType.Put, 25000, 25000, 0.05, 0.15).Price;
        Assert.True(atmPut > otmPut, "put price must rise as strike falls");
    }

    [Fact]
    public void VolUp_IncreasesOptionPrice()
    {
        var low = _pricer.Price(OptionType.Call, 25000, 25000, 0.05, 0.12).Price;
        var high = _pricer.Price(OptionType.Call, 25000, 25000, 0.05, 0.25).Price;
        Assert.True(high > low);
    }

    [Fact]
    public void NearExpiry_DeepItm_ConvergesToIntrinsic()
    {
        var q = _pricer.Price(OptionType.Call, spot: 25600, strike: 25000, yearsToExpiry: 1e-6, volatility: 0.13);
        Assert.Equal(600, q.Price, 1);
    }

    [Fact]
    public void Skew_OtmPutRicherThanOtmCall()
    {
        var surface = new VolatilitySurface(baseIv: 0.13, skewSlope: 0.35);
        var putIv = surface.IvFor(OptionType.Put, 25000, 24500, 1.0);
        var callIv = surface.IvFor(OptionType.Call, 25000, 25500, 1.0);
        Assert.True(putIv > callIv, "index skew: OTM puts trade above OTM calls");
        Assert.Equal(0.13, surface.IvFor(OptionType.Call, 25000, 25000, 1.0), 6);
    }

    [Fact]
    public void Futures_CostOfCarry()
    {
        var f = FuturesMath.Price(25000, 0.065, 30.0 / 365.0);
        Assert.True(f > 25000, "futures must trade over spot with positive carry");
        Assert.Equal(25000 * Math.Exp(0.065 * 30.0 / 365.0), f, 6);
    }
}
