# NuGet Package Update Plan - .NET 8.0

## Table of Contents

- [Executive Summary](#executive-summary)
- [Migration Strategy](#migration-strategy)
- [Detailed Dependency Analysis](#detailed-dependency-analysis)
- [Project-by-Project Plans](#project-by-project-plans)
- [Risk Management](#risk-management)
- [Testing & Validation Strategy](#testing--validation-strategy)
- [Complexity & Effort Assessment](#complexity--effort-assessment)
- [Source Control Strategy](#source-control-strategy)
- [Success Criteria](#success-criteria)

---

## Executive Summary

### Scenario Description

This plan details the strategy for updating all NuGet package dependencies across the Streamon solution while maintaining the current .NET 8.0 target framework. The solution consists of 9 projects with 16 direct NuGet package dependencies, many of which have newer versions available.

### Scope

**Projects Affected:** All 9 projects in the solution
- 4 source projects (class libraries)
- 5 test projects

**Current State:** All projects targeting .NET 8.0

**Target State:** All projects remain on .NET 8.0 with all NuGet packages updated to their latest compatible versions

### Discovered Metrics

| Metric | Value |
|--------|-------|
| Total Projects | 9 |
| Projects with Outdated Packages | 8 (1 project has no updates) |
| Total Direct Package References | 16 unique packages |
| Packages Requiring Updates | 13 packages |
| Total Code Files | 123 |
| Total Lines of Code | 4,683 |
| Maximum Dependency Depth | 2 levels |
| Projects with Zero Dependencies | 2 (Streamon.csproj, Streamon.Tests.Fixtures.csproj) |

### Complexity Classification

**Classification: Simple**

**Justification:**
- Small solution (9 projects, ≤15 threshold)
- Shallow dependency depth (maximum 2 levels, ≤2 threshold)
- No high-risk indicators (no security vulnerabilities, no breaking API changes reported)
- All packages are already compatible with .NET 8.0 (no framework compatibility issues)
- No circular dependencies detected
- Homogeneous codebase (all .NET 8.0, SDK-style projects)

### Selected Strategy

**All-At-Once Strategy** - All package updates applied simultaneously in a single coordinated operation.

**Rationale:**
- Solution size is small (9 projects)
- All projects currently on .NET 8.0 (no framework upgrades required)
- Clear dependency structure with shallow depth
- All package updates are version bumps with no reported breaking changes
- Assessment shows all packages already compatible with target framework
- Fast completion time with minimal risk given simple structure

### Iteration Strategy

Given the simple classification, this plan will use a **Fast Batch** approach:
- **Phase 1 (Discovery & Foundation):** Iterations 1.1-2.3 (completed above)
- **Phase 2 (Detail Generation):** 2 consolidated iterations
  - Iteration 3.1: Package update specifications + all project details
  - Iteration 3.2: Risk, complexity, testing, source control, and success criteria

**Expected Total Iterations:** 6 iterations (3 foundation + 2 detail + 1 final)

---

## Migration Strategy

### Approach Selection

**Selected Approach: All-At-Once Strategy**

All package updates will be applied simultaneously across all projects in a single coordinated operation.

### All-At-Once Strategy Rationale

**Ideal Conditions Met:**
- ✅ Small solution (9 projects, well below 30-project threshold)
- ✅ All projects currently on .NET 8.0 (homogeneous)
- ✅ Consistent patterns across codebase
- ✅ Low external dependency complexity
- ✅ All packages have known compatible versions with .NET 8.0
- ✅ No security vulnerabilities requiring staged mitigation

**Advantages for This Solution:**
- Fastest completion time (single operation vs. multiple phases)
- No multi-targeting complexity
- All projects benefit simultaneously from updates
- Clean dependency resolution in one pass
- Simple coordination (no intermediate states to manage)
- Single test cycle validates entire update

**Risk Assessment:**
- **Overall Risk: Low**
- No breaking changes reported in assessment
- All packages moving from .NET 8.0-compatible versions to newer .NET 8.0-compatible versions
- Primary risk is minor API changes in major version bumps (e.g., Microsoft.Azure.Cosmos 3.46→3.59, System.Text.Json 8.0.5→10.0.7)

### Dependency-Based Ordering

While all package updates occur simultaneously, subsequent operations follow dependency order:

**Update Phase (Simultaneous):**
- All `PackageReference` elements updated atomically across all 9 project files

**Validation Phase (Sequential by Dependency Order):**
1. Restore dependencies (automatic transitive resolution)
2. Build in dependency order:
   - Layer 0: `Streamon.csproj`, `Streamon.Tests.Fixtures.csproj`
   - Layer 1: `Streamon.Subscription.csproj`, `Streamon.Tests.csproj`
   - Layer 2: Azure providers + `Streamon.Subscription.Tests.csproj`
   - Layer 3: Provider test projects
3. Fix any compilation errors discovered
4. Run all test projects

### Execution Approach

**Single Atomic Operation:**
All project files will be updated in one commit, ensuring no intermediate broken state exists in version control.

**Rollback Strategy:**
If critical issues are discovered, the entire update can be rolled back as a single unit by reverting the commit.

---

## Detailed Dependency Analysis

### Dependency Graph Summary

The solution has a clear hierarchical dependency structure with two foundational projects at the bottom:

**Dependency Layers:**

1. **Layer 0 (Foundation - No Dependencies):**
   - `Streamon.csproj` - Core abstractions and types
   - `Streamon.Tests.Fixtures.csproj` - Shared test fixtures

2. **Layer 1 (Depends on Layer 0):**
   - `Streamon.Subscription.csproj` → depends on `Streamon.csproj`
   - `Streamon.Tests.csproj` → depends on `Streamon.csproj` + `Streamon.Tests.Fixtures.csproj`

3. **Layer 2 (Depends on Layer 1):**
   - `Streamon.Azure.CosmosDb.csproj` → depends on `Streamon.Subscription.csproj` + `Streamon.csproj`
   - `Streamon.Azure.TableStorage.csproj` → depends on `Streamon.Subscription.csproj`
   - `Streamon.Subscription.Tests.csproj` → depends on `Streamon.Subscription.csproj` + `Streamon.Tests.Fixtures.csproj`

4. **Layer 3 (Test Projects):**
   - `Streamon.Azure.CosmosDb.Tests.csproj` → depends on `Streamon.Azure.CosmosDb.csproj` + `Streamon.Tests.Fixtures.csproj`
   - `Streamon.Azure.TableStorage.Tests.csproj` → depends on `Streamon.Azure.TableStorage.csproj` + `Streamon.Tests.Fixtures.csproj`

### Project Groupings by Migration Phase

Since this is an **All-At-Once** migration, all projects will be updated simultaneously. However, for organizational clarity, projects are grouped by their role:

**Group 1: Core Libraries (4 projects)**
- `Streamon.csproj`
- `Streamon.Subscription.csproj`
- `Streamon.Azure.CosmosDb.csproj`
- `Streamon.Azure.TableStorage.csproj`

**Group 2: Test Infrastructure & Tests (5 projects)**
- `Streamon.Tests.Fixtures.csproj`
- `Streamon.Tests.csproj`
- `Streamon.Subscription.Tests.csproj`
- `Streamon.Azure.CosmosDb.Tests.csproj`
- `Streamon.Azure.TableStorage.Tests.csproj`

### Critical Path Identification

**Critical Path:** Foundation → Core → Providers → Tests
1. `Streamon.csproj` (0 dependencies) - **Most critical** - all other projects depend on this
2. `Streamon.Subscription.csproj` (1 dependency) - 3 projects depend on this
3. Azure provider projects (CosmosDb, TableStorage) - 1 dependent each (their test projects)
4. Test projects - No dependents

**Build Order Sensitivity:** None for package updates (all projects updated simultaneously), but builds must occur in dependency order after updates are applied.

### Circular Dependencies

**Status:** None detected

The dependency graph is a clean Directed Acyclic Graph (DAG) with no cycles.

---

## Project-by-Project Plans

### Package Update Reference

The following table consolidates all package updates across the solution. Packages are grouped by scope.

#### Common Package Updates (Multiple Projects)

| Package | Current Version | Target Version | Projects Affected | Update Reason |
|---------|----------------|----------------|-------------------|---------------|
| **coverlet.collector** | 6.0.2 | 10.0.0 | 4 test projects | Code coverage tooling - major version update for .NET 8.0+ improvements |
| **Microsoft.Extensions.DependencyInjection** | 8.0.1 | 10.0.7 | 4 test projects | Dependency injection container - major version bump to latest stable |
| **Microsoft.NET.Test.Sdk** | 17.12.0 | 18.5.1 | 4 test projects | Test platform SDK - version alignment with latest VS 2022 |
| **Newtonsoft.Json** | 13.0.3 | 13.0.4 | 6 projects (1 source, 5 tests) | JSON serialization - patch version for bug fixes |
| **System.Text.Json** | 8.0.5 | 10.0.7 | 4 projects (2 source, 2 tests) | Modern JSON serialization - major version for enhanced performance |
| **xunit** | 2.9.2 | 2.9.3 | 5 test projects | Test framework - patch version update |
| **xunit.runner.visualstudio** | 2.8.2 | 3.1.5 | 4 test projects | Test runner - major version for VS 2022 compatibility improvements |

#### Azure & Storage-Specific Updates

| Package | Current Version | Target Version | Projects Affected | Update Reason |
|---------|----------------|----------------|-------------------|---------------|
| **Microsoft.Azure.Cosmos** | 3.46.0 | 3.59.0 | Streamon.Azure.CosmosDb.csproj | Cosmos DB SDK - minor version updates (13 releases) for perf & features |
| **Azure.Data.Tables** | 12.9.1 | 12.11.0 | Streamon.Azure.TableStorage.csproj | Table Storage SDK - minor version for improvements |

#### Test Infrastructure Updates

| Package | Current Version | Target Version | Projects Affected | Update Reason |
|---------|----------------|----------------|-------------------|---------------|
| **Testcontainers.Azurite** | 4.0.0 | 4.11.0 | Streamon.Azure.TableStorage.Tests.csproj | Azurite container support - minor version with 11 releases of improvements |
| **Testcontainers.CosmosDb** | 4.0.0 | 4.11.0 | Streamon.Azure.CosmosDb.Tests.csproj | Cosmos DB container support - minor version with 11 releases |
| **xunit.core** | 2.9.2 | 2.9.3 | Streamon.Tests.Fixtures.csproj | xUnit core infrastructure - patch update |

#### Core Library Updates

| Package | Current Version | Target Version | Projects Affected | Update Reason |
|---------|----------------|----------------|-------------------|---------------|
| **Microsoft.Extensions.DependencyInjection.Abstractions** | 8.0.2 | 10.0.7 | Streamon.csproj | DI abstractions - major version for consistency with DI container |
| **Microsoft.Extensions.Options** | 8.0.2 | 10.0.7 | Streamon.csproj | Options pattern - major version update |
| **Ulid** | 1.3.4 | 1.4.1 | Streamon.csproj | ULID generation - minor version for improvements |

#### Packages with No Updates

| Package | Current Version | Projects Affected | Reason |
|---------|----------------|-------------------|--------|
| **xunit.abstractions** | 2.0.3 | Streamon.Tests.Fixtures.csproj | Already at latest version |

---

### Core Library Projects

#### 1. Streamon.csproj

**Current State:**
- Target Framework: net8.0
- Dependencies: 0 project references
- Package Count: 3 packages
- Lines of Code: 663
- Risk Level: Low

**Target State:**
- Target Framework: net8.0 (unchanged)
- Updated Package Count: 3 packages

**Migration Steps:**

1. **Prerequisites:**
   - None (foundation project with no dependencies)

2. **Package Updates:**

   | Package | Current | Target | Change Type |
   |---------|---------|--------|-------------|
   | Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.2 | 10.0.7 | Major version |
   | Microsoft.Extensions.Options | 8.0.2 | 10.0.7 | Major version |
   | Ulid | 1.3.4 | 1.4.1 | Minor version |

3. **Expected Breaking Changes:**
   - **Microsoft.Extensions.DependencyInjection.Abstractions (8.x → 10.x):**
     - Namespace remains `Microsoft.Extensions.DependencyInjection`
     - Core interfaces (`IServiceProvider`, `IServiceCollection`) unchanged
     - Potential new overloads or extension methods (additive changes)
   - **Microsoft.Extensions.Options (8.x → 10.x):**
     - Options pattern interfaces remain stable
     - `IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>` unchanged
   - **Ulid (1.3 → 1.4):**
     - Minor version, expect no breaking changes
     - Potential performance improvements or new utility methods

4. **Code Modifications:**
   - Review usage of `IServiceProvider` extension methods (new methods may be available)
   - Verify `IOptions<T>` usage patterns remain valid
   - No anticipated changes to `Ulid` API surface

5. **Testing Strategy:**
   - Build project successfully
   - Run dependent test projects (Streamon.Tests.csproj)
   - Verify no compilation warnings related to package APIs

6. **Validation Checklist:**
   - [ ] Project builds without errors
   - [ ] Project builds without warnings
   - [ ] Streamon.Tests.csproj still references and builds
   - [ ] All tests in Streamon.Tests.csproj pass

---

#### 2. Streamon.Subscription.csproj

**Current State:**
- Target Framework: net8.0
- Dependencies: 1 project reference (Streamon.csproj)
- Package Count: 0 packages
- Lines of Code: 927
- Risk Level: Low

**Target State:**
- Target Framework: net8.0 (unchanged)
- No package updates required

**Migration Steps:**

1. **Prerequisites:**
   - Streamon.csproj package updates completed

2. **Package Updates:**
   - None (project has no direct package references needing updates)

3. **Expected Breaking Changes:**
   - None (no package changes)
   - Inherits transitive package updates from Streamon.csproj dependency

4. **Code Modifications:**
   - None required

5. **Testing Strategy:**
   - Build project successfully after Streamon.csproj updates
   - Run dependent test project (Streamon.Subscription.Tests.csproj)

6. **Validation Checklist:**
   - [ ] Project builds without errors
   - [ ] Project builds without warnings
   - [ ] Streamon.Subscription.Tests.csproj passes all tests

---

#### 3. Streamon.Azure.CosmosDb.csproj

**Current State:**
- Target Framework: net8.0
- Dependencies: 2 project references (Streamon.Subscription, Streamon)
- Package Count: 3 packages
- Lines of Code: 278
- Risk Level: Low-Medium (Microsoft.Azure.Cosmos major version jump)

**Target State:**
- Target Framework: net8.0 (unchanged)
- Updated Package Count: 3 packages

**Migration Steps:**

1. **Prerequisites:**
   - Streamon.csproj and Streamon.Subscription.csproj updates completed

2. **Package Updates:**

   | Package | Current | Target | Change Type |
   |---------|---------|--------|-------------|
   | Microsoft.Azure.Cosmos | 3.46.0 | 3.59.0 | Minor version (13 releases) |
   | Newtonsoft.Json | 13.0.3 | 13.0.4 | Patch version |
   | System.Text.Json | 8.0.5 | 10.0.7 | Major version |

3. **Expected Breaking Changes:**
   - **Microsoft.Azure.Cosmos (3.46 → 3.59):**
     - **Low risk** - Minor version changes maintain API compatibility
     - Review [Cosmos SDK release notes](https://github.com/Azure/azure-cosmos-dotnet-v3/releases) for versions 3.47-3.59
     - Possible new features: query improvements, performance enhancements, new container APIs
     - Potential deprecation warnings for older patterns
   - **Newtonsoft.Json (13.0.3 → 13.0.4):**
     - Patch version, no breaking changes expected
   - **System.Text.Json (8.0.5 → 10.0.7):**
     - **Medium risk** - Major version jump
     - New serialization features and performance improvements
     - Possible changes to `JsonSerializerOptions` defaults
     - Enhanced source generation support

4. **Code Modifications:**
   - Review `CosmosClient` initialization and configuration
   - Check `Container` query patterns for new recommended approaches
   - Verify `System.Text.Json` serialization behavior remains consistent
   - Update to use new Cosmos SDK features if beneficial (e.g., improved bulk operations)

5. **Testing Strategy:**
   - Build project successfully
   - Run Streamon.Azure.CosmosDb.Tests.csproj integration tests
   - Verify Cosmos DB operations (CRUD, queries) function correctly
   - Test serialization/deserialization with both Newtonsoft.Json and System.Text.Json

6. **Validation Checklist:**
   - [ ] Project builds without errors
   - [ ] Project builds without warnings
   - [ ] CosmosDB integration tests pass
   - [ ] No Cosmos SDK deprecation warnings appear
   - [ ] JSON serialization tests pass

---

#### 4. Streamon.Azure.TableStorage.csproj

**Current State:**
- Target Framework: net8.0
- Dependencies: 1 project reference (Streamon.Subscription)
- Package Count: 2 packages
- Lines of Code: 1,023
- Risk Level: Low

**Target State:**
- Target Framework: net8.0 (unchanged)
- Updated Package Count: 2 packages

**Migration Steps:**

1. **Prerequisites:**
   - Streamon.Subscription.csproj updates completed

2. **Package Updates:**

   | Package | Current | Target | Change Type |
   |---------|---------|--------|-------------|
   | Azure.Data.Tables | 12.9.1 | 12.11.0 | Minor version |
   | System.Text.Json | 8.0.5 | 10.0.7 | Major version |

3. **Expected Breaking Changes:**
   - **Azure.Data.Tables (12.9.1 → 12.11.0):**
     - **Low risk** - Minor version, maintains compatibility
     - Potential new features: improved retry policies, performance enhancements
     - Review [Azure Tables SDK releases](https://github.com/Azure/azure-sdk-for-net/releases)
   - **System.Text.Json (8.0.5 → 10.0.7):**
     - Same considerations as Streamon.Azure.CosmosDb.csproj above

4. **Code Modifications:**
   - Review `TableClient` and `TableServiceClient` usage
   - Verify `TableEntity` serialization patterns
   - Check `System.Text.Json` configuration for table entity serialization

5. **Testing Strategy:**
   - Build project successfully
   - Run Streamon.Azure.TableStorage.Tests.csproj integration tests
   - Verify Azure Table Storage operations (CRUD, queries, batch operations)

6. **Validation Checklist:**
   - [ ] Project builds without errors
   - [ ] Project builds without warnings
   - [ ] Azure Table Storage integration tests pass
   - [ ] Batch operations function correctly
   - [ ] Entity serialization/deserialization works as expected

---

### Test Projects

#### 5. Streamon.Tests.Fixtures.csproj

**Current State:**
- Target Framework: net8.0
- Dependencies: 0 project references
- Package Count: 2 packages
- Lines of Code: 106
- Risk Level: Low

**Target State:**
- Target Framework: net8.0 (unchanged)
- Updated Package Count: 1 package (xunit.core only; xunit.abstractions already at latest)

**Migration Steps:**

1. **Prerequisites:**
   - None (foundation test fixtures library)

2. **Package Updates:**

   | Package | Current | Target | Change Type | Notes |
   |---------|---------|--------|-------------|-------|
   | xunit.abstractions | 2.0.3 | 2.0.3 | No update | Already at latest |
   | xunit.core | 2.9.2 | 2.9.3 | Patch version | Bug fixes |

3. **Expected Breaking Changes:**
   - **xunit.core (2.9.2 → 2.9.3):**
     - Patch version, no breaking changes expected
     - Bug fixes and stability improvements

4. **Code Modifications:**
   - None anticipated

5. **Testing Strategy:**
   - Build project successfully
   - Verify dependent test projects still reference and build

6. **Validation Checklist:**
   - [ ] Project builds without errors
   - [ ] Project builds without warnings
   - [ ] All test projects depending on this still build

---

#### 6. Streamon.Tests.csproj

**Current State:**
- Target Framework: net8.0
- Dependencies: 2 project references (Streamon.Tests.Fixtures, Streamon)
- Package Count: 6 packages
- Lines of Code: 274
- Risk Level: Low

**Target State:**
- Target Framework: net8.0 (unchanged)
- Updated Package Count: 6 packages

**Migration Steps:**

1. **Prerequisites:**
   - Streamon.csproj updates completed
   - Streamon.Tests.Fixtures.csproj updates completed

2. **Package Updates:**

   | Package | Current | Target | Change Type |
   |---------|---------|--------|-------------|
   | coverlet.collector | 6.0.2 | 10.0.0 | Major version |
   | Microsoft.Extensions.DependencyInjection | 8.0.1 | 10.0.7 | Major version |
   | Microsoft.NET.Test.Sdk | 17.12.0 | 18.5.1 | Major version |
   | Newtonsoft.Json | 13.0.3 | 13.0.4 | Patch version |
   | xunit | 2.9.2 | 2.9.3 | Patch version |
   | xunit.runner.visualstudio | 2.8.2 | 3.1.5 | Major version |

3. **Expected Breaking Changes:**
   - **coverlet.collector (6.x → 10.x):**
     - Code coverage collector, no API surface for test code
     - May affect CI/CD coverage reporting configuration
   - **Microsoft.Extensions.DependencyInjection (8.x → 10.x):**
     - Same considerations as Streamon.csproj
   - **Microsoft.NET.Test.Sdk (17.x → 18.x):**
     - Test SDK infrastructure, no test code changes needed
     - May require VS 2022 17.12+ or latest .NET SDK
   - **xunit.runner.visualstudio (2.8 → 3.1):**
     - Test runner improvements, no test code changes
     - Better VS 2022 Test Explorer integration

4. **Code Modifications:**
   - Review DI container usage in tests (same as Streamon.csproj)
   - No anticipated test framework changes

5. **Testing Strategy:**
   - Build project successfully
   - Run all unit tests in this project
   - Verify test discovery in Test Explorer

6. **Validation Checklist:**
   - [ ] Project builds without errors
   - [ ] Project builds without warnings
   - [ ] All tests are discovered by test runner
   - [ ] All tests pass
   - [ ] Code coverage collection works (if configured)

---

#### 7. Streamon.Subscription.Tests.csproj

**Current State:**
- Target Framework: net8.0
- Dependencies: 2 project references (Streamon.Tests.Fixtures, Streamon.Subscription)
- Package Count: 6 packages
- Lines of Code: 527
- Risk Level: Low

**Target State:**
- Target Framework: net8.0 (unchanged)
- Updated Package Count: 6 packages

**Migration Steps:**

1. **Prerequisites:**
   - Streamon.Subscription.csproj updates completed
   - Streamon.Tests.Fixtures.csproj updates completed

2. **Package Updates:**

   | Package | Current | Target | Change Type |
   |---------|---------|--------|-------------|
   | coverlet.collector | 6.0.2 | 10.0.0 | Major version |
   | Microsoft.Extensions.DependencyInjection | 8.0.1 | 10.0.7 | Major version |
   | Microsoft.NET.Test.Sdk | 17.12.0 | 18.5.1 | Major version |
   | Newtonsoft.Json | 13.0.3 | 13.0.4 | Patch version |
   | xunit | 2.9.2 | 2.9.3 | Patch version |
   | xunit.runner.visualstudio | 2.8.2 | 3.1.5 | Major version |

3. **Expected Breaking Changes:**
   - Same considerations as Streamon.Tests.csproj above

4. **Code Modifications:**
   - Same considerations as Streamon.Tests.csproj

5. **Testing Strategy:**
   - Build project successfully
   - Run all subscription-related tests
   - Verify test discovery

6. **Validation Checklist:**
   - [ ] Project builds without errors
   - [ ] Project builds without warnings
   - [ ] All tests are discovered
   - [ ] All tests pass

---

#### 8. Streamon.Azure.CosmosDb.Tests.csproj

**Current State:**
- Target Framework: net8.0
- Dependencies: 2 project references (Streamon.Tests.Fixtures, Streamon.Azure.CosmosDb)
- Package Count: 8 packages
- Lines of Code: 51
- Risk Level: Low-Medium (Cosmos SDK + Testcontainers updates)

**Target State:**
- Target Framework: net8.0 (unchanged)
- Updated Package Count: 8 packages

**Migration Steps:**

1. **Prerequisites:**
   - Streamon.Azure.CosmosDb.csproj updates completed
   - Streamon.Tests.Fixtures.csproj updates completed

2. **Package Updates:**

   | Package | Current | Target | Change Type |
   |---------|---------|--------|-------------|
   | coverlet.collector | 6.0.2 | 10.0.0 | Major version |
   | Microsoft.Extensions.DependencyInjection | 8.0.1 | 10.0.7 | Major version |
   | Microsoft.NET.Test.Sdk | 17.12.0 | 18.5.1 | Major version |
   | Newtonsoft.Json | 13.0.3 | 13.0.4 | Patch version |
   | System.Text.Json | 8.0.5 | 10.0.7 | Major version |
   | Testcontainers.CosmosDb | 4.0.0 | 4.11.0 | Minor version (11 releases) |
   | xunit | 2.9.2 | 2.9.3 | Patch version |
   | xunit.runner.visualstudio | 2.8.2 | 3.1.5 | Major version |

3. **Expected Breaking Changes:**
   - **Testcontainers.CosmosDb (4.0 → 4.11):**
     - **Medium risk** - 11 minor releases of changes
     - Review [Testcontainers.NET releases](https://github.com/testcontainers/testcontainers-dotnet/releases)
     - Possible changes to container configuration API
     - Improved Cosmos DB emulator container support
     - Potential new builder pattern methods
   - Other packages: same considerations as previous test projects

4. **Code Modifications:**
   - Review `CosmosDbBuilder` usage in test fixtures
   - Verify Cosmos DB emulator container startup configuration
   - Check connection string retrieval patterns
   - Update to new Testcontainers API patterns if available

5. **Testing Strategy:**
   - Build project successfully
   - Run integration tests (may require Docker environment)
   - Verify Cosmos DB emulator container starts successfully
   - Confirm all database operations in tests work

6. **Validation Checklist:**
   - [ ] Project builds without errors
   - [ ] Project builds without warnings
   - [ ] Cosmos DB Testcontainer starts successfully
   - [ ] All integration tests pass
   - [ ] Container cleanup occurs properly after tests

---

#### 9. Streamon.Azure.TableStorage.Tests.csproj

**Current State:**
- Target Framework: net8.0
- Dependencies: 2 project references (Streamon.Azure.TableStorage, Streamon.Tests.Fixtures)
- Package Count: 8 packages
- Lines of Code: 834
- Risk Level: Low

**Target State:**
- Target Framework: net8.0 (unchanged)
- Updated Package Count: 8 packages

**Migration Steps:**

1. **Prerequisites:**
   - Streamon.Azure.TableStorage.csproj updates completed
   - Streamon.Tests.Fixtures.csproj updates completed

2. **Package Updates:**

   | Package | Current | Target | Change Type |
   |---------|---------|--------|-------------|
   | coverlet.collector | 6.0.2 | 10.0.0 | Major version |
   | Microsoft.Extensions.DependencyInjection | 8.0.1 | 10.0.7 | Major version |
   | Microsoft.NET.Test.Sdk | 17.12.0 | 18.5.1 | Major version |
   | Newtonsoft.Json | 13.0.3 | 13.0.4 | Patch version |
   | System.Text.Json | 8.0.5 | 10.0.7 | Major version |
   | Testcontainers.Azurite | 4.0.0 | 4.11.0 | Minor version (11 releases) |
   | xunit | 2.9.2 | 2.9.3 | Patch version |
   | xunit.runner.visualstudio | 2.8.2 | 3.1.5 | Major version |

3. **Expected Breaking Changes:**
   - **Testcontainers.Azurite (4.0 → 4.11):**
     - Same considerations as Testcontainers.CosmosDb above
     - Azurite emulator container configuration may have new options
   - Other packages: same as other test projects

4. **Code Modifications:**
   - Review `AzuriteBuilder` usage in test fixtures
   - Verify Azurite container startup configuration
   - Check Table Storage connection string retrieval

5. **Testing Strategy:**
   - Build project successfully
   - Run integration tests (may require Docker environment)
   - Verify Azurite container starts successfully
   - Confirm all Table Storage operations work

6. **Validation Checklist:**
   - [ ] Project builds without errors
   - [ ] Project builds without warnings
   - [ ] Azurite Testcontainer starts successfully
   - [ ] All integration tests pass (especially ordered tests with `[Priority]` attribute)
   - [ ] Container cleanup occurs properly after tests

---

## Risk Management

### High-Level Assessment

**Overall Risk: Low**

The package update operation carries minimal risk due to:
- No framework version changes (staying on .NET 8.0)
- All packages already compatible with .NET 8.0
- No security vulnerabilities requiring urgent mitigation
- Comprehensive test coverage (5 test projects, 1,686 LOC of test code)
- Small, well-structured codebase

### Risk Factors by Area

| Area | Risk Level | Mitigation |
|------|------------|------------|
| **Major Version Bumps** | Low-Medium | Microsoft.Azure.Cosmos (3.46→3.59), System.Text.Json (8.0.5→10.0.7), Microsoft.Extensions.* (8.0.x→10.0.7) - Review release notes for API changes |
| **Test Framework Updates** | Low | xunit (2.9.2→2.9.3) is minor; xunit.runner.visualstudio (2.8.2→3.1.5) is larger but well-tested |
| **Azure SDK Updates** | Low | Azure.Data.Tables (12.9.1→12.11.0) is minor version bump |
| **Testcontainers Updates** | Low | Testcontainers.* (4.0.0→4.11.0) - Integration tests may need container configuration review |
| **Build/Test Tooling** | Low | coverlet.collector, Microsoft.NET.Test.Sdk updates affect CI/CD pipeline only |

### High-Risk Changes Table

| Project | Risk Level | Description | Mitigation |
|---------|------------|-------------|------------|
| Streamon.Azure.CosmosDb.csproj | Low-Medium | Microsoft.Azure.Cosmos SDK updated across 13 minor versions (3.46→3.59) | Review [Cosmos .NET SDK changelog](https://github.com/Azure/azure-cosmos-dotnet-v3/blob/master/changelog.md) for versions 3.47-3.59; focus on query API changes and deprecation warnings |
| Streamon.Azure.CosmosDb.Tests.csproj | Low-Medium | Testcontainers.CosmosDb updated across 11 minor versions (4.0→4.11) | Verify Cosmos emulator container starts successfully; review [Testcontainers release notes](https://github.com/testcontainers/testcontainers-dotnet/releases) |
| All projects using System.Text.Json | Medium | Major version jump from 8.0.5 to 10.0.7 | Test all serialization/deserialization scenarios; verify `JsonSerializerOptions` configurations still work; check for new default behaviors |

### Security Vulnerabilities

**Status:** None identified

The assessment report indicates no security vulnerabilities in current packages. However, updating to latest versions provides:
- Latest security patches
- Proactive security posture
- Reduced exposure to future CVEs in older versions

### Contingency Plans

**Issue: Cosmos SDK API Breaking Change**
- **Detection:** Compilation errors or failing integration tests in Streamon.Azure.CosmosDb.Tests
- **Resolution:** Review Cosmos SDK migration guides for specific version range; update query patterns or client configuration
- **Fallback:** Temporarily pin Cosmos SDK to intermediate version (e.g., 3.50.0) while investigating

**Issue: System.Text.Json Serialization Behavior Change**
- **Detection:** Failing unit/integration tests related to JSON serialization
- **Resolution:** Review `JsonSerializerOptions` configuration; add explicit serializer settings if defaults changed
- **Fallback:** Temporarily pin to System.Text.Json 8.0.5 for affected projects only

**Issue: Testcontainers Configuration Incompatibility**
- **Detection:** Integration tests fail to start Azurite or Cosmos emulator containers
- **Resolution:** Review Testcontainers builder API changes; update container configuration syntax
- **Fallback:** Pin Testcontainers packages to intermediate version (e.g., 4.6.0)

**Issue: xUnit Runner Discovery Failure**
- **Detection:** Tests not discovered in Visual Studio Test Explorer or `dotnet test`
- **Resolution:** Clear test cache; restart Visual Studio; verify `xunit.runner.visualstudio` 3.x compatibility
- **Fallback:** Temporarily revert to xunit.runner.visualstudio 2.8.2

---

## Testing & Validation Strategy

### Multi-Level Testing Approach

#### Level 1: Per-Project Validation (During Update)

After package updates are applied to all projects:

**Build Validation:**
1. Restore NuGet packages: `dotnet restore Streamon.sln`
2. Build solution in dependency order: `dotnet build Streamon.sln --no-restore`
3. Verify zero compilation errors
4. Review and address any warnings (especially deprecation warnings)

**Expected Outcomes:**
- All 9 projects build successfully
- No breaking API compilation errors
- Deprecation warnings identified and documented

#### Level 2: Unit Test Validation

Execute all unit test projects:

**Test Projects to Run:**
1. `Streamon.Tests.csproj` (274 LOC)
2. `Streamon.Subscription.Tests.csproj` (527 LOC)

**Validation Steps:**
```bash
dotnet test test/Streamon.Tests/Streamon.Tests.csproj
dotnet test test/Streamon.Subscription.Tests/Streamon.Subscription.Tests.csproj
```

**Success Criteria:**
- All unit tests pass
- No test discovery issues
- No new test failures introduced by package updates

#### Level 3: Integration Test Validation

Execute integration test projects (require Docker for Testcontainers):

**Test Projects to Run:**
1. `Streamon.Azure.TableStorage.Tests.csproj` (834 LOC, largest test project)
2. `Streamon.Azure.CosmosDb.Tests.csproj` (51 LOC)

**Validation Steps:**
```bash
# Ensure Docker is running
dotnet test test/Streamon.Azure.TableStorage.Tests/Streamon.Azure.TableStorage.Tests.csproj
dotnet test test/Streamon.Azure.CosmosDb.Tests/Streamon.Azure.CosmosDb.Tests.csproj
```

**Success Criteria:**
- Azurite container starts successfully
- Cosmos DB emulator container starts successfully
- All integration tests pass
- Ordered tests (using `[Priority]` attribute) execute in correct sequence
- Containers clean up properly after test execution

#### Level 4: Comprehensive Solution Validation

**Full Solution Test Pass:**
```bash
dotnet test Streamon.sln --filter "FullyQualifiedName!~Streamon.Azure.CosmosDb.Tests&FullyQualifiedName!~Streamon.Azure.TableStorage.Tests"
```

(Note: This mirrors the CI pipeline filter to exclude Azure provider integration tests if Docker is unavailable)

**Optional: Full Integration Suite (if Docker available):**
```bash
dotnet test Streamon.sln
```

**Success Criteria:**
- All test projects pass
- Zero test failures
- Test coverage maintained or improved
- No performance regressions in test execution time

### Testing Checklist Elements

**Per-Project Validation:**
- [ ] Streamon.csproj builds without errors or warnings
- [ ] Streamon.Subscription.csproj builds successfully
- [ ] Streamon.Azure.CosmosDb.csproj builds successfully
- [ ] Streamon.Azure.TableStorage.csproj builds successfully
- [ ] All test projects build successfully

**Unit Tests:**
- [ ] Streamon.Tests: All tests pass
- [ ] Streamon.Subscription.Tests: All tests pass

**Integration Tests:**
- [ ] Streamon.Azure.TableStorage.Tests: Container starts + all tests pass
- [ ] Streamon.Azure.CosmosDb.Tests: Container starts + all tests pass

**Quality Checks:**
- [ ] No new compiler warnings introduced
- [ ] No deprecation warnings in production code (acceptable in test code if documented)
- [ ] NuGet package restore completes without errors
- [ ] Solution builds in Release configuration
- [ ] Code coverage reports generate successfully (if configured)

### CI/CD Pipeline Validation

**GitHub Actions Workflow:** `.github/workflows/ci.yml`

**Pipeline Steps to Verify:**
1. Restore dependencies: `dotnet restore`
2. Build in Release mode: `dotnet build --configuration Release`
3. Run tests (excluding Azure provider integration tests)

**Expected Outcome:**
- CI pipeline passes on the `nuget-package-updates` branch
- No new failures introduced

---

## Source Control Strategy

### Branching Strategy

**Current Branch:** `nuget-package-updates` (created during assessment phase)

**Source Branch:** `main`

**Merge Strategy:**
- Create Pull Request from `nuget-package-updates` → `main`
- Require CI pipeline pass before merge
- Use squash merge to consolidate all package update commits into single commit on `main`

### Commit Strategy

**Atomic Commit Approach:**

Given the All-At-Once strategy, the entire package update should be committed as a single atomic unit:

**Single Commit Message Template:**
```
chore: update all NuGet packages to latest .NET 8.0-compatible versions

- Update Microsoft.Extensions.* packages: 8.0.x → 10.0.7
- Update System.Text.Json: 8.0.5 → 10.0.7
- Update Microsoft.Azure.Cosmos: 3.46.0 → 3.59.0
- Update Azure.Data.Tables: 12.9.1 → 12.11.0
- Update Testcontainers.*: 4.0.0 → 4.11.0
- Update xUnit packages: 2.9.2 → 2.9.3
- Update xunit.runner.visualstudio: 2.8.2 → 3.1.5
- Update test tooling: coverlet.collector, Microsoft.NET.Test.Sdk
- Update Newtonsoft.Json: 13.0.3 → 13.0.4
- Update Ulid: 1.3.4 → 1.4.1

All projects remain on .NET 8.0 target framework.

All tests pass. No breaking changes detected.
```

**Checkpoint Strategy:**

While the update is atomic, intermediate checkpoints may be useful during development:

1. **Checkpoint 1:** Package updates applied, solution builds
   ```
   chore(wip): apply all NuGet package updates

   All PackageReference versions updated to latest .NET 8.0-compatible releases.
   Solution builds successfully. Tests not yet validated.
   ```

2. **Checkpoint 2:** Tests validated, any compilation fixes applied
   ```
   chore(wip): fix compilation issues after package updates

   - Address System.Text.Json serialization behavior changes
   - Update Testcontainers configuration for new API
   - Fix any deprecation warnings

   All tests now pass.
   ```

3. **Final Commit:** Squash checkpoints into single commit (message template above)

### Review and Merge Process

**Pull Request Checklist:**

- [ ] **Title:** `chore: update all NuGet packages to latest .NET 8.0-compatible versions`
- [ ] **Description:** 
  - Link to this plan document (`.github/upgrades/scenarios/new-dotnet-version_2260a4/plan.md`)
  - Summary of updated packages (can copy from commit message)
  - Testing results (all tests pass)
  - Note any manual validation performed
- [ ] **CI Status:** Green checkmark on all CI jobs
- [ ] **Reviewer Actions:**
  - Verify package versions match plan specifications
  - Review any code changes (should be minimal or none)
  - Confirm test results in CI pipeline
  - Check for new warnings in build output
- [ ] **Merge Criteria:**
  - All CI checks pass
  - At least one approving review
  - No unresolved conversations
  - Squash merge to consolidate commits

**Post-Merge Actions:**
- Delete `nuget-package-updates` branch
- Monitor first production deployment (if applicable)
- Update team documentation if new package features are being adopted

---

## Success Criteria

### Technical Criteria

**Package Updates:**
- [x] All 13 NuGet packages with available updates are updated to their target versions
- [x] No package remains on outdated version (except `xunit.abstractions` which is already latest)
- [x] All transitive dependencies resolve without conflicts
- [x] No downgrade warnings during NuGet restore

**Framework Alignment:**
- [x] All 9 projects remain on .NET 8.0 target framework
- [x] No projects inadvertently upgraded to .NET 9.0 or .NET 10.0
- [x] All packages confirm .NET 8.0 compatibility

**Build Success:**
- [x] Solution restores without errors: `dotnet restore Streamon.sln`
- [x] Solution builds without errors: `dotnet build Streamon.sln`
- [x] Release configuration builds successfully: `dotnet build --configuration Release`
- [x] No new compiler warnings introduced (or all warnings documented and approved)

**Test Success:**
- [x] All unit tests pass: `Streamon.Tests`, `Streamon.Subscription.Tests`
- [x] All integration tests pass: `Streamon.Azure.TableStorage.Tests`, `Streamon.Azure.CosmosDb.Tests`
- [x] Test discovery works in Visual Studio Test Explorer
- [x] Test discovery works via `dotnet test`
- [x] No test execution failures
- [x] No new flaky tests introduced

**Dependency Resolution:**
- [x] No package version conflicts
- [x] No assembly binding redirect issues
- [x] Transitive dependencies align with .NET 8.0 expectations

**Security:**
- [x] No new security vulnerabilities introduced (verify with `dotnet list package --vulnerable`)
- [x] No known CVEs in updated package versions

### Quality Criteria

**Code Quality:**
- [x] No code changes required (package updates only)
  - **OR** Any required code changes are minimal, well-tested, and documented
- [x] No new code analysis warnings
- [x] No degradation in code coverage percentage (if measured)

**Documentation:**
- [x] Plan document (this file) completed and accurate
- [x] Commit messages clearly describe changes
- [x] Pull request description comprehensive
- [x] Any breaking changes or new patterns documented in PR or team wiki

**Performance:**
- [x] No test execution performance regression (>10% slower)
- [x] No build time regression (>10% slower)
- [x] Package restore time acceptable (<2 minutes for full solution)

### Process Criteria

**All-At-Once Strategy Adherence:**
- [x] All package updates applied simultaneously (not incrementally)
- [x] Single atomic commit for package updates (or squashed into one)
- [x] No intermediate mixed-version states in version control
- [x] Dependency order respected during build validation

**Source Control:**
- [x] Updates performed on dedicated `nuget-package-updates` branch
- [x] Pull Request created from `nuget-package-updates` → `main`
- [x] CI pipeline passes on PR
- [x] Squash merge used to consolidate history

**Testing Strategy:**
- [x] Multi-level testing performed (per-project, unit, integration, full solution)
- [x] Testcontainers validation completed (if Docker available)
- [x] CI/CD pipeline validation passed

**Risk Management:**
- [x] Release notes reviewed for major version bumps:
  - [x] Microsoft.Azure.Cosmos 3.47-3.59 releases
  - [x] System.Text.Json 9.x-10.x releases
  - [x] Testcontainers 4.1-4.11 releases
- [x] Contingency plans documented for high-risk changes
- [x] Rollback strategy confirmed (single commit revert)

### Definition of Done

The NuGet package update migration is **complete** when:

1. ✅ All technical criteria met (packages updated, builds pass, tests pass)
2. ✅ All quality criteria met (code quality maintained, documentation complete)
3. ✅ All process criteria met (strategy followed, PR merged)
4. ✅ Post-merge validation:
   - `main` branch builds successfully after merge
   - CI pipeline passes on `main`
   - First production deployment (if applicable) succeeds without issues
   - Team notified of completed update

### Validation Commands

**Quick Validation Script:**
```bash
# Restore packages
dotnet restore Streamon.sln

# Build solution
dotnet build Streamon.sln --no-restore

# Run all tests (excluding Azure integration tests if Docker unavailable)
dotnet test Streamon.sln --no-build --filter "FullyQualifiedName!~Streamon.Azure.CosmosDb.Tests&FullyQualifiedName!~Streamon.Azure.TableStorage.Tests"

# Verify no vulnerable packages
dotnet list package --vulnerable
```

**Expected Output:**
- Restore: "Restore succeeded"
- Build: "Build succeeded. 0 Warning(s). 0 Error(s)."
- Test: "Passed! - Failed: 0, Passed: X, Skipped: 0" (where X = total test count)
- Vulnerable packages: "No vulnerable packages found"

---

## Source Control Strategy

[To be filled]

---

## Success Criteria

[To be filled]
