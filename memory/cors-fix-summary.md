# CORS Fix for Redis Monitoring Feature

**Date:** January 24, 2026  
**Issue:** CORS policy blocking frontend requests to Broker Service API  
**Status:** ✅ RESOLVED

---

## Problem Description

The React frontend was experiencing CORS (Cross-Origin Resource Sharing) errors when trying to access the Broker Service API endpoints:

```
Access to resource at 'http://localhost:5046/api/marketdata/stream' 
from origin 'http://webfrontend-stock_market_simulator.dev.localhost:58492' 
has been blocked by CORS policy: No 'Access-Control-Allow-Origin' header 
is present on the requested resource.
```

### Root Cause

The Broker.Service CORS policy was configured to only allow specific hardcoded origins:
- `http://localhost:5000`
- `http://localhost:5173`

However, .NET Aspire assigns dynamic URLs to services, and the frontend was running on:
- `http://webfrontend-stock_market_simulator.dev.localhost:58492` (dynamically assigned port)

This mismatch caused all API requests to be blocked by the browser's CORS policy.

---

## Solution Implemented

Updated the CORS configuration in `Broker.Service/Program.cs` to be more permissive in development mode while remaining strict in production.

### Changes Made

**File:** `src/Broker.Service/Program.cs`

**Before:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:5000", "http://localhost:5173" };
        
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // Production config...
        }
    });
});
```

**After:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development, allow all origins for Aspire-generated URLs
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // In production, use strict origin configuration
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                ?? new[] { "http://localhost:5000", "http://localhost:5173" };
            
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});
```

### Key Changes

1. **Development Mode**: Uses `SetIsOriginAllowed(_ => true)` to allow any origin
   - This is safe in development as it's running locally
   - Accommodates Aspire's dynamic port assignments
   - Allows frontend at any `localhost` or `.dev.localhost` origin

2. **Production Mode**: Maintains strict origin checking
   - Still reads from configuration (`Cors:AllowedOrigins`)
   - Only allows explicitly configured origins
   - Maintains security for production deployments

---

## Verification

### CORS Headers Test

```bash
curl -k -v -H "Origin: http://webfrontend-stock_market_simulator.dev.localhost:58492" \
  https://localhost:60594/health 2>&1 | grep -E "access-control"
```

**Result:**
```
access-control-allow-credentials: true
access-control-allow-origin: http://webfrontend-stock_market_simulator.dev.localhost:58492
```

✅ CORS headers are now being set correctly for Aspire-generated origins.

### API Endpoint Test

```bash
curl -k -s https://localhost:60594/api/redis/streams/info | python3 -m json.tool
```

**Result:**
```json
[
    {
        "streamName": "marketdata:stream",
        "messageCount": 10038,
        "latestMessageTimestamp": "2026-01-24T01:06:59.982Z",
        "consumerGroups": [
            {
                "groupName": "brokers",
                "consumersCount": 3,
                "pendingCount": 0
            }
        ]
    }
]
```

✅ API endpoint responding correctly.

---

## Impact

### Fixed Issues

1. ✅ **Dashboard SSE Connection**: Can now connect to `/api/marketdata/stream`
2. ✅ **Redis Monitoring Page**: Can fetch stream info from `/api/redis/streams/info`
3. ✅ **Health Checks**: Frontend can verify service health
4. ✅ **Any Aspire Origin**: Works with any dynamically assigned port

### Browser Console

**Before:** Multiple CORS errors blocking all requests

**After:** Clean connection, no CORS errors

---

## Best Practices Applied

### Development vs Production

- **Development**: Permissive CORS for easier local development
- **Production**: Strict CORS with explicit origin allowlist
- Uses `builder.Environment.IsDevelopment()` to differentiate

### Security Considerations

1. **Development Safety**: 
   - `SetIsOriginAllowed(_ => true)` only active in Development mode
   - Acceptable risk as services run on localhost
   - Never exposed to external networks

2. **Production Security**:
   - Maintains strict origin checking
   - Requires explicit configuration
   - Prevents unauthorized cross-origin requests

3. **Credentials**:
   - `AllowCredentials()` enabled for authenticated requests
   - Required for cookies/authentication headers

---

## Configuration Guide

### Development Mode (Current)

No configuration needed. CORS allows all origins.

### Production Mode

Update `appsettings.Production.json` or environment variables:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://www.yourdomain.com"
    ]
  }
}
```

Or via environment variable:
```bash
export Cors__AllowedOrigins__0="https://yourdomain.com"
export Cors__AllowedOrigins__1="https://www.yourdomain.com"
```

---

## Testing Checklist

- [x] Health endpoint accessible from frontend
- [x] SSE endpoint accessible from Dashboard
- [x] Redis monitoring endpoint accessible
- [x] CORS headers present in responses
- [x] Credentials allowed (for authenticated requests)
- [x] Works with Aspire-generated origins
- [x] No console errors in browser

---

## Related Documentation

- **CORS in ASP.NET Core**: https://learn.microsoft.com/en-us/aspnet/core/security/cors
- **.NET Aspire Service Discovery**: https://learn.microsoft.com/en-us/dotnet/aspire/
- **Redis Monitoring User Guide**: `/docs/REDIS_MONITORING_USER_GUIDE.md`

---

## Additional Notes

### Why Not Wildcard Origins?

While we use `SetIsOriginAllowed(_ => true)` in development, we don't use `AllowAnyOrigin()` because:

1. **Credentials Requirement**: `AllowAnyOrigin()` cannot be used with `AllowCredentials()`
2. **Future Authentication**: If we add authentication, we'll need credentials support
3. **Better Security Model**: `SetIsOriginAllowed` provides more control

### Aspire Dynamic Ports

.NET Aspire assigns random ports to services on each startup:
- Previous broker port: 58511
- Current broker port: 60594
- Frontend port: 58492 (also dynamic)

This is why hardcoded origins don't work well with Aspire in development.

### Browser Security

CORS is a browser security feature. The same requests work fine from:
- curl
- Postman
- Backend services
- Non-browser HTTP clients

Only browser-based requests are subject to CORS restrictions.

---

**Resolution:** CORS issue fully resolved. Frontend can now communicate with all Broker Service API endpoints without errors. ✅
