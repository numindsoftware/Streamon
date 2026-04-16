![NuGet Version](https://img.shields.io/nuget/v/Streamon)
![GitHub License](https://img.shields.io/github/license/numindsoftware/Streamon)
[![.github/workflows/ci.yml](https://github.com/numindsoftware/Streamon/actions/workflows/ci.yml/badge.svg)](https://github.com/numindsoftware/Streamon/actions/workflows/ci.yml)
[![.github/workflows/cd.yml](https://github.com/numindsoftware/Streamon/actions/workflows/cd.yml/badge.svg)](https://github.com/numindsoftware/Streamon/actions/workflows/cd.yml)

Streamon
========

***Event streaming store platform for real-time data processing and analytics.***

Streamon is the attempt to create a common set of abstraction APIs over a variety of event streaming stores, such as Azure Table Storage, Azure Cosmos DB, and others.

The goal is to provide a simple and consistent way to work with event streams, allowing developers to focus on the business logic and not on the underlying storage implementation.

Streamon is not an Event Sourcing library, but it can be used as an Event/Stream store for existing ones.

## Providers
* In Memory (for testing and POC's)
* [Azure Table Storage](https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview)
* [Azure Cosmos DB](https://developer.azurecosmosdb.com/tools) (_in progress_, you can connect to CosmosDb using the Azure Table Storage provider above)

## Features

* POCO events, no base classes or inheritance required
* Event ids and Metadata detection by using both Attribute and Interface markers
* Customizable serialization and type resolution
* Flexible stream sorage naming and partitioning, e.g. allowing for multitenancy by using one stream per tenant
* Optimistic concurrency control
* Soft and hard deletion modes
* Global event positioning & tracking
* Subscriptions
* Projections (read model generation backed by storage providers)
* Snapshots and Checkpoints

## To Do's

* Telemetry
* Relational Stores (Entity Framework?)
* Claim Checks for large events
* Stream Sweeper (Archiving and Purging)

## Subscriptions

The `Streamon.Subscription` package provides a pipeline for consuming events from a stream store via polling-based subscriptions. Subscriptions track their progress using checkpoints, so they can resume from where they left off after restarts.

### Core Interfaces

| Interface | Responsibility |
|---|---|
| [`IEventHandler<TEvent>`](src/Streamon.Subscription/IEventHandler.cs) | Handles a single event type asynchronously |
| [`ICheckpointStore`](src/Streamon.Subscription/ICheckpointStore.cs) | Persists and retrieves the last processed position per subscription |
| [`ISubscriptionStreamReader`](src/Streamon.Subscription/ISubscriptionStreamReader.cs) | Reads events from a position and reports the last global position |
| [`IEventHandlerRegistry`](src/Streamon.Subscription/IEventHandlerRegistry.cs) | Discovers and stores handler delegates by event type |

### Subscription Types

| Type | Behavior |
|---|---|
| `StreamSubscriptionType.CatchUp` | Starts from `StreamPosition.Start` when no checkpoint exists — replays the full history |
| `StreamSubscriptionType.Live` | Starts from the current end of the stream — only new events are processed |
| `StreamSubscriptionType.InMemory` | In-memory only, useful for testing and temporary processing |

### Defining an Event Handler

Implement `IEventHandler<TEvent>` for each event type you want to handle:

```csharp
public class OrderCapturedHandler : IEventHandler<OrderCaptured>
{
    public ValueTask HandleAsync(EventHandlerContext<OrderCaptured> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Order captured: {context.Payload.Id} at position {context.GlobalPosition}");
        return ValueTask.CompletedTask;
    }
}

public class OrderShippedHandler : IEventHandler<OrderShipped>
{
    public ValueTask HandleAsync(EventHandlerContext<OrderShipped> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Order shipped: {context.Payload.Id}, tracking: {context.Payload.Tracking}");
        return ValueTask.CompletedTask;
    }
}
```

The `EventHandlerContext<T>` record provides the full event envelope to the handler:

| Property | Description |
|---|---|
| `SubscriptionId` | The subscription that received the event |
| `StreamId` | The stream the event belongs to |
| `EventId` | Unique event identifier |
| `StreamPosition` | Position within the stream |
| `GlobalPosition` | Position across all streams |
| `Timestamp` | When the event was recorded |
| `BatchId` | Groups events appended in the same call |
| `Payload` | The strongly-typed event payload (`T`) |
| `Metadata` | Optional per-event metadata dictionary |

A single class can implement multiple `IEventHandler<T>` interfaces to handle several event types:

```csharp
public class OrderEventHandlers : IEventHandler<OrderCaptured>, IEventHandler<OrderShipped>
{
    public ValueTask HandleAsync(EventHandlerContext<OrderCaptured> context, CancellationToken cancellationToken = default)
    {
        // handle OrderCaptured
        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(EventHandlerContext<OrderShipped> context, CancellationToken cancellationToken = default)
    {
        // handle OrderShipped
        return ValueTask.CompletedTask;
    }
}
```

### Registering Subscriptions (Dependency Injection)

Use `AddStreamSubscription` to register a subscription and configure its checkpoint store, stream reader, and event handlers via the fluent builder:

```csharp
services.AddStreamSubscription(SubscriptionId.From("order-processing"), StreamSubscriptionType.CatchUp)
    .UseTableStorageCheckpointStore(connectionString, streamTableName)
    .UseTableStorageSubscriptionStreamReader(connectionString, streamTableName)
    .AddEventHandler<OrderCapturedHandler>()
    .AddEventHandler<OrderShippedHandler>();
```

Multiple independent subscriptions can be registered in the same DI container — each is stored as a keyed singleton using its `SubscriptionId`:

```csharp
services.AddStreamSubscription(SubscriptionId.From("order-processing"), StreamSubscriptionType.CatchUp)
    .UseTableStorageCheckpointStore(connectionString, streamTableName)
    .UseTableStorageSubscriptionStreamReader(connectionString, streamTableName)
    .AddEventHandler<OrderCapturedHandler>();

services.AddStreamSubscription(SubscriptionId.From("analytics"), StreamSubscriptionType.Live)
    .UseTableStorageCheckpointStore(connectionString, streamTableName)
    .UseTableStorageSubscriptionStreamReader(connectionString, streamTableName)
    .AddEventHandler<AnalyticsHandler>();
```

### Polling for Events

The `SubscriptionManager` drives all registered subscriptions. Call `PollAsync` periodically (e.g. from a background service) to fetch and process new events:

```csharp
var manager = sp.GetRequiredService<SubscriptionManager>();

// Poll all subscriptions
await manager.PollAsync(cancellationToken);

// Or poll a specific subscription
var subscription = manager.Get(SubscriptionId.From("order-processing"));
await subscription.PollAsync(cancellationToken);
```

### Checkpoints

Checkpoints track the last successfully processed global position for each subscription. The `ICheckpointStore` interface supports:

- `GetCheckpointAsync(subscriptionId)` — returns the last saved position, or `StreamPosition.End` if none exists
- `SetCheckpointAsync(subscriptionId, position)` — persists the position after processing

The Azure Table Storage provider includes a built-in `TableCheckpointStore` implementation. For testing, you can implement `ICheckpointStore` with an in-memory dictionary.

### Projectors

For building read models or projections, implement `IEventInitialProjector<TEvent, TState>` (for creating initial state) and/or `IEventProjector<TEvent, TState>` (for updating existing state):

```csharp
public class OrderProjector : 
    IEventInitialProjector<OrderCaptured, OrderSummary>,
    IEventProjector<OrderShipped, OrderSummary>
{
    // Creates state from the first event
    public ValueTask<OrderSummary> ProjectAsync(EventHandlerContext<OrderCaptured> @event, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new OrderSummary(@event.Payload.Id, @event.Payload.Product, IsShipped: false));
    }

    public string GetIdentity(EventHandlerContext<OrderCaptured> @event, CancellationToken cancellationToken = default) => @event.Payload.Id;

    // Updates existing state
    public ValueTask<OrderSummary> ProjectAsync(OrderSummary state, EventHandlerContext<OrderShipped> @event, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(state with { IsShipped = true });
    }

    public string GetIdentity(EventHandlerContext<OrderShipped> @event, CancellationToken cancellationToken = default) => @event.Payload.Id;
}

public record OrderSummary(string Id, string Product, bool IsShipped);
```

> **Note:** The projector infrastructure (`EventProjectorBase<TProjector, TState>`) and its state read/write abstractions are still a work in progress. See the [copilot instructions](.github/copilot-instructions.md) for current status.

### Full Example

```csharp
// 1. Define your events
public record OrderCaptured(string Id, string Product, decimal Price);
public record OrderShipped(string Id, string Tracking);

// 2. Define your handler
public class OrderCapturedHandler : IEventHandler<OrderCaptured>
{
    public ValueTask HandleAsync(EventHandlerContext<OrderCaptured> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Processing order {context.Payload.Id} for {context.Payload.Product}");
        return ValueTask.CompletedTask;
    }
}

// 3. Register services
services.AddStreamon()
    .AddTableStorageStreamStore(connectionString, options =>
    {
        options.StreamTypeProvider = new StreamTypeProvider()
            .RegisterTypes<OrderCaptured>()
            .RegisterTypes<OrderShipped>();
    });

services.AddStreamSubscription(SubscriptionId.From("order-sub"), StreamSubscriptionType.CatchUp)
    .UseTableStorageCheckpointStore(connectionString, "streams")
    .UseTableStorageSubscriptionStreamReader(connectionString, "streams")
    .AddEventHandler<OrderCapturedHandler>();

// 4. Poll in a background service
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

## Azure Table Storage Provider Details

The Azure Table Storage provider is a simple implementation of the [`IStreamStore`](src/Streamon/IStreamStore.cs) interface.
It uses Azure Table Storage to store events in a single table.
The table is partitioned by the stream id and the row key is the event id.
The provider uses the [`Ulid`](https://github.com/Cysharp/Ulid) library to generate unique identifiers for events.

Table Storage only support batches of up to 100 entities, trying to write more than that will result in an exception.
The responsibility of handling this is left to the caller, as the provider does not implement any batching logic due to the fact that it can't guarantee the consistency of persistence across different batches.

## Thanks

For inspiration and ideas, thanks to:

[Streamstone](https://github.com/yevhen/Streamstone)
[Eveneum](https://github.com/Eveneum/Eveneum)
[Eventflow](https://geteventflow.net/)
and
[Eventuous](https://eventuous.dev/)

Icon:

[Pipe icons created by srip - Flaticon](https://www.flaticon.com/free-icons/pipe)

## Dependencies

* [CosmosDb Azure SDK](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/quickstart-dotnet) CosmosDb provider
* [Azure Storage SDK](https://learn.microsoft.com/en-us/azure/storage/) Azure Table Storage provider
* [xUnit](https://xunit.net/) for unit testing
* [Act](https://github.com/nektos/act) for local testing of Github Actions
* [Ulid](https://github.com/Cysharp/Ulid) for unique identifiers generation
* [Test Containers for .NET](https://testcontainers.com/guides/getting-started-with-testcontainers-for-dotnet/) for integration testing

## Continuous Integration & Deployment

The project is built and tested using GitHub Actions. The build artifacts are published to GitHub Packages.

Github actions are configured trigger a CI build on every push to the `main` branch. Packages will be published to nuget.org on every release.

Local testing and development of Github actions can be done using the [`act`](https://github.com/nektos/act) tool. 

## Contributing

Let's keep it simple and clean. Feel free to open an issue or a pull request, I'll review it and most likely than not, merge it.

## License

[MIT License](LICENSE)

