# Configuration Reference
## Stock Market Simulator - Complete Configuration Guide

**Version:** 1.0  
**Last Updated:** January 23, 2026

---

## Overview

This document provides a comprehensive reference for all configuration options across the Stock Market Simulator system. Each service can be configured through `appsettings.json` files and environment variables.

---

## Configuration Hierarchy

Configuration values are loaded in the following order (later sources override earlier ones):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User Secrets (Development only)
4. Environment Variables
5. Command-line arguments

---

## MarketSimulator Configuration

**Location:** `src/MarketSimulator/appsettings.json`

### Market Data Generation Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Symbols` | string[] | `["NIFTY50", "BANKNIFTY"]` | Array of stock symbols to simulate |
| `TickIntervalMs` | int | `300` | Milliseconds between price ticks (minimum: 100) |
| `Volatility` | double | `0.5` | Price volatility factor (0.0-5.0 recommended) |
| `InitialPrice` | decimal | `1000` | Starting price for all symbols |

### HTTP Client Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `IngestionApiUrl` | string | `http://localhost:5001` | Base URL for Market Ingestion API |

### Logging Configuration

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Logging:LogLevel:Default` | string | `Information` | Default log level for all components |
| `Logging:LogLevel:Microsoft.AspNetCore` | string | `Warning` | Log level for ASP.NET Core components |

### OpenTelemetry Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | string | None | OpenTelemetry collector endpoint |
| `OTEL_SERVICE_NAME` | string | `MarketSimulator` | Service name for telemetry |

### Example Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Extensions.Http": "Warning"
    }
  },
  "Symbols": ["AAPL", "GOOGL", "MSFT", "TSLA", "NVDA"],
  "TickIntervalMs": 200,
  "Volatility": 0.75,
  "InitialPrice": 150.00,
  "IngestionApiUrl": "http://localhost:5001"
}
```

---

## MarketIngestion.Api Configuration

**Location:** `src/MarketIngestion.Api/appsettings.json`

### Redis Connection Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `ConnectionStrings:cache` | string | `localhost:6379` | Redis connection string (fallback) |

> **Note:** When running under Aspire, Redis connection is automatically configured via service discovery.

### API Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `AllowedHosts` | string | `*` | Hosts that can make requests to this API |

### Logging Configuration

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Logging:LogLevel:Default` | string | `Information` | Default log level |
| `Logging:LogLevel:Microsoft.AspNetCore` | string | `Warning` | ASP.NET Core log level |

### OpenTelemetry Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | string | None | OpenTelemetry collector endpoint |
| `OTEL_SERVICE_NAME` | string | `MarketIngestion.Api` | Service name for telemetry |

### Validation Rules

The following validation rules are enforced on incoming market data:

- **Symbol**: Required, cannot be null or whitespace
- **Price**: Must be positive (> 0)
- **Volume**: Cannot be negative (>= 0)
- **Timestamp**: Cannot be more than 5 minutes in the future

### Example Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "cache": "localhost:6379"
  }
}
```

---

## Broker.Service Configuration

**Location:** `src/Broker.Service/appsettings.json`

### Redis Connection Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `ConnectionStrings:cache` | string | `localhost:6379` | Redis connection string (fallback) |
| `Redis:ConsumerNamePrefix` | string | `broker` | Prefix for Redis consumer group names |

> **Note:** Consumer names are automatically generated as: `{prefix}-{hostname}-{guid}` for horizontal scaling.

### CORS Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Cors:AllowedOrigins` | string[] | `["http://localhost:5000", "http://localhost:5173"]` | Allowed origins for browser requests |

### Channel Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Channel:Capacity` | int | `1000` | Maximum messages in bounded channel |

> **Note:** When channel is full, oldest messages are dropped (`BoundedChannelFullMode.DropOldest`).

### Redis Stream Consumer Settings

| Setting | Description | Hard-coded Value |
|---------|-------------|------------------|
| Stream Key | Redis stream name | `marketdata:stream` |
| Consumer Group | Redis consumer group | `brokers` |
| Batch Size | Messages per read | `10` |
| Reconnect Delay | Seconds between reconnect attempts | `5` |

### API Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `AllowedHosts` | string | `*` | Hosts that can make requests to this API |

### Logging Configuration

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Logging:LogLevel:Default` | string | `Information` | Default log level |
| `Logging:LogLevel:Microsoft.AspNetCore` | string | `Warning` | ASP.NET Core log level |

### OpenTelemetry Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | string | None | OpenTelemetry collector endpoint |
| `OTEL_SERVICE_NAME` | string | `Broker.Service` | Service name for telemetry |

### Production Configuration

**Location:** `src/Broker.Service/appsettings.Production.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://www.yourdomain.com"
    ]
  }
}
```

### Example Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5000",
      "http://localhost:5173",
      "http://localhost:3000"
    ]
  },
  "Redis": {
    "ConsumerNamePrefix": "broker"
  },
  "Channel": {
    "Capacity": 2000
  }
}
```

