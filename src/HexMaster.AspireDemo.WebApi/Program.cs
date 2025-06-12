using HexMaster.AspireDemo.WebApi.Controllers;
using HexMaster.AspireDemo.WebApi.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add Azure ServiceBus client
builder.AddAzureServiceBusClient(connectionName: "messaging");

// Register our message subscriber service
builder.Services.AddHostedService<MessageSubscriberService>();

// Configure OpenTelemetry to add our activity sources
builder.Services.AddApplicationTracing(tracerProviderBuilder =>
{
    tracerProviderBuilder
        .AddSource(nameof(PublisherController))
        .AddSource(nameof(MessageSubscriberService));
});

// Add health check for the message subscriber service
builder.Services.AddHealthChecks()
    .AddCheck<ServiceBusSubscriptionHealthCheck>(
        "servicebus-subscription",
        HealthStatus.Degraded,
        tags: new[] { "ready" });

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("scalar");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
