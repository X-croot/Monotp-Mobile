using Monotp.Services;

namespace Monotp.Views;

public partial class UnlockPage : ContentPage
{
    readonly VaultService _vault;

    public UnlockPage(VaultService vault)
    {
        InitializeComponent();
        _vault = vault;
    }

    void OnUnlock(object? sender, EventArgs e)
    {
        var pw = PwEntry.Text ?? string.Empty;
        if (_vault.Unlock(pw, out var error))
        {
            PwEntry.Text = string.Empty;
            App.GoTo(new VaultPage(_vault));
        }
        else
        {
            StatusLabel.Text = error;
            StatusLabel.IsVisible = true;
            PwEntry.Text = string.Empty;
        }
    }

    async void OnForgot(object? sender, EventArgs e)
    {
        bool proceed = await DisplayAlertAsync(
            "Forgot password",
            "Warning: this permanently erases ALL stored accounts. There is no recovery.",
            "Continue", "Cancel");
        if (!proceed) return;

        var input = await DisplayPromptAsync(
            "Confirm wipe",
            "Type DELETE to erase everything and start over.",
            "Erase & start over", "Cancel", "Type DELETE");

        if (input?.Trim() == "DELETE")
        {
            _vault.ForgotWipe();
            App.GoTo(new SetupPage(_vault));
        }
    }
}
