# Streamon

Event streaming store platform for real-time data processing and analytics.

## Providers
* In Memory
* [Azure Table Storage](https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview)
* [Azure Cosmos DB](https://developer.azurecosmosdb.com/tools) (_in progress_)

## Features

* POCO events, no base classes or inheritance required
* Event ids and Metadata detectiom by using both Attribute and Interface markers
* Customizable serialization and Type resolution
* Flexible Stream sorage naming and paritioning, e.g. allowing for multitenancy by using one stream per tenant
* Optimistic Concurrency Control.
* Soft and Hard Deletion.
* Global event positioning.

## To Do's

* Telemetry
* Snapshots and Checkpoints
* Subscriptions
* Relational Stores (Entity Framework?)
* Claim Checks for large events
* Stream Projections
* Stream Sweeper (Archiving and Purging)
 
## Thanks

For inspiration and ideas, thanks to:

[Streamstone](https://github.com/yevhen/Streamstone)
[Eveneum](https://github.com/Eveneum/Eveneum)
[Eventflow](https://geteventflow.net/)
and
[Eventuous](https://eventuous.dev/)

Icon:

[Pipe icons created by srip - Flaticon](https://www.flaticon.com/free-icons/pipe)

## License

MIT License

