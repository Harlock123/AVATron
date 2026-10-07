# Research dossier

Research done on 2026-10-07. The detailed notes, with file and line citations, are in
[`docs/research/mechanics-notes.md`](docs/research/mechanics-notes.md) (gameplay, from the original
source) and [`docs/research/hardware-av-notes.md`](docs/research/hardware-av-notes.md) (hardware,
operator manual, audio and visuals, history, ports, licensing). This page summarises them.

Confidence tags:
- **VERIFIED**: read in a primary artifact.
- **CORROBORATED**: two independent sources agree.
- **SECONDARY**: one secondary source only.
- **UNKNOWN**: not found, or the sources conflict.

## Coverage, honestly stated

**Inspected directly:**
- The original 6809 assembly source, two copies of the same listings.
- An independent annotated ROM disassembly.
- The current MAME `williams` driver and its support code.
- The March 1982 operator/instruction manual (Internet Archive OCR).
- Wikipedia.
- Two secondary development histories with first-person Jarvis/DeMar quotes.
- One 2007 primary Jarvis interview.
- Two contemporary 1982–83 magazine articles.
- Port manuals for the 7800, C64, Apple II and Lynx.
- KLOV.
- Licence files of about 15 GitHub reimplementations.
- NuGet package metadata.

**Not inspected:**
- RePlay, Play Meter and Cash Box scans; none were found online.
- The GDC 2014 Jarvis postmortem video; no transcript was available.
- Sean Riddle's Robotron page (404).
- The robotron-2084.co.uk subpages.
- **No frame-by-frame video analysis or MAME runs were performed.** No legally owned ROM was used, so nothing here was confirmed by running the original.

## Source ledger (summary)

The full ledgers, 33 entries across both notes files, record what was inspected and the licence of each source.

