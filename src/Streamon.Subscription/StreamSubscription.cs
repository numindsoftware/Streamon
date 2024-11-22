﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamon.Subscription;

#pragma warning disable CS9113 // Parameter is unread.
internal class StreamSubscription(IServiceProvider serviceProvider)
#pragma warning restore CS9113 // Parameter is unread.
{
    private readonly ConcurrentDictionary<Type, Type> _registeredHandlers = [];

    public StreamSubscription RegisterHandler<TEvent, THandler>() where THandler : IEventHandler
    {
        _registeredHandlers.TryAdd(typeof(TEvent), typeof(THandler));
        return this;
    }

    public Task DispatchAsync(object @event)
    {
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));
        var eventType = @event.GetType();
        if (!_registeredHandlers.TryGetValue(eventType, out var handlerType))
        {
            throw new InvalidOperationException($"No handler registered for event type {eventType}");
        }
        //ActivatorUtilities.CreateInstance()

        //ValueTask handlerTask = (ValueTask)Activator.CreateInstance(handlerType)!.HandleEventAsync((IEventConsumeContext)@event);

        throw new NotImplementedException();
    }
}