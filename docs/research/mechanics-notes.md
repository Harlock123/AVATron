# Robotron: 2084 (MAME `robotron`) — gameplay mechanics, with evidence

Research date: 2026-10-07. Every number below has a citation. Facts and behaviour only. **No code or graphics are copied into the recreation** (see §8).

Abbreviations: `SRC` = the original Williams/Vid Kidz 6809 assembly source, as mirrored in `mwenge/robotron/src/` (from `historicalsource/robotron`). Line numbers are for the mwenge copy after CR stripping. `ROM-DIS` = Scott Tunstall's annotated disassembly of the Solid Blue Label ROM.

Status tags:
- **VERIFIED**: I read it in the original source myself.
- **CORROBORATED**: the source plus a second independent source (ROM disassembly or MAME) agree.
- **DERIVED**: I computed it from verified source values. The arithmetic is shown.
- **SECONDARY**: only a fan or secondary site says it.
- **UNKNOWN**: I could not find it.

Unit conventions used by the source:
- **X** is in *bytes*. One byte is 2 horizontal pixels (4bpp). The 16-bit `OX16` holds byte.fraction, so `$0080` is half a byte, or 1 pixel.
- **Y** is in scan lines.
- **Time**: `NAP n` / `SLEEP` = n executive frames of about 16.6 ms (§1).

---

## 0. Source ledger

| # | Title | Author / org | URL | Accessed | Type | What I actually inspected | Claims supported | Confidence | License |
|---|---|---|---|---|---|---|---|---|---|
| S1 | robotron (assemblable source, Blue Label) | Robert Hogan (mwenge). Source originally by Eugene Jarvis and Larry DeMar, Vid Kidz / Williams 1982 | https://github.com/mwenge/robotron | 2026-10-07 | Original 6809 source listings plus a build that md5-checks against the blue-label ROMs (Makefile l.91-99) | Read fully or substantially: RRF.ASM (equates/RAM), RRS22.ASM (OS: exec, IRQ, sleep, collision, score), RRG23.ASM (game flow, player, lasers, wave tables), RRP8 (grunts, electrodes), RRH11 (humans, hulks), RRB10 (brains, progs, cruise missiles), RRC11 (sphereoids, enforcers, sparks), RRTK4 (quarks, tanks, shells), RRX7 (death), RRTESTB (CMOS defaults) | Almost everything in §1–§7 | High | **No license file** (GitHub API: license=null). The content is Williams copyrighted code |
| S2 | historicalsource/robotron | "historicalsource" GitHub org | https://github.com/historicalsource/robotron | 2026-10-07 | Upstream copy of the same listings | Cloned. Compared the `DEFALT` CMOS table: it has `GA1 = $02`, where S1 has `$03`, which S1 changed "to match robotron.sb7" | Confirms S1 provenance. Shows the difficulty-default discrepancy | High | None (license=null) |
| S3 | Robotron2084 `robomame.asm` | Scott Tunstall | https://github.com/ScottTunstall/Robotron2084 | 2026-10-07 | Annotated disassembly of the Solid Blue Label ROM (21k lines) | Grep and read: difficulty notes (l.4005-4030, 4065-4135); raw wave-count bytes (l.4305-4380); CMOS `difficulty_of_play` comment (l.302) | Independently confirms the wave-count bytes and the difficulty algorithm. Quotes Larry DeMar that the default difficulty moved from 5 to 3 in the 2nd release | High | None (license=null) |
| S4 | MAME `williams.cpp` driver | MAME team (Aaron Giles et al.) | https://github.com/mamedev/mame/blob/master/src/mame/williams/williams.cpp | 2026-10-07 | Emulator source | l.1530-1560 (clocks, screen raw params, scanline timers), l.941-965 (robotron inputs), l.2729 (ROM set), l.4003-4011 (set names) | Screen geometry, frame rate, IRQ cadence, input bits | High | BSD-3-Clause (file header) |
| S5 | joe07734/robotron | Joe Holt and Steve Hawley (1992 disassembly) | https://github.com/joe07734/robotron | 2026-10-07 | Partial commented disassembly | Read only the README and "about robotron.txt" | Nothing used. Its own text asks "please do not distribute" | n/a | None |
| S6 | robotron-2084.co.uk | fan site | https://www.robotron-2084.co.uk/ | 2026-10-07 | Fan site | Homepage via WebFetch | Nothing: the homepage has no wave or speed data. Subpages not explored | n/a | n/a |
| S7 | seanriddle.com | Sean Riddle | https://seanriddle.com/robotron.html | 2026-10-07 | — | Returned 404. Not accessed further | Nothing | n/a | n/a |
| S8 | synamaxmusic/robotron-2084, NICOH-YAY/robotron-recon | various | GitHub | 2026-10-07 | Retargets / reconstructions | Seen in search only, not inspected | — | — | None listed |

The Eugene Jarvis / Larry DeMar released listings are what S1/S2 contain. Their headers are "ROBOTRON 3-10-82 RELEASE 1" (JAPDATA.ASM) and "Christian Gingras … version 6" patches (RRCHRIS.ASM, RRELESE6.ASM).

---

## 1. Timing: frame rate, IRQ, scheduler

- **Video frame rate is about 60.1 Hz.** MAME uses a pixel clock of `MASTER_CLOCK*2/3` = 12 MHz × 2/3 = 8 MHz, with 512 clocks/line × 260 lines (S4 l.1556). DERIVED: 8e6 / 133,120 = 60.096 Hz. **CORROBORATED** by S1 RRF l.29 ("IRQ 240 (16 MS)") and RRS22 l.216 ("A=SLEEP TIME X 16MSEC").
- **Interrupts happen at 4 ms intervals.** RRF l.33: "CB1 IRQ 4 MS (0,$40,$80,$C0)", meaning on scanlines 0/64/128/192. MAME toggles VA11 every 32 scanlines (S4 l.1545-1546). **CORROBORATED.**
- **The IRQ handler does work twice per frame.** RRS22 l.1552-1615 does:
  - once when the beam passes line 128,
  - once when it is back above line 128.
- Each of these increments `TIMER`. The mid-screen pass:
  - calls `PLAYER` (player movement) once per frame (l.1564), and
  - adds velocity to all "motion objects" (the `OPTR` list) once per frame (`OPRC80`, l.1710-1740). The `OPTR` list holds sphereoids, enforcers, sparks, quarks and shells.
