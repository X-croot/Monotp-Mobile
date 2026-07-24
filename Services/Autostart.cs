namespace Monotp.Services;

public static class Autostart
{
    public static void SetAutostart(bool enable)
    {
#if WINDOWS
        try
        {
            using var run = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run");
            if (run == null) return;
            if (enable)
            {
                var exe = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                run.SetValue("monotp", exe);
            }
            else
            {
                run.DeleteValue("monotp", false);
            }
        }
        catch { }
#else
        _ = enable;
#endif
    }
}
