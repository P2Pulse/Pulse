using Microsoft.Extensions.DependencyInjection;
using Pulse.Core.Calls;

namespace Pulse.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPulse(this IServiceCollection services)
    {
        return services
            .AddSingleton<ICaller, FakeCaller>()
            .AddTransient<ICallInitiator, UdpCallInitiator>()
            .AddTransient<ICallAcceptor, UdpCallAcceptor>()
            .AddTransient<UdpStreamFactory>()
            .AddTransient<IncomingCallPoller>();
    }
}