- Objects are DMA-blitted during the IRQ, split by beam position to avoid tearing. **VERIFIED.**
- **The executive loop runs once per frame.** It waits until `TIMER >= 2`, i.e. two IRQ passes = one frame. It then runs the process list, decrementing each process's `PTIME`. A process whose count reaches 0 runs (RRS22 l.169-225). **VERIFIED.**
  - `NAP n,addr` = sleep n frames (RRF l.694-698).
  - It is cooperative multitasking: up to 120 processes and 20 "super processes" (RRF l.560, 566), plus 180 object cells (l.531).
- **Sleep cadence per object type (VERIFIED):**

| Object | Sleep between updates | Source |
|---|---|---|
| Player move | every frame (IRQ) | RRS22 l.1564 |
| Player laser process (fire input) | 1 frame | RRG23 l.972 |
| Each laser bolt | 1 frame | RRG23 l.996-1208 |
| Collision check, player vs everything | 1 frame | RRG23 l.824 |
| Grunt manager (all grunts) | 4 frames per pass. Each grunt moves after a random 1..ROBSPD passes | RRP8 l.162-169, 227 |
| Hulk | `HLKSPD` frames (wave table) | RRH11 l.78-80 |
| Human (family) | 8 frames | RRH11 l.360 |
| Brain | `BRNSPD` frames (wave table) | RRB10 l.230-232 |
| Prog | 3 frames | RRB10 l.531 |
| Cruise missile | 2 frames (2 steps per wake) | RRB10 l.708-710 |
| Sphereoid | 2 frames | RRC11 l.74, 104, 125 |
| Enforcer grow-in | 8 frames per growth image (5 images) | RRC11 l.275-281 |
| Enforcer AI | 3 frames | RRC11 l.292 |
| Spark AI | 4 frames | RRC11 l.447 |
| Quark | 3 frames | RRTK4 l.142, 174 |
| Tank grow-in | 12 frames per image | RRTK4 l.288 |
| Tank | `TNKSPD` = 2 frames (set in PLINIT) | RRTK4 l.333-335. RRG23 l.677-678 |
| Shell bounce check | 2 frames | RRTK4 l.580 |
| Game exec (wave-end check, grunt speed-up) | 15 frames | RRG23 l.467 |

---

## 2. Wave tables

### 2.1 Structure (VERIFIED, RRG23 l.526-652)

There are two kinds of table:
- 12 difficulty parameters, each laid out as a 3-byte header (`sign/delta, min, max`) followed by 40 bytes (one per wave).
- 9 count tables of 40 bytes each, with no header.

The order is fixed by the `ELIST` RAM layout (RRF l.617-643): ROBSPD, RMXSPD, ENFNUM, ENSTIM, CDPTIM, HLKSPD, BSHTIM, BRNSPD, TNKSHT, SHLSPD, TDPTIM, SQSPD, then counts ROBCNT, PSTCNT, MOMCNT, DADCNT, KIDCNT, HLKCNT, BRNCNT, CIRCNT, SQCNT.

`TNKCNT` is always set to 0 at wave start: "CLR ,X+ ;GET TANK COUNT" (l.592). Tanks only come from Quarks.

Names used in the source: ROB = Grunt, PST = Electrode ("post"), KID = Mikey, CIR/CIRCLE = Sphereoid, SQ/SQUARE = Quark, ENF = Enforcer, TNK = Tank.

**Waves above 40 (VERIFIED, l.552-555):** while the wave number is over 40, subtract 20. So:
- waves 41–60 use the rows for 21–40,
- 61→41→21, and so on.

The game cycles waves 21–40 forever. The wave counter is a byte. INC past 255 skips 0 and becomes 1 (l.420-422).

**Raw tables confirmed against the ROM:** S3 l.4320-4380 shows the same bytes in the ROM. For example, grunts `0F 11 16 22 14 20 00 23 3C 19…` and electrodes `05 0F 19 19 14 19 00 19 00 14…`. **CORROBORATED.**

### 2.2 Counts per wave (CORROBORATED: SRC RRG23 l.633-651 + ROM-DIS S3 l.4305-4380)

"family total" = MOM+DAD+MIKEY.

| Wave | GRUNT | ELECT | MOM | DAD | MIKEY | HULK | BRAIN | SPHER | QUARK | family total |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 15 | 5 | 1 | 1 | 0 | 0 | 0 | 0 | 0 | 2 |
| 2 | 17 | 15 | 1 | 1 | 1 | 5 | 0 | 1 | 0 | 3 |
| 3 | 22 | 25 | 2 | 2 | 2 | 6 | 0 | 3 | 0 | 6 |
| 4 | 34 | 25 | 2 | 2 | 2 | 7 | 0 | 4 | 0 | 6 |
| 5 | 20 | 20 | 15 | 0 | 1 | 0 | 15 | 1 | 0 | 16 |
| 6 | 32 | 25 | 3 | 3 | 3 | 7 | 0 | 4 | 0 | 9 |
| 7 | 0 | 0 | 4 | 4 | 4 | 12 | 0 | 0 | 10 | 12 |
| 8 | 35 | 25 | 3 | 3 | 3 | 8 | 0 | 5 | 0 | 9 |
| 9 | 60 | 0 | 3 | 3 | 3 | 4 | 0 | 5 | 0 | 9 |
| 10 | 25 | 20 | 0 | 22 | 0 | 0 | 20 | 1 | 0 | 22 |
| 11 | 35 | 25 | 3 | 3 | 3 | 8 | 0 | 5 | 0 | 9 |
| 12 | 0 | 0 | 3 | 3 | 3 | 13 | 0 | 0 | 12 | 9 |
| 13 | 35 | 25 | 3 | 3 | 3 | 8 | 0 | 5 | 0 | 9 |
| 14 | 27 | 5 | 5 | 5 | 5 | 20 | 0 | 2 | 0 | 15 |
| 15 | 25 | 20 | 0 | 0 | 22 | 2 | 20 | 1 | 0 | 22 |
| 16 | 35 | 25 | 3 | 3 | 3 | 3 | 0 | 5 | 0 | 9 |
| 17 | 0 | 0 | 3 | 3 | 3 | 14 | 0 | 0 | 12 | 9 |
| 18 | 35 | 25 | 3 | 3 | 3 | 8 | 0 | 5 | 0 | 9 |
| 19 | 70 | 0 | 3 | 3 | 3 | 3 | 0 | 5 | 0 | 9 |
| 20 | 25 | 20 | 8 | 8 | 8 | 2 | 20 | 2 | 0 | 24 |
| 21 | 35 | 25 | 3 | 3 | 3 | 8 | 0 | 5 | 0 | 9 |
| 22 | 0 | 0 | 3 | 3 | 3 | 15 | 0 | 0 | 12 | 9 |
| 23 | 35 | 25 | 3 | 3 | 3 | 8 | 0 | 5 | 0 | 9 |
| 24 | 0 | 0 | 3 | 3 | 3 | 13 | 0 | 6 | 7 | 9 |
| 25 | 25 | 20 | 25 | 0 | 1 | 1 | 21 | 1 | 0 | 26 |
| 26 | 35 | 25 | 3 | 3 | 3 | 8 | 0 | 5 | 0 | 9 |
| 27 | 0 | 0 | 3 | 3 | 3 | 16 | 0 | 0 | 12 | 9 |
| 28 | 35 | 25 | 3 | 3 | 3 | 8 | 0 | 5 | 1 | 9 |
| 29 | 75 | 0 | 3 | 3 | 3 | 4 | 0 | 5 | 1 | 9 |
| 30 | 25 | 20 | 0 | 25 | 0 | 1 | 22 | 1 | 1 | 25 |
| 31 | 35 | 25 | 3 | 3 | 3 | 8 | 0 | 5 | 1 | 9 |
| 32 | 0 | 0 | 3 | 3 | 3 | 16 | 0 | 0 | 13 | 9 |
| 33 | 35 | 25 | 3 | 3 | 3 | 8 | 0 | 5 | 1 | 9 |
| 34 | 30 | 0 | 3 | 3 | 3 | 25 | 0 | 2 | 2 | 9 |
| 35 | 27 | 15 | 0 | 0 | 25 | 2 | 23 | 1 | 2 | 25 |
| 36 | 35 | 25 | 3 | 3 | 3 | 8 | 0 | 5 | 2 | 9 |
| 37 | 0 | 0 | 3 | 3 | 3 | 16 | 0 | 0 | 14 | 9 |
| 38 | 35 | 25 | 3 | 3 | 3 | 8 | 0 | 5 | 2 | 9 |
| 39 | 80 | 0 | 3 | 3 | 3 | 6 | 0 | 5 | 1 | 9 |
| 40 | 30 | 15 | 10 | 10 | 10 | 2 | 25 | 1 | 1 | 30 |

