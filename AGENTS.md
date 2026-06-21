# Copilot Instructions — Streamon

## Project Overview

**Streamon** is a .NET 8 event streaming store platform that provides a common set of abstraction APIs
over multiple event store backends (Azure Table Storage, Azure Cosmos DB, In-Memory).
It is **not** an Event Sourcing framework, but it can be used as an event/stream store backend for one.

Published NuGet packages: `Streamon`, `Streamon.Subscription`, `Streamon.Azure.TableStorage`

---

## Solution Layout

```
src/
  Streamon/                           # Core abstractions, types, and in-memory provider
  Streamon.Subscription/              # Subscription pipeline: handlers, projectors, checkpoints
  Streamon.Azure.TableStorage/        # Azure Table Storage provider + subscription support
  Streamon.Azure.CosmosDb/            # Azure Cosmos DB provider (in progress — not production-ready)
test/
  Streamon.Tests/                     # Unit tests for the core library
  Streamon.Subscription.Tests/        # Unit tests for the subscription pipeline
  Streamon.Azure.TableStorage.Tests/  # Integration tests (Azurite via Testcontainers)
  Streamon.Tests.Fixtures/            # Shared test fixtures, event records, and helpers
sample/                               # .NET Aspire sample application
```

---

## Core Abstractions

### Storage Interfaces

| Interface | Responsibility |
|---|---|
| `IStreamStore` | Composes `IStreamReader + IStreamWriter + IStreamManager` |
| `IStreamReader` | `FetchEventsAsync(streamId, startPosition, endPosition, ct)` |
| `IStreamWriter` | `AppendEventsAsync(streamId, expectedPosition, events, metadata, ct)` |
| `IStreamManager` | `DeleteStreamAsync(streamId, expectedPosition, ct)` |
| `IStreamStoreProvisioner` | Creates named `IStreamStore` instances (one per table/container name) |
| `IStreamTypeProvider` | Serializes/deserializes event payloads via `System.Text.Json` |

### Core Value Types

- **`Event`** — The envelope record stored and transported. Properties:
  `StreamId`, `EventId`, `BatchId`, `StreamPosition`, `GlobalPosition`, `Timestamp`, `Payload` (`object`), `Metadata`.
- **`StreamId`** — Strong-typed stream identifier. Create with `new StreamId("order-123")` or `StreamId.From("order-123")`.
- **`StreamPosition`** — Strong-typed `long` position with special sentinels:
  - `StreamPosition.Start` (`0`) — beginning of stream; use when appending to a brand-new stream
  - `StreamPosition.End` (`long.MaxValue`) — logical end; **cannot** be used as `expectedPosition` on append
  - `StreamPosition.Any` (`-1`) — wildcard that bypasses all position comparison checks
- **`EventId`** — ULID-backed unique event identifier. Auto-generated when the payload does not expose one. Generate explicitly with `EventId.New()`.
- **`BatchId`** — Groups events appended in the same `AppendEventsAsync` call. Generate with `BatchId.New()`.
- **`EventMetadata`** — `Dictionary<string, string>` for arbitrary per-event metadata.

All identity types implement `IIdentity<TSelf, TValue>` and expose a `static From(value)` factory method.

---

## Event Definitions

Events are **plain C# records** — no base class or special inheritance required.

```csharp
// Minimal — class name becomes the type name in the type provider
public record OrderCaptured(string Id, string Product, decimal Price);

// Custom serialization name
[EventType("v1.order_shipped")]
public record OrderShipped(string Id, string Tracking);

// Explicit event ID via attribute on a property
public record OrderConfirmed([property: EventId] EventId ConfirmationId, string By);

// Explicit event ID via interface
public record OrderFulfilled(EventId EventId, string By) : IHasEventId;

// Custom metadata via interface
public record OrderCancelled(string Reason, EventMetadata Metadata) : IHasEventMetadata;
```

### Event Markers

| Marker | Purpose |
|---|---|
| `[EventType("name")]` | Custom type name used by `IStreamTypeProvider` for serialization |
| `[EventId]` | Marks a property or field as the event's unique identifier |
| `IHasEventId` | Interface for payloads that expose their own `EventId EventId` property |
| `IHasEventMetadata` | Interface for payloads that carry their own `EventMetadata Metadata` property |

If no explicit ID marker is present, a new ULID-based `EventId` is auto-generated on append.

---

## Subscription Pipeline

### Typed Event Handlers

Implement `IEventHandler<TEvent>` for single-event-type handling:

```csharp
public class OrderCapturedHandler : IEventHandler<OrderCaptured>
{
    public ValueTask HandleAsync(EventHandlerContext<OrderCaptured> context, CancellationToken cancellationToken = default)
    {
        // context exposes: SubscriptionId, StreamId, EventId, BatchId,
        //                  StreamPosition, GlobalPosition, Timestamp, Payload, Metadata
        return ValueTask.CompletedTask;
    }
}
```

### EventHandlerContext&lt;T&gt;

`EventHandlerContext<T>` is the context record passed to every handler. It is created from an `Event`
via the static factory `EventHandlerContext<T>.From(SubscriptionId subscriptionId, Event @event)`.
The factory casts `event.Payload` to `T`; an `InvalidCastException` will be thrown if the types do not match.

```csharp
public record EventHandlerContext<T>(
    SubscriptionId SubscriptionId,
    StreamId StreamId,
    EventId EventId,
    StreamPosition StreamPosition,
    StreamPosition GlobalPosition,
    DateTimeOffset Timestamp,
    BatchId BatchId,
    T Payload,
    EventMetadata? Metadata = default)
{
    public static EventHandlerContext<T> From(SubscriptionId subscriptionId, Event @event) => ...
}
```

### Stateful Projectors

For projections with persistent state implement one or both interfaces on a projector class:

- `IEventInitialProjector<TEvent, TState>` — creates new state from the first qualifying event
- `IEventProjector<TEvent, TState>` — updates existing state for subsequent events

Both return `ValueTask<TState>` and require a `GetIdentity` method that determines the storage key.

`EventProjectorBase<TProjector, TState>` (in `Streamon.Azure.TableStorage.Subscription`) is the base
class that wires up both interfaces via reflection in its static constructor. The `ProjectAsync` method
is **not yet fully implemented** — state read/write abstractions are pending (see Work in Progress).

### Checkpoint & Reader Interfaces

| Interface | Responsibility |
|---|---|
| `ICheckpointStore` | `GetCheckpointAsync` / `SetCheckpointAsync` per `SubscriptionId` |
| `ISubscriptionStreamReader` | `FetchAsync(fromPosition, ct)` yielding `Event`; `GetLastGlobalPositionAsync` |

`SubscriptionManager` holds all registered subscriptions and exposes `PollAsync()` to drive them all.

---

## Dependency Injection

### Core Registration (single / default stream)

A nameless `AddStreamon()` registers a **non-keyed** `IStreamStoreProvisioner`. Its base table name comes
from `TableStreamStoreOptions.StreamTableName` (default `Streamon`).

```csharp
services.AddStreamon()
    .UseTableStorageStreamStore(connectionString, options =>
    {
        options.StreamTableName = "Streamon";          // base table name (default)
        options.StreamTypeProvider = new StreamTypeProvider().RegisterTypes<OrderCaptured>();
        options.DeleteMode = StreamDeleteMode.Soft;   // default
        options.TransactionBatchSize = 100;            // Azure max; ~48 effective events per tx
    });
```

### Multiple (named) streams - keyed registration

Pass a name to `AddStreamon("name")` to register more than one stream store in the same container. When a
name is supplied the provisioner is registered with **keyed DI** (`TryAddKeyedScoped<IStreamStoreProvisioner>(name, ...)`)
and the **stream name becomes the base table name**. Named streams are resolved **only** by key -
they are **not** available through the non-keyed `GetService<IStreamStoreProvisioner>()`.

```csharp
services.AddStreamon("orders")
    .UseTableStorageStreamStore(connectionString, options => options.StreamTypeProvider = orderTypes);

services.AddStreamon("shipment")
    .UseTableStorageStreamStore(connectionString, options => options.StreamTypeProvider = shipmentTypes);
```

Table names are deterministic - the provisioning `suffix` is concatenated to the stream name
(`{name}{suffix}`):

| Registration | `CreateStoreAsync(suffix)` | Physical table |
|---|---|---|
| `AddStreamon("orders")` | `CreateStoreAsync()` | `orders` |
| `AddStreamon("orders")` | `CreateStoreAsync("ABC")` | `ordersABC` |
| `AddStreamon("orders")` | `CreateStoreAsync("DEF")` | `ordersDEF` |
| `AddStreamon("shipment")` | `CreateStoreAsync("ABC")` | `shipmentABC` |
| `AddStreamon("shipment")` | `CreateStoreAsync("DEF")` | `shipmentDEF` |

