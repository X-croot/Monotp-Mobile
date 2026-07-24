using Monotp.Services;

namespace Monotp.Views;

public partial class SetupPage : ContentPage
{
    readonly VaultService _vault;

    public SetupPage(VaultService vault)
    {
        InitializeComponent();
        _vault = vault;
    }

    void OnCreate(object? sender, EventArgs e)
    {
        var pw = PwEntry.Text ?? string.Empty;
        var confirm = PwConfirm.Text ?? string.Empty;

        if (pw.Length < 8)
        {
            ShowStatus("Password must be at least 8 characters.");
            return;
        }
        if (pw != confirm)
        {
            ShowStatus("Passwords do not match.");
            return;
        }

        if (!_vault.CreateVault(pw, out var error))
        {
            ShowStatus($"Could not create vault: {error}");
            return;
        }

        PwEntry.Text = string.Empty;
        PwConfirm.Text = string.Empty;
        App.GoTo(new VaultPage(_vault));
    }

    void ShowStatus(string msg)
    {
        StatusLabel.Text = msg;
        StatusLabel.IsVisible = true;
    }
}
