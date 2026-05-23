---
name: streamon
description: Use Streamon to build event/stream stores in .NET 8+ apps. Trigger this skill whenever the user mentions Streamon, event streaming with Azure Table Storage, appending or reading event streams, subscriptions and checkpoints, projections / read models backed by Table Storage, multi-tenant event partitioning via dynamic table names, or wiring up `AddStreamon` / `AddStreamSubscription` / `AddTableStorageStreamStore` in DI. Use this even when the user only says things like "event store", "stream subscription", "projection handler", "tenant isolation for events", or "polling worker for events" without naming Streamon explicitly.
---

# Streamon

Streamon is a .NET 8 abstraction over event/stream stores (in-memory, Azure Table Storage, Cosmos DB in progress). It is **not** an Event Sourcing framework — it is a storage layer that Event Sourcing or audit/analytics pipelines can sit on top of.

This skill explains how to:

1. Model events
2. Append and read streams via `IStreamStore`
3. Wire everything in DI (core + Table Storage provider)
4. Build subscriptions with handlers and projections
5. Use **dynamic table names** for **per-tenant partitioning** (multitenancy)

Keep `SKILL.md` short. For deep dives, see `references/`.

---

## NuGet packages

| Package | When to install |
|---|---|
| `Streamon` | Always — core abstractions, `Event`, IDs, `MemoryStreamStore`. |
| `Streamon.Subscription` | When the app needs to react to events (handlers / projections / checkpoints). |
| `Streamon.Azure.TableStorage` | When the backing store is Azure Table Storage (or Cosmos DB via the Table API). |

---

## 1. Define events (POCOs / records)

Events are **plain records**. No base class, no interface required.

```csharp
public record OrderCaptured(string OrderId, string Product, decimal Price);

[EventType("v1.order_shipped")]                              // custom serialized name
public record OrderShipped(string OrderId, string Tracking);

public record OrderConfirmed([property: EventId] EventId Id, string By); // explicit event id
public record OrderFulfilled(EventId EventId, string By) : IHasEventId;  // alt. via interface
public record OrderCancelled(string Reason, EventMetadata Metadata) : IHasEventMetadata;
```

- No `[EventId]` / `IHasEventId` → a ULID-based `EventId` is auto-generated on append.
- No `[EventType]` → the class name is used as the type name in the type provider.

Register the event types with the `IStreamTypeProvider` before reading streams:

```csharp
options.StreamTypeProvider = new StreamTypeProvider()
	.RegisterTypes<OrderCaptured>()
	.RegisterTypes<OrderShipped>();
// or scan an assembly:
// options.StreamTypeProvider = new StreamTypeProvider().RegisterTypes(typeof(OrderCaptured).Assembly);
```

---

## 2. Append / read streams

Always go through `IStreamStore`. Get one from `IStreamStoreProvisioner` — **one store per table name**.

```csharp
var provisioner = sp.GetRequiredService<IStreamStoreProvisioner>();
IStreamStore store = await provisioner.CreateStoreAsync("orders");

var streamId = new StreamId("order-123");

// Append to a NEW stream — use StreamPosition.Start (0)
await store.AppendEventsAsync(
	streamId,
	StreamPosition.Start,
	[ new OrderCaptured("order-123", "Widget", 9.99m) ]);

// Append to an EXISTING stream — pass the current position you expect
await store.AppendEventsAsync(
	streamId,
	StreamPosition.From(1),
	[ new OrderShipped("order-123", "TRACK-1") ]);

// Bypass concurrency check (rarely needed):
await store.AppendEventsAsync(streamId, StreamPosition.Any, [ /* events */ ]);

// Read events
var events = await store.FetchEventsAsync(streamId);                       // full stream
var tail   = await store.FetchEventsAsync(streamId, StreamPosition.From(5));// from position 5
```

`StreamPosition` sentinels:

