# AGENTS.md

Notes for anyone, human or AI agent, changing this code. `README.md` covers playing the game; `CONTROLS.md`,
`SAVES.md`, `FIDELITY.md`, `RESEARCH.md` and `KNOWN_ISSUES.md` cover controls, save files, arcade accuracy and open
problems. `ARCHITECTURE.md` is the detailed design. This file is the short version: how to build it and the rules
that keep it working.

## What this is

A recreation of Williams' 1982 arcade game Robotron: 2084, in C# on .NET 10 and Avalonia 12, with a Classic mode
(arcade rules) and a Modern mode (analog aiming and extras). The art and sound are original: sprites are defined in
code and every sound is synthesised at start-up.

## Commands

```bash
dotnet build AVATron.slnx                        # SDK from global.json (10.0.400, latest feature roll-forward)
dotnet test AVATron.slnx                         # xUnit v3 plus Avalonia headless tests; no window opens
dotnet run --project src/AVATron.Avalonia        # play it
dotnet run --project tools/AVATron.Screenshots   # regenerate docs/images/*.png from the renderer (optional out dir)
build/publish.sh [rid ...]                       # single-file builds as CI makes them (publish.ps1 on Windows)
```

- Run arguments: `--no-audio`, `--no-gamepad`, `--windowed`, `--start-wave <n>`, `--data-dir <path>`.
- Environment: `AVATRON_NO_AUDIO=1`, `AVATRON_DATA_DIR`, and `AVATRON_DEBUG_KEYS=1` (logs key events).
- `Directory.Build.props` sets net10.0, nullable and implicit usings, and central package versions in
  `Directory.Packages.props`. `TreatWarningsAsErrors` is off locally, but CI builds with `-warnaserror`, so keep
  warnings at zero.

## Layout

Dependencies point one way: `AVATron.Avalonia` -> `AVATron.Infrastructure` -> `AVATron.Core`.

- `src/AVATron.Core/`: the simulation, with no UI, I/O or packages.
  - `Simulation/`: `GameSession` (phases and game flow), `World` (one life within a wave, stepped by `World.Step`),
    `WaveBuilder`, `GameRules`, `FrameSnapshot` (what the renderer reads) and `GameRandom.cs`.
  - `Entities/`: the player, grunts, hulks, brains, family and the spawners.
  - `Waves/waves.json`: the 40 wave rows and the Bozo table, embedded and validated by `WaveTable.Parse`.
  - `Input/TickInput.cs`, `Sprites/`, `Scoring/HighScoreTable.cs`.
- `src/AVATron.Infrastructure/`: side effects. `Audio/` (synth, sound bank, mixer, SDL output), `Input/SdlGamepad.cs`,
  `Persistence/` (`DataPaths`, atomic `VersionedJsonStore`) and `Platform/SdlHost.cs`.
- `src/AVATron.Avalonia/`: the app (assembly `AVATron`). `Shell/AppController.cs` wires flow and settings,
  `GameView.cs` runs the frame loop, `Rendering/` draws into a 292x240 software framebuffer, and
  `Input/InputMapper.cs` turns keys and pads into `TickInput`.
- `tools/AVATron.Screenshots/`: renders the README pictures with fixed seeds and no window.
- `tests/AVATron.Tests/`: engine, scoring, persistence, audio, input and headless UI tests; helpers are in
  `TestSupport.cs`.
- `docs/research/`: hardware and mechanics notes behind `FIDELITY.md`.

## Rules that keep it working

- **Fixed ticks.** One tick is one arcade frame (1/60.096 s). `GameSession.Tick(TickInput)` is the only way input
  reaches the game; the engine has no clock. `AppController.Advance(seconds)` converts real time to ticks.
- **Seeded randomness.** All simulation randomness goes through the injected `IGameRandom` (`XorShiftRandom`), whose
  whole state is one `ulong`. The same seed and inputs must give the same game; a test checks this. Never use
  `Random.Shared` in Core.
- **Renderer reads, never steps.** Drawing uses only `FrameSnapshot`; rendering never advances the simulation.
- **Spawn through the world.** Entities create children only with `World.Spawn()`, and die only by setting `Dead`.
  Both are applied at the end of the step, so lists never change while being iterated.
- **Wave data, not wave numbers.** Behaviour reads `WaveParameters`; tuning goes in `waves.json`.
- **Input sign.** Stick `Y` grows downward (screen space): negative is up. `InputMapper` follows this for keys,
  d-pad and sticks.
- **Saves.** Settings and high scores live under `SpecialFolder.ApplicationData` (`%APPDATA%`), XDG config on Linux,
  or `Library/Application Support` on macOS, in an `AVATron` folder with three slots. Every file is written
  atomically and carries a `schemaVersion`; bump it and handle old versions when a format changes.
- **Tests stay out of real saves.** They pass a `DataPaths` under a fresh temp folder and use a plain `AudioMixer`
  with no output device and `NoGamepadSource`.

## Making a change

- Put gameplay in `AVATron.Core` with a test; `dotnet test` must pass.
- If the change shows on screen, regenerate the screenshots with the tool above and look at them.
- Update `README.md`, `CONTROLS.md`, `SAVES.md` or `FIDELITY.md` when what they describe changes.

## CI and releases

`.github/workflows/build.yml` builds with `-warnaserror` and tests on Ubuntu, Windows and macOS for every push and
pull request. It then publishes self-contained single-file builds for win-x64/x86/arm64, linux-x64/arm64/arm and
osx-x64/arm64, kept as workflow artifacts. Pushes to `main` replace the `latest` pre-release; a `v*` tag makes a
proper release. A release also asks the portfolio site to rebuild.
