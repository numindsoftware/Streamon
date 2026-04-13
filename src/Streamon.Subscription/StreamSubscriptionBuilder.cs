namespace Streamon.Subscription;

public class StreamSubscriptionBuilder(SubscriptionId subscriptionId, StreamSubscriptionType streamSubscriptionType = default, SubscriptionErrorHandling errorHandling = default, EventDispatchType eventDispatchType = default)
{
    private Func<ICheckpointStore>? _checkpointStoreFactory;
    private Func<ISubscriptionStreamReader>? _subscriptionStreamReaderFactory;
    private Func<Type, object>? _serviceResolver;
    private readonly List<Func<IEventHandler>> _handlerFactories = [];
    private readonly List<Type> _typedEventHandlerTypes = [];
    private EventPipelineBuilder EventPipelineBuilder { get; } = new();

    /// <summary>
    /// Gets the subscription identity associated with this builder instance.
    /// </summary>
    public SubscriptionId SubscriptionId => subscriptionId;

    /// <summary>
    /// Configures the subscription to use the specified <see cref="ICheckpointStore"/> instance.
    /// </summary>
    public StreamSubscriptionBuilder UseCheckpointStore(ICheckpointStore store) =>
        UseCheckpointStore(() => store);

    /// <summary>
    /// Configures the subscription to use the specified <see cref="ISubscriptionStreamReader"/> instance.
    /// </summary>
    public StreamSubscriptionBuilder UseSubscriptionStreamReader(ISubscriptionStreamReader reader) =>
        UseSubscriptionStreamReader(() => reader);

    /// <summary>
    /// Configures the subscription to resolve <see cref="ICheckpointStore"/> via a factory delegate.
    /// The factory is invoked once during <see cref="Build"/> and can close over any external state.
    /// </summary>
    public StreamSubscriptionBuilder UseCheckpointStore(Func<ICheckpointStore> factory)
    {
        _checkpointStoreFactory = factory;
        return this;
    }

    /// <summary>
    /// Configures the subscription to resolve <see cref="ISubscriptionStreamReader"/> via a factory delegate.
    /// The factory is invoked once during <see cref="Build"/> and can close over any external state.
    /// </summary>
    public StreamSubscriptionBuilder UseSubscriptionStreamReader(Func<ISubscriptionStreamReader> factory)
    {
        _subscriptionStreamReaderFactory = factory;
        return this;
    }

    // ── Strategy C: Service resolver (set by DI integration layer) ──────

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

    // ── Handler registration ────────────────────────────────────────────

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
        _handlerFactories.Add(factory);
        return this;
    }

    /// <summary>
    /// Registers an untyped <see cref="IEventHandler"/> by type. Requires a service resolver
    /// (set automatically when using the DI integration) or will throw at build time.
    /// </summary>
    public StreamSubscriptionBuilder AddEventHandler<T>() where T : class, IEventHandler
    {
        _handlerFactories.Add(() => (IEventHandler)ResolveService(typeof(T)));
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
    public StreamSubscription Build()
    {
        var checkpointStore = _checkpointStoreFactory?.Invoke()
            ?? throw new InvalidOperationException("No checkpoint store configured. Call UseCheckpointStore before building.");
        var subscriptionStreamReader = _subscriptionStreamReaderFactory?.Invoke()
            ?? throw new InvalidOperationException("No subscription stream reader configured. Call UseSubscriptionStreamReader before building.");

        List<IEventHandler> handlers = ResolveHandlers();

        var pipeline = EventPipelineBuilder.Build(async (@event, cancellationToken) =>
        {
            if (eventDispatchType == EventDispatchType.Concurrent)
            {
                var tasks = handlers.Select(async handler => handler.HandleAsync(@event, cancellationToken));
                await Task.WhenAll(tasks);
            }
            else
            {
                foreach (var handler in handlers)
                {
                    await handler.HandleAsync(@event, cancellationToken).ConfigureAwait(false);
                }
            }
        });

        return new StreamSubscription(subscriptionId, streamSubscriptionType, errorHandling, checkpointStore, subscriptionStreamReader, pipeline);
    }

    private List<IEventHandler> ResolveHandlers()
    {
        List<IEventHandler> handlers = [.. _handlerFactories.Select(f => f())];

        if (_typedEventHandlerTypes.Count > 0)
        {
            var typedHandler = new TypedEventHandler(subscriptionId);
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
