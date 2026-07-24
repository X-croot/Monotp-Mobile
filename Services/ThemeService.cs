using Monotp.Models;

namespace Monotp.Services;

public static class ThemeService
{
    public static void Apply(ThemeKind kind)
    {
        var app = Application.Current;
        if (app == null) return;

        var effective = kind;
        if (kind == ThemeKind.System)
            effective = app.RequestedTheme == AppTheme.Dark ? ThemeKind.Dark : ThemeKind.Light;

        Color bg, panel, text, weak, accent, danger = Color.FromRgb(210, 90, 90);
        bool dark;

        switch (effective)
        {
            case ThemeKind.Light:
                bg = Color.FromRgb(250, 250, 250);
                panel = Color.FromRgb(255, 255, 255);
                text = Color.FromRgb(20, 20, 24);
                weak = Color.FromRgb(110, 110, 118);
                accent = Color.FromRgb(70, 70, 80);
                dark = false;
                break;
            case ThemeKind.Sakura:
                bg = Color.FromRgb(255, 244, 248);
                panel = Color.FromRgb(255, 236, 243);
                text = Color.FromRgb(90, 45, 62);
                weak = Color.FromRgb(160, 110, 128);
                accent = Color.FromRgb(233, 143, 178);
                dark = false;
                break;
            case ThemeKind.Monochrome:
                bg = Color.FromRgb(0, 0, 0);
                panel = Color.FromRgb(18, 18, 18);
                text = Color.FromRgb(255, 255, 255);
                weak = Color.FromRgb(180, 180, 180);
                accent = Color.FromRgb(255, 255, 255);
                dark = true;
                break;
            default:
                bg = Color.FromRgb(15, 15, 17);
                panel = Color.FromRgb(28, 28, 33);
                text = Color.FromRgb(235, 235, 238);
                weak = Color.FromRgb(150, 150, 158);
                accent = Color.FromRgb(120, 120, 140);
                dark = true;
                break;
        }

        var res = app.Resources;
        res["BgColor"] = bg;
        res["PanelColor"] = panel;
        res["TextColorPrimary"] = text;
        res["TextColorWeak"] = weak;
        res["AccentColor"] = accent;
        res["DangerColor"] = danger;
        res["OnAccentColor"] = dark && effective != ThemeKind.Monochrome ? Colors.White : (effective == ThemeKind.Monochrome ? Colors.Black : Colors.White);

        app.UserAppTheme = dark ? AppTheme.Dark : AppTheme.Light;
    }
}
