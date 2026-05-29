namespace Streamon.Subscription;

public class StreamSubscriptionBuilder
{
    private Func<string, ISubscriptionStreamReader>? _subscriptionStreamReaderFactory;
    private Func<string, ICheckpointStore>? _checkpointStoreFactory;
    private Func<string, IEventInbox>? _eventInboxFactory;

    private Func<Type, object>? _serviceResolver;
    private readonly List<Func<string, IEventHandler>> _handlerFactories = [];
    private readonly List<Type> _typedEventHandlerTypes = [];
    private bool _useInboxDeduplication;
    private EventPipelineBuilder EventPipelineBuilder { get; } = new();

    public StreamSubscriptionBuilder(SubscriptionId subscriptionId, StreamSubscriptionOptions options) =>
        (SubscriptionId, Options) = (subscriptionId, options ?? throw new ArgumentNullException(nameof(options)));

    /// <summary>
    /// Gets the subscription identity associated with this builder instance.
    /// </summary>
    public SubscriptionId SubscriptionId { get; }

    /// <summary>
    /// Gets the configured <see cref="StreamSubscriptionOptions"/> (default names and per-component naming strategies).
    /// </summary>
    public StreamSubscriptionOptions Options { get; }

    /// <summary>
    /// Configures the subscription to resolve <see cref="ISubscriptionStreamReader"/> via a factory delegate.
    /// The factory is invoked once during <see cref="Build"/> and can close over any external state.
    /// </summary>
    public StreamSubscriptionBuilder UseSubscriptionStreamReader(Func<string, ISubscriptionStreamReader> factory)
    {
        _subscriptionStreamReaderFactory = factory;
        return this;
    }

    /// <summary>
    /// Configures the subscription to use the specified <see cref="ISubscriptionStreamReader"/> instance.
    /// </summary>
    public StreamSubscriptionBuilder UseSubscriptionStreamReader(ISubscriptionStreamReader reader) =>
        UseSubscriptionStreamReader(_ => reader);

    /// <summary>
    /// Configures the subscription to resolve <see cref="ICheckpointStore"/> via a factory delegate.
    /// The factory is invoked once during <see cref="Build"/> and can close over any external state.
    /// </summary>
    public StreamSubscriptionBuilder UseCheckpointStore(Func<string, ICheckpointStore> factory)
    {
        _checkpointStoreFactory = factory;
        return this;
    }

    /// <summary>
    /// Configures the subscription to use the specified <see cref="ICheckpointStore"/> instance.
    /// </summary>
    public StreamSubscriptionBuilder UseCheckpointStore(ICheckpointStore store) =>
        UseCheckpointStore(_ => store);

    public StreamSubscriptionBuilder UseEventInbox(Func<string, IEventInbox> factory)
    {
        _eventInboxFactory = factory;
        return this;
    }

    public StreamSubscriptionBuilder UseEventInbox(IEventInbox inbox) =>
        UseEventInbox(_ => inbox);

    /// <summary>
    /// Enables per-handler deduplication via the configured <see cref="IEventInbox"/>.
    /// When enabled, an <see cref="InboxDeduplicationMiddleware"/> is composed at the innermost
    /// position of <i>each handler's</i> dispatch pipeline so that an event already recorded for
    /// (<see cref="SubscriptionId"/>, consumer-name) short-circuits that handler's invocation and
    /// a successful invocation results in a single <see cref="IEventInbox.MarkProcessedAsync"/> call
    /// per handler.
    /// </summary>
    /// <param name="consumerName">
    /// Optional consumer name used by the inbox. When <see langword="null"/> (the default), each
    /// handler uses its own type name as the consumer name, matching the inbox contract
    /// ("a consumer is uniquely identified by a (SubscriptionId, consumer-name) pair — typically
    /// the handler's type name"). Specify an explicit value to share a single dedup record across
    /// every handler in the subscription.
    /// </param>
    /// <remarks>
    /// Requires <see cref="UseEventInbox(Func{string, IEventInbox})"/> (or its instance overload)
    /// to be configured; otherwise <see cref="Build"/> throws <see cref="InvalidOperationException"/>.
    /// </remarks>
    public StreamSubscriptionBuilder UseInboxDeduplication()
    {
        _useInboxDeduplication = true;
        return this;
    }

    /// <summary>
    /// Sets a general-purpose service resolver used to create handler instances registered via
    /// <see cref="AddEventHandler{T}()"/> and <see cref="AddTypedEventHandler{T}()"/>.
    /// Typically called by the DI integration layer to bridge <c>IServiceProvider</c>.
    /// </summary>
    internal StreamSubscriptionBuilder WithServiceResolver(Func<Type, object> resolver)
    {
        _serviceResolver = resolver;
        return this;
    }

