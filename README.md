# monotp — .NET MAUI Edition

**Minimal, fully-encrypted, cross-platform TOTP authenticator, rebuilt on .NET MAUI.**

![framework](https://img.shields.io/badge/framework-.NET%20MAUI-000000?style=flat-square)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Android%20%7C%20iOS%20%7C%20macOS-000000?style=flat-square)
![crypto](https://img.shields.io/badge/KDF-Argon2id-000000?style=flat-square)
![cipher](https://img.shields.io/badge/cipher-XChaCha20--Poly1305-000000?style=flat-square)
![license](https://img.shields.io/badge/license-MIT-000000?style=flat-square)

---

## About

**monotp** is a clean, black-and-white TOTP (Time-based One-Time Password) authenticator.
It stores every account secret **fully encrypted** on disk, protected by a **master password**
that is stretched with **Argon2id**. One app, no telemetry, no cloud.

This is a complete port of the original Rust/egui project to **.NET MAUI (C#/XAML)**, keeping
every feature intact while running natively on Windows, Android, iOS and macOS.

Built by [**X-croot**](https://github.com/X-croot).

## Features

- **RFC 6238 TOTP** — SHA1 / SHA256 / SHA512, 6–8 digits, configurable period.
- **Full encryption at rest** — vault sealed with **XChaCha20-Poly1305** (AEAD).
- **Argon2id master key** — memory-hard key derivation (~64 MiB, tunable).
- **Smart paste** — drop an `otpauth://` link *or* a raw base32 secret; issuer, account, digits, period and algorithm are auto-filled.
- **Live search** — instantly filter accounts by issuer or name.
- **Add / edit / delete** — full account management with a live code **preview** while adding.
- **Reveal / copy** — one-click copy with confirmation, plus per-entry secret reveal.
- **Two ways to reset your password:**
  - **Change master password** — the vault is decrypted in memory, then re-encrypted and overwritten with the new password. Your accounts stay intact.
  - **Forgot password** — a guarded, type-`DELETE` wipe that erases everything and lets you set up a fresh vault (there is no recovery — by design).
- **Countdown ring** — a live progress ring plus a shrinking progress bar per code.
- **Autostart on login** — one toggle (Windows `Run` registry key; no-op on platforms that don't apply).
- **Platform-native storage** — data lives under each OS's app-data directory (`FileSystem.AppDataDirectory/monotp/`): `config.json` + `vault.enc`.
- **Themes** — `System`, `Dark`, `Light`, `Sakura`, and a pure `Monochrome` (black & white) theme.

## Themes

| Theme | Description |
| --- | --- |
| System | Follows the OS light/dark preference |
| Dark | Deep neutral dark |
| Light | Clean light |
| Sakura | Soft cherry-blossom pink |
| Monochrome | Pure black & white, high contrast |

## Tech stack

`.NET 10` · `.NET MAUI` · `C#` / `XAML` · `Konscious.Security.Cryptography.Argon2` · `BouncyCastle.Cryptography` (ChaCha20-Poly1305 + HChaCha20) · `System.Security.Cryptography` (HMAC-SHA1/256/512) · `System.Text.Json`

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (the project targets `net10.0-*`).
- The right MAUI workload for your OS (see below).

> **Platform reality:** .NET MAUI has **no Linux desktop target**. On **Linux you can only build the Android head.** Windows heads build on Windows, and iOS/macOS heads build on a Mac. The `.csproj` enables each head only on the OS that can build it, so multi-targeting never breaks a single-OS machine.

### Linux (Android only)

```
sudo dotnet workload install maui-android
```

> Do **not** run `dotnet workload install maui` on Linux — the full `maui` workload is not supported there and will fail with "bu platformda desteklenmiyor". Use `maui-android`.
> Also install the Android SDK/JDK (OpenJDK 17). Set `ANDROID_HOME` / `JAVA_HOME` if not auto-detected.

### Windows

```
dotnet workload install maui
```

### macOS

```
sudo dotnet workload install maui
```

## Build & Run

Always pass `-f` with the target for your OS. A bare `dotnet build`/`restore` tries every head and fails on machines that cannot build all of them.

### Linux — Android (device/emulator attached)

```
dotnet build   -f net10.0-android -c Release
dotnet build -t:Run -f net10.0-android
```

### Windows

```
dotnet build -t:Run -f net10.0-windows10.0.19041.0
```

## Deploy / Publish

### Android (.apk / .aab) — works on Linux

```
dotnet publish -f net10.0-android -c Release
# Output: bin/Release/net10.0-android/publish/  (*-Signed.apk / *.aab)
```

Signed release build:

```
dotnet publish -f net10.0-android -c Release \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=monotp.keystore \
  -p:AndroidSigningKeyAlias=monotp \
  -p:AndroidSigningKeyPass=YOUR_PASS \
  -p:AndroidSigningStorePass=YOUR_PASS
```

### Windows (unpackaged .exe) — on Windows

```
dotnet publish -f net10.0-windows10.0.19041.0 -c Release -p:WindowsPackageType=None
```

### iOS / macOS — on a Mac

```
dotnet publish -f net10.0-ios -c Release
dotnet publish -f net10.0-maccatalyst -c Release
```

## Usage

1. On first launch, create a **master password** (min. 8 characters). This seals your vault — it is **never stored** and **cannot be recovered**.
2. Tap **+ Add account**, then either paste an `otpauth://` URI or fill in the fields manually (issuer, account, base32 secret).
3. Codes refresh automatically; tap **Copy** to place the current code on your clipboard.
4. Use **Menu → Lock vault** to wipe secrets from memory; unlock again with your master password.

## Security notes

- The master password is stretched with **Argon2id** using a random 16-byte salt (stored in `config.json`).
- The derived 256-bit key never touches disk; only the **encrypted** vault (`vault.enc`) is persisted.
- Encryption uses **XChaCha20-Poly1305** with a fresh random 24-byte nonce per save (HChaCha20 subkey + IETF ChaCha20-Poly1305).
- There is **no backdoor and no recovery**: lose the master password and the vault is unrecoverable — by design.

## Vault format

The plaintext vault is JSON (`{ "Entries": [...] }`), serialized then encrypted. On disk the vault file is laid out as `nonce (24 bytes) || ciphertext+tag`, identical to the reference construction.

## Credits

Original Rust/egui project and design by [**X-croot**](https://github.com/X-croot).
.NET MAUI port keeps the same author, behaviour, and MIT license.

## License

MIT © [X-croot](https://github.com/X-croot)
