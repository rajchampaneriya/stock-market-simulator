# Redis Monitoring: interpreting HTTP 503 under .NET Aspire

The Redis Monitoring page calls **GET /api/redis/streams/info** via the React dev server's Vite `/api` proxy. The API is hosted by `Broker.Service` and reads `marketdata:stream` from the Aspire `cache` Redis resource. No user credentials or live trading are involved.

## Identify the failing hop

Open browser DevTools > Network > `/api/redis/streams/info` > Response.

| Observed response | Interpretation | Action |
|---|---|---|
| HTTP 200 with `messageCount: 0` | Redis is reachable; stream empty | Check `market-simulator` and `ingestion-api` logs |
| HTTP 503 with JSON `code: redis_unavailable` | Broker connected to HTTP but Redis multiplexer unavailable | Inspect Aspire `cache`, Broker logs and Redis container |
| HTTP 503 with JSON `code: redis_timeout` | Redis command timed out | Inspect Redis container health, command latency and resource contention |
| HTTP 503 or network error without the Broker JSON body | Vite proxy may not reach Broker | Check `broker-service` status and proxy target in `vite.config.ts` |

**A failed request is not proof that the stream is empty.** The frontend now retries a transient 503 twice and reports the correct failure class; it preserves previously loaded data with a stale warning.

## Local checks

1. Start `src/stock-market-simulator.AppHost` using `dotnet run`. In the Aspire dashboard confirm `cache`, `broker-service`, `ingestion-api`, and `market-simulator` are running and healthy. `WaitFor(cache)` only handles startup dependencies; it does not guarantee Redis stays connected.
2. Use the resource details in Aspire to find the **actual** Broker endpoint. Ports are dynamically assigned; do not assume `5046` or `5002`.
3. Test the API from the same host: `curl -i http://<BROKER-HOST>:<BROKER-PORT>/api/redis/streams/info`. Also test the browser's proxied endpoint: `curl -i http://<FRONTEND-HOST>:<FRONTEND-PORT>/api/redis/streams/info`.
4. Test `/health` on the Broker. Check its logs for `Redis connection unavailable`, `Redis request timed out`, DNS/service discovery errors or connection-refused messages.
5. If Redis uses Docker, find the `cache` container using `docker ps` and test `docker exec -it <REDIS_CONTAINER_ID> redis-cli PING`. Expected response: `PONG`. Never publish Redis publicly to troubleshoot.
6. When the Broker returns 200 but stream count remains zero, inspect the Ingestion API and Market Simulator logs for failed writes. Test `redis-cli XLEN marketdata:stream` inside the container if needed.

## Architecture notes

- The Vite development proxy target is assigned by Aspire through `VITE_BROKER_URL`, with a local fallback when running Vite without the AppHost.
- The new `code` and `retryable` fields accompany the existing `error` JSON property to retain old consumers.
- Retry delay is bounded (600ms and 1200ms); a persistent outage remains visible rather than silently rendering zero streams.
- Do **not** bypass Redis or return a fabricated success response from the monitoring endpoint. This is an observability feature; failures must remain explicit.
- Redis data volumes persist across runs unless explicitly removed; do not delete a volume without intending to lose its data.

## Reproduction and validation

1. With Aspire healthy, the monitoring endpoint should respond 200 and show Redis counts.
2. Stop `cache` after startup; the page should show an unavailable message **not** `No Redis streams found`.
3. Restart `cache`; click Refresh and confirm the stream information recovers.
4. Stop only `broker-service` and confirm the frontend identifies a Broker/proxy availability issue instead of a Redis empty state.

The changes are limited to Redis-monitoring diagnostics and frontend request resilience. They do not affect options simulator prices, Redis stream data, or the original SSE channels.
