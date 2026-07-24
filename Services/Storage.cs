using System.Text.Json;
using Monotp.Models;

namespace Monotp.Services;

public class Storage
{
    readonly string _dir;

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };

    public Storage()
    {
        _dir = Path.Combine(FileSystem.AppDataDirectory, "monotp");
        Directory.CreateDirectory(_dir);
    }

    string ConfigFile => Path.Combine(_dir, "config.json");
    string VaultFile => Path.Combine(_dir, "vault.enc");

    public bool VaultExists => File.Exists(VaultFile);

    public AppConfig LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigFile))
            {
                var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigFile), JsonOpts);
                if (cfg != null) return cfg;
            }
        }
        catch { }
        return new AppConfig();
    }

    public void SaveConfig(AppConfig cfg)
        => File.WriteAllText(ConfigFile, JsonSerializer.Serialize(cfg, JsonOpts));

    public void DeleteVault()
    {
        if (File.Exists(VaultFile)) File.Delete(VaultFile);
    }

    public byte[] SaltFromConfig(AppConfig cfg) => Convert.FromBase64String(cfg.SaltB64);

    public void SaveVault(byte[] key, Vault vault)
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(vault, JsonOpts);
        var encrypted = Crypto.Encrypt(key, plaintext);
        Array.Clear(plaintext);
        File.WriteAllBytes(VaultFile, encrypted);
    }

    public Vault LoadVault(byte[] key)
    {
        var data = File.ReadAllBytes(VaultFile);
        var plaintext = Crypto.Decrypt(key, data);
        var vault = JsonSerializer.Deserialize<Vault>(plaintext, JsonOpts) ?? new Vault();
        Array.Clear(plaintext);
        return vault;
    }
}
