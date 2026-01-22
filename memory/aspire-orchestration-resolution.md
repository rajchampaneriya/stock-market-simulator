# Aspire Orchestration Resolution for .NET 10

**Date:** January 22, 2026  
**Issue:** Aspire DCP (Developer Control Plane) binaries not available for .NET 10
**Status:** Documented with workarounds

## Problem Statement

After upgrading to .NET 10, the Aspire AppHost cannot run because:

1. **DCP Not Available:** Aspire 9.5.2 requires DCP (Developer Control Plane) binaries
2. **Aspire Workload Deprecated:** The `dotnet workload install aspire` command no longer installs DCP in .NET 10
3. **Package Gap:** Aspire.Hosting.AppHost 9.5.2 NuGet package doesn't include DCP binaries
4. **CLI Limitations:** The aspire.cli tool (13.1.0) doesn't bundle DCP for version 9.5.2

### Error Message
```
System.AggregateException: One or more errors occurred.
Property CliPath: The path to the DCP executable used for Aspire orchestration is required.
Property DashboardPath: The path to the Aspire Dashboard binaries is missing.
```

## Investigation Steps Taken

### 1. Aspire CLI Installation
**Attempted:** Installed `aspire.cli` version 13.1.0 global tool
```bash
dotnet tool install -g aspire.cli
```

**Result:**  
✅ Tool installed successfully  
❌ Requires .NET 10 runtime (available)  
❌ Does not include DCP binaries for Aspire 9.5.2  
❌ Cannot run AppHost with version mismatch (CLI 13.1.0 vs Packages 9.5.2)

### 2. NuGet Package Analysis
**Checked Locations:**
- `~/.nuget/packages/aspire.hosting.apphost/9.5.2/` - No DCP binaries
- `~/.nuget/packages/aspire.hosting/9.5.2/` - No DCP binaries
- `~/.dotnet/tools/.store/aspire.cli/13.1.0/` - No DCP binaries for 9.5.2

**Finding:** DCP binaries are not distributed via NuGet packages

### 3. GitHub Releases Check
**Attempted:** Search for DCP binaries in dotnet/aspire GitHub releases
```bash
curl -s "https://api.github.com/repos/dotnet/aspire/releases" | grep "dcp"
```

**Result:** ❌ DCP binaries not published in releases

### 4. Docker Runtime Configuration
**Attempted:** Configure Aspire to use Docker instead of DCP
- Modified `appsettings.json` with `"ContainerRuntime": "docker"`
- Docker is installed (version 29.1.3)

**Result:** ❌ Aspire 9.5.2 still requires DCP paths even with Docker available

## Root Cause Analysis

### Architectural Transition in .NET 10
- **.NET 8/9:** Aspire workload installed DCP binaries to system directories
- **.NET 10:** Aspire moved to NuGet-only distribution model
- **Gap Period:** Aspire 9.x (for .NET 9) doesn't fully support .NET 10 tooling
- **Future:** Aspire 13.x (for .NET 10) will have better integration, but currently immature

### Version Incompatibility Matrix

| Component | Version | .NET Target | DCP Availability |
| --- | --- | --- | --- |
| Aspire Packages | 9.5.2 | .NET 9 | ❌ Not included |
| Aspire CLI | 13.1.0 | .NET 10 | ✅ Included, but incompatible |
| Project Target | net10.0 | .NET 10 | N/A |

## Solutions & Workarounds

### Option 1: Manual Service Execution (Recommended for Development)

Run each service independently without Aspire orchestration:

**Step 1: Start Redis**
```bash
docker run -d --name stock-redis -p 6379:6379 redis:latest
```

**Step 2: Start Services**
```bash
# Terminal 1 - Market Simulator
cd src/MarketSimulator
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
dotnet run

# Terminal 2 - Market Ingestion API
cd src/MarketIngestion.Api
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
dotnet run --urls "http://localhost:5001"

# Terminal 3 - Broker Service
cd src/Broker.Service
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
dotnet run --urls "http://localhost:5002"
```

**Advantages:**
- ✅ Works immediately
- ✅ No additional dependencies
- ✅ Full debugging support
- ✅ Simple and transparent

**Disadvantages:**
- ❌ Manual process management
- ❌ No unified dashboard
- ❌ Multiple terminal windows required

###Option 2: Docker Compose Orchestration

Create `docker-compose.yml` for service orchestration:

```yaml
version: '3.8'
services:
  redis:
    image: redis:latest
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
      
  ingestion-api:
    build:
      context: ./MarketIngestion.Api
    ports:
      - "5001:8080"
    environment:
      - ConnectionStrings__Redis=redis:6379
    depends_on:
      - redis
      
  broker-service:
    build:
      context: ./Broker.Service
    ports:
      - "5002:8080"
    environment:
      - ConnectionStrings__Redis=redis:6379
    depends_on:
      - redis
      
volumes:
  redis-data:
```

