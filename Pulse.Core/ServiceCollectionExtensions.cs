using Microsoft.Extensions.DependencyInjection;

namespace Pulse.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPulse(this IServiceCollection services)
    {
        services.AddSingleton<ICaller, FakeCaller>();
        return services;
    }
}