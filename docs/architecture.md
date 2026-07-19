# Architecture

## The one principle

**The simulation never touches Unity. Unity never mutates the simulation.**

```
┌─────────────────────────── unity/ (view) ────────────────────────────┐
│  Input → OrderFactory ──► Order queue                                │
│  Renderer ◄── interpolate(prevTick, currTick, alpha) ◄── SimSnapshot │
└──────────────────────────────┬───────────────────────────────────────┘
                               │ Orders in / events+state out
┌──────────────────────────────▼───────────────────────────────────────┐
│  sim/ (pure C#, netstandard2.1, deterministic)                       │
│  Game.Tick(orders[]):  15 Hz fixed                                   │
│    systems: production, economy, movement, combat, powers, AI        │
│  World: entities = component sets loaded from data/*.json            │
│  Math: Fixed (16.16), CellPos/LeptonPos (int), DeterministicRandom   │
│  Hash(): FNV state hash for desync detection + regression tests      │
└──────────────────────────────────────────────────────────────────────┘
```

## Why

1. **Testability here.** No Unity Editor exists in the dev environment. Every game rule is exercised by `dotnet test` — including complete headless AI-vs-AI games. Unity-side code stays thin (render + input only), minimizing untested surface.
2. **Lockstep multiplayer.** Broad multiplayer for an RTS = deterministic lockstep (bandwidth is orders, not state). That demands: integer math, seeded RNG, no wall clock, canonical iteration order, orders-only mutation. Baking this in later is a rewrite; baking it in now is nearly free.
3. **Replays/observers/AI for free.** Everything — player, AI, network, replay file — is just an `Order` producer.

## Determinism rules (enforced, non-negotiable in `sim/`)

- No `float`/`double` in simulation state or logic. Use `Fixed` (32-bit, 16.16) and integer leptons (256/cell, as the original engine).
- No `System.Random`, `DateTime`, `Environment.TickCount`, `Guid.NewGuid`. Use `DeterministicRandom` (seeded, part of hashed state).
- No LINQ over unordered collections in sim logic; dictionaries iterated via sorted keys or entity-id order.
- All state reachable from `World`; `World.ComputeHash()` covers positions, HP, credits, RNG state, queues.
- Regression gate: same seed + same order log ⇒ same hash after N ticks, asserted in CI.

## Data-driven content

`data/units.json`, `structures.json`, `weapons.json`, `warheads.json`, `techtree.json`, `terrain.json`, `superweapons.json` — component composition per blueprint (OpenRA trait pattern):

```json
{
  "id": "mbt_walker",
  "components": {
    "Health": { "max": 400, "armor": "heavy" },
    "Mobile": { "speed": 4, "locomotor": "walker", "rot": 5 },
    "Turreted": { "rot": 5 },
    "Armament": { "weapon": "cannon_120" },
    "Buildable": { "cost": 800, "queue": "vehicle", "prerequisites": ["factory"], "techLevel": 3 }
  }
}
```

`TiberiumDusk.Balance` loads + schema-validates JSON, exposes immutable blueprint objects to the sim. Locale files (`data/locale/he.json`, `en.json`) hold every display name — code and data IDs are language-neutral and EA-name-free.

## Repository layout

| Path | Contents | Toolchain |
|---|---|---|
| `docs/` | GDD, architecture, roadmap, research, balance changes | — |
| `sim/TiberiumDusk.Sim` | deterministic core | netstandard2.1 |
| `sim/TiberiumDusk.Balance` | JSON loading/validation | netstandard2.1 |
| `sim/TiberiumDusk.Sim.Tests` | xUnit tests, determinism + headless games | net8.0 |
| `data/` | all game content JSON + locales | — |
| `unity/` | Unity 6 LTS project (URP), references sim sources via asmdef | Unity (user's machine) |
| `server/` | lockstep relay (Phase 9) | net8.0 |
| `reference/` | original-game INI files, **git-ignored** (EA copyright) — local numeric reference only | — |

Unity consumes the sim as **source**: `unity/Assets/Sim.asmdef` + a symlink-free junction (the sim `.cs` files are included via `csc` compatible layout; simplest robust approach: the Unity project contains `Assets/Sim/` with the same sources added as a package reference `file:../../sim` — decided in Phase 2, with the constraint that sim sources stay single-sourced under `sim/`).

## Networking (Phase 9 target, designed for now)

- Delay-based lockstep: client sends orders for tick T+delay (2–4 ticks); server relays ordered bundles; all clients execute identically.
- Transport: WebSocket (WebGL-compatible); server = C# host reusing `TiberiumDusk.Sim` for order validation and optional headless verification.
- Desync: periodic state-hash exchange; mismatch → report + dump. Replay = initial state + order log.

## Testing strategy

- Unit tests per system (economy math, Verses damage, pathfinding, queue logic).
- Property tests: determinism (seed ⇒ hash), fixed-point arithmetic.
- Scenario tests: scripted order logs asserting outcomes (harvester round-trip credits, engineer capture, EMP timing).
- Full-game tests: built-in AI vs AI, N ticks, assert no exception + hash stability across two runs.
- CI: GitHub Actions `dotnet test` on every push.