| Value | Meaning |
|---|---|
| `Start` (0) | beginning — use to append to a brand-new stream |
| `End` (`long.MaxValue`) | logical end — **cannot** be passed as `expectedPosition` on append |
| `Any` (-1) | bypass concurrency check |

Domain exceptions to handle: `StreamNotFoundException`, `StreamConcurrencyException`, `DuplicateEventException`, `BatchSizeExceededException`, `StreamDeletedException`, `EventTypeNotFoundException`, `TableStorageOperationException`.

> Azure Table Storage transaction limit: 100 entities per batch; each event occupies 2 rows (event + id guard), so practical max is ~48 events per `AppendEventsAsync` call. The provider does **not** auto-split batches.

---

## 3. Dependency Injection — core + Table Storage

```csharp
using Streamon;
using Streamon.Azure.TableStorage;

services.AddStreamon()
	.AddTableStorageStreamStore(connectionString, options =>
	{
		options.StreamTypeProvider = new StreamTypeProvider().RegisterTypes<OrderCaptured>();
		options.DeleteMode = StreamDeleteMode.Soft;   // default
		options.TransactionBatchSize = 100;
	});
```

This registers `IStreamStoreProvisioner` (singleton). Resolve it and call `CreateStoreAsync(tableName)` to obtain an `IStreamStore`. Each call ensures the table exists and seeds the `__GLOBAL__/SO-META` global-position entity.

> Pass a pre-built `TableServiceClient` instead of a connection string when you need custom credentials (e.g. Managed Identity).

---

## 4. Subscriptions, handlers, and projections

### 4.1 Typed event handler

```csharp
public class OrderCapturedHandler : IEventHandler<OrderCaptured>
{
	public Task HandleAsync(EventHandlerContext<OrderCaptured> context, CancellationToken ct = default)
	{
		// context.{SubscriptionId, StreamId, EventId, BatchId, StreamPosition, GlobalPosition, Timestamp, Payload, Metadata}
		return Task.CompletedTask;
	}
}
```

A single class can implement multiple `IEventHandler<T>` interfaces.

### 4.2 Register a subscription

```csharp
using Streamon.Subscription;
using Streamon.Azure.TableStorage.Subscription;

services.AddStreamSubscription(
		SubscriptionId.From("order-processing"),
		StreamSubscriptionType.CatchUp,            // or Live
		SubscriptionErrorHandling.Stop,            // or Ignore
		EventDispatchType.Sequential)              // or Concurrent
	.UseTableStorageCheckpointStore(connectionString, streamTableName: "orders")
	.UseTableStorageSubscriptionStreamReader(connectionString, streamTableName: "orders")
	.AddTypedEventHandler<OrderCapturedHandler>();
```

| Subscription type | Behavior when no checkpoint exists |
|---|---|
| `CatchUp` | Replays from `StreamPosition.Start` |
| `Live` | Starts from current end of stream |

Each subscription is stored as a **keyed singleton** under `SubscriptionId.Value`, so multiple independent subscriptions can coexist.

### 4.3 Polling worker

```csharp
public class SubscriptionWorker(SubscriptionManager manager) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			await manager.PollAsync(stoppingToken);
			await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
		}
	}
}
```

Or drive a single subscription: `manager.Get(SubscriptionId.From("order-processing")).PollAsync(ct)`.

### 4.4 Projections (read models)

Implement either or both of:

- `IEventInitialProjector<TEvent, TState>` — creates new state from a first event
- `IEventProjector<TEvent, TState>` — updates existing state; provides `GetKey(...)` so the store can locate it

`TState` must implement `Azure.Data.Tables.ITableEntity` when the projection is backed by Table Storage.

