# JustBetweenUs

**JustBetweenUs** is a small cross-platform text-encryption utility — and, more
importantly, it is the **canonical reference application for the CodeBrix.Platform
NuGet packages** published from the
[CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform) repository.

The same application is implemented here in **two distinct styles**, sharing the same
view models, services, and image/animation assets:

1. **"CodeBrixPlatform" applications** (the `CodeBrixPlatform/` folder) — built on the
   Skia-based `CodeBrix.Platform.*` UI framework (a friendly fork of the Uno Platform),
   where one shared XAML UI runs on Windows, Linux (X11, Wayland, framebuffer), and
   macOS, all rendered through Skia.
2. **"Alternate platform" applications** (repo root) — native WinUI (Windows App SDK),
   WPF, and .NET MAUI apps that do **not** use the Skia-based UI framework, but instead
   use the native `CodeBrix.Platform.WinUI.*`, `CodeBrix.Platform.WPF.*`, and
   `CodeBrix.Platform.Mobile.*` toolkit packages.

If you want to see how any of the CodeBrix.Platform packages are meant to be consumed
in a real application, this repository is the place to look.

---

## What the application does

JustBetweenUs lets you encrypt and decrypt text messages — the kind of thing you might
use to share a note that is "just between us":

* Type or paste text, choose an encryption algorithm — **AES**, **Triple DES**
  (included as an obsolete/insecure example), or **Twofish** — and encrypt it to a
  Base64 string, or decrypt a Base64 string back to plain text.
* Encryption keys are simple text strings; a default key ships as an embedded resource
  so the app works out of the box.
* One-click **copy to clipboard** for the processed text.
* An **OS information** dialog (behind the animated star button) that shows what
  platform, OS version, .NET version, and processor architecture the app is running
  on — handy proof of just how many places the same code runs.
* SVG-based image buttons and a Lottie star animation, rendered identically across
  every head (see *Rendering fidelity* below).

> **Note:** this is a demonstration and reference application. The encryption code is
> real (BouncyCastle-backed), but it has not been security-audited — don't use it to
> protect anything truly sensitive.

The application was originally adapted from a sample provided by **Paul Ainsworth**.

---

## The two application styles

### 1. CodeBrixPlatform (Skia-based) applications — `CodeBrixPlatform/`

These heads run the **same shared XAML UI** (`JustBetweenUs.UI`, a shared project)
on every platform, rendered through Skia by the `CodeBrix.Platform.*` framework:

| Project | Platform / windowing |
| --- | --- |
| `JustBetweenUs.Win32Skia` | Windows, native Win32 window |
| `JustBetweenUs.WinWpfSkia` | Windows, Skia rendering hosted in a WPF window |
| `JustBetweenUs.LinuxX11` | Linux desktop, X11 |
| `JustBetweenUs.LinuxWayland` | Linux desktop, Wayland |
| `JustBetweenUs.LinuxFrameBuffer` | Linux, direct framebuffer (no display server — kiosk/embedded scenarios) |
| `JustBetweenUs.MacOS` | macOS |

Each head is a tiny `Program.cs` that selects its platform host and runs the shared
app:

```csharp
var host = CodeBrixPlatformHostBuilder.Create()
    .App(() => new App())
    .UseWindowsWin32()   // or .UseWindowsWpf() / .UseLinuxX11() / .UseLinuxWayland()
                         //    / .UseLinuxFrameBuffer() / .UseMacOS()
    .Build();

await host.RunAsync();
```

The shared class library `JustBetweenUs.Core` carries the app logic and the
CodeBrix.Platform package references, including the add-in packages for SVG rendering,
Lottie animation, SkiaSharp views, and bundled fonts. The `EmbeddedImage` /
`EmbeddedImageButton` controls in `JustBetweenUs.Core/Controls` show how to load
SVG images from embedded resources via an `embedded://AssemblyName/Resource.Name` URI.

### 2. Alternate platform (native UI) applications — repo root

These heads use each platform's **native UI stack**, with the CodeBrix toolkit
packages layered on top:

| Project | UI stack | CodeBrix packages used |
| --- | --- | --- |
| `JustBetweenUs.WinUI` | WinUI 3 (Windows App SDK) | `CodeBrix.Platform.WinUI.ApacheLicenseForever` (MVVM), `...WinUI.Skia...` (SVG image controls), `...WinUI.Lottie...` (Lottie player) |
| `JustBetweenUs.Wpf` | WPF | `CodeBrix.Platform.WPF.ApacheLicenseForever` (MVVM) |
| `Mobile/` (`JustBetweenUs.Mobile`) | .NET MAUI (Android, iOS, Mac Catalyst, Windows) | `CodeBrix.Platform.Mobile.ApacheLicenseForever` (MVVM) |

All three consume the **CodeBrix "Simple" MVVM toolkit** (namespace
`CodeBrix.Platform.Simple`): `SimpleViewModel`, `SimpleCommand`, `SimpleDialog`,
`SimpleMessaging`, `SimpleServiceResolver`, `SimpleEnum`, and `SimpleOsInfo`. The
shared view models in `Shared/ViewModels` are written once against that toolkit and
compiled into every head — including the Skia-based heads, which get the same toolkit
from the `CodeBrix.Platform.*` framework packages. That is the central trick of this
repository: **one `MainViewModel` drives nine different application heads.**

The WinUI head additionally demonstrates the Skia-rendered visual packages for native
WinUI:

```xml
xmlns:controls="using:CodeBrix.Platform.WinUI.Controls"
xmlns:lottie="using:CodeBrix.Platform.WinUI.Lottie"

<controls:EmbeddedImageButton
    ImageUriSource="embedded://JustBetweenUs.WinUI/JustBetweenUs.WinUI.Assets.padlock-icon.svg"
    Text="Encrypt" ImagePosition="Top" />

<lottie:AnimatedVisualPlayer AutoPlay="True">
    <lottie:LottieVisualSource UriSource="ms-appx:///Assets/star_icon.json" />
</lottie:AnimatedVisualPlayer>
```

### Rendering fidelity

A design goal demonstrated by this repository: the **same SVG files and the same
Lottie JSON** (in `Shared/Assets`) render **identically** in the Skia-based heads and
the native WinUI head. Both paths use the same underlying engines (Skia SVG parsing
and SkiaSharp.Skottie), so icons and animations look the same everywhere — the native
WinUI packages exist precisely so an app can leave the Skia-based UI framework without
changing how its visual assets render.

---

## Shared code

| Folder / project | Purpose |
| --- | --- |
| `Shared/ViewModels` | `MainViewModel` (all app behavior) and `EncryptionMode` (a `SimpleEnum`-based algorithm picker), file-linked into every head |
| `Shared/Helpers` | `HostHelper` — the app-side `IHostBuilderProvider` that hands `Host.CreateDefaultBuilder()` to `SimpleServiceResolver` |
| `Shared/Assets` | The SVG icons (embedded resources) and the Lottie `star_icon.json` used by all heads |
| `Shared/Testing` | `SimpleTestFixture` — a DI-container-backed xUnit fixture base |
| `JustBetweenUs.Encryption` | The encryption service (`IEncryptionService`): AES, Triple DES, and Twofish (via .NET and BouncyCastle cryptography), Base64 transport encoding, embedded default key, DI registration via `AddEncryption()` |
| `Tests/JustBetweenUs.Encryption.Tests` | xUnit tests for the encryption service, using the `SimpleTestFixture` pattern and SilverAssertions |

---

## Solutions — what to open where

| Solution | Use on | Contains |
| --- | --- | --- |
| `JustBetweenUs.sln` | Windows | Everything: all CodeBrixPlatform heads, WinUI, WPF, MAUI, encryption library and tests |
| `JustBetweenUs.Linux.sln` | Linux | The CodeBrixPlatform heads, encryption library and tests |
| `JustBetweenUs.Macos.sln` | macOS | The CodeBrixPlatform heads, the MAUI app, encryption library and tests |

Build with a current .NET SDK (see the `TargetFramework` values in the `.csproj`
files for the exact version expected). The MAUI head requires the .NET MAUI
workloads; the WinUI head builds/deploys like any packaged Windows App SDK app.

**Linux note:** on some Linux ARM64 environments the SkiaSharp native library needs
FreeType preloaded (for example
`export LD_PRELOAD=/usr/lib/aarch64-linux-gnu/libfreetype.so.6`). The comments at the
top of `CodeBrixPlatform/JustBetweenUs.LinuxX11/Program.cs` explain the details.

