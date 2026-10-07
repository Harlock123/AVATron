# AVATron: 2084

**AVATron: 2084** is a twin-stick arena shooter written in C# on **.NET 10** with an **Avalonia 12**
desktop front end. It faithfully recreates the gameplay of the 1982 Williams Electronics / Vid Kidz
arcade game *Robotron: 2084*, with optional modernisations.

> Unofficial fan project. Not affiliated with or endorsed by Williams Electronics, WMS Industries,
> Midway, Warner Bros. or the original authors. "Robotron" is a trademark of its owner and is used
> here only to describe the game being recreated. All graphics and sounds in this repository are
> original work (see [THIRD_PARTY.md](THIRD_PARTY.md)); no ROM data is included. Product naming lives
> in `src/AVATron.Avalonia/Shell/Branding.cs`.

Two presets share one engine:

- **Classic**: the arcade program's rules as documented from its source and from MAME. That means 8-way twin sticks, 60.096 Hz logic, the 40-wave table that loops waves 21–40, factory operator settings, single-channel priority sound and a flicker approximation.
- **Modern**: the same game with usability changes. It adds analog twin-stick aiming, no flicker, an optional wave-progress meter, an adjustable game speed, suspend/resume, and polyphonic audio.

See [FIDELITY.md](FIDELITY.md) for exactly what differs.

## Prerequisites

| | Version | Notes |
|---|---|---|
| .NET SDK | 10.0.x (validated with 10.0.400) | `global.json` pins 10.0.400, with `latestFeature` roll-forward |
| OS | Windows 10+, Linux (X11/XWayland), macOS 12+ | Only Linux aarch64 has actually been run (see [KNOWN_ISSUES.md](KNOWN_ISSUES.md)) |
| SDL2 | bundled | The NuGet package ships SDL2 natives for win-x64/arm64, linux-x64/arm64 and osx. Without it the game still runs, just without sound or gamepad |

## Build, run, test

```bash
dotnet build                       # whole solution
dotnet run --project src/AVATron.Avalonia
dotnet test                        # 121 headless tests (engine, persistence, audio mixer, app flow, Avalonia headless UI)
```

Command-line options for the game:

| Option | Effect |
|---|---|
| `--windowed` | ignore the saved fullscreen setting |
| `--no-audio` (or `AVATRON_NO_AUDIO=1`) | don't open an audio device |
| `--no-gamepad` | don't initialise SDL game controllers |
| `--data-dir <path>` (or `AVATRON_DATA_DIR`) | store settings, scores and suspend files elsewhere |
| `AVATRON_DEBUG_KEYS=1` | log key events to stdout |

## Publishing

```bash
build/publish.sh                       # all targets
build/publish.sh win-x64 linux-x64     # selected targets
pwsh build/publish.ps1 -Rids win-x64   # from Windows
```

| Target | Type | Status |
|---|---|---|
| win-x64 | self-contained folder (`AVATron.exe`, about 208 MB, untrimmed) | publishes from Linux; **not yet run on Windows** |
| linux-arm64 | framework-dependent | published and launched on Arch Linux aarch64 |
| linux-x64 | framework-dependent | publishes; not run |
| osx-arm64, osx-x64 | framework-dependent | osx-arm64 publishes; not run. There is no `.app` bundle or code signing yet |

Output goes to `publish/<rid>/`.

## Where things are

| Doc | Contents |
|---|---|
| [RESEARCH.md](RESEARCH.md) | research dossier, source ledger, confidence levels, open questions |
| [FIDELITY.md](FIDELITY.md) | reference version, fidelity matrix, Classic and Modern guide |
| [ARCHITECTURE.md](ARCHITECTURE.md) | projects, tick order, entity lifetime, rendering, input, audio |
| [CONTROLS.md](CONTROLS.md) | keyboard and gamepad reference, remapping |
| [SAVES.md](SAVES.md) | file formats, locations, schema versions |
| [THIRD_PARTY.md](THIRD_PARTY.md) | dependencies, licences, asset provenance |
| [KNOWN_ISSUES.md](KNOWN_ISSUES.md) | unverified behaviour, platform caveats, deferred work |
| [MILESTONE_STATUS.md](MILESTONE_STATUS.md) | what is done, tested, observed, and still open |
| `docs/research/` | the detailed research notes with line-level citations |
