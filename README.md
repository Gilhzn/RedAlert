# Tiberium Dusk

**עברית:** משחק אסטרטגיה בזמן־אמת תלת־מימדי בהשראת קלאסיקות ה-RTS האיזומטריות של סוף שנות ה-90 — קצירת משאבים, בניית בסיס, שני צדדים א־סימטריים, סופר־נשקים ומולטיפלייר lockstep. כל הנכסים (שמות, גרפיקה, סאונד) מקוריים.

**English:** A 3D real-time strategy game inspired by late-90s isometric RTS classics — resource harvesting, base building, two asymmetric factions, superweapons, and lockstep multiplayer. All assets (names, art, audio) are original.

## Documentation

| Doc | Contents |
|---|---|
| [docs/GDD.md](docs/GDD.md) | Game design document |
| [docs/architecture.md](docs/architecture.md) | Technical architecture (sim/view split, determinism, networking) |
| [docs/roadmap.md](docs/roadmap.md) | 10-phase build plan with per-phase prompts |
| [docs/research/](docs/research/) | Deep research: mechanics, rosters/tech trees, visual style |

## Structure

- `sim/` — deterministic game core, pure C# (netstandard2.1), no Unity. Test: `cd sim && dotnet test`
- `data/` — all game content as JSON (units, weapons, tech tree, locales he/en)
- `unity/` — Unity 6 client (URP), renders the sim; targets Desktop + WebGL
- `server/` — lockstep relay server (later phase)

## Status

All 10 roadmap phases are implemented: deterministic sim core, 3D Unity client,
economy/construction, combat, full faction rosters + tech trees, superweapons +
ion storms, skirmish AI with fog of war and replays, bilingual UI (he/en) with
procedural audio, lockstep multiplayer over a relay server, and WebGL/desktop
build tooling. See [docs/checklists/](docs/checklists/) for per-phase
verification and [docs/deploy.md](docs/deploy.md) for shipping.

## Development

```bash
cd sim && dotnet test TiberiumDusk.sln    # 105 tests: game logic, AI matches,
                                          # replay reproduction, real-socket lockstep
dotnet run --project server/TiberiumDusk.Server   # multiplayer relay
```

The Unity project (`unity/`) opens with Unity 6 LTS on a developer machine —
run the menu item "Tiberium Dusk → Setup Project" once, then Play. The
simulation itself is fully testable headless.
