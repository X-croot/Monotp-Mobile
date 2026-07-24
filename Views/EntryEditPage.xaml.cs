using Monotp.Models;
using Monotp.Services;

namespace Monotp.Views;

public partial class EntryEditPage : ContentPage
{
    readonly VaultService _vault;
    readonly int? _index;
    readonly Action _onDone;

    static readonly TotpAlgorithm[] Algos = { TotpAlgorithm.Sha1, TotpAlgorithm.Sha256, TotpAlgorithm.Sha512 };

    public EntryEditPage(VaultService vault, int? index, Action onDone)
    {
        InitializeComponent();
        _vault = vault;
        _index = index;
        _onDone = onDone;
        Title = index.HasValue ? "Edit account" : "Add account";

        DigitsPicker.Items.Add("6");
        DigitsPicker.Items.Add("7");
        DigitsPicker.Items.Add("8");
        foreach (var a in Algos) AlgoPicker.Items.Add(a.Label());

        if (index.HasValue)
        {
            SmartSection.IsVisible = false;
            var e = _vault.Vault.Entries[index.Value];
            IssuerEntry.Text = e.Issuer;
            AccountEntry.Text = e.Account;
            SecretEntry.Text = e.Secret;
            DigitsPicker.SelectedIndex = Math.Clamp((int)e.Digits - 6, 0, 2);
            PeriodEntry.Text = e.Period.ToString();
            AlgoPicker.SelectedIndex = Array.IndexOf(Algos, e.Algorithm);
            SaveBtn.Text = "Save changes";
        }
        else
        {
            DigitsPicker.SelectedIndex = 0;
            PeriodEntry.Text = "30";
            AlgoPicker.SelectedIndex = 0;
        }

        UpdatePreview();
    }

    void OnSmartChanged(object? sender, TextChangedEventArgs e)
    {
        var text = (SmartEntry.Text ?? string.Empty).Trim();
        if (text.StartsWith("otpauth://"))
        {
            var parsed = Totp.ParseOtpauth(text);
            if (parsed != null)
            {
                IssuerEntry.Text = parsed.Issuer;
                AccountEntry.Text = parsed.Account;
                SecretEntry.Text = parsed.Secret;
                DigitsPicker.SelectedIndex = Math.Clamp((int)parsed.Digits - 6, 0, 2);
                PeriodEntry.Text = parsed.Period.ToString();
                AlgoPicker.SelectedIndex = Array.IndexOf(Algos, parsed.Algorithm);
            }
        }
        else if (Totp.DecodeSecret(text) != null)
        {
            SecretEntry.Text = text;
        }
        UpdatePreview();
    }

    void OnSecretChanged(object? sender, TextChangedEventArgs e) => UpdatePreview();

    void UpdatePreview()
    {
        var secret = SecretEntry.Text ?? string.Empty;
        var key = Totp.DecodeSecret(secret);
        if (key != null)
        {
            var entry = BuildEntry();
            var code = Totp.Generate(key, entry.Period, entry.Digits, entry.Algorithm, Totp.NowUnix());
            PreviewLabel.Text = $"Preview: {Totp.SpacedCode(code)}";
            PreviewLabel.TextColor = (Color)(Application.Current!.Resources["TextColorPrimary"]);
        }
        else if (!string.IsNullOrEmpty(secret))
        {
            PreviewLabel.Text = "Secret is not valid base32";
            PreviewLabel.TextColor = (Color)(Application.Current!.Resources["DangerColor"]);
        }
        else
        {
            PreviewLabel.Text = string.Empty;
        }
    }

    Monotp.Models.Entry BuildEntry()
    {
        uint digits = (uint)(DigitsPicker.SelectedIndex + 6);
        if (DigitsPicker.SelectedIndex < 0) digits = 6;
        ulong.TryParse(PeriodEntry.Text, out var period);
        if (period < 15 || period > 90) period = period == 0 ? 30 : Math.Clamp(period, 15UL, 90UL);
        var algo = AlgoPicker.SelectedIndex >= 0 ? Algos[AlgoPicker.SelectedIndex] : TotpAlgorithm.Sha1;
        return new Monotp.Models.Entry
        {
            Issuer = (IssuerEntry.Text ?? string.Empty).Trim(),
            Account = (AccountEntry.Text ?? string.Empty).Trim(),
            Secret = (SecretEntry.Text ?? string.Empty).Trim(),
            Digits = digits,
            Period = period,
            Algorithm = algo
        };
    }

    async void OnSave(object? sender, EventArgs e)
    {
        var entry = BuildEntry();
        if (Totp.DecodeSecret(entry.Secret) == null)
        {
            ShowStatus("Invalid base32 secret.");
            return;
        }
        if (string.IsNullOrEmpty(entry.Account) && string.IsNullOrEmpty(entry.Issuer))
        {
            ShowStatus("Enter an issuer or account name.");
            return;
        }

        if (_index.HasValue && _index.Value < _vault.Vault.Entries.Count)
            _vault.Vault.Entries[_index.Value] = entry;
        else
            _vault.Vault.Entries.Add(entry);

        _vault.PersistVault();
        _onDone();
        await Navigation.PopModalAsync();
    }

    async void OnCancel(object? sender, EventArgs e) => await Navigation.PopModalAsync();

    void ShowStatus(string msg)
    {
        StatusLabel.Text = msg;
        StatusLabel.IsVisible = true;
    }
}
