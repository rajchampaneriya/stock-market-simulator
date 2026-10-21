# Options Selling Risk Simulator — usage and risk-model notes

**Educational only, not investment advice. No real orders or broker integration.**

## Run the screen

1. Enable `Features:EnableOptionsRisk=true` in Broker configuration.
2. Run the Aspire AppHost and open `/options-risk` in the React app.
3. Choose a seed and select **Open strategy**. This creates a new isolated session.
4. Watch **Capital & estimated margin**, **Net P/L & trading costs**, positions and coach suggestions.
5. In **Monte Carlo: hold vs hedge**, choose paths, seed, horizon and signed futures lots; press **Run projection**.
6. The comparison shows hold/hedge mean, median, probability of loss, VaR, expected shortfall and 5th/95th percentiles. Re-run when the live spot changes.

## Fees and execution

The `OptionsRisk:BrokerProfile` setting selects `Zerodha`, `Upstox`, or `AngelOne`.
These are **illustrative standard fee schedules**, not observed averages or personalized rates.
Verify brokerage, statutory levies, settlement rules and applicable effective dates against a current broker contract note.
`IndianFeeSchedule` holds the assumptions in the pure domain.

Every opening short option is filled at a simulated adverse price and charged sell-side fees.
A user-exited option leg is filled at an adverse buy price and charged buy-side fees.
A futures hedge uses the corresponding direction and fee schedule.
Slippage is embedded in fill-based realized/unrealized P/L; it is separately reported but not deducted twice.
The net P/L identity is:

```
mid-reference gross P/L = fill-based realized + fill-based unrealized + accumulated slippage
net P/L = mid-reference gross P/L - (cash fees + accumulated slippage)
        = fill-based realized + fill-based unrealized - cash fees
```

The model does not simulate bid/ask depth, liquidity shocks, partial fills, broker-specific plans,
margin interest, exercise/settlement nuances or additional expiry-day levies.

## Margin

The screen shows a **stress proxy**, not SPAN/ELM or a broker basket-margin quote.
For ±half/full configurable spot shocks and IV expansion, it reprices the open portfolio.
Required estimated margin = largest incremental stressed loss + configured exposure add-on.
Available capital = configured virtual capital + current net P/L − estimated required margin.

The proxy blocks opening and additional hedges when estimated available capital is insufficient.
It does not auto-liquidate a position if a later price move produces a margin shortfall.
Actual NSE and broker margin can be substantially different because of offsets,
intraday rules, expiry-day add-ons, concentration, portfolio offsets and collateral haircuts.

## Intraday market clock

The shared market engine generates GBM/OU/GARCH-like raw returns using a subsecond wall-clock tick assumption. The options adapter applies a deterministic EWMA return normalizer to the configured simulated trading-minute time scale before applying separately labelled event shocks. This preserves the original engine as the data source without silently inheriting its incompatible per-tick variance. It is still a synthetic model, not historical NIFTY calibration.

## Monte Carlo

The engine uses seeded lognormal paths with optional symmetric jumps and IV mean reversion.
Both alternatives use **identical simulated market paths** (common random numbers).
The hold branch includes already-paid cash fees. The hedge branch additionally includes
estimated futures entry and scenario-specific exit fees/slippage.

Results are model-conditioned risk scenarios, **not statistically calibrated forecasts**.
VaR and CVaR are loss measures over the selected horizon, not probabilities of ruin.
The model assumes no dynamic rehedging or changes to the existing option legs during projection.
It does not estimate intrapath peak margin or liquidations.

## Validation

The options CI workflow executes domain unit tests, Reqnroll Gherkin acceptance tests,
the Broker Service build, and the React TypeScript/Vite build.
Feature flags keep the original NIFTY dashboard and market-data SSE unchanged.
In-memory sessions have a retention limit and are not suitable for multi-instance production
without persistence, authorization, and coordinated ownership.
