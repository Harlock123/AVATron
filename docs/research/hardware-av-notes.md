# Robotron: 2084 (MAME "robotron"): hardware, audiovisual, operator, history, ports and reuse licensing

Research date: 2026-10-07. Scope: hardware, A/V, operator settings, history, ports, reuse licensing. Wave tables and enemy AI are out of scope because another agent covers them.

Status labels:
- **VERIFIED**: I read it in a primary or authoritative artifact, such as MAME source, original 1982 source, the 1982 operator manual, or nuget/GitHub metadata.
- **CORROBORATED**: two or more independent sources agree.
- **SECONDARY**: a single secondary source.
- **UNKNOWN**: I couldn't determine it, or the sources conflict.

> IP caution: the original Robotron source code (historicalsource/robotron) and the ROMs are © 1982 Williams Electronics. Every string in the source carries "COPYRIGHT 1982 WILLIAMS ELECTRONICS INC.", and the repo has no license. This report uses them only to establish facts such as sizes, timings and parameter meanings. Don't copy code, image data, sound ROM data or text verbatim into the project. Draw all graphics and synthesize all sounds independently. "Robotron" is a trademark (Williams/WMS, later Warner Bros. via Midway), so pick a different product name.

---

## Source ledger

| # | Title | Author/Org | URL | Accessed | Type | Platform/version | What I inspected | Claims supported | Confidence | Licensing |
|---|---|---|---|---|---|---|---|---|---|---|
| S1 | williams.cpp (Williams 6809 driver) | MAME team (Aaron Giles et al.) | https://github.com/mamedev/mame/blob/master/src/mame/williams/williams.cpp | 2026-10-07 | Emulator source | master; last commit touching file 774a180 (2026-07-24) | Header memory map, Robotron PIA bit map, `INPUT_PORTS_START(robotron)`, `williams_base` / `williams_b1` configs, ROM_START(robotron…), GAME() lines | CPU and clocks, screen raw params, sound HW, inputs, SC1 blitter, ROM sets, ROT0 | High | BSD-3-Clause (file header). Note the driver moved from `src/mame/midway/` to **`src/mame/williams/`** |
| S2 | williams_v.cpp | MAME | https://github.com/mamedev/mame/blob/master/src/mame/williams/williams_v.cpp | 2026-10-07 | Emulator source | master | Header blitter description (incl. Sean Riddle text), `screen_update`, `palette_init`, `video_counter_r` | Pixel format, palette resistor network, SC1 XOR bug | High | BSD-3-Clause |
| S3 | williamsblitter.cpp / .h | MAME | https://github.com/mamedev/mame/blob/master/src/mame/williams/williamsblitter.cpp | 2026-10-07 | Emulator source | master | `control_w`, `set_size_xor(4)` for SC1 and `(0)` for SC2 | Blitter XOR-4 bug | High | BSD-3-Clause |
| S4 | williams_m.cpp | MAME | https://github.com/mamedev/mame/blob/master/src/mame/williams/williams_m.cpp | 2026-10-07 | Emulator source | master | `va11_callback`, `count240_callback`, `snd_cmd_w`, `cmos_4bit_w`, watchdog | IRQ timing, sound latch, 4-bit CMOS | High | BSD-3-Clause |
| S5 | ioport.cpp | MAME | https://github.com/mamedev/mame/blob/master/src/emu/ioport.cpp | 2026-10-07 | Emulator source | master | joystick `frame_update` (opposing-direction lockout), field read mask by `m_way` | MAME handling of opposing directions | High | BSD-3-Clause |
| S6 | resnet.cpp / resnet.h | MAME | https://github.com/mamedev/mame/blob/master/src/emu/video/resnet.cpp | 2026-10-07 | Emulator source | master | `compute_resistor_weights`, `combine_weights` | Exact palette level computation (I reran the math) | High | BSD-3-Clause |
| S7 | Robotron original source (RR*.ASM) | Vid Kidz / Williams (uploaded by historicalsource) | https://github.com/historicalsource/robotron | 2026-10-07 | Original 6809 assembly | commit da11ac0 (2021-01-08). Appears to be a dev snapshot, not exactly Release 5 | RRTESTB (CMOS defaults, adjustment ranges), RRET (operator/attract text), RRS22 (sound sequencer, color tables), RRG23 / RRH11 / RRC11 / RRTK4 / RRB10 / RRP8 (sound tables, image W/H), RRF (play-field bounds, HS table), RRTEXT (fonts), RRSCRIPT (story text) | Sound events, palette cycling, sprite sizes, play-field bounds, HS table, fonts, adjustment ranges | High for this snapshot. Release 5 may differ | **No license. © 1982 Williams.** Reference only |
| S8 | Robotron Instruction Manual 16P-3005-101, Mar 82 | Williams Electronics | https://archive.org/details/robotron_instruction_manual_16p-3005-101_mar_82_ptm | 2026-10-07 | Operator manual (OCR djvu text) | March 1982 | Full OCR text | Defaults: 3 turns, 25,000 extra man, difficulty 5, 3 to 20 letters, adjustments list, initials entry, diagnostics | High (OCR is noisy in places) | © Williams. Facts only |
| S9 | Robotron: 2084 (Wikipedia) | Wikipedia | https://en.wikipedia.org/wiki/Robotron:_2084 (raw wikitext via action=raw) | 2026-10-07 | Encyclopedia | current revision | Development, Hardware, Release, Ports, Reception sections | History, ~19,000 cabinets, ports list, press | Medium (secondary) | CC BY-SA |
| S10 | The Development of Robotron | Tony Temple, The Arcade Blogger (2020-06-27) | https://arcadeblogger.com/2020/06/27/the-development-of-robotron/ | 2026-10-07 | Secondary article with first-person quotes | n/a | Jarvis and DeMar quotes, dev timeline, ~23,000 uprights | Twin-stick origin quote, 6-month schedule | Medium | © author |
| S11 | The History of Robotron: 2084 – Running Away While Defending Humanoids | Game Developer (Gamasutra). The fetch didn't confirm author or date | https://www.gamedeveloper.com/design/the-history-of-robotron-2084---running-away-while-defending-humanoids | 2026-10-07 | Secondary article with first-person Jarvis quotes | n/a | Jarvis quotes | Electrode-only prototype, "dialed up the robot count", home-port quote | Medium | © publisher |
| S12 | Eugeneology: An Interview with Eugene Jarvis | Brandon Sheffield, Game Developer, 2007-05-18 | https://www.gamedeveloper.com/production/eugeneology-an-interview-with-eugene-jarvis | 2026-10-07 | Primary interview | n/a | Whole article | Only passing Robotron mentions (control scheme is an "industry standard") | High (but thin) | © publisher |
| S13 | Classic Game Postmortem: Robotron 2084 (GDC 2014) | Eugene Jarvis / GDC | https://gdcvault.com/play/1020992/Classic-Game-Postmortem-Robotron (free tag); https://gdcvault.com/play/1020591/… (members) | 2026-10-07 | Primary talk (video) | 2014 | Landing page only. **I couldn't watch the video or find a transcript** | That it exists and covers "hardware, software, enemy dynamics and sound synthesis" | n/a | © GDC |
| S14 | Conquering Robotron 2084 | Videogaming Illustrated #3, Dec 1982 (vgpavilion) | http://vgpavilion.com/mags/1982/12/vi/conquering-robotron-2084/ | 2026-10-07 | Contemporary press | 1982 | Point values and strategy | Contemporary point list (conflicts with attract text, see B) | Medium | © publisher |
| S15 | Mastering Robotron | Owen Linzmayer, Creative Computing Video & Arcade Games v1n1, Spring 1983, p.21 | https://www.atarimagazines.com/cva/v1n1/masteringrobotron.php | 2026-10-07 | Contemporary press | 1983 | Rules, values | 3 lives, 25,000 extra man, 8-way sticks | Medium | © publisher |
| S16 | KLOV / Museum of the Game: Robotron 2084 | arcade-museum.com | https://www.arcade-museum.com/Videogame/robotron-2084 | 2026-10-07 | Database | n/a | ROM-label notes, controls | Yellow label default difficulty 5 vs blue label 3, 8-way sticks, mono | Medium-low (unsourced) | © KLOV |
| S17 | Robotron (sound) | Chris Lomont | https://lomont.org/software/misc/robotron | 2026-10-07 | Technical article | n/a | Sound board description | 6808 sound CPU, 6-bit command bus, no speech | Medium | His re-created code is his own (not Williams). The page gives no license terms |
| S18 | joe07734/robotron "sounds.txt" | joe07734 (GitHub) | https://github.com/joe07734/robotron | 2026-10-07 | Community notes | HEAD | sounds.txt (sound command listening notes) | Rough sound-ROM command descriptions | Low-medium (mixes Defender/Joust notes) | No license |
| S19 | Atari 7800 manual, C64 (Atarisoft) manual, Apple II (Atarisoft) manual | Atari / Atarisoft | https://archive.org/details/Robotron_2084_1987_Atari ; https://archive.org/details/Robotron_2084_1983_Atari (the item says 5200, but the **content is the C64 Atarisoft manual**) ; https://archive.org/details/AtarisoftRobotron2084 (Apple II) | 2026-10-07 | Port manuals (OCR) | 1983–87 | OCR text | Port differences | High for what they state | © Atari |
| S20 | Robotron 2084 Lynx manual | Shadowsoft / Atari, 1991 | https://api.regvault.org/api/v1/game/lynx/60c1dfbf112bbb2a49cf16eddb191842/manual | 2026-10-07 | Port manual (PDF pp.1–10 viewed) | 1991 | Controls and options pages | Lynx control types, title music | High | © Shadowsoft |
| S21 | NuGet registration API | nuget.org | https://api.nuget.org/v3/registration5-gz-semver2/{id}/index.json and flatcontainer nupkgs | 2026-10-07 | Package metadata | see section G | Latest versions, publish dates, licenses, `runtimes/` folders in nupkgs | .NET library choices | High | n/a |
| S22 | GitHub repo metadata and LICENSE files | GitHub API (`gh api`) | various (section G) | 2026-10-07 | Repo metadata | HEAD | LICENSE contents, trees | Reimplementation licensing, asset red flags | High | n/a |
| S23 | BASS licensing | un4seen | https://www.un4seen.com/bass.html | 2026-10-07 | Vendor page | n/a | Licence section | ManagedBass depends on non-free BASS | High | n/a |
| S24 | Avalonia issues #6945, #8792 | AvaloniaUI | https://github.com/AvaloniaUI/Avalonia/issues/6945 , https://github.com/AvaloniaUI/Avalonia/issues/8792 | 2026-10-07 | Issue tracker | Avalonia 12.1.3 current | Issue state and maintainer comment | No built-in gamepad API | High |  |
| S25 | OpenGameArt packs | phoenix1291; MouthlessGames | https://opengameart.org/node/79699 ; https://opengameart.org/content/8-bit-retro-sfx | 2026-10-07 | Asset pages (search-summary / fetch) | n/a | License fields | CC0 / CC-BY SFX options | Medium |  |