### Subscription Registration

`AddStreamonSubscription` takes a `SubscriptionId` and an optional `Action<StreamSubscriptionOptions>`.
The Table Storage subscription reader/checkpoint extensions accept an optional `streamName` parameter that
is used to compose the table to read events from (`{StreamTableName}{streamName}{suffix}`) - point it at the
same physical table your events were written to.

```csharp
services.AddStreamonSubscription(SubscriptionId.From("orders-sub"))
    .UseTableStorageCheckpointStore(connectionString)
    .UseTableStorageSubscriptionStreamReader(connectionString, streamName: "orders",
        options => options.StreamTableName = string.Empty) // reads table: "orders"
    .AddEventHandler<OrderCapturedHandler>();
```

- `StreamSubscriptionType.CatchUp` starts from `StreamPosition.Start` when no checkpoint exists
- `StreamSubscriptionType.Live` starts from the current stream end when no checkpoint exists
- `StreamSubscriptionType.InMemory` is in-memory only, useful for testing and temporary processing

Subscriptions are registered as **keyed singletons** (`AddKeyedSingleton`) using `SubscriptionId.Value`
as the key (and also non-keyed so the provisioner can enumerate them), which allows multiple independent
subscriptions within one DI container.

### Resolving at Runtime

```csharp
// Default (unnamed) stream store
var provisioner = sp.GetRequiredService<IStreamStoreProvisioner>();
var store = await provisioner.CreateStoreAsync();           // base table
var scoped = await provisioner.CreateStoreAsync("AcmeCo");  // base table + "AcmeCo"

// Named stream stores are resolved by key (one provisioner per registered name)
var ordersProvisioner = sp.GetRequiredKeyedService<IStreamStoreProvisioner>("orders");
var ordersStore = await ordersProvisioner.CreateStoreAsync("ABC"); // table: "ordersABC"

var manager = sp.GetRequiredService<SubscriptionManager>();
await manager.PollAsync(cancellationToken);
```

---

## Azure Table Storage Provider

- Each stream is a **partition** (`PartitionKey = StreamId.Value`).
- Row-key naming convention (all prefixes are configurable via `TableStreamStoreOptions`):

| Row key | Entity |
|---|---|
| `SO-STREAM` | Stream header / metadata |
| `SO-EVENT-{seq:padded}` | Event data |
| `SO-ID-{eventId}` | Event ID uniqueness guard |
| `SO-SNAP-{n}` | Snapshot |

- Optimistic concurrency is enforced via `expectedPosition` on every `AppendEventsAsync` call.
- **Soft delete** (default): sets `IsDeleted = true` and records `DeletedOn` on the stream entity.
- **Hard delete**: removes every entity in the partition one by one — slow and expensive.
- Each event occupies **two rows** (event data + ID guard), so the practical limit is ~48 events per transaction batch despite the Azure maximum of 100 entities.
- The current `FetchLatestGlobalPositionAsync` implementation performs a full table scan — this is a known performance issue and should not be replicated in new code.

---

## Error Handling

Handle these domain exceptions when calling `IStreamStore`:

| Exception | Trigger |
|---|---|
| `StreamNotFoundException` | Stream does not exist |
| `StreamConcurrencyException` | `expectedPosition` does not match current stream position |
| `DuplicateEventException` | An event with the same `EventId` already exists in the stream |
| `BatchSizeExceededException` | Event count exceeds the Table Storage transaction batch limit |
| `StreamDeletedException` | Append attempted on a soft-deleted stream |
| `EventTypeNotFoundException` | `IStreamTypeProvider` cannot resolve a type name during deserialization |
| `TableStorageOperationException` | Wraps Azure SDK `RequestFailedException` |

---

## Type Provider & Serialization

`StreamTypeProvider` uses `System.Text.Json` with `JsonSerializerDefaults.Web`.

- Register types before reading any stream: `provider.RegisterTypes<MyEvent>()` or by assembly.
- Without `[EventType]`, the **class name** is used as the serialization type name.
- `StreamTypeProvider` is registered as a singleton by `AddStreamon()`; the same instance should be
  shared between the store and any subscription stream readers.

---

## Testing Conventions

