# Architecture

```
AVATron.slnx
├─ src/AVATron.Core            simulation only (no UI, no I/O, no native code)
│   ├─ Input/        TickInput, StickInput, StickQuantizer (8-way / analog / digital)
│   ├─ Simulation/   Arena, World (one life within a wave), GameSession (game flow), WaveBuilder,
│   │                GameRules (Classic/Modern policy), GameEvents, FrameSnapshot, XorShiftRandom
│   ├─ Entities/     Player, PlayerShot, Grunt, Electrode, Human, Hulk, Brain, Prog, CruiseMissile,
│   │                Sphereoid, Enforcer, Spark, Quark, Tank, Shell, Effect
│   ├─ Waves/        waves.json (embedded, validated), WaveTable (difficulty, Bozo, loop rule)
│   ├─ Sprites/      Sprite (palette pixels + collision mask), SpriteLibrary (original art)
│   └─ Scoring/      HighScoreTable
├─ src/AVATron.Infrastructure  side effects
│   ├─ Persistence/  DataPaths, VersionedJsonStore (atomic writes, schema checks), Settings/HighScore/Suspend docs
│   ├─ Audio/        Synth (offline synthesis), SoundBank (cues and priorities), AudioMixer, SdlAudioOutput / NullAudioOutput
│   ├─ Input/        SdlGamepadSource (hot-plug) / NoGamepadSource
│   └─ Platform/     SdlHost (optional SDL2 init; never throws)
├─ src/AVATron.Avalonia       desktop app (assembly "AVATron")
│   ├─ Shell/        AppController (flow and policy wiring), Menus, Branding
│   ├─ Rendering/    FrameBuffer (software 292x240 BGRA), GameRenderer, WilliamsPalette, PixelFont
│   ├─ Input/        InputMapper (keys and pad to TickInput; bindings; edge detection)
│   └─ GameView, MainWindow, App, Program
├─ tools/AVATron.Screenshots   regenerates docs/images from the renderer (fixed seeds, no window)
└─ tests/AVATron.Tests        xunit.v3, Avalonia.Headless.XUnit
```

Dependencies only point downward: Avalonia → Infrastructure → Core. Core has no package dependencies.

## Engine contract

- **Input.** `GameSession.Tick(TickInput)` is the only way the player affects the game.
  - `TickInput` = move stick + fire stick (signed bytes) + buttons.
  - Pause, start and menu actions belong to the app layer, not the engine.
- **Fixed tick.** One tick is one arcade frame (1/60.096 s). The host decides how many ticks to run per real-time interval; it caps at 5 per UI frame and drops the remainder after a stall.
  - The engine has no clock of its own. Tests drive it tick by tick, which is what "injectable clock" means here.
  - `AppController.Advance(seconds)` is the real-time adapter.
- **Randomness.** All randomness goes through an injected `IGameRandom`, whose entire state is one `ulong`. The same seed plus the same inputs gives an identical game; `Same_seed_and_inputs_give_identical_games` checks this.
- **Output.** `FrameSnapshot.Fill(session, snapshot)` produces a flat list of `DrawItem`s plus HUD fields. The renderer reads only the snapshot, and rendering never advances the simulation.
- **Headless.** Core and the controller run without a window. The Avalonia headless tests cover the real window.

## Frame ordering

Per UI frame (`GameView.OnFrame`, driven by `TopLevel.RequestAnimationFrame`):

1. Poll the gamepad (SDL) and fold it into `InputMapper`.
2. Global keys (fullscreen), then screen logic (menus) **or** N simulation ticks.
3. Each tick: `InputMapper.BuildTickInput()` → `GameSession.Tick` → events → audio cues.
4. `FrameSnapshot.Fill` → `GameRenderer.Render` → framebuffer → `WriteableBitmap` → scaled `DrawImage`.
5. `InputMapper.EndFrame()` clears edge-triggered presses.

Within `World.Step` (one tick), the order is fixed:

1. Player move (every frame).
2. Motion objects integrate velocity (every frame, like the arcade's IRQ).
3. Laser process: may create one shot.
4. Existing shots advance; every shot is hit-tested against electrodes, then robots, then motion objects. A shot dies on its first hit or at a wall.
5. Processes:
   - the grunt manager every 4 frames;
   - every other entity whose `Sleep` reached 0 runs `Think()`, in creation order;
   - then the family.
6. Player collision: fatal objects in arcade order, then rescues.
7. Lifecycle: remove `Dead` entities, append queued spawns, age effects.
8. Exec every 15 frames: wave-end check and grunt speed-up.

`GameSession` wraps this with phases: `WaveIntro → Playing → (PlayerDying → WaveIntro | GameOver → Finished) | (WaveCleared → WaveIntro)`.

## Entity ownership and lifetime

- **Creation:**
  - `WaveBuilder.Populate` creates a wave's objects into a **new `World`**. A new `World` is built at every wave start and after every death, matching the arcade's PLINIT; that is what resets the rescue multiplier and the shell counter.
  - Entities create children only through `World.Spawn()`. Children are queued and added in step 7, so lists never change while being iterated.
- **Destruction:** only by setting `Dead`, in one of three ways:
  - by the entity itself (expiry, leaving the screen);
  - by `Destroy()` (shot, crushed), which awards points, adds an explosion effect and emits an event;
  - by `Human.Kill()` (skull).
  - Removal happens in step 7.
- **Effects:** explosions, skulls, popups and wall glows live in `World.Effects` with frame timers, so their timing is deterministic too.

## Data-driven waves

- `Waves/waves.json` holds:
  - parameter metadata: name, timer/rate kind, min, max;
  - 40 wave rows;
  - the Bozo table.
- `WaveTable.Parse` rejects malformed or out-of-range data with a message naming the field (tested).
- Behaviour code reads only `WaveParameters` / `WaveCounts`; it contains no wave numbers except documented arcade rules, such as safety-box sizes and electrode shapes.

## Classic vs Modern without two engines

- **Rules:** `GameRules` (immutable record) carries the only rule-affecting switches: analog move, analog fire, and the operator settings.
- **Presentation and system policy** live in the app layer:
  - flicker on/off, scaling, HUD extras, game speed (tick-rate multiplier);
  - mixer mode;
  - suspend availability;
  - pause on focus loss.
- `AppController.ApplySettings`/`StartGame` is the single place where a preset becomes policy.

## Rendering

- **Software framebuffer.** At 292×240 it's simpler and faster to write pixels directly than to issue a Skia call per sprite, and it guarantees exact pixels.
- **GPU scaling.** Each frame the buffer is copied into one `WriteableBitmap` (BGRA premultiplied), which `GameView.Render` draws scaled. The scale mode is `BitmapInterpolationMode.None` for crisp pixels, or `HighQuality` when Modern smooth scaling is on.
- **Destination rectangle** (`GameView.ComputeDestination`):
  - computed in device pixels using `RenderScaling`, so integer scaling stays sharp on HiDPI and Retina displays;
  - supports the 4:3 arcade pixel shape or square pixels;
  - always letterboxed or pillarboxed.
- **Cost:** the simulation takes about 6 µs per tick (measured: 14,911 ticks in 88 ms, Release, aarch64 VM). It runs on the UI thread inside the animation-frame callback, which costs less than a millisecond per frame, so no simulation thread is needed. All file I/O is small and only happens on menu actions.
- **Avalonia APIs** were confirmed by building against 12.1.3: `TopLevel.RequestAnimationFrame`, `WriteableBitmap.Lock`, `DrawingContext.PushRenderOptions`, `RenderScaling`, and the headless `KeyPressQwerty` / `CaptureRenderedFrame` used by the UI tests.

## Input

- **`InputMapper`:**
  - Held keys are a `HashSet`, so OS auto-repeat (observed: about 12 repeat `KeyDown`s per half second) cannot double inputs.
  - Presses are edge-detected per UI frame.
  - Bindings are an `InputAction → key names` map from the settings file.
  - Keyboard and d-pad act as switches (opposites cancel). Analog sticks are quantised to 8 directions in Classic and passed through in Modern, with a radial deadzone (default 0.2).
- **Gamepad:**
  - Avalonia 12 has no gamepad API (its issue #6945 is still open), so gamepads use SDL2 GameController via Silk.NET.SDL.
  - Devices are opened on `CONTROLLERDEVICEADDED` and released on removal, so hot-plug needs no restart.
  - Polling happens once per UI frame on the UI thread.
- **Window keys:** `MainWindow` handles keys at the tunnel stage, so Avalonia focus navigation never steals arrows or Tab. Focus loss releases all held keys.

## Audio

- **Synthesis:** `SoundBank.Build()` synthesises every cue once at start-up into float buffers (8-bit DAC quantised). Cues include pitch-swept pulse, saw and triangle waves, LFSR noise and arpeggios.
- **Mixer (`AudioMixer`):**
  - Thread-safe: commands go through a concurrent queue, and `Render()` runs on the audio thread.
  - **SingleChannelPriority** (Classic) reproduces the arcade sequencer rule.
  - **Polyphonic** (Modern) uses 12 voices, restarts a clip that is already playing instead of stacking it, steals the lowest-priority oldest voice, and soft-clips the output.
  - Effects and ambient have separate gains under a master gain.
- **Output:** `SdlAudioOutput` keeps about 40 ms queued (`SDL_QueueAudio`) from its own thread. Any failure gives a `NullAudioOutput` with a reason, and the game runs silently.

## Persistence

See [SAVES.md](SAVES.md). Every file is written atomically (temp file in the same directory, flush to disk, rename) and carries a `schemaVersion`.