    /// <summary>
    /// Registers a pre-constructed <see cref="IEventHandler"/> instance.
    /// </summary>
    public StreamSubscriptionBuilder AddEventHandler(IEventHandler handler) =>
        AddEventHandler(() => handler);

    /// <summary>
    /// Registers an <see cref="IEventHandler"/> resolved via a factory delegate on each build.
    /// </summary>
    public StreamSubscriptionBuilder AddEventHandler(Func<IEventHandler> factory)
    {
        _handlerFactories.Add(_ => factory());
        return this;
    }

    /// <summary>
    /// Registers an untyped <see cref="IEventHandler"/> by type. Requires a service resolver
    /// (set automatically when using the DI integration) or will throw at build time.
    /// </summary>
    public StreamSubscriptionBuilder AddEventHandler<T>() where T : class, IEventHandler
    {
        _handlerFactories.Add(_ => (IEventHandler)ResolveService(typeof(T)));
        return this;
    }

    /// <summary>
    /// Registers a strongly-typed <see cref="IEventHandler{TEvent}"/> by type. Composed behind a
    /// <see cref="TypedEventHandler"/> at build time. Requires a service resolver when using DI.
    /// </summary>
    public StreamSubscriptionBuilder AddTypedEventHandler<T>() where T : class
    {
        _typedEventHandlerTypes.Add(typeof(T));
        return this;
    }

    // ── Projection registration ─────────────────────────────────────────

    /// <summary>
    /// Registers a projection using the specified <see cref="IProjectionStore{TState}"/> instance.
    /// The projector of type <typeparamref name="TProjector"/> is resolved via the service resolver
    /// at build time. Use this overload when the store lifetime is managed externally.
    /// </summary>
    /// <typeparam name="TProjector">The projector type implementing
    /// <see cref="IEventInitialProjector{TEvent, TState}"/> and/or
    /// <see cref="IEventProjector{TEvent, TState}"/>.</typeparam>
    /// <typeparam name="TState">The projection state type.</typeparam>
    public StreamSubscriptionBuilder AddProjection<TProjector, TState>(IProjectionStore<TState> store)
        where TProjector : class =>
        AddProjection<TProjector, TState>(() => store);

    /// <summary>
    /// Registers a projection that resolves <see cref="IProjectionStore{TState}"/> via a factory delegate.
    /// The projector of type <typeparamref name="TProjector"/> is resolved via the service resolver
    /// at build time. The factory is invoked once during <see cref="Build"/>.
    /// </summary>
    /// <typeparam name="TProjector">The projector type implementing
    /// <see cref="IEventInitialProjector{TEvent, TState}"/> and/or
    /// <see cref="IEventProjector{TEvent, TState}"/>.</typeparam>
    /// <typeparam name="TState">The projection state type.</typeparam>
    public StreamSubscriptionBuilder AddProjection<TProjector, TState>(Func<IProjectionStore<TState>> storeFactory)
        where TProjector : class =>
        AddProjection<TProjector, TState>(_ => storeFactory());

    /// <summary>
    /// Registers a projection whose <see cref="IProjectionStore{TState}"/> is created from a suffix-aware
    /// factory. The suffix passed to <see cref="Build"/> is forwarded so the store can compose a
    /// per-tenant (or per-scope) backing resource name. Use this overload when each projection should
    /// pick its own naming strategy independent of the subscription-wide stream/checkpoint/inbox strategies.
    /// </summary>
    /// <typeparam name="TProjector">The projector type implementing
    /// <see cref="IEventInitialProjector{TEvent, TState}"/> and/or
    /// <see cref="IEventProjector{TEvent, TState}"/>.</typeparam>
    /// <typeparam name="TState">The projection state type.</typeparam>
    public StreamSubscriptionBuilder AddProjection<TProjector, TState>(Func<string, IProjectionStore<TState>> storeFactory)
        where TProjector : class
    {
        _handlerFactories.Add(suffix =>
        {
            var projector = ResolveService(typeof(TProjector));
            var store = storeFactory(suffix);
            var handler = new ProjectionEventHandler<TState>(SubscriptionId, store);
            handler.RegisterProjectorsFrom(projector);
            return handler;
        });
        return this;
    }

