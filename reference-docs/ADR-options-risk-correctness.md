# ADR — Options risk simulation correctness and service ownership

**Status:** Accepted in principle; incremental migration

## Context
The `sse-implementation` branch already hosts an educational options simulator inside Broker.Service with a pure `OptionsRisk.Core` domain and a React SSE consumer. Existing market-data SSE must not be disrupted.

## Decision
- **Target architecture:** a dedicated Options Simulation API hosting the pure domain and isolated, seeded session state. React renders server-owned state and submits explicit decisions. No trading integration.
- **Current incremental implementation:** retain the feature-flagged Broker adapter temporarily, avoiding a risky rewrite of working UI/SSE contracts. This is a transitional implementation, **not** a claim that session isolation is implemented.
- Black–Scholes is the replaceable `IOptionPricer` baseline. Correct zero-dividend gamma; verify with finite differences.
- Apply event-day shocks as a seeded log-price overlay with configurable persistence. The base GBM/OU/GARCH engine continues driving the unshocked path; event shocks must not vanish on the next tick.
- Hedge in integer futures lots to minimize remaining delta while respecting the configured risk band.
- Keep feature flag off by default. All decisions remain educational.

## Consequences and follow-up
- Broker currently has one shared in-memory run. Concurrent users can control the same session. Before exposing beyond a single-user demo, move to isolated session IDs, scoped control endpoints and per-session SSE, with retention/eviction and reconnect semantics.
- The domain does not model exchange margins, liquidity, charges, slippage, stochastic volatility surfaces, or calibrated jump processes.
- P/L projections are counterfactual scenario approximations, not forecasts.
- Reqnroll executable BDD coverage, expiry/calendar consistency, price-engine tick scaling, and CI validation remain release gates.
