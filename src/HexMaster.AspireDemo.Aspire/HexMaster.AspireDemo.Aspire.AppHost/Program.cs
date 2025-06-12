var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("messaging");
serviceBus.AddServiceBusQueue("message");
serviceBus.RunAsEmulator(em =>
{
    em.WithLifetime(ContainerLifetime.Persistent);
});
builder.AddProject<Projects.HexMaster_AspireDemo_WebApi>("hexmaster-aspiredemo-webapi")
    .WithReference(serviceBus);

builder.Build().Run();
