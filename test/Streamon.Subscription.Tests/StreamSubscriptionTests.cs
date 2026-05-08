using Microsoft.Extensions.DependencyInjection;
using Streamon.Tests.Fixtures;

namespace Streamon.Subscription.Tests;

public class StreamSubscriptionTests
{
    private static readonly SubscriptionId SubId = SubscriptionId.From("test-sub");

    private readonly CheckpointStore _checkpointStore = new();
    private readonly SubscriptionStreamReader _reader = new();

    public StreamSubscriptionTests()
    {
        _reader.Events.Add(new Event(StreamId.From("order-123"), EventId.New(), StreamPosition.From(1), StreamPosition.From(1), DateTimeOffset.Now, BatchId.New(), OrderEvents.OrderCaptured));
        _reader.Events.Add(new Event(StreamId.From("order-123"), EventId.New(), StreamPosition.From(2), StreamPosition.From(2), DateTimeOffset.Now, BatchId.New(), OrderEvents.OrderConfirmed));
        _reader.Events.Add(new Event(StreamId.From("order-123"), EventId.New(), StreamPosition.From(3), StreamPosition.From(3), DateTimeOffset.Now, BatchId.New(), OrderEvents.OrderShipped));
        _reader.Events.Add(new Event(StreamId.From("order-123"), EventId.New(), StreamPosition.From(4), StreamPosition.From(4), DateTimeOffset.Now, BatchId.New(), OrderEvents.OrderFulfilled));
        _reader.Events.Add(new Event(StreamId.From("order-124"), EventId.New(), StreamPosition.From(1), StreamPosition.From(5), DateTimeOffset.Now, BatchId.New(), OrderEvents.OrderCaptured));
        _reader.Events.Add(new Event(StreamId.From("order-124"), EventId.New(), StreamPosition.From(2), StreamPosition.From(6), DateTimeOffset.Now, BatchId.New(), OrderEvents.OrderCancelled));
    }

    private StreamSubscription CreateSubscription(
        IReadOnlyList<IEventHandler> handlers,
        StreamSubscriptionType type = StreamSubscriptionType.CatchUp,
        SubscriptionErrorHandling errorHandling = SubscriptionErrorHandling.Throw,
        IReadOnlyList<IEventMiddleware>? middlewares = null)
    {
        var builder = new StreamSubscriptionBuilder(SubId, type, errorHandling)
            .UseCheckpointStore(_checkpointStore)
            .UseSubscriptionStreamReader(_reader);

        foreach (var handler in handlers)
        {
            builder.AddEventHandler(handler);
        }

        if (middlewares is not null)
        {
            builder.ConfigureMiddleware(pipeline =>
            {
                foreach (var middleware in middlewares)
                {
                    pipeline.UseMiddleware(() => middleware);
                }
            });
        }

        return builder.Build();
    }

    // ── Subscription Types ──────────────────────────────────────────────

    [Fact]
    public async Task CatchUpSubscriptionProcessesAllEventsFromStart()
    {
        var handler = new TrackingHandler();
        var subscription = CreateSubscription([handler], StreamSubscriptionType.CatchUp);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(_reader.Events.Count, handler.HandledEvents.Count);
    }

    [Fact]
    public async Task CatchUpSubscriptionInitializesCheckpointAtStart()
    {
        var subscription = CreateSubscription([], StreamSubscriptionType.CatchUp);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        var checkpoint = await _checkpointStore.GetCheckpointAsync(SubId, cancellationToken: TestContext.Current.CancellationToken);
        // No handlers, but events still flow — checkpoint advances to last global position
        var lastGlobal = await _reader.GetLastGlobalPositionAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(lastGlobal, checkpoint);
    }

    [Fact]
    public async Task LiveSubscriptionInitializesCheckpointAtEnd()
    {
        var subscription = CreateSubscription([], StreamSubscriptionType.Live);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        var checkpoint = await _checkpointStore.GetCheckpointAsync(SubId, cancellationToken: TestContext.Current.CancellationToken);
        var lastGlobal = await _reader.GetLastGlobalPositionAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(lastGlobal, checkpoint);
    }

    [Fact]
    public async Task LiveSubscriptionSkipsExistingEvents()
    {
        var handler = new TrackingHandler();
        var subscription = CreateSubscription([handler], StreamSubscriptionType.Live);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(handler.HandledEvents);
    }

    [Fact]
    public async Task CatchUpSubscriptionAdvancesCheckpointPerEvent()
    {
        var handler = new TrackingHandler();
        var subscription = CreateSubscription([handler], StreamSubscriptionType.CatchUp);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        var checkpoint = await _checkpointStore.GetCheckpointAsync(SubId, cancellationToken: TestContext.Current.CancellationToken);
        var lastGlobal = await _reader.GetLastGlobalPositionAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(lastGlobal, checkpoint);
    }