**Wave classes (DERIVED from the table above):**
- **Brain waves** (Brains > 0): 5, 10, 15, 20, 25, 30, 35, 40. These are also the "lots of one family type" waves:
  - 5: 15 Mommies
  - 10: 22 Daddies
  - 15: 22 Mikeys
  - 20: 8/8/8
  - 25: 25 Mommies
  - 30: 25 Daddies
  - 35: 25 Mikeys
  - 40: 10/10/10
  - Brain waves start with the "transporter" entrance, not the zoom "appear" (RRG23 l.175-179).
- **Tank (Quark) waves** with no grunts or electrodes: 7, 12, 17, 22, 27, 32, 37 (10–14 Quarks), plus 24 (7 Quarks and 6 Sphereoids). From wave 28 on, 1–2 Quarks are also mixed into ordinary waves.
- **Grunt swarm waves** with no electrodes: 9 (60), 19 (70), 29 (75), 39 (80).
- **Hulk waves:** 14 (20 Hulks), 34 (25 Hulks).
- **Wave end condition (VERIFIED, RRG23 l.403-409):** the wave ends when Grunts + Sphereoids + Enforcers + Brains + Tanks + Quarks = 0. It is checked every 15 frames (l.413-467).
  - Hulks, Electrodes, family, Progs, sparks, shells and cruise missiles are *not* counted.
  - Progs are created without incrementing any counter (RRB10 `PROGST` l.401-427, `PRGKIL` l.535-566). So, as written, live Progs do not block the end of a wave.
- **A Sphereoid or Quark that has dropped all its children flies off-screen and is removed.** It decrements its counter, so it no longer blocks the wave end (RRC11 l.105-129; RRTK4 l.175-199).

### 2.3 Per-wave difficulty parameters (VERIFIED: SRC RRG23 l.596-632)

The values below are the raw table values before the difficulty adjustment in §2.4. Units:

| Parameter | Meaning and unit |
|---|---|
| ROBSPD | Grunt move-timer ceiling. Each grunt waits a uniform random 1..ROBSPD manager passes (4 frames each) between steps |
| RMXSPD | Floor that ROBSPD can be lowered to |
| ENFNUM | Enforcers per Sphereoid (and Tanks per Quark) ×2. Actual count per spawner = ceil(RMAX(ENFNUM)/2) |
| ENSTIM | Enforcer shot timer ceiling, in 3-frame enforcer ticks (RMAX) |
| CDPTIM | Sphereoid first-drop timer ceiling, in 10-frame animation cycles (RMAX) |
| HLKSPD | Frames between Hulk steps |
| BSHTIM | Brain cruise-missile timer ceiling, in brain steps (RMAX) |
| BRNSPD | Frames between Brain steps |
| THKSHT (TNKSHT) | Tank steps between shots (one step = 2 frames) |
| SHLSPD | Tank shell speed factor (/256) |
| TDPTIM | Quark first tank-drop timer ceiling, in 15-frame animation cycles |
| SQSPD | Quark speed factor |

`RANDU(n)` is uniform 1..n. `RMAX(n)` is random 1..n, top-weighted: take a random byte and halve it until it is ≤ n (RRS22 l.883-903).

Table headers as `sign/delta, min, max`:

```
ROBSPD $8E 10..20   RMXSPD $8E 3..10   ENFNUM $0E 8..12   ENSTIM $8E 13..40
CDPTIM $8E 12..40   HLKSPD $8E 5..9    BSHTIM $8E 25..80  BRNSPD $8E 6..10
THKSHT $8E 20..40   SHLSPD $0E 160..255 TDPTIM $8E 12..48 SQSPD  $0E 40..68
```