- **Framework**: xUnit
- **Unit tests**: No infrastructure. Use `MemoryStreamStore` (from `src/Streamon/Memory/`) directly.
- **Integration tests**: Use `Testcontainers` + `AzuriteBuilder` with the `IClassFixture<ContainerFixture>` pattern.
- **Shared fixtures**: Re-use types from `test/Streamon.Tests.Fixtures` — `OrderEvents`, `OrderCaptured`, `OrderShipped`, `PriorityAttribute`, `PriorityTestCollectionOrderer`.
- **Test ordering**: Apply `[Priority(n)]` and `[TestCaseOrderer(...)]` for ordered integration scenarios.
- **CI filter**: Azure provider integration tests are excluded from the automated pipeline:
  ```
  --filter "FullyQualifiedName!~Streamon.Azure.CosmosDb.Tests&FullyQualifiedName!~Streamon.Azure.TableStorage.Tests"
  ```
- Name test methods using `VerbNounCondition` — e.g., `AppendsEventsToNewStream`, `FailsWhenAddingDuplicateEvents`.

### Integration Test Skeleton

```csharp
[TestCaseOrderer("Streamon.Tests.Fixtures.PriorityTestCollectionOrderer", "Streamon.Tests.Fixtures")]
public class MyIntegrationTests(ContainerFixture fixture) : IClassFixture<ContainerFixture>
{
    [Fact, Priority(1)]
    public async Task AppendsEventsToNewStream()
    {
        var store = await fixture.TableStreamStoreProvisioner.CreateStoreAsync(nameof(MyIntegrationTests));
        var stream = await store.AppendEventsAsync(new StreamId("order-1"), StreamPosition.Start, [OrderEvents.OrderCaptured]);
        Assert.NotEmpty(stream);
    }
}
```

For an example covering **keyed multi-stream registration** (named stores, `{name}{suffix}` table
composition, and keyed resolution via `GetRequiredKeyedService<IStreamStoreProvisioner>(name)`), see
`test/Streamon.Azure.TableStorage.Tests/KeyedStreamRegistrationTests.cs`.

---

## Code Style

- Target **C# 12** — use collection expressions (`[]`), primary constructors, and pattern matching.
- Use `record` types for events and value objects.
- Use `ValueTask` for handler/projector hot paths; `Task` for I/O-bound store operations.
- Add `ConfigureAwait(false)` on all `await` calls in library (`src/`) code.
- Always pass `CancellationToken cancellationToken = default` as the last parameter on async methods.
- Use strongly-typed IDs everywhere; do not pass raw `string` or `long` where an identity type exists.
- Add XML doc comments on public extension methods, options classes, and non-obvious public members.
always use `AddKeyedSingleton` / `GetRequiredKeyedService` (or `TryAddKeyedScoped`) when introducing new subscription-scoped or named-stream services. Named stream stores registered via `AddStreamon("name")` are keyed by the stream name and must be resolved with `GetRequiredKeyedService<IStreamStoreProvisioner>("name")`.

---

## Work in Progress — Handle with Care

| Area | Status |
|---|---|
| `EventProjectorBase<TProjector, TState>.ProjectAsync` | Not implemented — state read/write abstractions (`ReadStateAsync` / `SaveStateAsync`) are pending |
| `IEventProjector` (non-generic) | Commented out in `IEventProjector.cs`; interface contract is being redesigned |
| `StreamSubscription.PollAsync` handler dispatch | Handler instance is resolved but `HandleAsync` is not yet invoked on it |
| `EventHandlerContext<T>.From` | Requires `(SubscriptionId, Event)` — the `EventProjectorBase` reflection call currently omits `SubscriptionId` |
| `Streamon.Azure.CosmosDb` | Partially implemented; do not treat as production-ready |
| Global position tracking | Full table scan in `FetchLatestGlobalPositionAsync`; do not replicate this pattern |
| Telemetry, Claim Checks, Stream Sweeper, Stream Projections | Listed as To Do — do not generate implementations without explicit context |

---

## CI / CD

- **CI** (`ci.yml`): triggers on push/PR to `main`; runs restore ? `build --configuration Release` ? test (Azure provider tests excluded).
- **CD** (`cd.yml`): triggers on GitHub release publish; builds, packs, and pushes `Streamon`, `Streamon.Subscription`, and `Streamon.Azure.TableStorage` to NuGet.org using the `NUGET_PUBLISH_KEY` secret.
- Version is derived from the Git release tag: `refs/tags/v{VERSION}`.
- Local GitHub Actions testing: use [`act`](https://github.com/nektos/act).


## Documentation

After every update make sure to update each project README.md file with the latest information about the project, and also update the main AGENTS.md file with any relevant information that should be included in the main documentation.