    [Fact]
    public async Task SecondPollOnlyProcessesNewEvents()
    {
        var handler = new TrackingHandler();
        var subscription = CreateSubscription([handler], StreamSubscriptionType.CatchUp);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);
        var countAfterFirst = handler.HandledEvents.Count;

        _reader.Events.Add(new Event(StreamId.From("order-125"), EventId.New(), StreamPosition.From(1), StreamPosition.From(7), DateTimeOffset.Now, BatchId.New(), OrderEvents.OrderCaptured));

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(countAfterFirst + 1, handler.HandledEvents.Count);
        Assert.Equal(StreamId.From("order-125"), handler.HandledEvents[^1].StreamId);
    }

    // ── Error Handling ──────────────────────────────────────────────────

    [Fact]
    public async Task ThrowErrorHandlingPropagatesException()
    {
        var failingHandler = new FailingHandler(failOnNth: 1);
        var subscription = CreateSubscription([failingHandler], errorHandling: SubscriptionErrorHandling.Throw);

        await Assert.ThrowsAsync<InvalidOperationException>(() => subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ThrowErrorHandlingDoesNotAdvanceCheckpointPastFailedEvent()
    {
        var failingHandler = new FailingHandler(failOnNth: 1);
        var subscription = CreateSubscription([failingHandler], errorHandling: SubscriptionErrorHandling.Throw);

        await Assert.ThrowsAsync<InvalidOperationException>(() => subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken));

        var checkpoint = await _checkpointStore.GetCheckpointAsync(SubId, cancellationToken: TestContext.Current.CancellationToken);
        // Checkpoint was set to Start during initialization but not advanced past the failed event
        Assert.Equal(StreamPosition.Start, checkpoint);
    }

    [Fact]
    public async Task IgnoreErrorHandlingSwallowsExceptionAndContinues()
    {
        var failingHandler = new FailingHandler(failOnNth: 1);
        var subscription = CreateSubscription([failingHandler], errorHandling: SubscriptionErrorHandling.Ignore);

        var exception = await Record.ExceptionAsync(() => subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }

    [Fact]
    public async Task IgnoreErrorHandlingAdvancesCheckpointPastFailedEvent()
    {
        var failingHandler = new FailingHandler(failOnNth: 1);
        var subscription = CreateSubscription([failingHandler], errorHandling: SubscriptionErrorHandling.Ignore);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        var checkpoint = await _checkpointStore.GetCheckpointAsync(SubId, cancellationToken: TestContext.Current.CancellationToken);
        var lastGlobal = await _reader.GetLastGlobalPositionAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(lastGlobal, checkpoint);
    }

    [Fact]
    public async Task IgnoreErrorHandlingProcessesRemainingEventsAfterFailure()
    {
        // Fail on 2nd event, remaining 4 events should still be processed
        var failingHandler = new FailingHandler(failOnNth: 2);
        var subscription = CreateSubscription([failingHandler], errorHandling: SubscriptionErrorHandling.Ignore);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(_reader.Events.Count, failingHandler.CallCount);
    }

    // ── Sequential Handler Dispatch ─────────────────────────────────────

    [Fact]
    public async Task HandlersExecuteSequentiallyByDefault()
    {
        var order = new List<string>();
        var handler1 = new OrderRecordingHandler("A", order);
        var handler2 = new OrderRecordingHandler("B", order);
        var subscription = CreateSubscription([handler1, handler2]);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        // For each event, A should run before B
        for (var i = 0; i < _reader.Events.Count; i++)
        {
            Assert.Equal("A", order[i * 2]);
            Assert.Equal("B", order[i * 2 + 1]);
        }
    }

    [Fact]
    public async Task MultipleHandlersAllReceiveEveryEvent()
    {
        var handler1 = new TrackingHandler();
        var handler2 = new TrackingHandler();
        var subscription = CreateSubscription([handler1, handler2]);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(_reader.Events.Count, handler1.HandledEvents.Count);
        Assert.Equal(_reader.Events.Count, handler2.HandledEvents.Count);
    }

    // ── TypedEventHandler Dispatch ──────────────────────────────────────

    [Fact]
    public async Task TypedEventHandlerDispatchesToMatchingType()
    {
        var typedHandler = new TypedEventHandler(SubId);
        typedHandler.RegisterHandlersFrom(new TestEventHandler());
        var subscription = CreateSubscription([typedHandler]);

        var exception = await Record.ExceptionAsync(() => subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }

    [Fact]
    public async Task TypedEventHandlerIgnoresNonMatchingTypes()
    {
        var tracking = new TrackingHandler();
        var typedHandler = new TypedEventHandler(SubId);
        // Only handles OrderShipped — all other event types are silently ignored by the TypedEventHandler
        typedHandler.RegisterHandlersFrom(new TestEventHandler());
        var subscription = CreateSubscription([typedHandler, tracking]);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        // All events still flow through the tracking handler
        Assert.Equal(_reader.Events.Count, tracking.HandledEvents.Count);
    }

    // ── Middleware Pipeline ──────────────────────────────────────────────

    [Fact]
    public async Task MiddlewareExecutesAroundHandlerDispatch()
    {
        var handler = new TrackingHandler();
        var middleware = new TrackingMiddleware();
        var subscription = CreateSubscription([handler], middlewares: [middleware]);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(_reader.Events.Count, middleware.BeforeEvents.Count);
        Assert.Equal(_reader.Events.Count, middleware.AfterEvents.Count);
        Assert.Equal(_reader.Events.Count, handler.HandledEvents.Count);
    }

    [Fact]
    public async Task MiddlewaresExecuteInRegistrationOrder()
    {
        var order = new List<string>();
        var outer = new OrderRecordingMiddleware("outer", order);
        var inner = new OrderRecordingMiddleware("inner", order);
        var handler = new TrackingHandler();
        var subscription = CreateSubscription([handler], middlewares: [outer, inner]);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Per event: outer-before, inner-before, inner-after, outer-after
        Assert.Equal("outer-before", order[0]);
        Assert.Equal("inner-before", order[1]);
        Assert.Equal("inner-after", order[2]);
        Assert.Equal("outer-after", order[3]);
    }

    [Fact]
    public async Task ShortCircuitMiddlewarePreventsHandlerExecution()
    {
        var handler = new TrackingHandler();
        var middleware = new ShortCircuitMiddleware();
        var subscription = CreateSubscription([handler], middlewares: [middleware]);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(_reader.Events.Count, middleware.InterceptedEvents.Count);
        Assert.Empty(handler.HandledEvents);
    }

    [Fact]
    public async Task MiddlewareErrorRespectErrorHandlingSetting()
    {
        var failingMiddleware = new FailingMiddleware();
        var handler = new TrackingHandler();
        var subscription = CreateSubscription([handler], middlewares: [failingMiddleware], errorHandling: SubscriptionErrorHandling.Throw);

        await Assert.ThrowsAsync<InvalidOperationException>(() => subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.Empty(handler.HandledEvents);
    }

    [Fact]
    public async Task MiddlewareErrorIgnoredWhenConfigured()
    {
        var failingMiddleware = new FailingMiddleware();
        var handler = new TrackingHandler();
        var subscription = CreateSubscription([handler], middlewares: [failingMiddleware], errorHandling: SubscriptionErrorHandling.Ignore);

        var exception = await Record.ExceptionAsync(() => subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }

    // ── Concurrent / Async Dispatch via Middleware ───────────────────────

    [Fact]
    public async Task ConcurrentMiddlewareDispatchesOnThreadPool()
    {
        var handler = new ConcurrentTrackingHandler();
        var middleware = new ConcurrentDispatchMiddleware();
        var subscription = CreateSubscription([handler], middlewares: [middleware]);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(_reader.Events.Count, handler.HandledEvents.Count);
    }

    // ── Empty Subscription ──────────────────────────────────────────────

    [Fact]
    public async Task PollWithNoEventsCompletesWithoutError()
    {
        var emptyReader = new SubscriptionStreamReader();
        var handler = new TrackingHandler();
        var subscription = new StreamSubscriptionBuilder(SubId, StreamSubscriptionType.CatchUp)
            .UseCheckpointStore(_checkpointStore)
            .UseSubscriptionStreamReader(emptyReader)
            .AddEventHandler(handler)
            .Build();

        var exception = await Record.ExceptionAsync(() => subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Null(exception);
        Assert.Empty(handler.HandledEvents);
    }

    [Fact]
    public async Task PollWithNoHandlersStillAdvancesCheckpoint()
    {
        var subscription = CreateSubscription([]);

        await subscription.PollAsync(cancellationToken: TestContext.Current.CancellationToken);

        var checkpoint = await _checkpointStore.GetCheckpointAsync(SubId, cancellationToken: TestContext.Current.CancellationToken);
        var lastGlobal = await _reader.GetLastGlobalPositionAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(lastGlobal, checkpoint);
    }

    // ── Helper types ────────────────────────────────────────────────────

    private class OrderRecordingHandler(string name, List<string> order) : IEventHandler
    {
        public Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
        {
            order.Add(name);
            return Task.CompletedTask;
        }
    }

    private class OrderRecordingMiddleware(string name, List<string> order) : IEventMiddleware
    {
        public async Task InvokeAsync(Event context, EventHandlerDelegate next, CancellationToken cancellationToken = default)
        {
            order.Add($"{name}-before");
            await next(context, cancellationToken).ConfigureAwait(false);
            order.Add($"{name}-after");
        }
    }

    private class FailingMiddleware : IEventMiddleware
    {
        public Task InvokeAsync(Event context, EventHandlerDelegate next, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated middleware failure");
        }
    }
}