| Wave | ROBSPD | RMXSPD | ENFNUM | ENSTIM | CDPTIM | HLKSPD | BSHTIM | BRNSPD | THKSHT | SHLSPD | TDPTIM | SQSPD |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 20 | 9 | 10 | 30 | 30 | 8 | 64 | 8 | 32 | 176 | 16 | 50 |
| 2 | 15 | 7 | 10 | 28 | 28 | 8 | 64 | 8 | 32 | 176 | 16 | 50 |
| 3 | 15 | 6 | 10 | 26 | 26 | 7 | 64 | 8 | 32 | 176 | 16 | 50 |
| 4 | 15 | 5 | 10 | 24 | 24 | 7 | 64 | 8 | 32 | 176 | 16 | 50 |
| 5 | 15 | 5 | 10 | 22 | 30 | 7 | 64 | 8 | 32 | 176 | 16 | 50 |
| 6 | 15 | 5 | 10 | 20 | 20 | 7 | 40 | 7 | 32 | 176 | 16 | 50 |
| 7 | 15 | 5 | 10 | 18 | 18 | 7 | 40 | 7 | 32 | 176 | 16 | 50 |
| 8 | 15 | 4 | 10 | 18 | 16 | 6 | 38 | 7 | 30 | 176 | 16 | 50 |
| 9 | 15 | 4 | 10 | 16 | 18 | 6 | 38 | 7 | 30 | 176 | 16 | 50 |
| 10 | 15 | 4 | 10 | 14 | 25 | 6 | 38 | 7 | 30 | 176 | 16 | 50 |
| 11 | 14 | 4 | 10 | 14 | 12 | 6 | 38 | 7 | 30 | 176 | 16 | 50 |
| 12 | 14 | 4 | 10 | 14 | 12 | 5 | 38 | 7 | 30 | 176 | 16 | 50 |
| 13 | 14 | 4 | 10 | 14 | 12 | 5 | 38 | 7 | 30 | 176 | 16 | 56 |
| 14 | 14 | 4 | 10 | 14 | 25 | 5 | 38 | 7 | 28 | 176 | 16 | 56 |
| 15 | 14 | 4 | 10 | 14 | 25 | 5 | 38 | 7 | 28 | 176 | 15 | 56 |
| 16 | 13 | 4 | 10 | 14 | 12 | 5 | 38 | 6 | 28 | 176 | 15 | 56 |
| 17 | 13 | 4 | 10 | 14 | 12 | 5 | 36 | 6 | 28 | 176 | 15 | 56 |
| 18 | 13 | 4 | 10 | 14 | 12 | 5 | 36 | 6 | 28 | 176 | 15 | 56 |
| 19 | 13 | 4 | 10 | 14 | 18 | 5 | 36 | 6 | 28 | 176 | 15 | 56 |
| 20 | 13 | 4 | 10 | 14 | 20 | 5 | 36 | 6 | 28 | 176 | 15 | 56 |
| 21 | 14 | 4 | 11 | 15 | 14 | 5 | 32 | 6 | 30 | 184 | 14 | 56 |
| 22 | 14 | 4 | 11 | 15 | 14 | 5 | 32 | 6 | 30 | 184 | 14 | 56 |
| 23 | 14 | 4 | 11 | 15 | 14 | 5 | 32 | 6 | 30 | 184 | 14 | 56 |
| 24 | 14 | 4 | 11 | 15 | 14 | 5 | 32 | 6 | 30 | 184 | 14 | 56 |
| 25 | 14 | 4 | 11 | 15 | 14 | 5 | 32 | 6 | 30 | 184 | 14 | 56 |
| 26 | 14 | 4 | 11 | 15 | 25 | 5 | 32 | 6 | 30 | 184 | 14 | 56 |
| 27 | 13 | 3 | 11 | 15 | 14 | 5 | 32 | 6 | 28 | 184 | 14 | 56 |
| 28 | 13 | 3 | 11 | 15 | 14 | 5 | 30 | 6 | 28 | 184 | 14 | 56 |
| 29 | 13 | 4 | 11 | 15 | 18 | 5 | 30 | 6 | 28 | 184 | 14 | 60 |
| 30 | 13 | 3 | 11 | 14 | 25 | 5 | 30 | 6 | 28 | 184 | 14 | 60 |
| 31 | 13 | 3 | 11 | 14 | 12 | 5 | 30 | 6 | 28 | 192 | 14 | 60 |
| 32 | 13 | 3 | 11 | 14 | 12 | 5 | 30 | 6 | 26 | 192 | 14 | 60 |
| 33 | 12 | 3 | 11 | 14 | 12 | 5 | 25 | 6 | 26 | 192 | 14 | 60 |
| 34 | 12 | 3 | 11 | 14 | 12 | 5 | 25 | 6 | 26 | 192 | 14 | 60 |
| 35 | 12 | 3 | 11 | 14 | 25 | 5 | 25 | 6 | 26 | 192 | 14 | 60 |
| 36 | 12 | 3 | 11 | 14 | 12 | 5 | 25 | 6 | 26 | 192 | 14 | 60 |
| 37 | 12 | 3 | 11 | 14 | 12 | 5 | 25 | 6 | 24 | 192 | 14 | 60 |
| 38 | 12 | 3 | 11 | 14 | 12 | 5 | 25 | 6 | 24 | 192 | 14 | 60 |
| 39 | 15 | 4 | 11 | 14 | 18 | 5 | 25 | 6 | 24 | 192 | 14 | 60 |
| 40 | 12 | 3 | 11 | 14 | 20 | 5 | 25 | 6 | 24 | 192 | 14 | 60 |

### 2.4 Difficulty adjustment ("Difficulty of play", CMOS `GA1`) — VERIFIED RRG23 l.528-593, CORROBORATED S3 l.4065-4135

**How the adjustment works:**
- Let d = the operator setting (0–10; RRTESTB l.145 limits it to 0..$10 BCD).
- **Easy setting (d < 5):** the game forces d = 5 when either:
  - the wave is ≥ 14, or
  - the wave is ≥ 5 and the player has ≥ 3 lives (l.532-543).
- **Magnitude and sign:** m = |d − 5|. The sign is that of (d − 5).
- **For each of the 12 parameters:** adj = round(value × m × (delta & $1F) / 256). The delta is 14 for every parameter, so the step is about 5.5% per difficulty point.
- **Direction:** the header's bit 7 sets which way the adjustment goes.
  - `$8E` parameters are timers, so easier = larger.
  - `$0E` parameters (ENFNUM, SHLSPD, SQSPD) get smaller when easier.
- **Clamping:** the result is clamped to [min, max].
- The **count tables are never adjusted.**

