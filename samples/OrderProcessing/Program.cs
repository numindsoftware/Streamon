using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Streamon;
using Streamon.Azure.TableStorage;
using Streamon.Subscription;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services
    .AddStreamon()
    .AddTableStorageStreamStore("UseDevelopmentStorage=true", options =>
    {
        //options.TableName = "OrderProcessing";
        //options.PartitionKey = "OrderProcessing";
    });

builder.Services.AddStreamSubscription(SubscriptionId.From("OrderProcessing"), StreamSubscriptionType.Live)
    .AddEventHandler<object>();

builder.Services.AddStreamSubscription(SubscriptionId.From("OrderProjection"), StreamSubscriptionType.CatchUp)
    .AddEventHandler<object>();

builder.Build().Run();
