# Streamon NuGet Package Update Tasks

## Overview

This document tracks the execution of NuGet package updates across the Streamon solution. All 9 projects will have their package dependencies updated to the latest .NET 8.0-compatible versions in a single atomic operation, followed by comprehensive testing and validation.

**Progress**: 1/3 tasks complete (33%) ![0%](https://progress-bar.xyz/33)

---

## Tasks

### [✓] TASK-001: Verify prerequisites *(Completed: 2026-05-07 22:28)*
**References**: Plan §Implementation Timeline Phase 0

- [✓] (1) Verify .NET 8.0 SDK is installed and accessible via `dotnet --version`
- [✓] (2) SDK version is 8.0.x or higher (**Verify**)

---

### [▶] TASK-002: Atomic package upgrade across all projects
**References**: Plan §Implementation Timeline Phase 1, Plan §Package Update Reference, Plan §Project-by-Project Plans, Plan §Breaking Changes (if applicable)

- [ ] (1) Update all package references across all 9 projects per Plan §Package Update Reference (13 packages requiring updates: Microsoft.Extensions.* 8.0.x→10.0.7, System.Text.Json 8.0.5→10.0.7, Microsoft.Azure.Cosmos 3.46.0→3.59.0, Azure.Data.Tables 12.9.1→12.11.0, Testcontainers.* 4.0.0→4.11.0, xunit packages, coverlet.collector, Microsoft.NET.Test.Sdk, Newtonsoft.Json, Ulid)
- [ ] (2) All package references updated to target versions (**Verify**)
- [ ] (3) Restore all dependencies via `dotnet restore Streamon.sln`
- [ ] (4) All dependencies restored successfully (**Verify**)
- [ ] (5) Build solution via `dotnet build Streamon.sln --no-restore` and fix any compilation errors discovered (reference Plan §Project-by-Project Plans for expected breaking changes in Microsoft.Azure.Cosmos, System.Text.Json, and Microsoft.Extensions packages)
- [ ] (6) Solution builds with 0 errors (**Verify**)
- [ ] (7) Commit changes with message: "TASK-002: Update all NuGet packages to latest .NET 8.0-compatible versions"

---

### [ ] TASK-003: Run full test suite and validate upgrade
**References**: Plan §Testing & Validation Strategy

- [ ] (1) Run all unit test projects: `dotnet test test/Streamon.Tests/Streamon.Tests.csproj` and `dotnet test test/Streamon.Subscription.Tests/Streamon.Subscription.Tests.csproj`
- [ ] (2) Run all integration test projects (requires Docker): `dotnet test test/Streamon.Azure.TableStorage.Tests/Streamon.Azure.TableStorage.Tests.csproj` and `dotnet test test/Streamon.Azure.CosmosDb.Tests/Streamon.Azure.CosmosDb.Tests.csproj`
- [ ] (3) Fix any test failures discovered (reference Plan §Project-by-Project Plans for expected issues with Testcontainers API changes, System.Text.Json serialization behavior, or Azure SDK updates)
- [ ] (4) Re-run failed tests after fixes applied
- [ ] (5) All tests pass with 0 failures (**Verify**)
- [ ] (6) Commit test fixes with message: "TASK-003: Complete NuGet package update testing and validation"

---


