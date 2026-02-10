using Microsoft.Extensions.DependencyInjection;

namespace EventBusClient;

public static class DependencyInjection
{
    public static IServiceCollection AddEventBusClient(this IServiceCollection services)
    {
        return services;
    }
}
