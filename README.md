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

## Development

```bash
cd sim
dotnet test           # all game-logic tests, no Unity needed
```

The Unity project (from Phase 2) is opened with Unity 6 LTS on a developer machine; the simulation itself is fully testable headless.
