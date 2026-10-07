# Milestone status

Status as of 2026-10-07. All observations of the running game were made on **Linux aarch64** (Arch VM, Hyprland/XWayland, PipeWire). On Windows and macOS only the automated tests have run (GitHub Actions); the game itself hasn't been launched there.

**Legend:**
- ✅ done
- 🟡 partial
- ⏳ not started or deferred

Evidence tags used below:
- **[test]**: covered by the automated suite (`dotnet test`, 132 passing).
- **[observed]**: seen working in the real app (screenshots or audio capture).
- **[untested]**: implemented but not exercised.

| # | Milestone | State | Notes |
|---|---|---|---|
| 1 | Research and specification | ✅ | RESEARCH.md and `docs/research/` (source ledger, confidence tags), FIDELITY.md (matrix, reference version), open questions. No MAME/ROM runs or video analysis |
| 2 | Skeleton and engine validation | ✅ | Solution with 4 projects. Avalonia 12.1.3 + SkiaSharp 3.119.4 on net10.0 confirmed **[observed]**. Fixed 60.096 Hz loop **[test]**. Determinism and lifecycle **[test]**. README build steps verified locally. GitHub Actions CI passes build and 132 tests on Windows, Ubuntu and macOS **[test]** |
| 3 | Core gameplay | ✅ | Twin-stick keyboard **[test][observed]**. Shots: cadence, 4-shot cap, 6 px/frame, wall death **[test]**. Hard walls **[test]**. Grunts **[test][observed]**. Score and lives HUD **[observed]**. Death, restart and game over **[test]**. Wave start/clear **[test]**. Gamepad **[untested]**: no device available |
| 4 | Complete enemy roster | ✅ | Hulk, Brain, Prog, Cruise missile, Sphereoid, Enforcer, Spark, Quark, Tank, Shell, Electrode and the family, each with the cited behaviour **[test]**. All 40 table waves plus the loop build and run **[test]** |
| 5 | Full wave system and scoring | ✅ | Complete 40-wave data with the 21–40 loop, difficulty and Bozo **[test]**. All point values, extra man, rescue ladder **[test]**. Initials entry and high-score table **[test]**. Wave-start presentation **[observed]**; wave-clear marquee and game-over rendering **[test]** |
| 6 | Audio | ✅ | 28 synthesised cues plus an optional ambient hum. Single-channel priority mode (Classic) and 12-voice mode (Modern) **[test]**. Volume categories. Graceful failure: `NullAudioOutput` and a bounded command queue when no device consumes it **[test]**; `--no-audio` start **[observed]**. Real output recorded from the PipeWire monitor: jingle, march and laser cadence, peak −5.6 dBFS **[observed]** |
| 7 | Modern mode and accessibility | 🟡 | Analog move and aim **[test]**. Deadzone setting. Flicker suppression **[test]**. Pause **[test]**. Suspend/resume **[test]**. Game speed. Key remapping UI **[test]**. Settings screen **[test]**. Reduced motion, no-audio, separate tables, 3 slots. **Missing:** gamepad remapping, and a real-pad test of hot-plug |
| 8 | Validation, packaging, docs | 🟡 | Full suite passes. All docs written. Every push builds self-contained single-file packages for 8 platforms (win x64/x86/arm64, linux x64/arm64/arm, osx x64/arm64) and publishes them to a rolling `latest` pre-release; `v*` tags make releases. The linux-arm64 single file launches with audio **[observed]**. **Open:** run on Windows and macOS, `.app` bundle, trimming, MAME-based fidelity checks, legal review of name and data |

## Handoff: next concrete tasks

1. **Validate on Windows.**
   - Download `AVATron-win-x64.zip` from the `latest` release (or run `build\publish.ps1`), then launch `AVATron.exe`.
   - Check fullscreen (F11), HiDPI integer scaling, SDL audio and an Xbox pad with hot-plug.
2. **Gamepad on Linux:** confirm SDL sees the pad while the Avalonia window has focus. The `SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS` hint is already set.
3. **Fidelity checks against MAME** with a legally owned `robotron` set. Settle these in this order:
   1. default difficulty;
   2. tank-shell counter behaviour;
   3. progs and wave end;
   4. marquee and appear timings;
   5. flicker under load.

   Then update FIDELITY.md and bump `GameSession.EngineVersion` if the simulation changes.
4. **Two-player alternating mode** and **gamepad remapping.**
5. **Clear the product name.** It was renamed to AVATron: 2026 (`Shell/Branding.cs`, `DataPaths.FolderName`); have the name reviewed before any public release.

## Commands (all verified on Linux aarch64)

```bash
dotnet build
dotnet test
dotnet run --project src/AVATron.Avalonia -- --windowed
build/publish.sh linux-arm64
```