**Advantages:**
- ✅ Single command startup: `docker-compose up`
- ✅ Service dependency management
- ✅ Consistent environments
- ✅ Industry standard

**Disadvantages:**
- ❌ Requires Dockerfiles for each service
- ❌ Debugging more complex
- ❌ Doesn't use Aspire structure

### Option 3: Wait for Aspire 13.x Maturity

Monitor Aspire development and upgrade when tooling improves:

- Track: https://github.com/dotnet/aspire/issues
- Current Status: Aspire 13.x is preview (released but limited tooling)
- ETA: Q1-Q2 2026 for stable .NET 10 support

**Advantages:**
- ✅ Official support
- ✅ Full feature set
- ✅ Integrated dashboard

**Disadvantages:**
- ❌ Unknown timeline
- ❌ Delays development

### Option 4: Downgrade to .NET 9

Temporarily use .NET 9 with Aspire 9.5.2:

1. Install .NET 9 SDK
2. Change target framework to `net9.0` in all projects
3. Run `dotnet build`
4. Run AppHost with DCP from .NET 9 workload

**Advantages:**
- ✅ Aspire orchestration works immediately
- ✅ Full dashboard support
- ✅ Proven stable configuration

**Disadvantages:**
- ❌ Not using latest .NET features
- ❌ Temporary rollback
- ❌ Migration effort

## Recommended Approach

**For Immediate Development:** Use **Option 1** (Manual Service Execution)
- Fastest path to productivity
- No additional setup
- Full control and visibility

**For Team Environments:** Implement **Option 2** (Docker Compose)
- Consistent across team members
- Simple onboarding
- Aligns with production deployment patterns

**Long-term Strategy:**  
- Continue monitoring Aspire 13.x development
- Keep Aspire structure in code (AppHost project)
- Migrate back to native Aspire orchestration when tooling matures
- Current code is ready - just needs DCP binaries

## Configuration Changes Made

### 1. Added Comments to Program.cs
Enhanced code documentation explaining Aspire resource relationships and orchestration intent.

### 2. Modified appsettings.json
Attempted configuration changes (non-functional without DCP):
```json
{
  "Aspire": {
    "ContainerRuntime": "docker"
  }
}
```

### 3. Installed Aspire CLI
```bash
dotnet tool install -g aspire.cli
# Version: 13.1.0
# Location: ~/.dotnet/tools/aspire
```

## Current Project State

### ✅ What Works
- All projects compile successfully
- NuGet packages are compatible
- Project structure is correct
- Aspire orchestration code is valid
- Docker is available and working

### ❌ What Doesn't Work
- AppHost execution (requires DCP)
- Aspire Dashboard (requires DCP)
- Container orchestration via Aspire

### 🔧 Required for Full Aspire Orchestration
1. DCP binaries for .NET 10 + Aspire 9.5.2
2. Aspire Dashboard binaries
3. Configuration pointing to these binaries

OR

1. Upgrade to Aspire 13.x when tooling is stable
2. Install via future workload or NuGet package

## Verification Commands

### Check Build Status
```bash
cd src
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
dotnet build
# Expected: Build succeeded
```

### Check Aspire CLI
```bash
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
aspire --version
# Expected: 13.1.0+<commit>
```

### Check Docker
```bash
docker --version
# Expected: Docker version 29.1.3+
```

### Test Individual Services
```bash
# Start Redis
docker run -d --name test-redis -p 6379:6379 redis:latest

# Test Ingestion API
cd src/MarketIngestion.Api
export PATH="$HOME/.dotnet:$PATH"
dotnet run
# Expected: Listening on http://localhost:5001

# Stop Redis
docker stop test-redis && docker rm test-redis
```

## Next Steps

1. **Immediate:** Use Option 1 (Manual Execution) for development
2. **This Week:** Create helper scripts for easier service management
3. **Next Sprint:** Implement Docker Compose (Option 2) for team consistency
4. **Monitor:** Track Aspire 13.x releases for .NET 10 DCP availability

## Additional Resources

- [Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Aspire GitHub Repository](https://github.com/dotnet/aspire)
- [Aspire Support Policy](https://aka.ms/aspire/support-policy)
- [.NET 10 Release Notes](https://github.com/dotnet/core/tree/main/release-notes/10.0)

## Conclusion

The project successfully upgraded to .NET 10 and builds without errors. Aspire orchestration structure is maintained in code. The only blocker is the lack of DCP binaries for .NET 10 + Aspire 9.5.2 combination. Workaround solutions provide immediate functionality while we wait for official tooling to mature.

**Aspire orchestration is not removed** - it's ready to be activated once DCP becomes available for this configuration.