**Factory default:**
- The blue-label ROM default is **3** (S1 RRTESTB l.75 `FCB $03 ;GA1`, changed by the repo maintainer "to match robotron.sb7"). The md5 checks in the Makefile tie it to the real ROM.
- S2 (the raw listing) has `$02`.
- S3 quotes Larry DeMar: "In that 2nd release the default difficulty was also moved down from 5 to 3."
- The CMOS comment in S3 l.302 reads "0 = extra liberal, 5 = recommended, 10 = extra conservative".
- **CORROBORATED: the default difficulty is 3.** With 3, waves 1–13 are about 11% easier unless the player has ≥ 3 lives on waves 5–13.
- **To reproduce the raw table exactly, use difficulty 5.**

### 2.5 "Bozo" assist table — VERIFIED RRG23 l.471-513, CORROBORATED S3 l.4005-4030

**When it applies.** On each (re)start of waves 1–4, if:
- the player is on their **last life**, or
- (waves 1–2 only) the player has fewer than (NSHIP − 1) lives,

then (CDPTIM, ENSTIM, ROBSPD, RMXSPD) are overridden as follows:

| Wave | CDPTIM | ENSTIM | ROBSPD | RMXSPD |
|---|---|---|---|---|
| 1 | 38 | 96 | 30 | 15 |
| 2 | 38 | 96 | 25 | 12 |
| 3 | 36 | 48 | 20 | 10 |
| 4 | 30 | 30 | 15 | 7 |

### 2.6 Defaults

All VERIFIED in RRTESTB l.64-76 and RRG23 l.72-93.
- **Lives per game:** `NSHIP = 3`. The count is decremented as each life starts, so the game begins with 2 reserve men shown.
- **Extra man:** `REPLAY = $25` is converted into BCD score bytes as 0002 5000, i.e. **every 25,000 points**.
  - The source comment "REPLAY @10,000" is stale. The arithmetic in RRG23 l.81-93 and the check in RRS22 l.1486-1513 give 25,000, and the next threshold is +25,000 each time.
  - **VERIFIED by computation.**
- **Lives display:** reserve men are drawn up to a maximum of 7 icons (RRG23 l.1271-1301).

---

## 3. Player

**Controls (CORROBORATED: RRF l.35-49 + MAME S4 l.941-955):**
- Two 8-way sticks, each made of 4 switches.
- Move: PIA2 bits 0–3 = Up, Down, Left, Right.
- Fire: PIA2 bits 6–7 = Up, Down; PIA3 bits 0–1 = Left, Right.

**Movement (VERIFIED, RRG23 l.684-770):**
- It runs **every frame** from the IRQ.
- A table maps the 4-bit stick state to (dx, dy):
  - **Horizontal:** dx = ±1 byte-unit is added as `$0080` to the 16-bit X, i.e. **1 pixel per frame**.
  - **Vertical:** **1 scan line per frame**.
  - **Diagonals** move 1 px and 1 line per frame.
- **Opposing or invalid combinations give no movement:** U+D (0011), L+R (1100), any 3- or 4-switch combination, and 0111/1011/1101/1110/1111.
- **Clamping:** X is held to bytes [XMIN=7, XMAX−3=140] and Y to [YMIN=24, YMAX−11=223]. The move is simply rejected if it would leave the box (each axis separately).
- **Size and animation:** the player sprite is 4 bytes × 12 lines (8×12 px) (l.1392). The walk animation advances every 2 frames (l.737-742). Sequences are 4-frame L/R/D/U cycles. Diagonals use the L or R animation.
- **Start position:** `PCOORD = $4A7C`, i.e. byte X 74 (≈ pixel 148), Y 124 (RRF l.78).

**Firing (VERIFIED, RRG23 l.911-986):**
- The laser process polls the fire stick every frame.
- **If the 4-bit fire state changes**, the hold counter resets and nothing fires that frame.
- **While the state is unchanged**, the counter increments each frame. It **fires on count 2, then every count that is a multiple of 8.** So the first shot comes 2 frames after the direction settles, the next 6 frames later, then **one shot every 8 frames** (~7.5 shots/s) while held.
- **Shot cap:** at most **4 player shots on screen** (`LCNT < 4`). If 4 are out, the counter is held so a shot fires as soon as a slot frees.
- **Neutral fire stick:** no shot (table entry 0 is empty).
- **Opposing directions** (U+D, L+R) and 3-switch combinations: no shot (empty entries, or index ≥ 11 rejected).
- **Shot speed (each frame):**
  - Horizontal: 3 bytes = **6 px/frame**.
  - Vertical: **6 lines/frame**.
  - Diagonal: 6 px + 6 lines/frame (l.988-1208).
- **Shot images:** horizontal 3 bytes × 1 line (6 px long); vertical 1 byte × 6 lines; diagonal 3 bytes × 6 lines (l.1367-1389).
- **Spawn offsets from the player's top-left (bytes, lines):**

| Direction | Offset |
|---|---|
| Up | (+2, −1) |
| Down | (+2, +4) |
| Left | (0, +4) |
| Right | (+2, +4) |
| Up-left | (0, 0) |
| Down-left | (0, +4) |
| Up-right | (+2, 0) |
| Down-right | (+2, +4) |

- **What a shot hits:** each frame the shot is collision-tested against Electrodes, then the robot list, then motion objects. It **dies on the first hit** (one kill per shot) or at the wall, where it makes a brief wall "glow" (l.1027-1100, 1210-1234).
- **Shots pass through family members** (the human list is not tested).
- The shot's direction (`LASDIR`) is passed to the victim. It sets the explosion direction and the Hulk push-back.

---

## 4. Enemies and family

### Grunt ("ROBOT") — VERIFIED RRP8 l.148-249

**Movement:**
- One manager process loops every 4 frames.
- Each grunt counts down a timer. When it reaches 0, the timer is reloaded to RANDU(ROBSPD) and the grunt steps **toward the player on both axes at once**:
  - **X:** ±2 bytes (**4 px**), only if not already level (ties go +X).
  - **Y:** ±4 lines, unless within about 1 line.
- Movement is clamped to the playfield.
- **Example:** wave 1 has ROBSPD=20, so a grunt steps every 4–80 frames (mean ≈ 42).
- Each step cycles a 3-frame walk animation. A "robot move" sound plays when any grunt moved in that pass.

**Speed-up (two mechanisms):**
1. **Per grunt killed:** ROBSPD ← ROBSPD × 0xE0/256 (×0.875), but never below RMXSPD (l.241-246).
2. **Over time** (RRG23 l.437-458): every 15×15 = **225 frames** (the first after 18×15 = 270), and only while fewer than 30 grunts remain:
   - RMXSPD −= 1 and ROBSPD −= 2.
   - **If the player scored nothing since the last check, it is doubled:** RMXSPD −= 2 and ROBSPD −= 4 ("BONE HIM FOR STALLING").
   - RMXSPD has a floor of 1. ROBSPD has a floor of RMXSPD.

