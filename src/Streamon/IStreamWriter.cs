﻿namespace Streamon;

public interface IStreamWriter
{
    Task<IEnumerable<Event>> AppendEventsAsync(
        StreamId streamId,
        StreamPosition expectedPosition, 
        IEnumerable<object> events,
        EventMetadata? metadata = default,
        CancellationToken cancellationToken = default);

    public event EventHandler<StreamEventArgs>? EventsAppended;
}
