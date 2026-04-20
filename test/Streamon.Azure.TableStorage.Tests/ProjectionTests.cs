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
            [new OrderCaptured("1", new OrderAddress("123 Main St", "Cityville", "12345"), [new OrderItem("Computer", 1, 1000m)]), new OrderShipped("1", "T1234567890")]);

        await store.AppendEventsAsync(
            new StreamId("order-2"),
            StreamPosition.Start,
            [new OrderCaptured("2", new OrderAddress("456 Elm St", "Townsville", "67890"), [new OrderItem("Monitor", 1, 500m)]), new OrderCancelled("2")]);

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

        var entity = await fixture.ProjectionStore.ReadAsync(new OrderProjectionEntity { OrderId = "1" });
        Assert.NotNull(entity);
        Assert.NotNull(entity.Items);
        Assert.Single(entity.Items);
        Assert.Equal("Computer", entity.Items[0].Name);
        Assert.Equal(1, entity.Items[0].Quantity);
        Assert.Equal(1000m, entity.Items[0].UnitPrice);
    }

    [Fact, Priority(3)]
    public async Task ProjectsTrackingFromOrderShipped()
    {
        var entity = await fixture.ProjectionStore.ReadAsync(new OrderProjectionEntity { OrderId = "1" });
        Assert.NotNull(entity);
        Assert.Equal("T1234567890", entity.Tracking);
        Assert.False(entity.IsCancelled);
    }

    [Fact, Priority(4)]
    public async Task ProjectsCancellationFromOrderCancelled()
    {
        var entity = await fixture.ProjectionStore.ReadAsync(new OrderProjectionEntity { OrderId = "2" });
        Assert.NotNull(entity);
        Assert.NotNull(entity.Items);
        Assert.Single(entity.Items);
        Assert.Equal("Monitor", entity.Items[0].Name);
        Assert.Equal(500m, entity.Items[0].UnitPrice);
        Assert.True(entity.IsCancelled);
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

        var entity = await fixture.ProjectionStore.ReadAsync(new OrderProjectionEntity { OrderId = "3" });
        Assert.Null(entity);
    }

    [Fact, Priority(6)]
    public async Task ProjectsMultipleStreamsIndependently()
    {
        var entity1 = await fixture.ProjectionStore.ReadAsync(new OrderProjectionEntity { OrderId = "1" });
        var entity2 = await fixture.ProjectionStore.ReadAsync(new OrderProjectionEntity { OrderId = "2" });

        Assert.NotNull(entity1);
        Assert.NotNull(entity2);

        Assert.Equal("Computer", entity1.Items![0].Name);
        Assert.Equal("T1234567890", entity1.Tracking);
        Assert.False(entity1.IsCancelled);

        Assert.Equal("Monitor", entity2.Items![0].Name);
        Assert.Null(entity2.Tracking);
        Assert.True(entity2.IsCancelled);
    }

    [Fact, Priority(7)]
    public async Task ProjectsItemsAddedToOrder()
    {
        var store = await fixture.StreamStoreProvisioner.CreateStoreAsync(ProjectionFixture.StreamTableName);

        await store.AppendEventsAsync(
            new StreamId("order-4"),
            StreamPosition.Start,
            [
                new OrderCaptured("4", new OrderAddress("789 Oak St", "Villageville", "11223"), [new OrderItem("Bundle", 1, 0m)]),
                new OrderItemsAdded("4", [new OrderItem("Keyboard", 2, 49.99m), new OrderItem("Mouse", 1, 29.99m)])
            ]);

        var subscription = fixture.SubscriptionManager.Get(SubscriptionId.From("projection-sub"));
        await subscription.PollAsync();

        var entity = await fixture.ProjectionStore.ReadAsync(new OrderProjectionEntity { OrderId = "4" });
        Assert.NotNull(entity);
        Assert.NotNull(entity.Items);
        // Initial "Bundle" item from OrderCaptured + 2 added items
        Assert.Equal(3, entity.Items.Count);
        Assert.Contains(entity.Items, i => i.Name == "Bundle" && i.Quantity == 1);
        Assert.Contains(entity.Items, i => i.Name == "Keyboard" && i.Quantity == 2);
        Assert.Contains(entity.Items, i => i.Name == "Mouse" && i.Quantity == 1);
    }

    [Fact, Priority(8)]
    public async Task ProjectsAdditionalItemsAppendedToExistingList()
    {
        var store = await fixture.StreamStoreProvisioner.CreateStoreAsync(ProjectionFixture.StreamTableName);

        await store.AppendEventsAsync(
            new StreamId("order-4"),
            StreamPosition.Any,
            [new OrderItemsAdded("4", [new OrderItem("Monitor Stand", 1, 79.99m)])]);

        var subscription = fixture.SubscriptionManager.Get(SubscriptionId.From("projection-sub"));
        await subscription.PollAsync();

        var entity = await fixture.ProjectionStore.ReadAsync(new OrderProjectionEntity { OrderId = "4" });
        Assert.NotNull(entity);
        Assert.NotNull(entity.Items);
        Assert.Equal(4, entity.Items.Count);
        Assert.Contains(entity.Items, i => i.Name == "Bundle");
        Assert.Contains(entity.Items, i => i.Name == "Keyboard");
        Assert.Contains(entity.Items, i => i.Name == "Mouse");
        Assert.Contains(entity.Items, i => i.Name == "Monitor Stand");
    }

    [Fact, Priority(9)]
    public async Task ProjectsItemsCancelledFromOrder()
    {
        var store = await fixture.StreamStoreProvisioner.CreateStoreAsync(ProjectionFixture.StreamTableName);

        await store.AppendEventsAsync(
            new StreamId("order-4"),
            StreamPosition.Any,
            [new OrderItemsCancelled("4", ["Mouse", "Bundle"])]);

        var subscription = fixture.SubscriptionManager.Get(SubscriptionId.From("projection-sub"));
        await subscription.PollAsync();

        var entity = await fixture.ProjectionStore.ReadAsync(new OrderProjectionEntity { OrderId = "4" });
        Assert.NotNull(entity);
        Assert.NotNull(entity.Items);
        Assert.Equal(2, entity.Items.Count);
        Assert.DoesNotContain(entity.Items, i => i.Name == "Mouse");
        Assert.DoesNotContain(entity.Items, i => i.Name == "Bundle");
        Assert.Contains(entity.Items, i => i.Name == "Keyboard");
        Assert.Contains(entity.Items, i => i.Name == "Monitor Stand");
    }

    [Fact, Priority(10)]
    public async Task ProjectsItemsReplacedOnOrder()
    {
        var store = await fixture.StreamStoreProvisioner.CreateStoreAsync(ProjectionFixture.StreamTableName);

        await store.AppendEventsAsync(
            new StreamId("order-4"),
            StreamPosition.Any,
            [new OrderItemsReplaced("4", [new OrderItem("Laptop", 1, 1299.99m)])]);

        var subscription = fixture.SubscriptionManager.Get(SubscriptionId.From("projection-sub"));
        await subscription.PollAsync();

        var entity = await fixture.ProjectionStore.ReadAsync(new OrderProjectionEntity { OrderId = "4" });
        Assert.NotNull(entity);
        Assert.NotNull(entity.Items);
        Assert.Single(entity.Items);
        Assert.Equal("Laptop", entity.Items[0].Name);
        Assert.Equal(1, entity.Items[0].Quantity);
        Assert.Equal(1299.99m, entity.Items[0].UnitPrice);
    }

    [Fact, Priority(11)]
    public async Task ProjectsItemsCancelledWhenListIsEmpty()
    {
        var store = await fixture.StreamStoreProvisioner.CreateStoreAsync(ProjectionFixture.StreamTableName);

        await store.AppendEventsAsync(
            new StreamId("order-4"),
            StreamPosition.Any,
            [new OrderItemsCancelled("4", ["Laptop"])]);

        var subscription = fixture.SubscriptionManager.Get(SubscriptionId.From("projection-sub"));
        await subscription.PollAsync();

        var entity = await fixture.ProjectionStore.ReadAsync(new OrderProjectionEntity { OrderId = "4" });
        Assert.NotNull(entity);
        Assert.NotNull(entity.Items);
        Assert.Empty(entity.Items);
    }

    [Fact, Priority(12)]
    public async Task ProjectsAddressChangedOnOrder()
    {
        var store = await fixture.StreamStoreProvisioner.CreateStoreAsync(ProjectionFixture.StreamTableName);

        await store.AppendEventsAsync(
            new StreamId("order-1"),
            StreamPosition.Any,
            [new OrderAddressChanged("1", new OrderAddress("123 Main St", "Springfield", "62704"))]);

        var subscription = fixture.SubscriptionManager.Get(SubscriptionId.From("projection-sub"));
        await subscription.PollAsync();

        var entity = await fixture.ProjectionStore.ReadAsync(new OrderProjectionEntity { OrderId = "1" });
        Assert.NotNull(entity);
        Assert.NotNull(entity.ShippingAddress);
        Assert.Equal("123 Main St", entity.ShippingAddress.Street);
        Assert.Equal("Springfield", entity.ShippingAddress.City);
        Assert.Equal("62704", entity.ShippingAddress.ZipCode);
    }

    [Fact, Priority(13)]
    public async Task ProjectsAddressChangedOverwritesPreviousAddress()
    {
        var store = await fixture.StreamStoreProvisioner.CreateStoreAsync(ProjectionFixture.StreamTableName);

        await store.AppendEventsAsync(
            new StreamId("order-1"),
            StreamPosition.Any,
            [new OrderAddressChanged("1", new OrderAddress("456 Oak Ave", "Shelbyville", "62565"))]);

        var subscription = fixture.SubscriptionManager.Get(SubscriptionId.From("projection-sub"));
        await subscription.PollAsync();

        var entity = await fixture.ProjectionStore.ReadAsync(new OrderProjectionEntity { OrderId = "1" });
        Assert.NotNull(entity);
        Assert.NotNull(entity.ShippingAddress);
        Assert.Equal("456 Oak Ave", entity.ShippingAddress.Street);
        Assert.Equal("Shelbyville", entity.ShippingAddress.City);
        Assert.Equal("62565", entity.ShippingAddress.ZipCode);
        // Verify items are untouched
        Assert.NotNull(entity.Items);
        Assert.Equal("Computer", entity.Items[0].Name);
    }
}