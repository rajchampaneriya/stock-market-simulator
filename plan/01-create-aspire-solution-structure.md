# Task 01: Create Aspire Solution Structure

## Objective
Set up the foundational .NET Aspire solution with proper project structure and references.

## Prerequisites
- .NET 10 SDK installed
- .NET Aspire workload installed (`dotnet workload install aspire`)

## Acceptance Criteria
- [ ] Solution file `StockMarketSimulator.sln` created
- [ ] AppHost project created at `src/AppHost/`
- [ ] All project folders created under `src/`:
  - AppHost/
  - Shared.Contracts/
  - MarketSimulator/
  - MarketIngestion.Api/
  - Broker.Service/
  - WebApp/
- [ ] Project references properly configured
- [ ] Solution builds successfully (`dotnet build`)

## Implementation Steps
1. Create solution: `dotnet new sln -n StockMarketSimulator`
2. Create Aspire AppHost: `dotnet new aspire-apphost -n AppHost -o src/AppHost`
3. Create class library for contracts: `dotnet new classlib -n Shared.Contracts -o src/Shared.Contracts`
4. Create worker service: `dotnet new worker -n MarketSimulator -o src/MarketSimulator`
5. Create minimal API: `dotnet new webapi -n MarketIngestion.Api -o src/MarketIngestion.Api`
6. Create API service: `dotnet new webapi -n Broker.Service -o src/Broker.Service`
7. Add all projects to solution
8. Configure project references

## Verification
```bash
dotnet build
# Should build without errors
```

## Related Sections
- Technical Approach Section 4 (Project Structure)
- Technical Approach Section 7 (Phase 1: Foundation)

## Estimated Effort
1-2 hours
