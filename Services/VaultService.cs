using Monotp.Models;

namespace Monotp.Services;

public class VaultService
{
    public Storage Storage { get; }
    public AppConfig Config { get; set; }
    public Vault Vault { get; set; } = new();
    public byte[]? Key { get; set; }

    public VaultService()
    {
        Storage = new Storage();
        Config = Storage.LoadConfig();
    }

    public bool VaultExists => Storage.VaultExists;

    public void PersistVault()
    {
        if (Key != null) Storage.SaveVault(Key, Vault);
    }

    public void SaveConfig() => Storage.SaveConfig(Config);

    public void Lock()
    {
        if (Key != null) Array.Clear(Key);
        Key = null;
        Vault = new Vault();
    }

    public bool Unlock(string password, out string error)
    {
        error = string.Empty;
        try
        {
            var salt = Storage.SaltFromConfig(Config);
            var key = Crypto.DeriveKey(password, salt, Config.Kdf);
            var vault = Storage.LoadVault(key);
            Vault = vault;
            Key = key;
            return true;
        }
        catch
        {
            error = "Wrong master password.";
            return false;
        }
    }

    public bool CreateVault(string password, out string error)
    {
        error = string.Empty;
        var salt = Crypto.RandomSalt();
        var key = Crypto.DeriveKey(password, salt, Config.Kdf);
        Config.SaltB64 = Convert.ToBase64String(salt);
        Config.Initialized = true;
        Vault = new Vault();
        try
        {
            Storage.SaveVault(key, Vault);
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
        SaveConfig();
        Key = key;
        return true;
    }

    public bool ChangePassword(string newPassword, out string error)
    {
        error = string.Empty;
        var salt = Crypto.RandomSalt();
        var key = Crypto.DeriveKey(newPassword, salt, Config.Kdf);
        try
        {
            Storage.SaveVault(key, Vault);
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
        Config.SaltB64 = Convert.ToBase64String(salt);
        SaveConfig();
        if (Key != null) Array.Clear(Key);
        Key = key;
        return true;
    }

    public void ForgotWipe()
    {
        Storage.DeleteVault();
        Config.Initialized = false;
        Config.SaltB64 = string.Empty;
        SaveConfig();
        if (Key != null) Array.Clear(Key);
        Key = null;
        Vault = new Vault();
    }
}