**Interactions:**
- **Grunts and Electrodes:** each step, the grunt is collision-tested against Electrodes. On contact **both the Electrode and the Grunt are destroyed**, and the **player is credited 100** (ROBKIL scores unconditionally).
- **Grunts do not harm family** (they are never tested against the human list).
- **Size:** 5 bytes × 13 lines (RRP8 l.670).

### Electrode ("POST") — VERIFIED RRP8 l.100-146, 250-290; RRG23 l.371-401

- **Stationary.**
- **Kills the player on touch** (player collision tests the post list, RRG23 l.809-813).
- **Destroyed by:** player shots (**0 points**: there is no SCORE call in PSTKIL), Grunts (above), and Hulks walking into them (RRH11 l.66-69).
- **Family members steer around them** (CKOBS, RRH11 l.345-352).
- **Shape by wave** (DERIVED from WCTAB "POST IMAGES" offsets `00,10,20,30,40,50,70,80,00,60` with 16-byte entries). Index = (wave−1) mod 10:

| Waves | Shape |
|---|---|
| 1 | star |
| 2 | snowflake |
| 3 | square |
| 4 | triangle |
| 5 | vertical bar |
| 6 | diamond |
| 7 | spike |
| 8 | spiral |
| 9 | star |
| 10 | "2084" |

- **Colours:** wall, post and laser-glow colours also cycle with period 10 (l.393-401).
- **Death animation:** 3 frames shrinking, with 6/3/2-frame holds.
- **Size:** mostly 5 bytes × 9 lines.

### Hulk — VERIFIED RRH11 l.29-261

**Movement:**
- **Only up, down, left or right** ("MOVE UP,DOWN,L,R ONLY").
- One step every **HLKSPD frames** (wave 1: 8, down to 5 from wave 12).
- **Step size:** horizontal alternates 3 and 4 px (dx bytes −1.5/−2 → table $FD/$FC); vertical is 2 lines.

**Targeting:**
- At spawn, **~75%** (SEED ≤ $C0) are given a family member as target (round-robin through the human table). The rest target "nobody" and therefore chase the **player**.
- If the target is dead, the Hulk goes after the player.
- **Direction choice:** every 1–32 steps a new direction is chosen. It alternates between seeking the target's X and Y, with ±16 random jitter.

**Interactions:**
- **Kills family on contact** (skull + sound, no score) and **destroys Electrodes**.
- **Shot by the player:** **not killed.** It is pushed in the shot's direction by 1 or 2 bytes horizontally (random) and 1 or 2 lines vertically (random), bounded to the playfield (l.98-137).
- **Counts:** never decrements a counter (invulnerable). Not counted toward the wave end.
- **Size:** 7 bytes × 16 lines.

### Family (Mommy, Daddy, Mikey) — VERIFIED RRH11 l.320-503

**Movement:**
- One step every **8 frames**.
- **8 directions.** Each step is 1 or 2 px horizontally (alternating, average 1.5 px) and/or 1 line vertically.
- **New random direction** every 1–128 steps, or immediately on hitting an Electrode or the wall.

**Rescue (player touch):**
- Score **1000 × n**, where n = rescues so far in this life/wave, **capped at 5** (1000, 2000, 3000, 4000, 5000, 5000…).
- The value shows as a floating score sprite for 60 frames.
- The counter `SAVCNT` is cleared in `PLINIT`, which runs at **every wave start and after every death** (RRG23 l.155-156, 672). So the bonus also resets when you die.

**Other deaths:**
- **Killed by a Hulk:** skull for 90 frames, no score.
- **Killed by a Brain:** becomes a Prog (no skull).

**Sizes:** Mom 4×14, Dad 5×13, Mikey 3×11 (bytes × lines).

### Brain — VERIFIED RRB10 l.21-395

**Spawn:** near a random screen edge, within 16 bytes of left/right or 32 lines of top/bottom.

**Movement:**
- Targets the **nearest family member by Manhattan distance**. With no family left, it targets the player.
- **Speed:** one step per **BRNSPD frames** (8 → 6).
- **Each step:** ±1 byte (2 px) in X, but only when more than 2 bytes away, **and** ±1 line in Y. So it moves diagonally.

**Reprogramming:**
- Triggered when the Brain is within 3 bytes / 3 lines of its target.
- The human is pulled beside the Brain and flickers for 20 iterations × ~4 frames (~80 frames).
- The human then turns into a **Prog**, and the Brain picks a new victim.
- **Killing the Brain mid-conversion** turns the human into a skull.

**Score:** 500.

**Cruise missiles:**
- Each Brain fires when its shot timer (RMAX(BSHTIM) brain steps) expires.
- **At most 8 cruise missiles exist at once** (`BCMCNT`).
- **Speed:** 1 byte (2 px) and/or 1 line per move, 2 moves per 2-frame wake, so ~**2 px and 1 line per frame**.
- **Steering:** it re-aims every RMAX(7) wakes toward the player ±6 jitter, picking X and/or Y.
- It **reflects off the walls**, leaves a trail of 8 points, and **never expires** on its own.
- **Shootable** for **25** points (RRB10 l.616-772).
- **Size:** 7 × 16.

### Prog — VERIFIED RRB10 l.396-566

- Moves every **3 frames**: 2 bytes (4 px) horizontally **or** 4 lines vertically.
- Heads toward the player plus a random offset. It re-picks the axis or target at random (~11% chance each step) or when blocked.
- Drawn with a trailing "shadow" of earlier images.
- **Score:** 100.
- Kills the player on contact (it is in the robot list).

### Sphereoid ("CIRCLE") — VERIFIED RRC11 l.23-250

**Spawn and movement:**
- Spawns at the **left or right edge** (XMIN+2 or XMAX−8), random Y.
- **Movement physics:** every 2 frames it adds a random acceleration and then damps.
  - **Velocity limits:** X ±1 byte/frame (2 px/frame), Y ±2 lines/frame.
  - **Damping:** X loses v/64 and Y loses v/128 per update.
  - A new random acceleration is picked every 1–15 updates. X acceleration is ±16/256 and Y acceleration is ±32/256.
  - The IRQ applies the velocity every frame.

