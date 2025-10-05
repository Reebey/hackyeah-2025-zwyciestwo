using front.Services;
using Microsoft.Extensions.Logging;

namespace front
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

            builder.Services.AddHttpClient("NotificationsClient", client =>
            {
                client.BaseAddress = new Uri("https://api.example.com/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            builder.Services.AddSingleton<PowiadomieniaSerwis>();
            builder.Services.AddSingleton<ZgloszenieSerwis>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