---

## Frontend Configuration

**Location:** `src/frontend/.env` or `src/frontend/.env.local`

### Environment Variables

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `VITE_BROKER_URL` | string | None | Base URL for Broker Service SSE endpoint |

> **Note:** When running under Aspire, this is automatically configured.

### Example Configuration

```env
VITE_BROKER_URL=http://localhost:5002
```

### Frontend Component Configuration

The following settings are configurable in the TypeScript code:

**Price Aggregator** (`src/frontend/src/utils/priceAggregator.ts`):

| Setting | Default | Description |
|---------|---------|-------------|
| `windowSizeMs` | `60000` | Time window for candlestick aggregation (60 seconds) |
| `maxHistory` | `30` | Maximum number of historical candles to retain |

**SSE Reconnection** (`src/frontend/src/hooks/useMarketData.ts`):

| Setting | Default | Description |
|---------|---------|-------------|
| Initial Delay | `1000ms` | Initial reconnection delay |
| Max Delay | `30000ms` | Maximum reconnection delay |
| Backoff Factor | `2` | Exponential backoff multiplier |

---

## Aspire AppHost Configuration

**Location:** `src/stock-market-simulator.AppHost/appsettings.json`

### Resource Definitions

Resources are defined in code (`AppHost.cs`), not configuration files:

- **Redis**: Named `"cache"`, with data volume and Redis Commander
- **Market Ingestion API**: Named `"ingestion-api"`
- **Market Simulator**: Named `"market-simulator"`
- **Broker Service**: Named `"broker-service"`
- **Frontend**: Named `"webfrontend"`, Vite application

### Service Dependencies

The following wait dependencies are configured:

- Market Simulator waits for Market Ingestion API
- Broker Service waits for Redis
- Frontend waits for Broker Service

---

## Health Check Endpoints

All services expose health check endpoints:

| Service | Endpoint | Dependencies Checked |
|---------|----------|---------------------|
| Market Ingestion API | `/health` | Redis connection |
| Broker Service | `/health` | Redis connection |

---

## Common Configuration Patterns

### Development vs Production

Use environment-specific configuration files:

```
appsettings.json              # Base configuration
appsettings.Development.json  # Development overrides
appsettings.Production.json   # Production overrides
```

### User Secrets (Development Only)

For sensitive data in development:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:cache" "your-connection-string"
```

### Environment Variables

Override any configuration with environment variables:

```bash
export Symbols__0="AAPL"
export Symbols__1="GOOGL"
export TickIntervalMs=500
export Channel__Capacity=5000
```

> **Note:** Use double underscores (`__`) for nested configuration (e.g., `Logging__LogLevel__Default`).

### Docker Environment Variables

When running in containers:

```yaml
environment:
  - Logging__LogLevel__Default=Information
  - TickIntervalMs=200
  - OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
```

---

## Performance Tuning Recommendations

### High Throughput Scenarios

```json
{
  "TickIntervalMs": 100,
  "Channel__Capacity": 5000,
  "Volatility": 0.3
}
```

### Low Latency Requirements

```json
{
  "TickIntervalMs": 50,
  "Channel__Capacity": 10000
}
```

### Resource Constrained Environments

```json
{
  "TickIntervalMs": 1000,
  "Channel__Capacity": 500,
  "Symbols": ["NIFTY50"]
}
```

---

## Troubleshooting

### Common Issues

**Issue:** Redis connection failures

**Solution:**
1. Check `ConnectionStrings:cache` setting
2. Verify Redis is running: `redis-cli ping`
3. Check network connectivity and firewall rules

**Issue:** High memory usage

**Solution:**
1. Reduce `Channel:Capacity` setting
2. Increase `TickIntervalMs` to reduce message frequency
3. Reduce number of `Symbols` being simulated

**Issue:** CORS errors in browser

**Solution:**
1. Add your origin to `Cors:AllowedOrigins` in Broker.Service
2. Ensure protocol (http/https) and port match exactly
3. Restart Broker.Service after configuration change

---

## Configuration Best Practices

1. **Never commit secrets**: Use User Secrets or environment variables
2. **Document custom values**: Add comments in JSON files
3. **Use environment-specific files**: Maintain separate configs for dev/prod
4. **Validate on startup**: Log configuration values on application start
5. **Use strongly-typed options**: Bind configuration to typed classes
6. **Set reasonable defaults**: Allow services to run with minimal configuration

---

## See Also

- [High-Level Design](HIGH_LEVEL_DESIGN.md) - System architecture overview
- [Low-Level Design](LOW_LEVEL_DESIGN.md) - Implementation details
- [README.md](../../README.md) - Quick start guide
- [.NET Configuration Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
- [Aspire Configuration](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/configuration)

