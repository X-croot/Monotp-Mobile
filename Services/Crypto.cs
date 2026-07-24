using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Monotp.Models;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Monotp.Services;

public static class Crypto
{
    public const int SaltLen = 16;
    public const int NonceLen = 24;
    public const int KeyLen = 32;

    public static byte[] RandomBytes(int n)
    {
        var b = new byte[n];
        RandomNumberGenerator.Fill(b);
        return b;
    }

    public static byte[] RandomSalt() => RandomBytes(SaltLen);

    public static byte[] DeriveKey(string password, byte[] salt, KdfParams p)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = (int)p.PCost,
            MemorySize = (int)p.MCost,
            Iterations = (int)p.TCost
        };
        return argon2.GetBytes(KeyLen);
    }

    public static byte[] Encrypt(byte[] key, byte[] plaintext)
    {
        var nonce = RandomBytes(NonceLen);
        var subKey = HChaCha20(key, nonce.AsSpan(0, 16));
        var ietfNonce = new byte[12];
        Array.Copy(nonce, 16, ietfNonce, 4, 8);

        var cipher = new Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305();
        cipher.Init(true, new AeadParameters(new KeyParameter(subKey), 128, ietfNonce));
        var outBuf = new byte[cipher.GetOutputSize(plaintext.Length)];
        int len = cipher.ProcessBytes(plaintext, 0, plaintext.Length, outBuf, 0);
        cipher.DoFinal(outBuf, len);

        var result = new byte[NonceLen + outBuf.Length];
        Array.Copy(nonce, 0, result, 0, NonceLen);
        Array.Copy(outBuf, 0, result, NonceLen, outBuf.Length);
        Array.Clear(subKey);
        return result;
    }

    public static byte[] Decrypt(byte[] key, byte[] data)
    {
        if (data.Length < NonceLen)
            throw new CryptographicException("ciphertext too short");

        var nonce = data.AsSpan(0, NonceLen);
        var ct = data.AsSpan(NonceLen);
        var subKey = HChaCha20(key, nonce.Slice(0, 16));
        var ietfNonce = new byte[12];
        nonce.Slice(16, 8).CopyTo(ietfNonce.AsSpan(4));

        var cipher = new Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305();
        cipher.Init(false, new AeadParameters(new KeyParameter(subKey), 128, ietfNonce));
        var ctArr = ct.ToArray();
        var outBuf = new byte[cipher.GetOutputSize(ctArr.Length)];
        int len = cipher.ProcessBytes(ctArr, 0, ctArr.Length, outBuf, 0);
        cipher.DoFinal(outBuf, len);
        Array.Clear(subKey);
        return outBuf;
    }

    static byte[] HChaCha20(byte[] key, ReadOnlySpan<byte> nonce16)
    {
        Span<uint> s = stackalloc uint[16];
        s[0] = 0x61707865; s[1] = 0x3320646e; s[2] = 0x79622d32; s[3] = 0x6b206574;
        for (int i = 0; i < 8; i++)
            s[4 + i] = BinaryPrimitives.ReadUInt32LittleEndian(key.AsSpan(i * 4, 4));
        for (int i = 0; i < 4; i++)
            s[12 + i] = BinaryPrimitives.ReadUInt32LittleEndian(nonce16.Slice(i * 4, 4));

        for (int i = 0; i < 10; i++)
        {
            Qr(s, 0, 4, 8, 12);
            Qr(s, 1, 5, 9, 13);
            Qr(s, 2, 6, 10, 14);
            Qr(s, 3, 7, 11, 15);
            Qr(s, 0, 5, 10, 15);
            Qr(s, 1, 6, 11, 12);
            Qr(s, 2, 7, 8, 13);
            Qr(s, 3, 4, 9, 14);
        }

        var outp = new byte[32];
        int[] idx = { 0, 1, 2, 3, 12, 13, 14, 15 };
        for (int i = 0; i < 8; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(outp.AsSpan(i * 4, 4), s[idx[i]]);
        return outp;
    }

    static void Qr(Span<uint> s, int a, int b, int c, int d)
    {
        s[a] += s[b]; s[d] = Rotl(s[d] ^ s[a], 16);
        s[c] += s[d]; s[b] = Rotl(s[b] ^ s[c], 12);
        s[a] += s[b]; s[d] = Rotl(s[d] ^ s[a], 8);
        s[c] += s[d]; s[b] = Rotl(s[b] ^ s[c], 7);
    }

    static uint Rotl(uint x, int n) => (x << n) | (x >> (32 - n));
}
