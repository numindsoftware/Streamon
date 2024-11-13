using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamon.TableStorage;

internal class Partition
{
    public const string HeadRowKey = "SO-HD-";
    public const string EventRowKey = "SO-ET-";
    public const string EventIdRowKey = "SO-ID-";
    // FUTURE: public const string EventStateProjection = "SI0-SP-";

    private readonly TableClient _table;
    private readonly EventStoreOptions _options;
    private readonly string _partitionKey;

    public Partition(TableClient table, EventStoreOptions options, string partitionKey)
    {
        _table = table;
        _options = options;
        _partitionKey = partitionKey;
    }

    //public Task WriteEvents(IEnumerable<Event> events)
    //{


    //}


    //private TableTransactionAction EventTransactionAction(Event @event)
    //{
    //    var entity = new TableEntity(_partitionKey, $"{EventIdRowKey}{@event.Sequence:d10}");

    //    var sequenceField = $"{_options.EntityFieldPrefix}{nameof(@event.Sequence)}";
    //    entity.TryAdd(sequenceField, @event.Sequence);
    //    var typeField = $"{_options.EntityFieldPrefix}{nameof(@event.Type)}";
    //    entity.TryAdd(typeField, @event.Type);

    //    // add metadata
    //    foreach (var pair in @event.Metadata)
    //        entity.TryAdd($"{_options.MetadataFieldPrefix}{pair.Key}", pair.Value);

    //    // add payload

    //}

    //public IEnumerable<TableTransactionAction> TransactionActions { get; set; }


}
