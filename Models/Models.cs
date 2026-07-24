using System.Text.Json.Serialization;

namespace Monotp.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TotpAlgorithm
{
    Sha1,
    Sha256,
    Sha512
}

public static class TotpAlgorithmExtensions
{
    public static string Label(this TotpAlgorithm a) => a switch
    {
        TotpAlgorithm.Sha1 => "SHA1",
        TotpAlgorithm.Sha256 => "SHA256",
        TotpAlgorithm.Sha512 => "SHA512",
        _ => "SHA1"
    };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ThemeKind
{
    System,
    Dark,
    Light,
    Sakura,
    Monochrome
}

public static class ThemeKindExtensions
{
    public static readonly ThemeKind[] All =
    {
        ThemeKind.System,
        ThemeKind.Dark,
        ThemeKind.Light,
        ThemeKind.Sakura,
        ThemeKind.Monochrome
    };

    public static string Label(this ThemeKind t) => t switch
    {
        ThemeKind.System => "System",
        ThemeKind.Dark => "Dark",
        ThemeKind.Light => "Light",
        ThemeKind.Sakura => "Sakura",
        ThemeKind.Monochrome => "Monochrome",
        _ => "System"
    };
}

public class KdfParams
{
    public uint MCost { get; set; } = 65536;
    public uint TCost { get; set; } = 3;
    public uint PCost { get; set; } = 1;
}

public class Entry
{
    public string Issuer { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public uint Digits { get; set; } = 6;
    public ulong Period { get; set; } = 30;
    public TotpAlgorithm Algorithm { get; set; } = TotpAlgorithm.Sha1;

    public Entry Clone() => new()
    {
        Issuer = Issuer,
        Account = Account,
        Secret = Secret,
        Digits = Digits,
        Period = Period,
        Algorithm = Algorithm
    };
}

public class Vault
{
    public List<Entry> Entries { get; set; } = new();
}

public class AppConfig
{
    public ThemeKind Theme { get; set; } = ThemeKind.System;
    public string SaltB64 { get; set; } = string.Empty;
    public bool Autostart { get; set; } = false;
    public bool Initialized { get; set; } = false;
    public KdfParams Kdf { get; set; } = new();
}