```csharp
public class OrderSummary : ITableEntity
{
	public string PartitionKey { get; set; } = "";
	public string RowKey { get; set; } = "";
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string OrderId { get; set; } = "";
	public string Product { get; set; } = "";
	public bool IsShipped { get; set; }
}

public class OrderProjector :
	IEventInitialProjector<OrderCaptured, OrderSummary>,
	IEventProjector<OrderShipped, OrderSummary>
{
	public OrderSummary Project(EventHandlerContext<OrderCaptured> e, CancellationToken ct = default) => new()
	{
		PartitionKey = e.StreamId.Value,           // see §5 for tenant-aware partition keys
		RowKey       = e.Payload.OrderId,
		OrderId      = e.Payload.OrderId,
		Product      = e.Payload.Product,
		IsShipped    = false,
	};

	public OrderSummary GetKey(EventHandlerContext<OrderShipped> e, CancellationToken ct = default) => new()
	{
		PartitionKey = e.StreamId.Value,
		RowKey       = e.Payload.OrderId,
	};

	public ValueTask<OrderSummary> ProjectAsync(OrderSummary state, EventHandlerContext<OrderShipped> e, CancellationToken ct = default)
	{
		state.IsShipped = true;
		return ValueTask.FromResult(state);
	}
}
```

Wire it on the subscription builder:

```csharp
services.AddSingleton<OrderProjector>();

services.AddStreamSubscription(SubscriptionId.From("order-projection"), StreamSubscriptionType.CatchUp)
	.UseTableStorageCheckpointStore(connectionString, "orders")
	.UseTableStorageSubscriptionStreamReader(connectionString, "orders")
	.AddTableStorageProjection<OrderProjector, OrderSummary>(
		connectionString,
		tableName: "orderSummaries",
		partitionKeySelector: s => s.PartitionKey,
		rowKeySelector:       s => s.RowKey);
```

Notes:

- Complex (non-native) projection properties are auto-serialized to JSON columns.
- If both `IEventInitialProjector<E,S>` and `IEventProjector<E,S>` exist for the same `E`, the update projector wins.
- An update projector is skipped silently when no existing state is found for the key.

---

## 5. Dynamic tables & multi-tenant partitioning

Streamon does **not** hard-code a table name. `IStreamStoreProvisioner.CreateStoreAsync(name)` accepts any valid Azure Table name, which is the primary mechanism for partitioning data across tenants, environments, bounded contexts, or time windows.

Inside one table, the **stream id** is the Table Storage `PartitionKey`. Combining both gives a two-level partitioning model:

```
Table        →  per-tenant / per-bounded-context isolation boundary
PartitionKey →  per-stream (per aggregate / per entity) within that boundary
```

### 5.1 Strategy A — Table per tenant (strongest isolation)

Best when: tenants need physical isolation, separate quotas, independent retention, or per-tenant subscriptions.

```csharp
public interface ITenantStreamStoreFactory
{
	Task<IStreamStore> GetStoreAsync(string tenantId, CancellationToken ct = default);
}

public sealed class TenantStreamStoreFactory(IStreamStoreProvisioner provisioner) : ITenantStreamStoreFactory
{
	private readonly ConcurrentDictionary<string, Task<IStreamStore>> _cache = new();

	public Task<IStreamStore> GetStoreAsync(string tenantId, CancellationToken ct = default) =>
		_cache.GetOrAdd(tenantId, t => provisioner.CreateStoreAsync(TableName(t), ct));

	// Azure table name rules: 3-63 chars, alphanumeric, must start with a letter.
	private static string TableName(string tenantId) =>
		"tenant" + new string(tenantId.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}

services.AddSingleton<ITenantStreamStoreFactory, TenantStreamStoreFactory>();
```

Per-tenant subscription registration (one keyed subscription per tenant):

```csharp
foreach (var tenantId in tenants)
{
	var tableName = $"tenant{tenantId}";
	services.AddStreamSubscription(SubscriptionId.From($"orders-{tenantId}"), StreamSubscriptionType.CatchUp)
		.UseTableStorageCheckpointStore(connectionString, tableName)
		.UseTableStorageSubscriptionStreamReader(connectionString, tableName)
		.AddTypedEventHandler<OrderCapturedHandler>();
}
```