Sources I couldn't access:
- easyemu.mameworld.info/images/robotron.pdf: DNS failure, and no Wayback snapshot. I used the IA copy of the same manual (S8) instead.
- exotica "Lost in Translation" Robotron page: blocked by a bot check.
- classicarcadegaming.com forum: 403.
- gdconf.com news article: 404.
- GDC 2014 video: no transcript, so not viewed.
- I found no online scans of RePlay, Play Meter or Cash Box Robotron coverage. Wikipedia cites Cash Box 1983-05-14 p.42 (https://retrocdn.net/images/0/0e/CashBox_US_1983-05-14.pdf), but I didn't open it.

---

## A. Hardware / MAME

### A1. Driver location
**VERIFIED (S1).** In current mamedev/mame the files are `src/mame/williams/williams.cpp`, `williams_v.cpp`, `williams_m.cpp`, `williams.h`, `williamsblitter.cpp/.h`. They are no longer under `midway/`, and there is no `williams_a.cpp`. Sound uses the generic `williams_state::williams_base` config in williams.cpp. `src/mame/shared/williamssound.cpp` is for later boards and doesn't apply to Robotron.

### A2. CPU and clocks
**VERIFIED (S1).**
```cpp
static constexpr XTAL MASTER_CLOCK = (XTAL(12'000'000));
static constexpr XTAL SOUND_CLOCK = (XTAL(3'579'545));
...
MC6809E(config, m_maincpu, MASTER_CLOCK/3/4);          // = 1.000 MHz
M6808(config, m_soundcpu, SOUND_CLOCK); // internal clock divider of 4, effective frequency is 894.886kHz
```
- Main CPU: MC6809E at 1 MHz.
- Sound CPU: M6808 at 3.579545 MHz / 4 = 894.886 kHz.
- Robotron uses the machine config `williams_b1` = `williams_base` + `WILLIAMS_BLITTER_SC1(config, m_blitter, 0xc000, …)`.
- GAME line:
  `GAME( 1982, robotron, 0, williams_b1, robotron, williams_state, empty_init, ROT0, "Williams / Vid Kidz", "Robotron: 2084 (Release 5, solid blue label)", MACHINE_SUPPORTS_SAVE )`

### A3. Screen
**VERIFIED (S1).**
```cpp
m_screen->set_video_attributes(VIDEO_UPDATE_SCANLINE | VIDEO_ALWAYS_UPDATE);
m_screen->set_raw(MASTER_CLOCK*2/3, 512, 6, 298, 260, 7, 247);
```
Derived values (my arithmetic):

| Parameter | Value |
|---|---|
| Pixel clock | 12 MHz × 2/3 = **8 MHz** |
| htotal / vtotal | **512 / 260** |
| Visible X | 6..297, so **292 px** wide |
| Visible Y | 7..246, so **240 lines** |
| Refresh | 8,000,000 / (512 × 260) = **60.096 Hz**. Use ~60.1 Hz; 60 Hz is close enough for a remake, but if you need the exact cadence, step the simulation at 60.096 Hz |
| Orientation | **ROT0** (horizontal monitor). The original is a 19" raster CRT (S9) |

- Pixel aspect: 292 × 240 shown at 4:3 gives pixels about 1.10 : 1 (slightly wide). MAME displays the visible area at 4:3 by default.
- Frame buffer (S2): 4 bpp. Each byte holds two horizontal pixels (high nibble = left). Memory is column-major: "Pixels (2,0) and (3,0) come from the byte at offset 256." So the address is `x/2 * 256 + y`.
- Timing IRQs (S1/S4): a timer fires every 32 scanlines and drives VA11 into PIA CB1 (`m_pia[1]->cb1_w(BIT(scanline, 5))`). A "count240" line goes to CA1 at scanline ≥ 240. The game uses these for mid-frame and end-of-frame interrupts (about 4 IRQs per frame plus the 240 signal).
- Video counter read at $CB00 returns `vpos & 0xfc`.

### A4. Palette
**VERIFIED (S1, S2, S6).**
- 16 color registers at $C000–$C00F. The header comment says "16 bytes of BBGGGRRR". **The byte layout is BBGGGRRR, not RRRGGGBB.** Red is bits 0–2, green bits 3–5, blue bits 6–7.

```cpp
static constexpr int resistances_rg[3] = { 1200, 560, 330 };
static constexpr int resistances_b[2]  = { 560, 330 };
compute_resistor_weights(0, 255, -1.0, 3, resistances_rg, weights_r, 0, 0, 3, resistances_rg, weights_g, 0, 0, 2, resistances_b, weights_b, 0, 0);
int const r = combine_weights(weights_r, BIT(i, 0), BIT(i, 1), BIT(i, 2));
int const g = combine_weights(weights_g, BIT(i, 3), BIT(i, 4), BIT(i, 5));
int const b = combine_weights(weights_b, BIT(i, 6), BIT(i, 7));
```

I reran MAME's algorithm in Python (S6 logic, autoscale):

| Field | Weights | Levels by field value |
|---|---|---|
| R/G (3 bits, value 0..7) | ≈ 37.61, 80.60, 136.78 | **0, 38, 81, 118, 137, 174, 217, 255** |
| B (2 bits, value 0..3) | ≈ 94.55, 160.45 | **0, 95, 160, 255** |

- That gives 256 possible colors. Any 16 can be on screen at once, and the palette can change at any time; that's how color cycling works (see C).
- MAME's comment says the real circuit has transistor and pull-up effects it deliberately ignores. Treat these levels as "MAME-accurate", not "CRT-accurate".

### A5. Blitter (Special Chips)
**VERIFIED (S2, S3).**
- Robotron uses **SC1** (VTI VL2001, "5410-09866"). The manual's ROM summary lists "Special Chip 1 A-5410-09911".
- MAME quotes Sean Riddle: "SC1s have a small bug. When you tell the SC1 the size of the data to move, you have to exclusive-or the width and height with 4. The SC2s eliminate this bug."
- Implementation: `int w = m_width ^ m_size_xor; int h = m_height ^ m_size_xor;` with `set_size_xor(4)` for SC1 and `(0)` for SC2.
- Registers: CA00 control, CA01 mask, CA02-3 source, CA04-5 dest, CA06 width, CA07 height.
- Control bits: source/dest linear-vs-screen stride; bit 3 foreground-only (color 0 is transparent); bit 4 solid color; bit 5 shift right one pixel; bits 6/7 even/odd pixel only; plus a slow mode.
- Blits halt the CPU. Riddle measured about 910 KB/s.
- For a remake, this only matters as a reason to draw sprites with **color 0 transparent**, as **solid-color silhouettes** for flashes, and with **2-pixel horizontal granularity**. The XOR-4 bug has no gameplay effect to reproduce.

### A6. Sound hardware
**VERIFIED (S1, S4).**
```cpp
SPEAKER(config, "speaker").front_center();
MC1408(config, "dac", 0).add_route(ALL_OUTPUTS, "speaker", 0.25); // mc1408.ic6
m_pia[2]->writepa_handler().set("dac", FUNC(dac_byte_interface::data_w));
```
- The sound path is a 6808 writing an 8-bit DAC, mono. The sound ROM is a single 4 KB ROM: `video_sound_rom_3_std_767.ic12` (P/N A-5342-09910 in MAME; the manual lists "Video Sound ROM 3 A-5343-09910").
- Commands are 6 bits from PIA (`c80e rom_pia_datab bits 0-5 = 6 bits to sound board`). `snd_cmd_w` ORs in 0xC0 ("the high two bits are set externally").
- **No speech: VERIFIED.**
  - `williams_b1` (Robotron) has no HC55516.
  - HC55516 CVSD appears only in `sinistar_upright` / `sinistar_cockpit` configs and the Joust 2 board.
  - Lomont (S17) also says nothing about speech for Robotron and notes Sinistar added it.

### A7. Inputs
**VERIFIED (S1, S5).**
```cpp
static INPUT_PORTS_START( robotron )
	PORT_START("IN0")
	PORT_BIT( 0x01, IP_ACTIVE_HIGH, IPT_JOYSTICKLEFT_UP ) PORT_NAME("Move Up")
	PORT_BIT( 0x02, IP_ACTIVE_HIGH, IPT_JOYSTICKLEFT_DOWN ) PORT_NAME("Move Down")
	PORT_BIT( 0x04, IP_ACTIVE_HIGH, IPT_JOYSTICKLEFT_LEFT ) PORT_NAME("Move Left")
	PORT_BIT( 0x08, IP_ACTIVE_HIGH, IPT_JOYSTICKLEFT_RIGHT ) PORT_NAME("Move Right")
	PORT_BIT( 0x10, IP_ACTIVE_HIGH, IPT_START1 )
	PORT_BIT( 0x20, IP_ACTIVE_HIGH, IPT_START2 )
	PORT_BIT( 0x40, IP_ACTIVE_HIGH, IPT_JOYSTICKRIGHT_UP ) PORT_NAME("Fire Up")
	PORT_BIT( 0x80, IP_ACTIVE_HIGH, IPT_JOYSTICKRIGHT_DOWN ) PORT_NAME("Fire Down")
	PORT_START("IN1")
	PORT_BIT( 0x01, IP_ACTIVE_HIGH, IPT_JOYSTICKRIGHT_LEFT ) PORT_NAME("Fire Left")
	PORT_BIT( 0x02, IP_ACTIVE_HIGH, IPT_JOYSTICKRIGHT_RIGHT ) PORT_NAME("Fire Right")
	PORT_START("IN2")  // Auto Up / Advance / Coin3 / High Score Reset / Coin1 / Coin2 / Tilt
```

- **Each stick is four digital switches, so 8 directions.**
  - The manual (S8) says: "MOVE JOYSTICK (LEFT) maneuvers … in any of eight directions: N-NE-E-SE-S-SW-W-NW. FIRE JOYSTICK (RIGHT) fires … in same eight directions."
  - KLOV (S16) lists "Joystick: 8-way" for both sticks.
  - Wikipedia says the production sticks were made by Wico.
  - **CORROBORATED.**
- **MAME's handling of opposing directions (VERIFIED, S5):** the Robotron fields have no `PORT_4WAY` / `PORT_8WAY`, so `m_way` is 0. That is treated as non-4-way, and MAME masks with `joystick->current()`. Each frame MAME does:
  ```cpp
  // lock out opposing directions (left + right or up + down)
  if ((m_current & (UP_BIT | DOWN_BIT)) == (UP_BIT | DOWN_BIT)) m_current &= ~(UP_BIT | DOWN_BIT);
  if ((m_current & (LEFT_BIT | RIGHT_BIT)) == (LEFT_BIT | RIGHT_BIT)) m_current &= ~(LEFT_BIT | RIGHT_BIT);
  ```
  So up+down (or left+right) at the same time becomes neutral on that axis, unless the user runs `-joystick_contradictory`. A physical leaf-switch 8-way stick can't close both opposite switches anyway. **Recommendation:** apply the same cancel-to-neutral rule to keyboard and analog input. For analog sticks, quantize to 8 directions with a deadzone.
- **No DIP switches.** All settings live in battery-backed **1K×4 CMOS** at $CC00–$CFFF. `cmos_4bit_w` stores `data | 0xf0`, so only 4 bits are valid. MAME's NVRAM defaults to all zeros, which on first boot triggers "factory settings restored".
- Coin-door switches: Auto-Up/Manual-Down, Advance, High Score Reset, and a slam tilt.

### A8. ROM sets in MAME
**VERIFIED (S1).**

| Set | Description |
|---|---|
| robotron | Release 5, solid blue label |
| robotronyo | Release 4, yellow/orange label |
| robotronr3 | Release 3, censored prototype "recovered from the Vid Kidz dev system" |
| robotronun | Unidesa license |
| robotron87 | 1987 "shot-in-the-corner" fix ("aka Release 6") |
| robotron12 | 2012 "wave 201 start" hack |
| robotrontd | 2015 tie-die V2 hack |

**Conflict:** KLOV (S16) says the blue label fixes the shot-in-the-corner bug. MAME attributes the fix to the 1987 patch. I'm treating MAME as authoritative, so this is **UNKNOWN/conflict**.

---

## B. Operator / instruction manual and game adjustments

### B1. Defaults from the 1982 manual (S8)
**VERIFIED** (16P-3005-101, March 1982).

- "pressing 1-player start initiates a 1-player, \*3-turn game." The \* means adjustable.
- "Arriving at 25,000\* points (or any multiple of 25,000), the mutant is awarded a new opportunity…"
- "The number of turns per game can be set anywhere from 1 to 20 (3 recommended). Difficulty is factory-programmed at 5 (moderate; recommended)."
- Game Adjustment screen items, in order:

| Item | Default shown in Figure 2 |
|---|---|
| EXTRA MAN EVERY | 25000 RECOMMENDED |
| TURNS PER PLAYER | 3 |
| PRICING SELECTION | 3 = 1/QUARTER 4/DOLLAR |
| LEFT / CENTER / RIGHT SLOT UNITS | 1 / 4 / 1 |
| UNITS REQUIRED FOR CREDIT | 1 |
| UNITS REQUIRED FOR BONUS CREDIT | 0 |
| MINIMUM UNITS FOR ANY CREDIT | 0 |
| FANCY ATTRACT MODE | YES |
| DIFFICULTY OF PLAY | 5 |
| LETTERS FOR HIGH SCORE NAME | 3 |
| RESTORE FACTORY SETTINGS | action |
| CLEAR BOOKKEEPING TOTALS | action |
| HIGH SCORE TABLE RESET | action |
| AUTO CYCLE | action |
| SET ATTRACT MODE MESSAGE | action |
| SET HIGH SCORE NAME | action |

- UI hint text: "USE -MOVE- LEVER TO SELECT ADJUSTMENT / USE -FIRE- LEVER TO CHANGE THE VALUE / PRESS ADVANCE TO EXIT".
- Highest-score name: "The number of letters allowed the highest scoring player for entering his name can be varied from 3 to 20 and is recommended as 3."
- **Initials entry:** "Select letters with the MOVE joystick. Push up to move forward through the alphabet; pull down to move backward. Then push the FIRE joystick up to lock in the letter."
- **Free play:** pricing selection code 9.
- **Corrupted HS entries** are replaced with "a score of 4,000 points and no initials."
- **Bookkeeping screen fields:** left/center/right slot coins, paid credits, extra men earned, play time in minutes, men played, credits played, average time per credit, average turns per credit.

### B2. Adjustment ranges and CMOS defaults from source (S7, RRTESTB.ASM)
**VERIFIED for the dev snapshot.**
- `ADJTBL` gives (min, max) in BCD:
  - Replay level 0..$50, shown with "000" appended, so 0–50,000. Zero shows "NO EXTRA MEN".
  - Ships per credit 1..$20.
  - Pricing 0..9.
  - Slot multipliers 0..99.
  - Free play / fancy attract 0..1.
  - **Master difficulty 0..$10 (0–10).**
  - **Letters in "God's" name 3..$20.**
- `DEFALT` in this snapshot: `$25` replay (= 25,000; the comment "REPLAY @10,000" is stale), `$3` ships, `$03` pricing, slots 1/4/1, **`$02` master difficulty**, `$03` letters.
- **Default difficulty conflict:**

| Source | Default difficulty |
|---|---|
| Manual (S8) | 5 |
| KLOV (S16): yellow label | 5 |
| KLOV (S16): blue label | 3 |
| Dev source (S7) | 2 |

  **UNKNOWN for Release 5.** You can resolve it empirically: boot MAME `robotron` with an empty nvram and open the adjustments screen. That needs a legally owned ROM; I didn't do it.

### B3. High score tables (S7)
**VERIFIED for the snapshot.**
- RRF.ASM:
  - `SCRSIZ EQU 14 NUMBER OF NIBBLES IN A SCORE ENTRY`: 3 initials plus an 8-digit BCD score, in 4-bit CMOS.
  - `GODINT RMB 6` / `GODSCR RMB 48`: the #1 "God" entry, whose name can be up to 20 letters.
  - `CMSCOR RMB 37*SCRSIZ   37 BACKED UP SCORES, 36 OF WHICH ARE VISIBLE`.
  - `TODAYS RMB 10*SCRSIZ     TODAYS GREATEST`.
- So the persistent all-time table is **1 champion + 36 visible entries**, and there's a separate **Today's Greatest list of 10**, laid out "5 PER COLUMN, 2 COLUMNS" (RRTABLE.ASM).
- Screen strings:
  - "ROBOTRON HEROES"
  - "ALL TIME" / "HEROES"
  - "YOU ARE THE GREATEST ROBOTRON HERO / ENTER YOUR NAME (UP TO n LETTERS)"
  - "YOU ARE A ROBOTRON HERO / ENTER YOUR INITIALS"
  - "5 ENTRIES MAXIMUM PER PLAYER / LOWEST ENTRY REPLACED". This limits each player's initials to 5 entries in the table.
- The default table has three-letter initials (e.g. 'BIL', 'DRJ' as the first default entries). The full default list is copyrighted data; don't copy it.
- KLOV says the default top score is 131,682 (yellow) or 151,782 (blue). SECONDARY.

### B4. Attract and instruction text (S7, RRET.ASM / RRSCRIPT.ASM)
**VERIFIED for the snapshot.** Paraphrase it rather than copying it.

- **Story:** in 2084 man perfects the Robotrons, robots so advanced that man is inferior to his own creation. Guided by infallible logic, they conclude the human race is inefficient and must be destroyed. You are the last hope of mankind, with superhuman powers (a genetic engineering error, per the port manuals). Your mission is to stop the Robotrons and save the last human family.
- **Enemy intro pages:**
  - GRUNT ("Ground Roving Unit Network Terminator")
  - HULK ("seek out and eliminate the last human family")
  - SPHEROIDS and QUARKS ("programmed to manufacture Enforcer and Tank Robotrons")
  - BRAIN Robotrons (reprogram humans into PROGS)
  - ELECTRODES ("be sure to avoid")
- **Point-value strings in the attract table:**
  - GRUNT – 100
  - SPHEREOID – 1000, QUARK – 1000
  - ENFORCER – 150, TANK – 200
  - BRAIN – 500, CRUISE MISSILE – 25
  - PROG – 100
  - Also "INDESTRUCTABLE HULK" (sic), MOMMY / DADDY / MIKEY, "SAVE THE LAST HUMAN FAMILY", and "ROBOTRON: 2084".
- **Press point values conflict:**
  - Videogaming Illustrated Dec 1982 (S14): Spark 25, Shell 50, Cruise Missile 75, Enforcer 200, Tank 300.
  - Creative Computing 1983 (S15): Missile 75, Shell 50, Spark 25, Enforcer 200, Tank 300.
  - The attract text in S7 says Enforcer 150, Tank 200, Cruise Missile 25.
  - **UNKNOWN which is right for Release 5.** The gameplay agent should resolve this from the score routines or a MAME run.
- **Human rescue scoring:** 1,000, 2,000, 3,000, 4,000, then 5,000 per wave. CORROBORATED by S14, S15 and the port manuals; the RRH11 bonus images are P1000..P5000.
- Other screen strings: "DESIGNED BY VID KIDZ", "GAME OVER", "n WAVE" (wave counter), "PLAYER n", "CREDITS".

---

## C. Visuals

### C1. Play field
**VERIFIED (S7, RRF.ASM).**
- Bounds: `XMIN EQU 7`, `XMAX EQU $8F`, `YMIN EQU 24`, `YMAX EQU 234`.
- X is in **bytes** (2 px each), so the arena spans about pixels 14–287 by lines 24–234.
- `BORDER` draws a rectangle one byte or line outside those bounds in `WALCOL`.
- The score and lives row sits above line 24.

### C2. Sprite sizes
**VERIFIED (S7)** from image descriptors (`FCB W,H`, W in bytes, so pixels = 2 × W). These are bounding boxes including padding. Dev-snapshot values.

| Entity (label) | W bytes × H | ≈ pixels |
|---|---|---|
| Player (MANLP/MANRP/MANUP/MANDP 1–3) | 4×12 | 8×12 |
| Player lives icon (MNPIC) | 3×8 | 6×8 |
| Player laser (ULPIC / LLPIC / DLLPIC / ULLPIC) | 1×6, 3×1, 3×6 diag | vertical 2×6, horizontal 6×1, diagonal 6×6 |
| Grunt (RWDP1–4) | 5×13 | 10×13 |
| Hulk (HLK*P1–3) | 7×16 | 14×16 |
| Brain (BR*P1–3) | 7×16 | 14×16 |
| Prog (PGXPIC) | 6×16 | 12×16 |
| Cruise missile (CMPIC) | 3×4 | 6×4 |
| Spheroid (CIRP0–7) | 8×15 | 16×15 |
| Enforcer (ENFP0, ENGP1–5 growth) | 5×11 | 10×11 |
| Enforcer spark (SPKP0–3) | 4×7 | 8×7 |
| Quark (SQP0–8) | 8×15 | 16×15 |
| Tank (TNKP1–4; MTNKP1–4 growth 2×4 → 6×12) | 7×16 | 14×16 |
| Tank shell (SHLP1) | 4×7 | 8×7 |
| Electrode / "post" (PSP*, 3 frames × 6 shapes) | 5×9 (one shape 3×9) | 10×9 |
| Mommy (MLP/MRP/MDP/MUP 1–3) | 4×14 | 8×14 |
| Daddy (DLP…) | 5×13 | 10×13 |
| Mikey (KIDLP…) | 3×11 | 6×11 |
| Human-death skull (SKULP) | 6×11 | 12×11 |
| Rescue bonus "1000…5000" (P1000–P5000) | 6×5 | 12×5 |

Walkers have 4 facings × 3 frames.

### C3. Palette assignment and color cycling
**VERIFIED (S7, RRS22.ASM).**

- **Initial 16-entry pseudo color RAM (`CRTAB`), with source comments:**

| Index | Value | Source comment |
|---|---|---|
| 0 | $00 | (blank) |
| 1 | $07 | RED |
| 2 | $17 | ORANGE |
| 3 | $C7 | PURPLE |
| 4 | $1F | BROWN |
| 5 | $3F | YELLOW |
| 6 | $38 | GREEN BRT. |
| 7 | $C0 | BLUE |
| 8 | $A4 | GRAY |
| 9 | $FF | WHITE |
| A | $38 | LASER FLASH |
| B | $17 | RGB |
| C | $CC | DECAY |
| D | $81 | LASER |
| E | $81 | BLU PURP RED |
| F | $07 | RED-GOLD |

  Decode each value with BBGGGRRR and the A4 levels (e.g. $07 = R 255, G 0, B 0).

- **Palette slots B–F are animated by table-driven processes.** `TABDRI` steps through a 0-terminated byte list, writes the entry to PCRAM+slot, then sleeps N frames:

| Process | Slot | Sleep | Table |
|---|---|---|---|
| RGB | $B | 8 frames | $38, $07, $C0: green, red, blue cycle |
| DECAY | $C | 2 frames | $C0 → … → white-ish → … → 0 (fade) |
| BPR "BLU-PURPLE RED" | $E | 1 frame | 26-step ramp |
| RGOLD | $F | 6 frames | $07, $07, $2F |
| LASER | $D | 2 frames | 37-entry `COLTAB` rainbow |

- **Laser flash (slot A):** set to $FF (white) for 2 frames, then a random COLTAB entry for 6 frames, repeat.
- **Hardware color cycling is per palette slot, so every sprite using a slot changes color in sync.** A remake should do the same with palette indices and a shader or LUT, not per-sprite tints. alldritt/Robotron's notes describe exactly the desync problem you get otherwise.
- **Per-wave tables (RRG23.ASM, 10 entries each, indexed by wave mod 10):**
  - `WCTAB` border/wall color: $22, $55, $11, $EE, $77, $33, $44, $88, $00, $CC
  - Electrode ("post") color
  - Electrode image set
  - Laser color
  - These are nibble-pair color indices (e.g. $22 = index 2 in both pixels).
  - So the border color changes every wave. Wave transitions run `RMST` "marquee" effects and redraw the border.
- **Text colors** use the same index pairs ($AA, $BB, $99, …).
- **Special effects:**
  - Appear/explode routines (RRX7: "START AN APPEAR", "HORIZ+VERT", "DUAL DIAGONALS", horizontal appear/explode) make objects materialize and explode as line-spread versions of their image.
  - RRM1 `MARQ` draws "LINKY MARQUIS" rectangles in outer/inner colors for the wave-start transition.
  - The press noted the wave-completion effect and shimmering Brain-wave colors (S9: Sharpe in Play Meter, and JoyStik).

### C4. Fonts
**VERIFIED (S7, RRTEXT.ASM).** Two fonts:
- `LFONT EQU 7`: "SET NEW FONT HEIGHT TO SEVEN PIXEL"
- `SFONT EQU 8`: "SET NEW FONT HEIGHT TO FIVE PIXEL"

So there's a large 7-px-high font and a small 5-px-high font. The glyph widths are about 5 and 3 px, inferred and **UNKNOWN exactly**.

No permissively licensed font that specifically reproduces the Williams font turned up. Generic CC-BY options exist ("AW Cathode", "Arcade Cabinet" 9×7 on itch.io), but I didn't check their license files. **Recommendation:** hand-draw an original 5×7 and 3×5 bitmap font. That's trivial and avoids licensing questions.

### C5. Attract sequence
**VERIFIED as content, order SECONDARY.** The attract mode includes:
- logo and title ("ROBOTRON: 2084", "SAVE THE LAST HUMAN FAMILY")
- the story script
- enemy introduction pages with point values
- the ROBOTRON HEROES high-score tables
- the operator message ("PRESENTED BY WILLIAMS ELECTRONICS INC" by default)
- a demo game (when FANCY ATTRACT MODE is YES)
- credits and "DESIGNED BY VID KIDZ"

---

## D. Audio

- **No music and no speech.** Sound is one mono channel managed by a priority sequencer (RRS22 `SNDLDV` / `SNDSEQ`). VERIFIED (S1, S7). Wikipedia also says "uses a priority scheme to determine which sounds to play on a single channel".
- **Sequence format:** "SNDPRI,N\*(REPCNT,SNDTMR(16MSEC),SND#);END:REPCNT=0".
  - A request with priority lower than the current sound is dropped.
  - The timer is in frames, about 16 ms each.
  - `SNDOUT` writes $3F (reset) and then the **complement** of the sound number masked to 6 bits.

### D1. Game-side sound events
**VERIFIED (S7).** Format: priority / (repeat, frames, sound#) steps.

| Event | Label | Sequence |
|---|---|---|
| Player laser | LASSND | $D0 / (1,8,#1) |
| Player death | PDSND | $EE / (2,8,#11)(1,$20,#17) |
| Start 1P / 2P | ST1SND / ST2SND | $F0 / (1,$10,#28) / (1,$10,#25) |
| Wave end | WVSND | $E0 / ($1D,4,#0E) |
| Coin | CNSND | $FF / (1,$20,#0C) |
| Replay / extra man | RPSND | $EF / (1,$20,#1E) |
| Save a human | SAVSND | $E0 / (1,$20,#0D) |
| Human killed | HKSND | $E0 / (1,$18,#1A) |
| Grunt hit | RBSND | $D0 / (1,$0C,#14)(1,8,#17) |
| Grunt ("ROBOT") move | RMVSND | $C0 / (1,$0A,#06) |
| Electrode ("POST") kill | PSKSND | $D0 / (1,8,#17) |
| Hulk hit | HKHSND | $D0 / (1,$10,#06) |
| Hulk kill | HLKSND | $D0 / (3,4,#17) |
| Brain kill | BKSND | multi-step |
| Brain shoot | BSHSND | multi-step |
| Cruise-missile kill | CMKSND | multi-step |
| Programming (brain converting human) | PRGSND | — |
| Human→Prog final conversion | HPSND | — |
| Prog kill | PGKSND | — |
| Spheroid ("CIRCLE") kill | CRKSND | — |
| Enforcer drop-off | ENDSND | — |
| Enforcer shoot | ENFSND | — |
| Enforcer kill | ENKSND | — |
| Spark kill | SPKSND | — |
| Tank kill / fire / drop | TNKSND / TKFSND / TKDSND | — |
| Shell rebound | SRBSND | — |
| Shell kill | SHKSND | — |
| Quark ("SQUARE") kill | SQKSND | — |
| Smart-clear | TR1SND "CLEAR THE SYSTEM" | — |
| High score tune | HSTUNE | — |

- **"Robot move" (RMVSND)** is an event sound tied to grunt movement steps, not a continuous drone. How often it fires is a gameplay question for the other agent.
- **The sound ROM side** (what each command synthesizes) isn't documented authoritatively.
  - joe07734's sounds.txt (S18) is a listening log that mixes Robotron, Defender and Joust notes, e.g. "41 Extra guy (R, D)", "45 Human death (R)", "1010010 – Pick up human (R gulgulgul…)", "1011110 – r firing, bombers exploding". **Low confidence.**
  - Lomont (S17) re-created the algorithms in his own code: "parameterized routines that share the lower RAM", a 6-bit command bus, and a DAC at about 894.75 kHz sample rate.

### D2. Recommendation
- **Synthesize procedurally**, rather than sampling MAME output (which would be Williams-derived).
- Use simple oscillators, noise, pitch sweeps and an LFSR into an 8-bit-quantized DAC model. The Williams routines are basically:
  - "GWAVE"-style wavetable pitch sweeps
  - noise bursts with decay (explosions)
  - frequency-modulated "thwips"
- Implement your own priority-sequencer clone of the table format above. That alone gives the authentic single-channel "newest high-priority sound cuts the old one" feel.
- CC0 / CC-BY fallbacks:
  - "SFX: The Ultimate 2017 8 bit Mini pack" by phoenix1291, CC0, 210 SFX (OGA node 79699; license per search summary, not opened)
  - "8-bit Retro Sfx" by MouthlessGames, CC-BY 3.0, 5 SFX (verified by fetch)

  These won't sound like Williams hardware, so synthesis is the better choice for fidelity.

---

## E. History

### E1. Release and development
- **Release:** debuted at the Amusement Operators Expo in March 1982. SECONDARY (S9, which cites Cash Box 1983-05-14 and other refs). Copyright date 1982 is VERIFIED (S7, S8).
- **Developers:** Eugene Jarvis and Larry DeMar of Vid Kidz, for Williams. The working title was "Robot Wars". Built on a Gimix 6809 dev system. CORROBORATED (S9, S10). The ROM string "DESIGNED BY VID KIDZ" is VERIFIED (S7).
- **Six-month development:** CORROBORATED (S9, S10). Jarvis (S10): "It was built in six months. We designed all the graphics and animation in about two weeks… Then, we spent four out of the six months of Robotron's development PLAYING the thing."
- **Twin-stick origin:** CORROBORATED (S9, S10, S11). Jarvis (S10): "Some guy jumped a red light and the shock of the impact through the steering wheel completely shattered my right hand. … my broken hand meant I couldn't press the fire button any more. … So, both those things combined to give me the idea for a dual-joystick control where you could move in one direction and fire in another."
  - It was a car accident and a right **hand/wrist** injury, not an arm.
  - The prototype used a Stargate board plus two Atari 2600 joysticks (S9).
- **Design quotes from S11** (first-person Jarvis, originally from his own writing; author and date of the article not confirmed by fetch):
  - "It was fun for about fifteen minutes, running the robots into the electrodes. But pacifism has its limits."
  - "We wired up the 'fire' joystick and the chaos was unbelievable. Next we dialed up the Robot count on the terminal. 10 was fun. How about 20? 30, 60, 90, 120!"
  - "Robotron has always been frustrating in non-arcade versions because of the lack of the dual-joystick control. … it is very nice to have a 300-pound arcade cabinet stabilizing your joysticks."
  - The last one directly supports **not** importing port control schemes.
- **Further Jarvis quotes from S10:**
  - "That confinement is the key element in what makes Robotron feel the way it does."
  - "It's that God's-eye view that does it."
- **DeMar (S10)** on the human-death graphic: the censored prototype video (MAME robotronr3) captured "a very small window of time" before the skull and crossbones.
- **Primary-interview gap:**
  - The only direct primary interview I could read (S12, 2007) mentions Robotron only in passing.
  - The GDC 2014 postmortem (S13) is the best primary source and is tagged free on GDC Vault, but I couldn't watch it.
  - So the quotes above are first-person but come through secondary publications. **Treat them as CORROBORATED, not VERIFIED.**
- **Sales:**
  - Wikipedia (S9): about 19,000 cabinets.
  - Arcade Blogger (S10): about 23,000 uprights.
  - **UNKNOWN / conflict.** Neither cites a Williams document I could inspect.
- **Contemporary press:**
  - Videogaming Illustrated Dec 1982 (S14) and Creative Computing V&AG Spring 1983 (S15): read directly.
  - Play Meter (Roger Sharpe, 4/4), RePlay (Todd Erickson, on difficulty settings), Electronic Games (Kunkel), and JoyStik (color effects) are known only via Wikipedia (SECONDARY). I found no scans online.
- **Legal:** Disney sued over "Tron" in May 1982; the suit was dismissed on 1983-01-12 (S9, SECONDARY).

---

## F. Ports and how they differ (don't import these)

| Port | Source | Notable differences from arcade |
|---|---|---|
| Atari 5200 (1984) | S9 | Shipped with a holder for two 5200 controllers |
| Atari 7800 (1986, David Brown) | S19 manual | **5 lives**; difficulty Novice / Intermediate / Advanced / Expert / Challenge; one-controller mode fires in the direction of movement with the button; two-controller twin-stick optional; pause |
| Commodore 64 (Atarisoft, Tom Griner) | S19 (IA item mislabeled "5200") | F-key options: 1–2 players, **difficulty 1–9**, "optional one or two joystick control"; fire button **pauses**; **"You have five lives"**; Brain listed at 300 pts (OCR), Enforcer 150 |
| Apple II (Atarisoft, Steven Hays) | S19 | Joystick, keyboard or paddle modes; "Use the joystick buttons to fire and rotate the direction of fire"; different point list (Enforcer 200, "Cross" 200, "Cannonball" 25, Cruise Missile 100); score drawn in a right-hand border |
| Atari 8-bit (Judy Bogart), VIC-20, IBM PC, BBC Micro, Atari ST | S9 | Mostly single-stick or keyboard. Wikipedia: "most early conversions did not have dual joysticks". Details not inspected |
| ZX Spectrum | S9 | Unreleased Atarisoft conversion of Wild West Hero |
| Atari 2600 | S9 | Announced at CES 1983, **never released** |
| Atari Lynx (1991, Shadowsoft, Dave Dies) | S20 | Single d-pad plus A/B with three fire schemes (A: hold to lock direction, B flips 180°; B: rotate clockwise; C: auto-fire with rotate buttons); **title-screen music** (arcade has none); screen flip; skill level option; "your man will implode … while imploding … you are indestructable" (Lynx text) |
| Xbox Live Arcade (2005) | S9 | HD graphics, online boards, co-op split move/shoot; delisted Feb 2010 |
| Emulated compilations | S9 | Williams Arcade's Greatest Hits (1996), Midway's Greatest Arcade Hits (2000), Midway Arcade Treasures (2003), Midway Arcade Origins (2012): emulation, so arcade-faithful |

Rules for the recreation:
- 3 men default (not 5).
- No music.
- 8-way twin sticks only.
- Difficulty uses the arcade CMOS range 0–10.
- Use the arcade point and wave rules from the arcade code, not from the port manuals.

---

## G. Open-source reimplementations and .NET libraries

### G1. GitHub "robotron" repos
LICENSE files were checked via the GitHub API on 2026-10-07.

| Repo | Lang | What it is | LICENSE (actual file) | Original assets? |
|---|---|---|---|---|
| historicalsource/robotron | 6809 asm | **Original Williams/Vid Kidz source** | **None**; files say © 1982 Williams | Contains the original image and sound-table data in source form. **Red flag: reference only** |
| joe07734/robotron | C / notes | "Annotated sources": ROM disassembler plus notes (memory map, sounds.txt) | None | ROM-derived. Reference only |
| ScottTunstall/Robotron2084 | asm | Annotated ROM disassembly (robomame.asm) | None | ROM-derived. Red flag |
| pobtastic/robotron2084 | SkoolKit | Disassembly (SkoolKit, so likely ZX Spectrum) | GPL-3.0 (GitHub metadata) | Disassembly of copyrighted code. Avoid |
| fschuhi/Robotron_2084 | 6502 asm | Apple II disassembly | None | Avoid |
| MiSTer-devel/Arcade-Robotron_MiSTer | Verilog | FPGA hardware model | No LICENSE file in tree | **ROMs not included** (README: "ROMs are not included!") |
| jboone/robotron-fpga-verilog, sharebrained/robotron-fpga | Verilog / VHDL | FPGA | None | sharebrained/ROMs holds only scripts (make_rom_file.py) |
| Ernir/robotron | JavaScript | 2014 University of Iceland course remake | **None** | Sprites drawn in code or canvas; no image or audio files in tree |
| alldritt/Robotron | Obj-C / SpriteKit | iPad/Mac remake | **None** | **Red flag.** Notes.md: "The sprite artwork … has been taken from various places around the web. The audio clips have been taken from Robotron videos on YouTube." |
| stridera/robotron2084gym | Python | Gymnasium RL re-implementation (entities per enemy) | **None** | resources/sprites.jpg plus sprites.txt look like a ripped sprite sheet. **Probable red flag** (not opened) |
| gmn/Robohack | C / ncurses | ASCII clone | GPL-2.0 ("GPL" file) | Original ASCII |
| clort81/blopotron | C | Terminal Robotron-style game | FSL-1.1-MIT (README) | Original ANSI sprites |
| eoinmcg/dorksquad | JS | Robotron riff (Game Off 2017) | MIT (LICENSE.md) | Original |
| paulscottrobson/neo-robotron | asm | Neo6502 port | MIT | Has graphics/*.png. Provenance not checked |

Takeaway: no permissively licensed, faithful, asset-clean reimplementation exists to borrow from. Use MAME (BSD-3) for hardware facts, and the original source only as a fact reference.

### G2. .NET input and audio libraries
NuGet registration API, 2026-10-07.

| Package | Latest stable (date) | License | Notes |
|---|---|---|---|
| **Silk.NET.SDL** | 2.23.0 (2026-01-23) | MIT | SDL2 bindings. Native via **Ultz.Native.SDL 2.32.10** (SDL 2.32.10) with runtimes win-x86/x64/arm64, linux-x64/**arm64**/arm, osx, ios. Silk.NET.Input 2.23.0 (MIT) wraps SDL/GLFW gamepads |
| **SDL3-CS** (edwardgushchin) | 3.4.18 (2026-10-03) | **zlib** (repo license) | SDL3 bindings for net7–net10. Natives in **SDL3-CS.Native 3.4.2** (2026-03-17): win-x64/arm64, linux-x64/**arm64**, osx-x64/arm64. Very active (pushed 2026-10-07) |
| **ppy.SDL3-CS** (osu! team) | 2026.1002.1 (2026-10-02) | MIT | net8.0. Bundles natives for win/linux (x86, x64, **arm64**, arm), osx, android, ios. Good choice for SDL3 gamepad API (`SDL_Gamepad*`) |
| ppy.SDL2-CS | 1.0.82 (2021) stable; 1.0.741-alpha (2024-03-25) | zlib (Ethan Lee SDL2# header) | Effectively superseded by ppy.SDL3-CS |
| **Silk.NET.OpenAL** | 2.23.0 (2026-01-23) | MIT | Bindings. **Silk.NET.OpenAL.Soft.Native 1.23.1** is **LGPL-2.0-or-later** (OpenAL Soft; dynamic linking OK). linux-arm64 included |
| **OpenTK.Audio.OpenAL** | 4.9.4 (2025-03-17); 5.0.0-pre.14 | MIT | Needs OpenAL Soft installed or bundled (LGPL) |
| **ManagedBass** | 4.0.2 (2025-10-03) | Wrapper MIT | **Depends on un4seen BASS, which is NOT free**: "free for non-commercial use"; commercial €950+ or shareware €125 (S23). Avoid for an open project |
| **NAudio** | 3.1.0 (2026-09-07) | MIT | v3 README: NAudio.Core is cross-platform (DSP, mixing). Output is Windows-only via Wasapi/WinMM/Asio/Dmo, **plus new NAudio.Alsa (Linux)**. **No macOS output.** OK for synthesis math, not as the sole output path |
| SoundFlow | 1.4.1 (2026-05-11) | MIT (repo) | Cross-platform (miniaudio-based). Optional alternative |
| **Avalonia** | 12.1.3 (2026-09-22) | MIT | **No built-in gamepad API: VERIFIED.** Issue #6945 "Gamepad/controller input support" is still OPEN (since 2021). In #8792 maintainer maxkatz6 says "Audio input/output is out of the scope… Controller support is planned". Use SDL for gamepads (and optionally SDL audio). Avalonia keyboard events work for keys |

**Recommendation for AVATron (Avalonia, Linux aarch64 dev box):**
- Use **ppy.SDL3-CS (MIT)** or **SDL3-CS (zlib)** for gamepad input. Init only `SDL_INIT_GAMEPAD`, with no SDL window, and poll from the game loop.
- For audio, use SDL3's audio stream from the same binding. That gives one native dependency (SDL3, zlib) for both input and audio, with linux-arm64 natives available. Silk.NET.OpenAL plus OpenAL Soft (LGPL) is the alternative.
- Synthesize the sound in managed code into an 8-bit-quantized buffer, resampled from a nominal 894,886 Hz model rate or just generated directly at 48 kHz.
- Map twin sticks as left stick or D-pad for move, right stick or face buttons for fire. Quantize to 8 directions and cancel opposite directions as MAME does (A7).