    /// <summary>
    /// Registers a projection where both the projector and <see cref="IProjectionStore{TState}"/> are
    /// resolved from the service container at build time. Requires DI integration
    /// (<see cref="ServiceCollectionExtensions.AddStreamonSubscription"/>).
    /// </summary>
    /// <typeparam name="TProjector">The projector type implementing
    /// <see cref="IEventInitialProjector{TEvent, TState}"/> and/or
    /// <see cref="IEventProjector{TEvent, TState}"/>.</typeparam>
    /// <typeparam name="TState">The projection state type.</typeparam>
    public StreamSubscriptionBuilder AddProjection<TProjector, TState>()
        where TProjector : class
    {
        _handlerFactories.Add(_ =>
        {
            var projector = ResolveService(typeof(TProjector));
            var store = (IProjectionStore<TState>)ResolveService(typeof(IProjectionStore<TState>));
            var handler = new ProjectionEventHandler<TState>(SubscriptionId, store);
            handler.RegisterProjectorsFrom(projector);
            return handler;
        });
        return this;
    }

    /// <summary>
    /// Adds a middleware to the event dispatch pipeline. Middlewares execute in registration order,
    /// wrapping the terminal handler dispatch. Use for cross-cutting concerns such as tracing,
    /// partitioning, or concurrent dispatch.
    /// </summary>
    public StreamSubscriptionBuilder ConfigureMiddleware(Action<EventPipelineBuilder> configure)
    {
        configure(EventPipelineBuilder);
        return this;
    }

    /// <summary>
    /// Builds the <see cref="StreamSubscription"/> using previously registered factories, instances,
    /// and/or service resolver. Throws <see cref="InvalidOperationException"/> if required
    /// infrastructure components have not been configured.
    /// </summary>
    /// <remarks>
    /// The configured event pipeline (user middlewares + optional inbox deduplication) is composed
    /// <i>per handler</i>, so middlewares observe each (event, handler) combination individually and
    /// each handler dedups under its own consumer name. The <see cref="StreamSubscriptionOptions.EventDispatchType"/>
    /// setting controls whether per-handler pipelines run sequentially or concurrently for a given event.
    /// </remarks>
    public StreamSubscription Build(string suffix = "")
    {
        var checkpointStore = _checkpointStoreFactory?.Invoke(suffix)
            ?? throw new InvalidOperationException("No checkpoint store configured. Call UseCheckpointStore before building.");
        var subscriptionStreamReader = _subscriptionStreamReaderFactory?.Invoke(suffix)
            ?? throw new InvalidOperationException("No subscription stream reader configured. Call UseSubscriptionStreamReader before building.");

        var handlers = ResolveHandlers(suffix);

        IEventInbox? inbox = null;
        if (_useInboxDeduplication)
        {
            inbox = _eventInboxFactory?.Invoke(suffix)
                ?? throw new InvalidOperationException("Inbox deduplication is enabled but no IEventInbox is configured. Call UseEventInbox before UseInboxDeduplication.");
        }

        // Compose the configured event pipeline (user middlewares + optional inbox dedup) per handler,
        // so each (event, handler) combination flows through its own middleware chain and each handler
        // dedups under its own consumer name.
        var perHandlerPipelines = handlers.Select(handler =>
        {
            EventHandlerDelegate terminal = (@event, cancellationToken) => handler.HandleAsync(@event, cancellationToken);
            if (inbox is not null)
            {
                var consumerName = handler.GetType().FullName ?? handler.GetType().Name;
                var dedup = new InboxDeduplicationMiddleware(inbox, SubscriptionId, consumerName);
                var inner = terminal;
                terminal = (@event, cancellationToken) => dedup.InvokeAsync(@event, inner, cancellationToken);
            }
            return EventPipelineBuilder.Build(terminal);
        }).ToArray();

        async Task dispatch(Event @event, CancellationToken cancellationToken)
        {
            if (Options.EventDispatchType == EventDispatchType.Concurrent)
            {
                var tasks = perHandlerPipelines.Select(p => p(@event, cancellationToken));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                foreach (var p in perHandlerPipelines)
                {
                    await p(@event, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return new StreamSubscription(SubscriptionId, Options.StreamSubscriptionType, Options.ErrorHandling, checkpointStore, subscriptionStreamReader, dispatch);
    }

    private List<IEventHandler> ResolveHandlers(string suffix)
    {
        List<IEventHandler> handlers = [.. _handlerFactories.Select(f => f(suffix))];

        if (_typedEventHandlerTypes.Count > 0)
        {
            var typedHandler = new TypedEventHandler(SubscriptionId);
            foreach (var handlerType in _typedEventHandlerTypes)
            {
                var instance = ResolveService(handlerType);
                typedHandler.RegisterHandlersFrom(instance);
            }
            handlers.Add(typedHandler);
        }

        return handlers;
    }

    private object ResolveService(Type type) =>
        _serviceResolver?.Invoke(type)
            ?? throw new InvalidOperationException(
                $"Cannot resolve service of type '{type.Name}'. Register the handler with a factory delegate " +
                $"or use the DI integration (AddStreamSubscription) which sets up a service resolver automatically.");
}
