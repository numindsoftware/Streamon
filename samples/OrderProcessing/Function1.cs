using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Streamon;

namespace OrderProcessing;

public class Function1(
        IStreamStoreProvisioner streamStoreProvisioner,
        ILogger<Function1> logger)
{

    [Function("Function1")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }

    [Function("Function2")]
    public async Task<IActionResult> CreateOrder([HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] CreateOrderRequest createOrderRequest)
    {
        var streamStore = await streamStoreProvisioner.CreateStoreAsync();

        var orderPlaced = OrderPlaced.From(createOrderRequest, Ulid.NewUlid().ToString());

        await streamStore.AppendEventsAsync(
            streamId: StreamId.From($"orders-{orderPlaced.OrderId}"),
            expectedPosition: StreamPosition.Start,
            events: [orderPlaced]);

        logger.LogInformation("New order created with id: {orderId}", orderPlaced.OrderId);

        return new CreatedResult();
    }
}

public record OrderItem(
    string Sku, 
    int Quantity);

public record CreateOrderRequest(
    string CustomerOrderNumber, 
    string CustomerName, 
    string ShippingAddress,
    OrderItem[] Items);

#region Events
public record OrderPlaced(
    string OrderId, 
    string CustomerOrderNumber, 
    string CustomerName, 
    string ShippingAddress,
    OrderItem[] Items)
{
    public static OrderPlaced From(CreateOrderRequest request, string orderId)
    {
        return new OrderPlaced(
            orderId,
            request.CustomerOrderNumber,
            request.CustomerName,
            request.ShippingAddress,
            [.. request.Items.Select(i => new OrderItem(i.Sku, i.Quantity))]);
    }
}

public record OrderAccepted(string OrderId);

public record OrderShipped(string OrderId);

public record OrderCompleted(string OrderId);

public record OrderCancelled(string OrderId, string Reason = "");
#endregion Events
