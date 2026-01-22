# .NET 10 Upgrade Summary

**Date:** January 22, 2026
**Objective:** Upgrade project from .NET 8.0 to .NET 10 SDK with Aspire orchestration support

## Executive Summary

Successfully upgraded the Stock Market Simulator solution to .NET 10.0.102. All projects compile successfully. However, Aspire orchestration (DCP) requires additional configuration in .NET 10 due to architectural changes in the Aspire framework.

## Upgrade Steps Completed

### 1. .NET 10 SDK Installation

**Action:** Installed .NET 10.0.102 SDK using the official installation script
```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0
```

**Result:**
- SDK installed to: `~/.dotnet/sdk/10.0.102`
- System-wide .NET 8.0.405 remains at: `/usr/local/share/dotnet/sdk/8.0.405`

### 2. Project Target Framework Updates

Updated all project files from `net8.0` to `net10.0`:

| Project | Type | Status |
| --- | --- | --- |
| Shared.Contracts | Class Library | ✅ Updated |
| AppHost | Console App | ✅ Updated |
| MarketSimulator | Worker Service | ✅ Updated |
| Broker.Service | Web API | ✅ Updated |
| MarketIngestion.Api | Web API | ✅ Updated |

### 3. NuGet Package Updates

Updated packages to .NET 10 compatible versions:

**Aspire Packages:**
- `Aspire.Hosting.AppHost`: 8.2.0 → 9.5.2
- `Aspire.Hosting.Redis`: 8.2.0 → 9.5.2
- `Aspire.StackExchange.Redis`: 8.2.0 → 9.5.2

**Microsoft Packages:**
- `Microsoft.Extensions.Hosting`: 8.0.0 → 10.0.2
- `Microsoft.Extensions.Http`: 8.0.0 → 10.0.2

**Third-Party Packages:**
- `AspNetCore.HealthChecks.Redis`: 8.0.1 → 9.0.0
- `StackExchange.Redis`: 2.8.0 → 2.10.1
- `Swashbuckle.AspNetCore`: 6.5.0 → 10.1.0

**Rationale:** Used Aspire 9.5.2 instead of 13.1.0 due to better tooling support and maturity. Version 13.x is preview-level for .NET 10.

### 4. Solution Build Verification

**Command:** `dotnet build`
**Result:** ✅ Build succeeded - all 5 projects compiled successfully

```
Shared.Contracts net10.0 succeeded
MarketSimulator net10.0 succeeded  
Broker.Service net10.0 succeeded
MarketIngestion.Api net10.0 succeeded
AppHost net10.0 succeeded

Build succeeded in 3.2s
```

### 5. SDK Configuration

Created `global.json` in `/src` directory to ensure .NET 10 SDK is used:
```json
{
  "sdk": {
    "version": "10.0.102",
    "rollForward": "latestFeature"
  }
}
```

## Outstanding Issues

### Aspire Orchestration (DCP) Not Available

**Issue:** Aspire workload architecture changed in .NET 10. The DCP (Developer Control Plane) and Dashboard components are no longer installed via `dotnet workload install aspire`.

**Error Message:**
```
Property CliPath: The path to the DCP executable used for Aspire orchestration is required.
Property DashboardPath: The path to the Aspire Dashboard binaries is missing.
```

**Root Cause:**
- Aspire workload is deprecated in .NET 10
- Framework message: "The Aspire workload is deprecated and no longer necessary. Aspire is now available as NuGet packages"
- DCP and Dashboard must be installed separately or configured manually
- Aspire 13.x (for .NET 10) is in preview and lacks full tooling support

**Impact:**
- Solution builds successfully
- Cannot run AppHost orchestration without DCP/Dashboard
- Redis container orchestration unavailable
- Aspire dashboard UI cannot launch

## Solutions and Next Steps

### Option 1: Manual DCP Installation (Recommended for Production)

Install Aspire dashboard and DCP components separately:
```bash
# Install Aspire project templates (may include DCP)
dotnet new install Aspire.ProjectTemplates

# Or manually download and configure DCP binaries
# Configure paths in appsettings.json or environment variables
```

### Option 2: Use Docker Compose Instead

Replace Aspire orchestration with Docker Compose for local development:
- Create `docker-compose.yml` for Redis and services
- Simpler setup, proven technology
- No dependency on Aspire tooling

### Option 3: Temporary Rollback to .NET 9

Use .NET 9 with Aspire 9.x which has mature tooling:
- Target framework: `net9.0`
- Aspire version: `9.5.2`
- Full DCP/Dashboard support via workload

### Option 4: Run Services Individually

For immediate development:
```bash
# Run Redis separately
docker run -d -p 6379:6379 redis:latest

# Run each service individually
dotnet run --project MarketSimulator
dotnet run --project MarketIngestion.Api
dotnet run --project Broker.Service
```

## Files Modified

```
src/
├── global.json (created)
├── AppHost/AppHost.csproj
├── Broker.Service/Broker.Service.csproj
├── MarketIngestion.Api/MarketIngestion.Api.csproj
├── MarketSimulator/MarketSimulator.csproj
└── Shared.Contracts/Shared.Contracts.csproj
```

## Technical Decisions

### Framework Selection
- **Target:** .NET 10.0.102 (latest stable)
- **Rationale:** Future-proof, performance improvements, latest C# features

### Aspire Version
- **Choice:** 9.5.2 (not 13.1.0)
- **Rationale:** 
  - Version 9.x is stable and production-ready
  - Version 13.x is preview with incomplete tooling
  - Better community support and documentation

### Global.json Creation
- Ensures consistent SDK version across team
- Prevents fallback to system .NET 8 installation
- Enables .NET 10-specific features

## Environment Configuration

### Current State
- Primary SDK: .NET 10.0.102 (`~/.dotnet/`)
- Secondary SDK: .NET 8.0.405 (`/usr/local/share/dotnet/`)
- Active SDK (with PATH): .NET 10.0.102
- Aspire Templates: 13.1.0 installed
- Aspire Workload: Deprecated/Not functional

### PATH Configuration
Add to shell profile (`~/.zshrc` or `~/.bashrc`):
```bash
export PATH="$HOME/.dotnet:$PATH"
```

## Verification Commands

```bash
# Check .NET version
dotnet --version  # Should show: 10.0.102

# List installed SDKs
dotnet --list-sdks

# Build solution
cd src && dotnet build

# Check for package updates
dotnet list package --outdated
```

## Recommendations

1. **Immediate:** Use Option 4 (run services individually) for development
2. **Short-term:** Implement Option 2 (Docker Compose) for local orchestration
3. **Long-term:** Monitor Aspire 13.x maturity and migrate when tooling stabilizes
4. **Alternative:** Consider .NET 9 with Aspire 9.x if orchestration is critical

## References

- .NET 10 Download: https://aka.ms/dotnet/download
- Aspire Documentation: https://aka.ms/aspire/support-policy
- GitHub Issues: Track Aspire .NET 10 support progress

## Conclusion

The upgrade to .NET 10 is functionally complete. All projects build successfully and are ready for development. Aspire orchestration requires additional setup due to architectural changes in .NET 10. The solution can proceed with alternative orchestration methods while Aspire tooling matures.
