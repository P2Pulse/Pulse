using Microsoft.AspNetCore.Components.WebView.Maui;
using MudBlazor.Services;
using Pulse.Client.Data;
using Pulse.Core;
using Pulse.Core.Calls;

namespace Pulse.Client
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif

            builder.Services.AddSingleton<WeatherForecastService>();  // TODO remove this.
            builder.Services.AddMudServices();
            builder.Services.AddPulse();

            return builder.Build();
        }
    }
}