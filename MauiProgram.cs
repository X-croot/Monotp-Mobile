using Monotp.Services;

namespace Monotp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddSingleton<VaultService>();

        return builder.Build();
    }
}