Pros: full isolation, easy delete-a-tenant (`DeleteStore`), independent throughput. Cons: many tables to manage; no cross-tenant queries.

### 5.2 Strategy B — Shared table, tenant-prefixed stream ids

Best when: tenants are lightweight and you want one operational surface.

```csharp
StreamId ForTenant(string tenantId, string streamKey) =>
	new($"{tenantId}::{streamKey}");

await store.AppendEventsAsync(ForTenant("acme", "order-123"), StreamPosition.Start, [ /* events */ ]);
```

Because `StreamId` becomes the Azure `PartitionKey`, each tenant's streams live in their own Table Storage partitions — you still get partition-level parallelism, but everything sits in one table.

Tenant-aware projection partition keys:

```csharp
PartitionKey = $"{tenantId}::{e.Payload.CustomerId}",
RowKey       = e.Payload.OrderId,
```

Pros: one table, one subscription cursor. Cons: no physical isolation; tenant deletion = scanning + per-row deletes.

### 5.3 Strategy C — Hybrid

- Large/regulated tenants → dedicated table (Strategy A)
- Small/free-tier tenants → shared table with prefix (Strategy B)

The factory in 5.1 + the prefixing helper in 5.2 compose cleanly.

### 5.4 Other partitioning axes

The same `CreateStoreAsync(name)` mechanism works for:

- **Bounded contexts** — `orders`, `billing`, `inventory`
- **Environments** — `ordersStaging`, `ordersProd`
- **Time/cold partitions** — `orders2024`, `orders2025` (combine with a stream sweeper)

---

## 6. Testing

- **Unit tests:** use `Streamon.Memory.MemoryStreamStore` / `MemoryStreamStoreProvisioner` — no infrastructure.
- **Integration tests:** use `Testcontainers` + `AzuriteBuilder` and call the real `TableStreamStoreProvisioner`.
- Re-use fixtures from `Streamon.Tests.Fixtures` (`OrderEvents`, `PriorityAttribute`, `PriorityTestCollectionOrderer`).

```csharp
var provisioner = new MemoryStreamStoreProvisioner();
var store = await provisioner.CreateStoreAsync(nameof(MyTest));
await store.AppendEventsAsync(new StreamId("s-1"), StreamPosition.Start, [ new OrderCaptured(...) ]);
```

---

## 7. Conventions to follow when generating Streamon code

- Target **C# 12 / .NET 8**; use records, primary constructors, collection expressions (`[ ]`).
- Use strong-typed IDs everywhere (`StreamId`, `EventId`, `BatchId`, `SubscriptionId`, `StreamPosition`) — never raw `string`/`long`.
- `Task` for I/O; `ValueTask` for handler/projector hot paths.
- Add `ConfigureAwait(false)` on every `await` in library code under `src/`.
- Always accept `CancellationToken cancellationToken = default` as the **last** parameter on async methods.
- Multiple subscriptions ⇒ keyed DI (`AddKeyedSingleton` / `GetRequiredKeyedService`).
- Don't replicate `FetchLatestGlobalPositionAsync`'s full-table scan in new code.

---

## 8. Known work-in-progress areas — avoid generating implementations here without explicit user direction

- `EventProjectorBase<TProjector, TState>.ProjectAsync` (state read/write abstractions pending)
- Non-generic `IEventProjector` (contract being redesigned)
- `Streamon.Azure.CosmosDb` (partial)
- Telemetry, Claim Checks, Stream Sweeper, dedicated Stream Projections

---

## Further reading

- Repo `README.md` — narrative overview and full sample.
- `.github/copilot-instructions.md` — authoritative engineering rules and current WIP status.
- `src/Streamon.Azure.TableStorage/Subscription/StreamSubscriptionBuilderExtensions.cs` — Table Storage subscription/projection wiring.
- `src/Streamon.Azure.TableStorage/TableStreamStoreOptions.cs` — every tunable option for the Table Storage provider.
