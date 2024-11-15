namespace Streamon.Azure.TableStorage.Tests;

[CollectionDefinition(nameof(ContainerCollection), DisableParallelization = true)]
public class ContainerCollection : ICollectionFixture<ContainerFixture> { }
