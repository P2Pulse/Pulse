using System.Net.Http.Headers;
using MudBlazor.Services;
using Pulse.Client.Authentication;
using Pulse.Client.Calls;
using Pulse.Client.Data;
using Pulse.Core;

namespace Pulse.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        builder.Services.AddSingleton<WeatherForecastService>(); // TODO remove this.
        builder.Services.AddMudServices();
        builder.Services.AddPulse();
        builder.Services.AddSingleton<IAccessTokenProvider, HardCodedAccessTokenProvider>();

        const string serverHttpClient = "Pulse.Server";
        builder.Services.AddHttpClient(serverHttpClient, (services, client) =>
        {
            client.BaseAddress = new Uri("https://pulse.gurgaller.com");
            var accessToken = services.GetRequiredService<IAccessTokenProvider>().AccessToken;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        });
        builder.Services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(serverHttpClient));

        builder.Services.AddSingleton<CurrentCallAccessor>();

        builder.Services.AddSingleton<IncomingCallPoller>();

        var mauiApp = builder.Build();
        _ = mauiApp.Services.GetRequiredService<IncomingCallPoller>();
        return mauiApp;
    }
}