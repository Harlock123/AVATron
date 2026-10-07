# Saves and settings

## Locations

| OS | Folder |
|---|---|
| Windows | `%APPDATA%\Robotron2084\` |
| Linux | `$XDG_CONFIG_HOME/Robotron2084/` (default `~/.config/Robotron2084/`) |
| macOS | `~/Library/Application Support/Robotron2084/` (the platform's conventional equivalent) |
| Any | overridden by `--data-dir <path>` or `ROBOTRON_DATA_DIR` |

```
Robotron2084/
  settings.json
  slot1/ highscores-classic.json  highscores-modern.json  suspend-modern.json
  slot2/ …
  slot3/ …
```

- There are three save slots, each holding its own high-score tables and Modern suspend file. Pick one under *Settings → Save slot*.
- Classic and Modern keep separate tables, because Modern's analog aiming makes scores incomparable.

## Formats

Everything is indented JSON with camelCase names, and every file has a `schemaVersion`.

| File | Version | Contents |
|---|---|---|
| `settings.json` | 1 | Fields: preset, slot, operator adjustments (difficulty 0–10, men 1–20, extra man 0–50,000), volumes 0–1, display flags, deadzone, Modern speed 0.25–1, key bindings `{ "MoveUp": ["W"], … }` |
| `highscores-*.json` | 1 | Up to 10 entries: `{initials, score, wave, date}` |
| `suspend-modern.json` | 1 | Fields: `engineVersion`, `saved`, `stateHash`, `state: { rules, checkpoint, inputs, ticksBeforeCheckpoint }` |

## Safety

- **Atomic writes:** each file is written to a temp file in the same folder, flushed to disk, then renamed over the old file. A crash leaves either the old file or the new one, never a half-written one.
- **On load**, three cases fall back to defaults and keep the problem file:
  - a file that doesn't parse;
  - a file that fails validation (e.g. a volume of 7, too many score entries);
  - a file whose `schemaVersion` is newer than this build supports.

  In each case the file is renamed to `<name>.bak-<UTC timestamp>` and a message appears at the bottom of the screen. Nothing is silently overwritten while the bad file is still in place.
- **A missing file** simply means defaults.

## Version migration policy

- `VersionedJsonStore` accepts versions from `MinSupportedVersion` to `CurrentVersion`. Older supported versions go through a `Migrate` function and are rewritten at the current version on the next save.
- Versions outside that range are set aside as above (reset strategy).
- All documents are at version 1, so there are no migrations yet. When a format changes:
  1. Bump `Version`.
  2. Add a `migrate` lambda.
  3. Keep `minSupportedVersion` at the oldest version you can still read.
- **Suspend files** also carry `engineVersion` (`GameSession.EngineVersion`). Resuming replays recorded inputs, so a file from a build whose simulation changed would diverge; such files are rejected as incompatible. **Bump `EngineVersion` whenever simulation behaviour changes.**

## Suspend / resume (Modern only)

**How it works:**
- *Pause → Suspend and quit* writes the last wave-start checkpoint (score, men, wave, RNG state, grunt speed, surviving counts) plus the run-length-encoded inputs since then.
- Closing the window during a Modern game does the same automatically.
- Resuming rebuilds the checkpoint and replays those inputs, which reproduces the game exactly. A state hash is checked afterwards.
- You resume into the pause menu.

**Rules:**
- The file is deleted when you resume, so it can't be used as a repeatable save state.
- **Classic** has no mid-game save, matching arcade convention. A crash-recovery save for Classic is not implemented (see KNOWN_ISSUES.md).
