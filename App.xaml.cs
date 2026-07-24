using Monotp.Services;
using Monotp.Views;

namespace Monotp;

public partial class App : Application
{
    public static VaultService Vault { get; private set; } = null!;

    readonly VaultService _vault;

    public App(VaultService vault)
    {
        InitializeComponent();
        Vault = vault;
        _vault = vault;
        ThemeService.Apply(vault.Config.Theme);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Page start = _vault.VaultExists && _vault.Config.Initialized
            ? new UnlockPage(_vault)
            : new SetupPage(_vault);
        return new Window(new NavigationPage(start));
    }

    public static void GoTo(Page page)
    {
        var app = Current;
        if (app is null) return;
        if (app.Windows.Count > 0)
            app.Windows[0].Page = new NavigationPage(page);
    }
}
