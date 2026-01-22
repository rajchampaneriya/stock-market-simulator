# Task 03: Setup Redis in Aspire AppHost

## Objective
Configure Redis as part of the Aspire orchestration with proper data volume and optional Redis Commander UI.

## Prerequisites
- Task 01 completed (AppHost project exists)
- Aspire.Hosting.Redis NuGet package installed

## Acceptance Criteria
- [ ] Redis container added to AppHost configuration
- [ ] Data volume configured for persistence
- [ ] Redis Commander UI enabled (optional)
- [ ] Redis accessible on localhost:6379
- [ ] AppHost builds and runs successfully
- [ ] Redis shows as healthy in Aspire dashboard

## Implementation

### AppHost/Program.cs
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Redis with data volume and optional commander UI
var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithRedisCommander();

builder.Build().Run();
```

## Verification
1. Run AppHost: `dotnet run --project src/AppHost`
2. Open Aspire Dashboard: http://localhost:15888
3. Verify Redis container is running
4. Verify Redis Commander is accessible (if enabled)
5. Test Redis connection:
   ```bash
   redis-cli ping
   # Should return: PONG
   ```

## Related Sections
- Technical Approach Section 5.6 (Aspire AppHost)
- Technical Approach Section 7 (Phase 1: Foundation)

## Estimated Effort
1 hour
