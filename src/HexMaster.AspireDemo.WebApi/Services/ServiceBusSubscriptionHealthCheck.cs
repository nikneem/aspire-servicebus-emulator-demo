using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HexMaster.AspireDemo.WebApi.Services;

public class ServiceBusSubscriptionHealthCheck : IHealthCheck
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<ServiceBusSubscriptionHealthCheck> _logger;

    public ServiceBusSubscriptionHealthCheck(
        ServiceBusClient serviceBusClient, 
        ILogger<ServiceBusSubscriptionHealthCheck> logger)
    {
        _serviceBusClient = serviceBusClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a receiver that will be disposed immediately to check the connection
            var receiver = _serviceBusClient.CreateReceiver("message");
            await receiver.DisposeAsync();
            
            return HealthCheckResult.Healthy("ServiceBus subscription is healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ServiceBus subscription health check failed");
            return HealthCheckResult.Unhealthy("ServiceBus subscription is unhealthy", ex);
        }
    }
}