**Enforcer drops:**
- **First drop timer:** RMAX(CDPTIM) × 10 frames.
- **Then:** one Enforcer every RMAX(CDPTIM/4) × 16 frames.
- **Number per Sphereoid:** ceil(RMAX(ENFNUM)/2), so 1–5 (1–6 from wave 21).
- **No drop if 8 Enforcers are already alive** (`ENFCNT >= 8`). The Sphereoid waits.
- After its last drop it **flies horizontally off-screen at 1 byte/frame** and is removed (counted as gone).

**Score:** 1000.

### Enforcer — VERIFIED RRC11 l.251-427

**Appearance and movement:**
- Grows in through 5 images × 8 frames (~40 frames).
- Every 3 frames, it picks a new velocity every 0–31 ticks. The velocity is (player position ± up to 16 bytes/lines random − own position) × 2/256 per frame, so it closes the gap in ≈ 128 frames.

**Shooting:**
- Fires a **spark** when its shot timer (RMAX(ENSTIM) × 3 frames) expires.
- **At most 20 sparks globally** (`SPKCNT`).
- **Initial spark velocity:** (player ±16 random − enforcer) × 4/256 per frame, so it reaches the aim point in ≈ 64 frames.
- **Curve:** a random acceleration of ±16/256 is added every 4 frames.
- **Lifetime:** 20–35 ticks × 4 frames.
- **Sparks are shootable: 25 points.**

**Score:** 150.

### Quark ("SQUARE") and Tank — VERIFIED RRTK4 l.24-603

**Quark spawn and movement:**
- Spawns at the **top or bottom edge** (Y = YMIN+2 or YMAX−14), random X.
- **Velocity:** X = RMAX(SQSPD) × 4/256 bytes/frame and Y = RMAX(SQSPD) × 8/256 lines/frame.
  - The sign of each axis is random, but it points away from an edge it is near.
  - Velocity is re-rolled every 1–32 ticks (3 frames each).
- **Maximum speed** (wave 1, SQSPD 50): ~0.78 byte (1.6 px) and 1.6 lines per frame.

**Tank drops:**
- **First drop:** RMAX(TDPTIM) × 15 frames.
- **Then:** every RMAX(TDPTIM/2+1) × 3 frames.
- **Number per Quark:** ceil(RMAX(ENFNUM)/2).
- **At most 20 Tanks alive.**
- After its last drop it **flees vertically at 2 lines/frame** and is removed.

**Quark score:** 1000.

**Tank:**
- Grows in through 4 images × 12 frames.
- **Movement:** 1 px horizontally plus (if more than 16 lines away vertically) 1 line per **2 frames**.
- **Target:** ~38% of the time (SEED ≤ $60) heads for the player, otherwise for a random point. New direction every 1–32 steps, or when blocked by the wall.
- **Fires** every TNKSHT steps (the first one +0–31 steps later).
- **Score:** 200.

**Tank shells:**
- **Direct shots (50%):** aimed at the player ±16. Velocity = distance × SHLSPD/256 × 8/256 per frame. For SHLSPD 176, that reaches the aim point in ≈ 46 frames.
- **Bank shots (50%):** aimed at a point on a wall, chosen so the rebound heads toward the player's half.
- **Wall bounces:** shells **bounce off the walls** (velocity complemented), checked every 2 frames, with a rebound sound.
- **Lifetime:** 48–79 ticks × 2 frames.
- **Score:** 25 when shot.
- **Shell cap and an apparent bug:** at most about 21 shells (`SHLCNT > 20` blocks firing). As written, `SHLCNT` is decremented only when a shell is *shot*, not when it times out (`SHLDIE`). It is reset at each life/wave start (`PLINIT`). Treat this as source behaviour; I did not confirm it in play.
- **Size:** Tank 7 × 16. Quark 8 × 15.

---

## 5. Scoring

All **CORROBORATED**: the SCORE(A=power-of-ten selector, B=BCD) calls are decoded from SRC RRS22 l.1450-1484. The values match common knowledge and the Prog/Brain/Quark values in S3.

| Event | Points | Source |
|---|---|---|
| Grunt | 100 (`$0110`) | RRP8 l.237. Also awarded when a grunt dies on an Electrode |
| Hulk | — invulnerable, pushed back | RRH11 l.98-137 |
| Brain | 500 (`$0150`) | RRB10 l.392 |
| Prog | 100 (`$0110`) | RRB10 l.564 |
| Cruise missile | 25 (`$0025`) | RRB10 l.768 |
| Sphereoid | 1000 (`$0210`) | RRC11 l.203 |
| Enforcer | 150 (`$0115`) | RRC11 l.423 |
| Spark | 25 | RRC11 l.458 |
| Quark | 1000 (`$0210`) | RRTK4 l.120 |
| Tank | 200 (`$0120`) | RRTK4 l.346 |
| Tank shell | 25 | RRTK4 l.599 |
| Electrode | 0 | RRP8 l.250-267 (no SCORE call) |
| Human rescue | 1000/2000/3000/4000/5000, capped at 5000, reset each life/wave | RRH11 l.449-468, 496-503 |
| Extra man | every 25,000 (factory) | §2.6 |

**Score format:** 8 BCD digits stored, 7 shown (RRS22 l.1534-1547). The active player's score flashes.

---

## 6. Collision, death, wave start, restart

**Collision (VERIFIED, RRS22 l.1034-1159):** a software, **two-phase** test.
1. Axis-aligned bounding boxes, in byte X / line Y.
2. Over the overlapping rectangle, both sprite images are scanned **byte by byte**. A hit needs a non-zero byte in both at the same position. It is **pixel-mask collision at 2-pixel horizontal resolution**, not a blitter-based test.

Some objects (Sphereoids, Quarks, cruise missiles) give a fixed "phony" fat image for collision against shots (`FONIPC`), but the player is always tested with real images ("NO PHONIES ON PLAYER").

**Player collision** runs every frame in this order (RRG23 l.802-824):
1. the robot list (Grunts, Hulks, Brains, Progs, Tanks, cruise missiles), **fatal**;
2. Electrodes, **fatal**;
3. motion objects (Sphereoids, Enforcers, sparks, Quarks, shells), **fatal**;
4. family, which is a **rescue**.

**Death sequence (VERIFIED, RRG23 l.826-893; RRX7 l.433-469):**
- Sound, then all processes are killed (GNCIDE), so **everything freezes**.
- The player flashes 10 cycles × (2+6) frames = 80 frames.
- Then a 7-step colour fade × 4 frames (28 frames). That is about **1.8 s** in total.
- If no lives remain: "GAME OVER" for 120 frames. In a 2-player game the other player's turn follows.

