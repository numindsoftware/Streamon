using System.Reflection;
using System.Text.Json;

namespace Streamon.Subscription;

public class StreamSubscriptionBuilder(SubscriptionId subscriptionId, StreamSubscriptionType streamSubscriptionType = default, SubscriptionErrorHandling errorHandling = default, EventDispatchType eventDispatchType = default)
{
    private Func<string, ISubscriptionStreamReader>? _subscriptionStreamReaderFactory;
    private Func<string, ICheckpointStore>? _checkpointStoreFactory;
    private Func<string, IEventInbox>? _eventInboxFactory;

    private Func<IStreamTypeProvider>? _streamTypeProviderFactory;
    private Func<Type, object>? _serviceResolver;
    private Func<string, string, string> _nameConvention = (prefix, suffix) => prefix + suffix;
    private readonly List<Func<IEventHandler>> _handlerFactories = [];
    private readonly List<Type> _typedEventHandlerTypes = [];
    private EventPipelineBuilder EventPipelineBuilder { get; } = new();

    public StreamSubscriptionBuilder UseStreamTypeProvider(IStreamTypeProvider typeProvider) =>
        UseStreamTypeProvider(() => typeProvider);

    public StreamSubscriptionBuilder UseStreamTypeProvider(Func<IStreamTypeProvider> factory)
    {
        _streamTypeProviderFactory = factory;
        return this;
    }

    /// <summary>
    /// Gets the subscription identity associated with this builder instance.
    /// </summary>
    public SubscriptionId SubscriptionId => subscriptionId;

    /// <summary>
    /// Overrides how component names are composed from the registration-time prefix
    /// (e.g. <c>"StreamonCheckpoint"</c>) and the provisioning-time suffix
    /// (e.g. <c>"Contoso"</c>). Default: <c>(p, s) =&gt; p + s</c>, yielding
    /// <c>"StreamonCheckpointContoso"</c>.
    /// </summary>
    /// <remarks>
    /// Applied uniformly to the stream-reader, checkpoint-store, and event-inbox table names.
    /// When the suffix is empty (no name passed to the provisioner), the convention should
    /// return the prefix unchanged so existing registrations remain backward compatible.
    /// </remarks>
    public StreamSubscriptionBuilder UseNamingConvention(Func<string, string, string> convention)
    {
        _nameConvention = convention ?? throw new ArgumentNullException(nameof(convention));
        return this;
    }

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

    public StreamSubscriptionBuilder UseEventInbox(IEventInbox inbox)
    {
        _eventInboxFactory = _ => inbox;
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
        where TProjector : class
    {
        _handlerFactories.Add(() =>
        {
            var projector = ResolveService(typeof(TProjector));
            var store = storeFactory();
            var handler = new ProjectionEventHandler<TState>(subscriptionId, store);
            handler.RegisterProjectorsFrom(projector);
            return handler;
        });
        return this;
    }

    /// <summary>
    /// Registers a projection where both the projector and <see cref="IProjectionStore{TState}"/> are
    /// resolved from the service container at build time. Requires DI integration
    /// (<see cref="ServiceCollectionExtensions.AddStreamSubscription"/>).
    /// </summary>
    /// <typeparam name="TProjector">The projector type implementing
    /// <see cref="IEventInitialProjector{TEvent, TState}"/> and/or
    /// <see cref="IEventProjector{TEvent, TState}"/>.</typeparam>
    /// <typeparam name="TState">The projection state type.</typeparam>
    public StreamSubscriptionBuilder AddProjection<TProjector, TState>()
        where TProjector : class
    {
        _handlerFactories.Add(() =>
        {
            var projector = ResolveService(typeof(TProjector));
            var store = (IProjectionStore<TState>)ResolveService(typeof(IProjectionStore<TState>));
            var handler = new ProjectionEventHandler<TState>(subscriptionId, store);
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
    public StreamSubscription Build(string suffix = "")
    {
        var checkpointStore = _checkpointStoreFactory?.Invoke(suffix)
            ?? throw new InvalidOperationException("No checkpoint store configured. Call UseCheckpointStore before building.");
        var subscriptionStreamReader = _subscriptionStreamReaderFactory?.Invoke(suffix)
            ?? throw new InvalidOperationException("No subscription stream reader configured. Call UseSubscriptionStreamReader before building.");

        List<IEventHandler> handlers = ResolveHandlers();

        var pipeline = EventPipelineBuilder.Build(async (@event, cancellationToken) =>
        {
            if (eventDispatchType == EventDispatchType.Concurrent)
            {
                var tasks = handlers.Select(handler => handler.HandleAsync(@event, cancellationToken));
                await Task.WhenAll(tasks).ConfigureAwait(false);
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

    /// <summary>Composes a component name from its registration-time prefix and a provisioner-supplied suffix.</summary>
    public string ComposeName(string prefix, string suffix) => _nameConvention(prefix, suffix);

    private object ResolveService(Type type) =>
        _serviceResolver?.Invoke(type)
            ?? throw new InvalidOperationException(
                $"Cannot resolve service of type '{type.Name}'. Register the handler with a factory delegate " +
                $"or use the DI integration (AddStreamSubscription) which sets up a service resolver automatically.");
}
