using Azure.Data.Tables;
using Streamon.Subscription;
using Streamon.Tests.Fixtures;

namespace Streamon.Azure.TableStorage.Tests;

[TestCaseOrderer("Streamon.Tests.Fixtures.PriorityTestCollectionOrderer", "Streamon.Tests.Fixtures")]
public class ProjectionTests(ProjectionFixture fixture) : IClassFixture<ProjectionFixture>
{
    [Fact, Priority(1)]
    public async Task AppendsEventsForProjection()
    {
        var store = await fixture.StreamStoreProvisioner.CreateStoreAsync(ProjectionFixture.StreamTableName);

        await store.AppendEventsAsync(
            new StreamId("order-1"),
            StreamPosition.Start,
            [new OrderCaptured("1", "Computer", 1000), new OrderShipped("1", "T1234567890")]);

        await store.AppendEventsAsync(
            new StreamId("order-2"),
            StreamPosition.Start,
            [new OrderCaptured("2", "Monitor", 500), new OrderCancelled("2")]);

        var stream1 = await store.FetchEventsAsync(new StreamId("order-1"));
        var stream2 = await store.FetchEventsAsync(new StreamId("order-2"));
        Assert.Equal(2, stream1.Count());
        Assert.Equal(2, stream2.Count());
    }

    [Fact, Priority(2)]
    public async Task ProjectsInitialStateFromOrderCaptured()
    {
        var subscription = fixture.SubscriptionManager.Get(SubscriptionId.From("projection-sub"));
        await subscription.PollAsync();

        var entity = await fixture.ProjectionTableClient.GetEntityAsync<OrderProjectionEntity>("1", "1");
        Assert.NotNull(entity.Value);
        Assert.Equal("Computer", entity.Value.Product);
        Assert.Equal(1000, entity.Value.Price);
    }

    [Fact, Priority(3)]
    public async Task ProjectsTrackingFromOrderShipped()
    {
        var entity = await fixture.ProjectionTableClient.GetEntityAsync<OrderProjectionEntity>("1", "1");
        Assert.Equal("T1234567890", entity.Value.Tracking);
        Assert.False(entity.Value.IsCancelled);
    }

    [Fact, Priority(4)]
    public async Task ProjectsCancellationFromOrderCancelled()
    {
        var entity = await fixture.ProjectionTableClient.GetEntityAsync<OrderProjectionEntity>("2", "2");
        Assert.NotNull(entity.Value);
        Assert.Equal("Monitor", entity.Value.Product);
        Assert.Equal(500, entity.Value.Price);
        Assert.True(entity.Value.IsCancelled);
    }

    [Fact, Priority(5)]
    public async Task SkipsUpdateWhenNoInitialStateExists()
    {
        var store = await fixture.StreamStoreProvisioner.CreateStoreAsync(ProjectionFixture.StreamTableName);

        // Append only an update event (no OrderCaptured) for a new stream
        await store.AppendEventsAsync(
            new StreamId("order-3"),
            StreamPosition.Start,
            [new OrderShipped("3", "T9999999999")]);

        var subscription = fixture.SubscriptionManager.Get(SubscriptionId.From("projection-sub"));
        await subscription.PollAsync();

        var response = await fixture.ProjectionTableClient.GetEntityIfExistsAsync<OrderProjectionEntity>("3", "3");
        Assert.False(response.HasValue);
    }

    [Fact, Priority(6)]
    public async Task ProjectsMultipleStreamsIndependently()
    {
        var entity1 = await fixture.ProjectionTableClient.GetEntityAsync<OrderProjectionEntity>("1", "1");
        var entity2 = await fixture.ProjectionTableClient.GetEntityAsync<OrderProjectionEntity>("2", "2");

        Assert.Equal("Computer", entity1.Value.Product);
        Assert.Equal("T1234567890", entity1.Value.Tracking);
        Assert.False(entity1.Value.IsCancelled);

        Assert.Equal("Monitor", entity2.Value.Product);
        Assert.Null(entity2.Value.Tracking);
        Assert.True(entity2.Value.IsCancelled);
    }
}