| ID | Source | Type | Inspected | Supports | Confidence | Reuse licence |
|---|---|---|---|---|---|---|
| M1 | `historicalsource/robotron` + `mwenge/robotron` (Jarvis/DeMar 6809 source; mwenge's build md5-checks against the Solid Blue Label ROMs) | original source | RRF, RRS22, RRG23, RRP8, RRH11, RRB10, RRC11, RRTK4, RRX7, RRTESTB, RRET, RRTEXT | timing, wave tables, all enemy behaviour, scoring, collision, sizes, sound priorities, palette cycling | High | **None. © 1982 Williams.** Used for facts only |
| M2 | ScottTunstall/Robotron2084 `robomame.asm` | ROM disassembly | wave bytes, difficulty code, DeMar quote | independently confirms the wave table and default difficulty 3 | High | None. Facts only |
| M3 | MAME `src/mame/williams/*.cpp`, `emu/ioport.cpp`, `emu/video/resnet.cpp` | emulator source | screen parameters, inputs, palette network, blitter, sound config | 292×240 at 60.096 Hz, 8-way sticks with opposites cancelled, palette levels, mono DAC, no speech | High | BSD-3-Clause |
| M4 | Instruction manual 16P-3005-101 (Mar 1982), archive.org | operator manual | full OCR | 3 turns, extra man every 25,000, difficulty factory 5 (an early release), initials entry by stick | High (noisy OCR) | © Williams. Facts only |
| M5 | Wikipedia "Robotron: 2084" | encyclopedia | raw wikitext | history, ports, reception | Medium | CC BY-SA |
| M6 | Arcade Blogger, "The Development of Robotron" (2020) | secondary, with quotes | full article | twin-stick origin, six-month schedule | Medium | © author |
| M7 | Game Developer: "History of Robotron" and the 2007 Jarvis interview | secondary / primary | full articles | design intent quotes | Medium | © publisher |
| M8 | Videogaming Illustrated (Dec 1982); Creative Computing V&AG (1983) | contemporary press | articles | point values (these conflict with the source; see below) | Medium | © publishers |
| M9 | Port manuals: 7800, C64, Apple II, Lynx | port documentation | OCR / PDF | how the ports differ, so those differences are *not* imported | High for what they state | © Atari / Shadowsoft |
| M10 | KLOV | database | page | label/difficulty notes | Medium-low | © KLOV |
| M11 | GitHub reimplementations (alldritt, stridera, Ernir, MiSTer, others) | code | LICENSE files and trees | none is both permissively licensed and asset-clean, so nothing was reused | High | see THIRD_PARTY.md |

## Reference version

**MAME set `robotron`, "Release 5, solid blue label"** (Williams / Vid Kidz, 1982):
- The `mwenge` build of the released source reproduces those ROMs (md5 check).
- The default difficulty of 3 is corroborated for that label by the disassembly's DeMar quote and by KLOV.

## Key findings (all used by the engine)

**Display and timing:**
- Raw framebuffer 304×256, visible 292×240.
- Logic runs at 60.096 Hz (8 MHz / (512×260)).
- Playfield is pixels 14–287 by lines 24–234, with the border at px 12–289 × lines 22–236.
- Shown at 4:3, so pixels are about 1.10 : 1. **CORROBORATED.**

**Controls:**
- Two 8-way digital sticks, 4 switches each.
- Opposing switches cancel; MAME locks them out and the source's table treats them as invalid.
- No analog input. **CORROBORATED.**

**Player:**
- Moves 1 px and/or 1 line per frame.
- First shot 2 frames after the fire direction settles, then one every 8 frames, with at most 4 on screen.
- Shots travel 6 px or 6 lines per frame, kill one target each, and pass through the family.
- No shot on a neutral or contradictory fire stick. **VERIFIED.**

**Waves:**
- A 40-row table gives 9 counts and 12 tuning parameters per wave. Waves above 40 subtract 20, so waves 21–40 loop. **CORROBORATED** (source plus ROM bytes).
- Brain waves are 5, 10, …, 40. Quark/Tank waves are 7, 12, 17, 22, 27, 32, 37 and 24. Grunt swarms are waves 9, 19, 29, 39.
- The wave ends when Grunts, Sphereoids, Enforcers, Brains, Tanks and Quarks all reach zero.

**Difficulty:**
- The operator setting runs 0–10. Each point away from 5 scales the timers about 5.5%.
- Easy settings are cancelled from wave 14 on, or from wave 5 if the player has 3 or more men in reserve.
- A "Bozo" table eases waves 1–4 when the player is on their last man. **VERIFIED.**

**Enemies:**
- Each enemy's step period, step size, targeting rule, spawn edge, child limits (8 enforcers, 20 sparks, 20 tanks, 8 cruise missiles) and interactions (grunt + electrode both die; hulks crush family and electrodes; brains reprogram humans into progs in about 80 frames) are **VERIFIED**. Details are in `mechanics-notes.md` §4.

**Scoring:**

| Target | Points |
|---|---|
| Grunt | 100 |
| Brain | 500 |
| Prog | 100 |
| Sphereoid | 1000 |
| Enforcer | 150 |
| Quark | 1000 |
| Tank | 200 |
| Spark, shell, cruise missile | 25 |
| Electrode | 0 |
| Human rescue | 1000 × n, capped at 5000; resets each life and each wave |

- Extra man every 25,000 (the source comment says 10,000 but is stale).
- 3 men per game. **VERIFIED.**

**Collision and death:**
- Collision is a bounding box first, then the two images are compared.
- Death freezes everything for about 1.8 s. The same wave then restarts with the survivors at new random positions, and enforcers fold back into sphereoids.
- There is no post-spawn invincibility. Enemies are simply inert until the wave goes live, and each type has its own start delay.
- No enemy may spawn inside a safety box around the player start, and the box shrinks as waves advance. **VERIFIED.**

**Audio:**
- Mono, from a 6808 CPU driving an 8-bit DAC.
- One sound at a time through a priority sequencer: equal or higher priority interrupts.
- No music and **no speech** (speech is Sinistar). **VERIFIED.**

**Palette:**
- 16 of 256 colours on screen at once. Each byte is BBGGGRRR through a resistor DAC.
- Colour cycling works per palette slot. The border colour changes every wave. **VERIFIED.**

## Conflicts between sources (and what we chose)

| Topic | Disagreement | Choice |
|---|---|---|
| Default difficulty | Manual (Mar 1982) says 5. KLOV says 5 for the yellow label and 3 for the blue label. The dev snapshot says 2. The mwenge Release 5 build and the DeMar quote say 3 | **3** (blue label = MAME `robotron`), adjustable 0–10 in Settings |
| Point values | The original attract text and SCORE calls give Enforcer 150, Tank 200, Missile 25. Magazines from 1982–83 give 200, 300, 75 (shell 50) | **Source values.** The magazines may describe an earlier release or simply be mistaken; this is unresolved |
| Extra-man comment | Source comment says "10,000"; the arithmetic gives 25,000; the manual says 25,000 | **25,000** |
| Twin-stick origin story | Retellings say a broken arm; the Jarvis quote says his right hand was shattered in a car accident | We record the quote (history only) |
| Cabinet sales | About 19,000 (Wikipedia) vs about 23,000 (Arcade Blogger) | Not used |
| "Shot-in-the-corner" fix | KLOV says the blue label fixed it; MAME says the 1987 patch did | Not modelled; see open questions |

## Open questions (need a MAME run with a legally owned ROM, or more primary material)

1. **Unconfirmed in play:**
   - Is the tank-shell counter really lowered only when a shell is shot? The source reads that way, and the engine reproduces it.
   - Do live progs really not block the end of a wave?
2. **Wave-transition timing:** the length of the inter-wave marquee and the per-object zoom-in are reconstructed (72 frames and 16 frames).
3. **Explosion shapes:** these depend on the shot direction and were not traced.
4. **Sound timbres:** what each sound-ROM command actually synthesises is undocumented. All our sounds are designed by ear to the described character.
5. **Flicker:** how much flicker the real hardware produces under load was not measured. Ours is a budget-based approximation.
6. **Exact `RMAX` edge cases and some unit interpretations:** for example, whether a ±16 jitter is in bytes or lines. Each one is marked in code comments and FIDELITY.md.
7. **High-score table shape:** the original has one champion plus 36 visible all-time entries, a separate "Today's Greatest" list of 10, and a limit of 5 entries per player. We implement a top-10 table, as the project brief required (see FIDELITY.md).
8. **Release differences:** yellow-label vs blue-label behaviour, and the 1987 bug fixes.
