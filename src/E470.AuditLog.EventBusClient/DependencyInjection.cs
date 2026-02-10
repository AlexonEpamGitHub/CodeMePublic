using Microsoft.Extensions.DependencyInjection;

namespace E470.AuditLog.EventBusClient;

public static class DependencyInjection
{
    public static IServiceCollection AddEventBusClient(this IServiceCollection services)
    {
        return services;
    }
}
