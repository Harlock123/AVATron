# Fidelity baseline and matrix

## Reference version

- **Baseline:** MAME ROM set **`robotron`**, "Release 5, solid blue label" (Williams / Vid Kidz, 1982).
- **Primary evidence:**
  - The Jarvis/DeMar 6809 source. Its `mwenge` build md5-checks against those ROMs.
  - An independent ROM disassembly.
  - The MAME driver.
- **Home ports are not used for behaviour.** Ports differ: the 7800/C64 have 5 lives, C64/Apple II use a single stick or the keyboard, the Lynx has music, and Apple II uses different point values. Port-only behaviour is listed in RESEARCH.md only so it can be avoided.

**Evidence labels** used below (details and citations in RESEARCH.md and `docs/research/`):
- **V**: verified in the source.
- **C**: corroborated by two independent sources.
- **R**: reconstruction assumption, documented here.
- **M**: modern addition.

**Numeric compatibility:**
- Positions are 8.8 fixed point in raw arcade pixels and lines. One arcade byte = 2 px = 512 units, so the original's byte/fraction arithmetic (e.g. velocity damping `v/64`, grunt slow-down `×0xE0/256`) maps exactly to integers.
- There are no floats in the simulation. The one exception is Modern analog shot direction, which is rounded once when the shot is created.
- The random number generator is our own xorshift. **Random sequences are not bit-identical to the arcade**, so layouts and AI choices are statistically, not literally, faithful.

## Fidelity matrix

