![NuGet Version](https://img.shields.io/nuget/v/Streamon)
![GitHub License](https://img.shields.io/github/license/numindsoftware/Streamon)
[![.github/workflows/ci.yml](https://github.com/numindsoftware/Streamon/actions/workflows/ci.yml/badge.svg)](https://github.com/numindsoftware/Streamon/actions/workflows/ci.yml)
[![.github/workflows/cd.yml](https://github.com/numindsoftware/Streamon/actions/workflows/cd.yml/badge.svg)](https://github.com/numindsoftware/Streamon/actions/workflows/cd.yml)

# Streamon

Event streaming store platform for real-time data processing and analytics.

## Providers
* In Memory
* [Azure Table Storage](https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview)
* [Azure Cosmos DB](https://developer.azurecosmosdb.com/tools) (_in progress_, can be used in CosmosDb still by using the Table Api)

## Features

* POCO events, no base classes or inheritance required
* Event ids and Metadata detection by using both Attribute and Interface markers
* Customizable serialization and type resolution
* Flexible stream sorage naming and partitioning, e.g. allowing for multitenancy by using one stream per tenant
* Optimistic concurrency control
* Soft and hard deletion modes
* Global event positioning & tracking
* Subscriptions
* Snapshots and Checkpoints

## To Do's

* Telemetry
* Relational Stores (Entity Framework?)
* Claim Checks for large events
* Stream Projections
* Stream Sweeper (Archiving and Purging)

## Azure Table Storage Provider Details

The Azure Table Storage provider is a simple implementation of the `IStreamStore` interface.
It uses Azure Table Storage to store events in a single table.
The table is partitioned by the stream id and the row key is the event id.
The provider uses the `Ulid` library to generate unique identifiers for events.

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

Github actions are configured to publish the packages to GitHub Packages on every push to the `master` branch.

Local testing and development of Github actions can be done using the [`act`](https://github.com/nektos/act) tool. 

## License

[MIT License](LICENSE)

