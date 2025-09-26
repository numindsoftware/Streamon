const int BlobPort = 11000;
const int QueuePort = 11001;
const int TablePort = 11002;

var builder = DistributedApplication.CreateBuilder(args);
var storage = builder.AddAzureStorage("streamon-sample-storage")
    .RunAsEmulator(container => container
    .WithBlobPort(BlobPort)
    .WithQueuePort(QueuePort)
    .WithTablePort(TablePort)
    .WithDataVolume("streamon-sample-storage"));
var blobs = storage.AddQueues("blobs");
var queues = storage.AddQueues("queues");
var tables = storage.AddTables("tables");

builder
    .AddProject<Projects.OrderProcessing>("streamon-sample-app", "OrderProcessing")
    .WithExternalHttpEndpoints()
    .WithReference(tables)
    .WithReference(queues)
    .WithReference(blobs)
    .WithEnvironment("AzureWebJobsStorage", () => $"AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:{BlobPort}/devstoreaccount1;QueueEndpoint=http://127.0.0.1:{QueuePort}/devstoreaccount1;TableEndpoint=http://127.0.0.1:{TablePort}/devstoreaccount1;");

builder.Build().Run();