---

## The CodeBrix.Platform NuGet packages demonstrated here

All packages are produced from the
[CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform) repository. The
package-ID suffix (`.ApacheLicenseForever`, `.MitLicenseForever`) declares each
package's license — and a promise that the license will never change.

**Skia-based UI framework (used by the `CodeBrixPlatform/` heads):**

| Package | Purpose |
| --- | --- |
| `CodeBrix.Platform.ApacheLicenseForever` | The core Skia-based XAML UI framework |
| `CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever` | Windows (Win32) platform head |
| `CodeBrix.Platform.Runtime.Skia.Wpf.ApacheLicenseForever` | Windows (WPF-hosted) platform head |
| `CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever` | Linux X11 platform head |
| `CodeBrix.Platform.Runtime.Skia.Wayland.ApacheLicenseForever` | Linux Wayland platform head |
| `CodeBrix.Platform.Runtime.Skia.FrameBuffer.ApacheLicenseForever` | Linux framebuffer platform head |
| `CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever` | macOS platform head |
| `CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever` | 2D (SkiaSharp) drawing integration |
| `CodeBrix.Platform.Svg.ApacheLicenseForever` | `SvgImageSource` support (vector SVG rendering) |
| `CodeBrix.Platform.Lottie.ApacheLicenseForever` | Lottie animation (`AnimatedVisualPlayer` + `LottieVisualSource`) |
| `CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever` | SkiaSharp view/canvas integration |
| `CodeBrix.Platform.Fonts.OpenSans.ApacheLicenseForever` | Bundled Open Sans font |
| `CodeBrix.SkiaSvg.MitLicenseForever` | Skia SVG parsing/rendering engine |

**Native-platform toolkit packages (used by the root-level heads):**

| Package | Purpose |
| --- | --- |
| `CodeBrix.Platform.WinUI.ApacheLicenseForever` | "Simple" MVVM toolkit for WinUI (Windows App SDK) |
| `CodeBrix.Platform.WinUI.Skia.ApacheLicenseForever` | Skia graphics + vector SVG image controls for WinUI |
| `CodeBrix.Platform.WinUI.Lottie.ApacheLicenseForever` | Skia (Skottie) Lottie animation player for WinUI |
| `CodeBrix.Platform.WPF.ApacheLicenseForever` | "Simple" MVVM toolkit for WPF |
| `CodeBrix.Platform.Mobile.ApacheLicenseForever` | "Simple" MVVM toolkit for .NET MAUI |

(Testing bonus: the test project uses `SilverAssertions.ApacheLicenseForever`, a
fluent-assertion library from the same author.)

---

## Repository layout

```
JustBetweenUs.sln                  All projects (open on Windows)
JustBetweenUs.Linux.sln            Linux development
JustBetweenUs.Macos.sln            macOS development
│
├─ CodeBrixPlatform/               The Skia-based (CodeBrix.Platform UI) applications
│   ├─ JustBetweenUs.UI/           Shared XAML UI (shared project)
│   ├─ JustBetweenUs.Core/         Shared app library + CodeBrix.Platform packages
│   ├─ JustBetweenUs.Win32Skia/      Win32 head
│   ├─ JustBetweenUs.WinWpfSkia/      WPF-hosted Skia head
│   ├─ JustBetweenUs.LinuxX11/     Linux X11 head
│   ├─ JustBetweenUs.LinuxWayland/ Linux Wayland head
│   ├─ JustBetweenUs.LinuxFrameBuffer/  Linux framebuffer head
│   └─ JustBetweenUs.MacOS/        macOS head
│
├─ JustBetweenUs.WinUI/            Native WinUI 3 app (CodeBrix.Platform.WinUI.* packages)
├─ JustBetweenUs.Wpf/              Native WPF app (CodeBrix.Platform.WPF package)
├─ Mobile/                         .NET MAUI app (CodeBrix.Platform.Mobile package)
│
├─ Shared/                         View models, helpers, assets, test fixture (file-linked)
├─ JustBetweenUs.Encryption/       Encryption service library (BouncyCastle)
└─ Tests/                          xUnit tests for the encryption service
```

---

## License

Licensed under the **Apache License, Version 2.0** — see [LICENSE](LICENSE).

The application concept was adapted from a sample provided by Paul Ainsworth.