**After death the same wave restarts with the remaining enemies (VERIFIED, RRG23 l.851-893 and l.104-208):**
- The remaining counts of every type, including family, are saved and restored. Positions are re-randomized and the screen is rebuilt.
- **Enforcers left alive are converted back into Sphereoids:** +floor(n/4), or at least 1 if there are no Sphereoids. The total is capped at the wave's original Sphereoid count.
- Sparks, shells, cruise missiles and Progs are not carried over. Progs are not in the saved list; they simply vanish.
- **Grunt speed:** RMXSPD is restored to its saved value, and ROBSPD is raised to at least that value.
- The rescue bonus multiplier resets.

**Wave start (VERIFIED, RRG23 l.104-208, 211-270):**
- **Brain waves** use the "transporter" (pixel-glitter) materialization, then a fixed 150-frame wait.
- **Other waves** use the zoom "APPEAR" effect:
  - One robot-list object (Grunt, Hulk, Brain, Tank) is started per frame. Every 4th one uses a horizontal appear (l.222-249).
  - After 32 starts, it also expands the refresh list.
- **Then** the player "super appear", vertical then horizontal (6 frames), then a diagonal appear (4 frames).
- **Then** enemies turn on (`STATUS` cleared) **and the player collision process starts in the same step** (l.185-188). So there is **no post-spawn invincibility**. Instead, enemies are inert until that moment, and individual AIs add a short start delay:
  - Grunt manager: NAP 10 (RRP8 l.154).
  - Brain: NAP 12 (RRB10 l.151).
  - Hulk and Tank: poll STATUS every 8 or 15 frames.
- **Spawn safety box:** Grunts, Hulks, Tanks and (with a slightly larger box) Electrodes are never placed inside a box around the player's start. The box shrinks with the wave (RRP8 l.60-86 `SAFTAB`), in bytes X × lines Y:

| Waves | X range (bytes) | Y range (lines) |
|---|---|---|
| 1 | $1A–$7A | $40–$B0 |
| 2 | $1A–$7A | $48–$A8 |
| 3 | $2A–$6A | $50–$A0 |
| 4 | $30–$60 | $54–$9D |
| 5–9 | $35–$59 | $5D–$96 |
| ≥ 10 | $38–$5C | $62–$94 |

- **Unconstrained spawns:** family spawn anywhere at random (with a 1–8 frame stagger). Brains spawn near the edges. Grunts and Electrodes also avoid overlapping existing screen pixels (`ASCAN`).
- **Between waves:** after a wave is cleared, the "marquee" effect plays (RMST) before the next wave.

---

## 7. Screen geometry

**Raw and visible area:**
- **Raw framebuffer:** 304 × 256 (152 bytes × 256 lines, 4bpp; X byte addresses 0..$97). RRF/RRS22 address the screen as `X*256+Y`.
- **MAME visible area for `robotron`:** horizontal pixels 6..297 and lines 7..246, i.e. **292 × 240** (S4 l.1556 `set_raw(..., 512, 6, 298, 260, 7, 247)`). **CORROBORATED.**
- Robotron is horizontal (ROT0) on a 4:3 monitor, so its pixels are not square.

**Playfield (VERIFIED, RRF l.67-70; RRG23 l.1306-1319):**
- **Inner bounds:** XMIN = byte 7 (px 14), XMAX = byte $8F = 143 (px 286–287). YMIN = line 24, YMAX = line 234.
- **Border:** drawn in the per-wave wall colour.
  - Vertical bars are 1 byte (2 px) wide at byte columns 6 and 144 (px 12–13 and 288–289), spanning lines 22–236.
  - Horizontal bars are 2 lines thick at lines 22–23 and 235–236, spanning bytes 7..143.
  - So the **outer rectangle is px 12–289 × lines 22–236**.

**HUD:**
- Above the playfield, at Y = 14 (`P1ORG = $18*256+14`): Player 1 score at byte X $18 (px 48), with the reserve-men icons after it (`P1MAN` ≈ byte $2E, px 92).
- Player 2 is at +$40 bytes (px 176) (RRF l.71-76).
- Each man icon is 3 bytes × 8 lines, spaced 4 bytes apart, up to 7 (RRG23 l.1274-1301, 1354).
- The wave message ("WAVE n") is printed via text message #104. **UNKNOWN:** its exact position. I did not trace RRTEXT; it is believed to be below the playfield.

---

## 8. Licensing and clean-room notes

- **S1, S2, S3 and S5 carry no license** (GitHub reports `license: null`). The 6809 source and ROM data are © 1982 Williams Electronics. The source itself embeds "(C) 1982 WILLIAMS ELECTRONICS INC." (RRG23 l.210).
- S5 explicitly asks not to be distributed.
- MAME's driver is BSD-3-Clause, but it only describes hardware.
- **Recommendation:** use only the *facts* in this report: numbers, rules and timings. Do not copy routine structure, table bytes as binary blobs, sprite bitmaps, colour tables, sounds or text.
  - Re-express the tables as your own data files.
  - Draw new sprites at the documented sizes.
  - Do not reuse the hidden copyright strings or the "Jap zapper" anti-tamper logic.

---

## 9. Gaps and caveats

- **Difficulty default:** 3 in the blue-label ROM (S1 + S3), versus $02 in the raw historicalsource listing. Decide which to emulate; 5 = the unadjusted table.
- **Exact score values on the floating rescue sprite:** they match 1000–5000 (VERIFIED). The UI and sound are not documented here.
- **Not traced:**
  - the exact sound, text and message positions;
  - the transporter timing in detail;
  - the explosion styles (they depend on shot direction);
  - the 2-player cocktail flip.
- **Unconfirmed in play:** the tank-shell counter behaviour (§4) and "Progs don't block the wave end" are read literally from the source. Confirm both in MAME.
- **Release differences:** MAME `robotron` = "Release 5, solid blue label". The S1 build also links `RRELESE6.ASM` patch bytes and md5-verifies the sb ROMs. The yellow-label (`robotronyo`) and 1987 bug-fix sets may differ in details such as the default difficulty and bug fixes (RRCHRIS.ASM fixes the "enforcer diagonal explosion bug").
- **Fan sites (S6/S7) gave nothing usable.** No SECONDARY-only claims are used.
