# Known issues, caveats and deferred work

## Platform validation

- **Only Linux aarch64 has been exercised.** That means Arch Linux on a VM, with Hyprland/Wayland running the app through XWayland, PipeWire audio, and .NET SDK 10.0.400.
- **Windows:** the self-contained `win-x64` package publishes from Linux but has **never been run on Windows**. The brief named Windows as the first validation platform; that validation is still to do.
- **macOS:** `osx-arm64` publishes, but it is not run, not bundled as a `.app`, and not signed or notarised. Gatekeeper will block it unless you allow it manually.
- **linux-x64:** published, not run.
- **Windows package size:** 208 MB, because trimming hasn't been attempted. Avalonia supports trimming with compiled bindings; this needs testing.
- **No CI pipeline has been run.** `.github/workflows/ci.yml` is provided but has never executed.

## Input

- **No physical gamepad was available.** The SDL2 GameController path, including hot-plug, compiles and initialises ("No gamepad connected"), but has not been tested with a real controller.
- Gamepad buttons can't be remapped; only keyboard keys can.
- Only the first connected gamepad is used, and there is no 2-player mode (see below).
- **Test-tool artifact, not a game bug:** wtype key injection under XWayland delivered wrong key codes. Hyprland's own key dispatch delivered them correctly.

## Not yet implemented (deferred)

- **Two-player alternating games.** The arcade supports 2 players taking turns; only 1 player is implemented.
- **Coins and credits.** The game is effectively on free play. Operator bookkeeping screens are not implemented.
- **Attract-mode demo game.** The arcade's "fancy attract" plays a demo. Ours cycles title, roster and score pages but plays no demo.
- **Story/instruction pages.** The arcade's attract text is copyrighted, so it was not reproduced. A paraphrased roster page is shown instead.
- **High-score table shape.** Ours is a top-10 table per preset and slot, as the brief asked. The arcade had a champion with a name of up to 20 letters, 36 all-time entries, a "Today's Greatest" list of 10, and a limit of 5 entries per player.
- **Classic crash-recovery save.** It is allowed by the brief but not implemented. If the program crashes during a Classic game, the game is lost, as on the arcade.
- **Sprite facings.** Walkers have 4 facings × 3 frames in the arcade. Ours:
  - the player has 4 facings;
  - the family and hulks only mirror left/right;
  - grunts, brains and tanks have one facing.
- **Gamepad remapping,** and selecting among several pads.

## Reconstructions and historical uncertainty

Every item below is labelled **R** in [FIDELITY.md](FIDELITY.md) and needs a MAME run with a legally owned ROM to settle:

- **Wave transitions:** the length and look of the inter-wave marquee (72 frames), the 16-frame zoom-in per object, the transporter effect on brain waves, and the explosion shapes.
- **Flicker:** the amount and pattern under load. Ours skips a rotating subset of objects once more than 72 are drawn.
- **Sound:** every timbre. Priorities come from the source where it gives them; the sounds themselves are designed by ear.
- **Collision resolution:** 1 px rather than the arcade's 2 px horizontal resolution, using our own sprite masks.
- **Walls:** sphereoids, enforcers, sparks and quarks bounce off walls. The tank bank-shot geometry is a reconstruction.
- **Tanks after death:** tanks fold back into quarks after a death by the same rule the arcade uses for enforcers → sphereoids.
- **Unit interpretations:** several jitter values (±16 bytes vs lines, ±6) and the exact `RMAX` edge case.
- **Behaviours reproduced from the source but unconfirmed in play:**
  - the tank-shell counter is lowered only when a shell is shot, so tanks may stop firing late in a life;
  - live progs don't block the end of a wave.
- **Random sequences** are not the arcade's, so a given layout will never match a given arcade game.
- **Conflicting source values:** the default difficulty (3 chosen) and the Enforcer/Tank/missile point values (source chosen over 1982 magazines).

## Legal and distribution

- **Not cleared for distribution:** see the caveats in [THIRD_PARTY.md](THIRD_PARTY.md). In short, the product is now named AVATron: 2026, but that name hasn't been checked against the original trademark, and the wave numbers derive from analysis of a copyrighted program.
- **Avalonia build telemetry** is on unless you set `AVALONIA_TELEMETRY_OPTOUT=1`.

## Minor

- **Reduced Motion** calms colour cycling and effect sizes but keeps the death flash. It is short, but not removed.
- **Pause stops sound.** Pausing stops currently playing effects; they don't resume.
- **Fullscreen on Wayland:** it uses the compositor's fullscreen through XWayland. With a tiling window manager the window may be tiled until fullscreen is toggled.
