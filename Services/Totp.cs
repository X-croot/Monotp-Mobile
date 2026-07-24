using System.Security.Cryptography;
using System.Text;
using Monotp.Models;

namespace Monotp.Services;

public static class Totp
{
    static readonly string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static byte[]? DecodeSecret(string secret)
    {
        var cleaned = new StringBuilder();
        foreach (var c in secret)
        {
            if (char.IsWhiteSpace(c)) continue;
            cleaned.Append(char.ToUpperInvariant(c));
        }
        if (cleaned.Length == 0) return null;

        int bits = 0, value = 0;
        var output = new List<byte>();
        foreach (var c in cleaned.ToString())
        {
            int idx = Base32Alphabet.IndexOf(c);
            if (idx < 0) return null;
            value = (value << 5) | idx;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                output.Add((byte)((value >> bits) & 0xFF));
            }
        }
        return output.ToArray();
    }

    static uint Hotp(byte[] key, ulong counter, uint digits, TotpAlgorithm algo)
    {
        var counterBytes = new byte[8];
        for (int i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xFF);
            counter >>= 8;
        }

        byte[] hash = algo switch
        {
            TotpAlgorithm.Sha256 => new HMACSHA256(key).ComputeHash(counterBytes),
            TotpAlgorithm.Sha512 => new HMACSHA512(key).ComputeHash(counterBytes),
            _ => new HMACSHA1(key).ComputeHash(counterBytes)
        };

        int offset = hash[^1] & 0x0f;
        uint binary = ((uint)(hash[offset] & 0x7f) << 24)
                      | ((uint)(hash[offset + 1] & 0xff) << 16)
                      | ((uint)(hash[offset + 2] & 0xff) << 8)
                      | ((uint)(hash[offset + 3] & 0xff));

        return binary % (uint)Math.Pow(10, digits);
    }

    public static ulong NowUnix() => (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static string Generate(byte[] key, ulong period, uint digits, TotpAlgorithm algo, ulong unixTime)
    {
        ulong counter = unixTime / Math.Max(1UL, period);
        uint code = Hotp(key, counter, digits, algo);
        return code.ToString().PadLeft((int)digits, '0');
    }

    public static ulong SecondsRemaining(ulong period, ulong unixTime)
    {
        ulong p = Math.Max(1UL, period);
        return p - (unixTime % p);
    }

    public static Monotp.Models.Entry? ParseOtpauth(string uri)
    {
        const string prefix = "otpauth://totp/";
        if (!uri.StartsWith(prefix)) return null;
        var rest = uri.Substring(prefix.Length);

        string labelPart, query;
        int q = rest.IndexOf('?');
        if (q >= 0)
        {
            labelPart = rest.Substring(0, q);
            query = rest.Substring(q + 1);
        }
        else
        {
            labelPart = rest;
            query = string.Empty;
        }

        var label = UrlDecode(labelPart);
        string issuer, account;
        int colon = label.IndexOf(':');
        if (colon >= 0)
        {
            issuer = label.Substring(0, colon).Trim();
            account = label.Substring(colon + 1).Trim();
        }
        else
        {
            issuer = string.Empty;
            account = label.Trim();
        }

        string secret = string.Empty;
        uint digits = 6;
        ulong period = 30;
        var algo = TotpAlgorithm.Sha1;

        foreach (var pair in query.Split('&'))
        {
            int eq = pair.IndexOf('=');
            if (eq < 0) continue;
            var k = pair.Substring(0, eq);
            var v = UrlDecode(pair.Substring(eq + 1));
            switch (k)
            {
                case "secret": secret = v; break;
                case "issuer": if (string.IsNullOrEmpty(issuer)) issuer = v; break;
                case "digits": if (uint.TryParse(v, out var d)) digits = d; break;
                case "period": if (ulong.TryParse(v, out var p)) period = p; break;
                case "algorithm":
                    algo = v.ToUpperInvariant() switch
                    {
                        "SHA256" => TotpAlgorithm.Sha256,
                        "SHA512" => TotpAlgorithm.Sha512,
                        _ => TotpAlgorithm.Sha1
                    };
                    break;
            }
        }

        if (string.IsNullOrEmpty(secret)) return null;

        return new Monotp.Models.Entry
        {
            Issuer = issuer,
            Account = account,
            Secret = secret,
            Digits = digits,
            Period = period,
            Algorithm = algo
        };
    }

    static string UrlDecode(string s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        var outp = new List<byte>(bytes.Length);
        int i = 0;
        while (i < bytes.Length)
        {
            var b = bytes[i];
            if (b == (byte)'%' && i + 2 < bytes.Length)
            {
                var h = HexVal(bytes[i + 1]);
                var l = HexVal(bytes[i + 2]);
                if (h.HasValue && l.HasValue)
                {
                    outp.Add((byte)(h.Value * 16 + l.Value));
                    i += 3;
                    continue;
                }
                outp.Add(b);
                i += 1;
            }
            else if (b == (byte)'+')
            {
                outp.Add((byte)' ');
                i += 1;
            }
            else
            {
                outp.Add(b);
                i += 1;
            }
        }
        return Encoding.UTF8.GetString(outp.ToArray());
    }

    static int? HexVal(byte b)
    {
        if (b >= '0' && b <= '9') return b - '0';
        if (b >= 'a' && b <= 'f') return b - 'a' + 10;
        if (b >= 'A' && b <= 'F') return b - 'A' + 10;
        return null;
    }

    public static string SpacedCode(string code)
    {
        foreach (var c in code)
            if (!char.IsDigit(c)) return code;
        int n = code.Length;
        int mid = n / 2 + n % 2;
        return $"{code.Substring(0, mid)} {code.Substring(mid)}";
    }
}