| System | Original behaviour | Ev. | Implementation | Classic | Modern | Verified by | Uncertainty |
|---|---|---|---|---|---|---|---|
| Frame rate | Logic and video at 60.096 Hz; processes sleep in whole frames | C | `Arena.FrameRate`; host steps a fixed-tick engine with an accumulator | 60.096 Hz | × game-speed setting (50–100%), shown on the HUD | `Simulation_runs_at_arcade_rate_from_real_time` | — |
| Playfield | px 14–287 × lines 24–234, hard walls (no wrap); border px 12–289 × 22–236 | V | `Arena` constants; clamps per axis | same | same | `Player_stops_at_every_edge_and_corner` (8 directions) | — |
| Display | 292×240 visible area of 304×256, 4:3 monitor | C | 292×240 framebuffer; 4:3 or square pixels; integer scaling; HiDPI-aware | nearest-neighbour | optional smooth scaling | `Destination_is_centred_and_keeps_aspect`, screenshots | — |
| Move stick | 8-way digital; 1 px and/or 1 line per frame; opposites cancel | C | `StickQuantizer`, `Player.Move` | analog pads are quantised to 8 directions | analog magnitude and direction, with the same per-axis maximum speed | quantiser and movement tests | — |
| Fire stick | 8-way; shot 2 frames after the direction settles, then every 8 frames; at most 4 shots; nothing on neutral or opposites; 6 px/frame; one kill per shot; passes through family | V | `Player.UpdateFire`, `PlayerShot` | 8 directions | any angle (larger axis = 6 px/frame); the cadence resets only when the stick returns to neutral | `Fire_cadence_…`, `Shot_limit_…`, `Shots_move_6…`, `Shot_kills_one…`, `Shots_pass_through_family` | — |
| Collision | bounding box, then image compare at 2-px horizontal resolution; "fat" stand-in images for shots vs sphereoid, quark and missile | V | `Sprite.Overlaps` at **1-px** resolution on our own sprite masks; solid stand-ins for the same three | same | same | `Mask_collision_respects_transparent_pixels` | R: 1-px rather than 2-px resolution, and our art differs from the original images |
| Player-test order | robots → electrodes → motion objects (fatal) → family (rescue) | V | `World.CheckPlayer` | same | same | rescue and death tests | — |
| Lives | 3 men per game; extra man every 25,000; up to 7 icons drawn | V | `GameRules`, `GameSession.AddPoints` | operator-adjustable (1–20 men; 0–50,000) | same, plus a "+n" count beyond 7 icons | `Extra_man_every_25000`, `Game_starts_with_two_in_reserve…` | — |
| Death | everything freezes; player flashes 80 frames then fades 28; same wave restarts with survivors at new positions; enforcers fold to sphereoids (n/4, at least 1, capped); progs and projectiles lost; grunt speed kept | V | `GameSession` (PlayerDying → BeginWave) | same | same | `Death_restarts_…`, `Enforcers_fold_back…` | R: tanks are folded into quarks by the same rule |
| Spawn safety | no grunts, hulks, tanks or electrodes in a box around the start, shrinking over waves 1–10; no post-spawn invincibility | V | `WaveBuilder.SafetyBox` | same | same | `Wave_placement_keeps…` | R: electrode extra margin of +4 |
| Wave table | 40 rows × (9 counts + 12 parameters); >40 subtract 20 | C | `waves.json` (validated) + `WaveTable` | same | same | golden-count tests, loop test, malformed-data tests | — |
| Difficulty | operator 0–10; ±5.5% per step; easy cancelled from wave 14, or from wave 5 with ≥3 reserve; Bozo assist waves 1–4 on the last man | V | `WaveTable.Effective/IsBozo` | factory **3** (adjustable) | same | `Difficulty_5_is_the_raw_table…`, `Bozo_…` | default conflict (see RESEARCH) |
| Wave end | Grunts + Sphereoids + Enforcers + Brains + Tanks + Quarks = 0, checked every 15 frames | V | `World.Exec/BlockingCount` | same | same | `Wave_clears_when_only…` | progs not counted (as written in the source) |
| Grunt | manager runs every 4 frames; each grunt steps after a random 1..ROBSPD passes; 4 px + 4 lines toward the player; ×7/8 speed-up per kill; time speed-up every 225 frames (doubled when the player is stalling); dies with an electrode it touches (+100) | V | `Grunt`, `World.UpdateGrunts/Exec` | same | same | grunt tests | — |
| Hulk | 4-way; step every HLKSPD frames (3/4 px or 2 lines); about 75% hunt family, otherwise the player; pushed by shots; kills family and electrodes | V | `Hulk` | same | same | `Hulk_is_invulnerable…`, `Hulk_moves_only_orthogonally…` | R: jitter units |
| Brain | nearest human by Manhattan distance; step every BRNSPD frames (2 px + 1 line); 80-frame reprogramming; cruise missiles (max 8; 2 px/1 line twice per 2 frames; re-aim; wall bounce) | V | `Brain`, `CruiseMissile` | same | same | `Brain_reprograms…`, `Killing_brain_mid_conversion…` | R: exact placement of a captured human |
| Prog | every 3 frames, 4 px or 4 lines toward the player plus an offset; afterimage trail | V | `Prog` | same | same | prog size/creation test | R: offset range |
| Sphereoid / Enforcer | edge spawn; random acceleration and damping; 1–5 (1–6) enforcers, no drop while 8 are alive; leaves when spent; enforcer grows in for 40 frames, glides, fires sparks (max 20) | V | `Sphereoid`, `Enforcer`, `Spark` | same | same | `Sphereoid_drops…`, `Enforcer_count_never_exceeds_eight` | R: wall bounce for sphereoids, enforcers and sparks; enforcer harmless while growing |
| Quark / Tank | top/bottom spawn; drops tanks (max 20 alive); tank grows in for 48 frames, crawls, fires direct or bank shells that bounce; shell counter lowered only when a shell is shot | V | `Quark`, `Tank`, `Shell` | same | same | `Quark_drops_tanks…` | R: bank-shot geometry (mirror target); counter behaviour unconfirmed in play |
| Electrode | static; fatal; shot for 0; ten shapes cycling by wave | V | `Electrode`, `ElectrodeShapeForWave` | same | same | electrode tests | art is original |
| Family | step every 8 frames, 8 directions, re-pick every 1–128 steps or at walls/electrodes; rescue 1000…5000 cap; multiplier resets on death or wave | V | `Human`, `World.CheckPlayer` | same | same | rescue tests | — |
| Scoring | values in RESEARCH.md | V | `EntityScores` | same | same | `Point_values_match…` | magazine values conflict |
| Wave intro | one robot materialises per frame; every 4th horizontally; brain waves use a "transporter" effect plus a 150-frame wait; the player appears last; AI start delays (grunts 10, brains 12, hulks 8, tanks 15 frames) | V/R | `GameSession`, `FrameSnapshot` | same | same | screenshots | R: 16-frame zoom per object, transporter look |
| Wave clear | "marquee" rectangle effect | V/R | `GameRenderer.DrawMarquee` | 72 frames | same | screenshots | R: length and look |
| High scores | 1 champion + 36 all-time + "Today's Greatest" 10; 3 initials entered with the move stick; 5 entries per player | V | top-10 table per preset per slot; initials typed or chosen with the stick | top 10 | top 10 | high-score tests | **Deliberate deviation** (project brief) |
| Palette | 16 slots of BBGGGRRR via a resistor DAC; slot cycling; border colour changes every wave | V | `WilliamsPalette` (MAME levels; our own colour picks and cycles) | cycling | same (slower with Reduced Motion) | `Palette_decodes…`, `Border_is_drawn…` | R: specific colour sequences |
| Flicker | hardware draws during IRQs; objects flicker under load | SECONDARY | when items exceed a budget (72), a rotating subset is skipped each frame | **on** | suppressed (setting) | `Flicker_only_happens_over_budget` | R: budget value and pattern |
| Sound | mono; one sound at a time by priority (equal or higher interrupts); no music or speech | V | `AudioMixer` SingleChannelPriority; priorities from source where given; synthesised timbres | single channel | polyphonic (12 voices, voice stealing) | mixer tests; output recorded | R: every timbre |
| Ambient | none in the original (the "march" is the grunt-step sound) | V | optional hum | off | optional (off by default) **M** | — | — |
| Pause / suspend | none on the arcade | — | pause menu; suspend = checkpoint plus input replay | pause only (no suspend) | pause and suspend/resume; window focus loss pauses **M** | suspend tests | — |
| Wave progress | none | — | HUD meter **M** | off | optional | render test | — |
| Start wave (practice) | none: games always began at wave 1 | — | title-menu choice 1–99 **M**; the chosen wave plays exactly as it would if reached normally (score 0, full reserve, that wave's effective parameters) | available; kept out of high scores | same | `StartWaveTests` | — |

## Classic vs Modern at a glance

| | Classic | Modern |
|---|---|---|
| Rules, waves, scoring, enemy AI | arcade | **identical** |
| Move / fire input | 8-way quantised | analog |
| Flicker | approximated | suppressed (toggle) |
| Scaling | nearest-neighbour | nearest or smooth |
| Game speed | 100% | 50–100% (labelled on the HUD) |
| Audio | single channel, priority | 12-voice mix |
| Suspend save | no (high scores only) | yes, single-use |
| Pausing on focus loss | no (manual pause available) | yes |
| High-score table | separate Classic table | separate Modern table |

## Disclosed accommodations

- **Pause** is available in Classic. It does not change rules or timing; the arcade had none.
- **Reduced Motion** (both presets) slows colour cycling and shortens the flash and spread effects. It is visual only.
- **Operator settings** (difficulty, men per game, extra-man score) mirror the arcade's adjustment screen. Changing them changes difficulty exactly as the operator settings did. The defaults are the factory values.
- **Game speed below 100%** (Modern only) slows the whole simulation uniformly. The HUD shows `SPEED n%` whenever it is not 100%.
