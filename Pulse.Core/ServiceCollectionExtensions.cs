using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Pulse.Core.Authentication;
using Pulse.Core.Calls;

namespace Pulse.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPulse(this IServiceCollection services)
    {
        const string serverHttpClient = "Pulse.Server";

        services.AddTransient<HttpUnauthorizedHandler>();
        services.AddHttpClient(serverHttpClient, (serviceProvider, client) =>
        {
            client.BaseAddress = new Uri("https://pulse.gurgaller.com");
            var accessToken = serviceProvider.GetRequiredService<IAccessTokenStorage>().AccessToken;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }).AddHttpMessageHandler<HttpUnauthorizedHandler>();
        services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(serverHttpClient));

        return services
            .AddTransient<ICallInitiator, UdpCallInitiator>()
            .AddTransient<ICallAcceptor, UdpCallAcceptor>()
            .AddTransient<UdpStreamFactory>()
            .AddTransient<AccountRegistrar>()
            .AddTransient<Authenticator>()
            .AddTransient<IncomingCallPoller>();
    }
}