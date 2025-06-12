using OpenTelemetry.Trace;

namespace HexMaster.AspireDemo.WebApi.Services;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddApplicationTracing(
        this IServiceCollection services,
        Action<TracerProviderBuilder> configure)
    {
        services.AddOpenTelemetry().WithTracing(builder =>
        {
            configure(builder);
        });
        
        return services;
    }
}