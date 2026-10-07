# Third-party notices and asset provenance

## Runtime dependencies (shipped with the app)

Versions are pinned in `Directory.Packages.props`. Licences were read from each package's nuspec in the local NuGet cache on 2026-10-07.

| Package | Version | Licence | Use |
|---|---|---|---|
| Avalonia, .Desktop, .Skia, .Themes.Fluent, .X11, .Win32, .Native, .FreeDesktop(.AtSpi), .HarfBuzz, .Remote.Protocol | 12.1.3 | MIT | UI framework |
| Avalonia.Angle.Windows.Natives | 2.1.27548.20260419 | BSD-3-Clause (ANGLE Project Authors) | Windows GPU backend |
| SkiaSharp (+ NativeAssets Linux/macOS/Win32/WebAssembly) | 3.119.4 | MIT (Skia itself: BSD-3-Clause) | 2D graphics backend used by Avalonia |
| HarfBuzzSharp (+ NativeAssets) | 8.3.1.3 | MIT (HarfBuzz: "Old MIT") | text shaping (Avalonia) |
| MicroCom.Runtime | 0.11.6 | MIT | Avalonia interop |
| Tmds.DBus.Protocol | 0.94.1 | MIT | Linux desktop integration (Avalonia) |
| Silk.NET.SDL, Silk.NET.Core, Silk.NET.Maths | 2.23.0 | MIT | SDL2 bindings (gamepad and audio) |
| Ultz.Native.SDL (SDL 2.32.10 binaries) | 2.32.10 | zlib | SDL2 native library |
| Microsoft.Extensions.DependencyModel | 9.0.9 | MIT | Silk.NET dependency |
| Microsoft.DotNet.PlatformAbstractions | 3.1.6 | MIT | Silk.NET dependency |
| .NET runtime (self-contained Windows build) | 10.0.x | MIT | runtime |

**Obligations:** keep the copyright and licence notices for MIT, BSD and zlib components in redistributions. Bundling this file together with the upstream licence texts satisfies that. zlib additionally asks that altered versions of SDL be marked as such; we ship SDL unmodified.

## Build- and test-only dependencies

| Package | Version | Licence |
|---|---|---|
| Avalonia.BuildServices (transitive, **build-time telemetry**, see below) | 11.3.2 | MIT |
| xunit.v3, xunit.runner.visualstudio | 3.2.2 / 3.1.5 | Apache-2.0 |
| Microsoft.NET.Test.Sdk | 18.10.1 | MIT |
| Avalonia.Headless, Avalonia.Headless.XUnit | 12.1.3 | MIT |

> **Avalonia build telemetry.** Avalonia.BuildServices sends anonymous usage data **at build time only**. According to its README: hashed project and machine names, target framework, OS, IDE and CI type; nothing is collected from the running game. Set `AVALONIA_TELEMETRY_OPTOUT=1` in your environment to disable it. This repository does not opt out on your behalf.

## Libraries considered and rejected

| Library | Reason |
|---|---|
| ManagedBass | depends on BASS, which is free only for non-commercial use |
| NAudio | no macOS output |
| Silk.NET.OpenAL + OpenAL Soft | workable, but OpenAL Soft is LGPL; SDL2 already covers audio and gamepads |
| SDL3 bindings (ppy.SDL3-CS MIT, SDL3-CS zlib) | good alternatives; SDL2 via Silk.NET was chosen for its stable, long-published bindings. Switching is localised to `Infrastructure/Platform`, `Audio/AudioOutput.cs` and `Input/SdlGamepad.cs` |

## Asset provenance

| Asset | Origin | Licence |
|---|---|---|
| All sprites (player, family, every enemy, electrodes, skull, shots, life icon) | Hand-authored for this project as ASCII pixel art in `src/AVATron.Core/Sprites/SpriteLibrary.cs`; sphereoid, quark and prog images are generated procedurally. Only the **bounding sizes** were taken from research; no original image data was viewed or copied | project code |
| 5×7 pixel font | Hand-authored in `src/AVATron.Avalonia/Rendering/PixelFont.cs` | project code |
| All sound effects | Synthesised at runtime by `Synth`/`SoundBank` (oscillators, LFSR noise, envelopes). No samples, no MAME or ROM audio | project code |
| Colour values | The arcade DAC's 256-colour space, using levels computed by MAME's resistor model (BSD-3 MAME; facts only). Colour choices and cycling sequences are our own | project code |
| Wave table (`waves.json`) | Numeric game parameters re-expressed as our own JSON from published analyses of the 1982 program (RESEARCH.md M1/M2). The original program is © 1982 Williams Electronics; **no code or binary data was copied**, only the numbers that describe the game's behaviour | see note |
| Game rules and behaviour | Implemented independently from documented behaviour | project code |

## Reuse and trademark caveats (please read before distributing)

- **No ROM, cabinet art, original graphics, original sound data or original text** is included.
- The original source and the disassemblies that document the game carry **no licence** and are © Williams. They were used only as references for facts.
- Whether the factual parameter table in `waves.json` is free of claims has **not been legally reviewed**. This repository is **not** cleared for distribution.
- **"Robotron" and "Robotron: 2084" are trademarks.** The product has been renamed **AVATron: 2084**, and the old name no longer appears in the game's UI, window title, executable or data folder. The docs still name the original game when describing what is recreated. The title screen also shows an "unofficial, not affiliated" disclaimer. Whether "AVATron: 2084" (keeping "2084" and the "-tron" ending) is far enough from the original mark has **not been legally reviewed**. All naming lives in `Shell/Branding.cs`, and the data folder name in `DataPaths.FolderName`.
- None of the GitHub reimplementations surveyed was reused. Several have no licence or appear to ship ripped assets (RESEARCH.md M11).
