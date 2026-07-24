using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Monotp.Models;
using Monotp.Services;

namespace Monotp.Views;

public class EntryVm : INotifyPropertyChanged
{
    public Monotp.Models.Entry Model { get; }
    public int Index { get; set; }
    public RingDrawable Ring { get; } = new();

    byte[]? _key;
    bool _valid;
    string _code = "------";
    ulong _remaining;
    bool _revealed;
    bool _copied;

    public EntryVm(Monotp.Models.Entry model)
    {
        Model = model;
        _key = Totp.DecodeSecret(model.Secret);
        _valid = _key != null;
    }

    public string Head => string.IsNullOrEmpty(Model.Issuer) ? Model.Account : Model.Issuer;
    public string SubHead => Model.Account;
    public bool HasSubHead => !string.IsNullOrEmpty(Model.Issuer) && !string.IsNullOrEmpty(Model.Account);
    public string Secret => Model.Secret;
    public bool Invalid => !_valid;

    public string DisplayCode => Totp.SpacedCode(_code);
    public double Fraction => _remaining / (double)Math.Max(1UL, Model.Period);

    public Color BarColor => _remaining <= 5
        ? Color.FromRgb(210, 90, 90)
        : (Application.Current?.Resources["AccentColor"] as Color ?? Colors.Gray);

    public bool Revealed => _revealed;
    public string RevealLabel => _revealed ? "Hide" : "Reveal";
    public string CopyLabel => _copied ? "Copied!" : "Copy";
    public bool Valid => _valid;
    public string RawCode => _code;

    public void Tick(ulong now)
    {
        if (_valid && _key != null)
            _code = Totp.Generate(_key, Model.Period, Model.Digits, Model.Algorithm, now);
        else
            _code = "------";

        _remaining = Totp.SecondsRemaining(Model.Period, now);
        Ring.Fraction = Fraction;
        Ring.Remaining = (int)_remaining;
        Ring.RingColor = Application.Current?.Resources["TextColorPrimary"] as Color ?? Colors.White;

        Raise(nameof(DisplayCode));
        Raise(nameof(Fraction));
        Raise(nameof(BarColor));
    }

    public void ToggleReveal()
    {
        _revealed = !_revealed;
        Raise(nameof(Revealed));
        Raise(nameof(RevealLabel));
    }

    public void MarkCopied(bool value)
    {
        _copied = value;
        Raise(nameof(CopyLabel));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class VaultPage : ContentPage
{
    readonly VaultService _vault;
    readonly ObservableCollection<EntryVm> _all = new();
    public ObservableCollection<EntryVm> Items { get; } = new();

    IDispatcherTimer? _timer;
    string _search = string.Empty;

    public VaultPage(VaultService vault)
    {
        InitializeComponent();
        _vault = vault;
        BindingContext = this;

        foreach (var t in ThemeKindExtensions.All)
            ThemePicker.Items.Add(t.Label());
        ThemePicker.SelectedIndex = Array.IndexOf(ThemeKindExtensions.All, _vault.Config.Theme);

        Reload();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => TickAll();
        _timer.Start();
        TickAll();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
    }

    void Reload()
    {
        _all.Clear();
        for (int i = 0; i < _vault.Vault.Entries.Count; i++)
            _all.Add(new EntryVm(_vault.Vault.Entries[i]) { Index = i });
        CountLabel.Text = $"{_vault.Vault.Entries.Count} account(s)";
        ApplyFilter();
        TickAll();
    }

    void ApplyFilter()
    {
        Items.Clear();
        var needle = _search.ToLowerInvariant();
        foreach (var vm in _all)
        {
            if (needle.Length == 0
                || vm.Model.Issuer.ToLowerInvariant().Contains(needle)
                || vm.Model.Account.ToLowerInvariant().Contains(needle))
                Items.Add(vm);
        }
    }

    void TickAll()
    {
        var now = Totp.NowUnix();
        foreach (var vm in Items) vm.Tick(now);
    }

    void OnThemeChanged(object? sender, EventArgs e)
    {
        var idx = ThemePicker.SelectedIndex;
        if (idx < 0) return;
        var theme = ThemeKindExtensions.All[idx];
        _vault.Config.Theme = theme;
        ThemeService.Apply(theme);
        _vault.SaveConfig();
        TickAll();
    }

    void OnSearch(object? sender, TextChangedEventArgs e)
    {
        _search = e.NewTextValue ?? string.Empty;
        ApplyFilter();
        TickAll();
    }

    async void OnAdd(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new NavigationPage(new EntryEditPage(_vault, null, () =>
        {
            Reload();
        })));
    }

    async void OnEdit(object? sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is EntryVm vm)
        {
            await Navigation.PushModalAsync(new NavigationPage(new EntryEditPage(_vault, vm.Index, () =>
            {
                Reload();
            })));
        }
    }

    async void OnCopy(object? sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is EntryVm vm && vm.Valid)
        {
            await Clipboard.SetTextAsync(vm.RawCode);
            vm.MarkCopied(true);
            await Task.Delay(1600);
            vm.MarkCopied(false);
        }
    }

    void OnReveal(object? sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is EntryVm vm)
            vm.ToggleReveal();
    }

    async void OnDelete(object? sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is EntryVm vm)
        {
            bool ok = await DisplayAlertAsync("Delete account", $"Remove \"{vm.Head}\"?", "Delete", "Cancel");
            if (!ok) return;
            _vault.Vault.Entries.RemoveAt(vm.Index);
            _vault.PersistVault();
            Reload();
        }
    }

    async void OnMenu(object? sender, EventArgs e)
    {
        string autoLabel = _vault.Config.Autostart ? "Disable start on login" : "Start on login";
        var choice = await DisplayActionSheetAsync("Menu", "Cancel", null,
            "Change master password", autoLabel, "Lock vault");

        if (choice == "Change master password")
            await ChangePassword();
        else if (choice == autoLabel)
        {
            _vault.Config.Autostart = !_vault.Config.Autostart;
            Autostart.SetAutostart(_vault.Config.Autostart);
            _vault.SaveConfig();
        }
        else if (choice == "Lock vault")
        {
            _vault.Lock();
            App.GoTo(new UnlockPage(_vault));
        }
    }

    async Task ChangePassword()
    {
        var np = await DisplayPromptAsync("Change master password", "New password (min 8 chars)", "Next", "Cancel");
        if (np == null) return;
        if (np.Length < 8)
        {
            await DisplayAlertAsync("Error", "Password must be at least 8 characters.", "OK");
            return;
        }
        var confirm = await DisplayPromptAsync("Change master password", "Confirm new password", "Update", "Cancel");
        if (confirm == null) return;
        if (np != confirm)
        {
            await DisplayAlertAsync("Error", "Passwords do not match.", "OK");
            return;
        }
        if (_vault.ChangePassword(np, out var err))
            await DisplayAlertAsync("Done", "Master password updated.", "OK");
        else
            await DisplayAlertAsync("Error", err, "OK");
    }
}
