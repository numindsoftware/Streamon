using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamon;

public static class EventStoreExtensions
{
    //public static Task<IEnumerable<Event>> ReadAsync(this IEventReader eventReader, StreamId streamId, CancellationToken cancellationToken = default) =>
    //    eventReader.ReadAsync(streamId, StreamSequence.Start, StreamSequence.End, cancellationToken);

    //public static Task<IEnumerable<Event>> ReadAsync(this IEventReader eventReader, StreamId streamId, StreamSequence startPosition, CancellationToken cancellationToken = default) =>
    //    eventReader.ReadAsync(streamId, startPosition, StreamSequence.End, cancellationToken);

    //public static Task<StreamSequence> WriteAsync(this IEventWriter eventWriter, StreamId streamId, Event @event, CancellationToken cancellationToken = default) =>
    //    eventWriter.WriteAsync(streamId, StreamSequence.Any, new[] { @event }, cancellationToken);

    //public static Task<StreamSequence> WriteAsync(this IEventWriter eventWriter, StreamId streamId, StreamSequence expectedSequence, Event @event, CancellationToken cancellationToken = default) =>
    //    eventWriter.WriteAsync(streamId, expectedSequence, new[] { @event }, cancellationToken);

    //public static Task<StreamSequence> WriteAsync(this IEventWriter eventWriter, StreamId streamId, IEnumerable<Event> events, CancellationToken cancellationToken = default) =>
    //    eventWriter.WriteAsync(streamId, StreamSequence.Any, events, cancellationToken);
}
