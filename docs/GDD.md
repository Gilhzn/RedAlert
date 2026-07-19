# Tiberium Dusk — Game Design Document

A 3D real-time strategy game inspired by the mechanics and atmosphere of classic late-90s isometric RTS (Command & Conquer: Tiberian Sun). All names, art, audio and text are original. Game rules and numeric balance are derived from the publicly documented behavior of the original engine (facts and numbers; see `docs/research/`).

> Deep references (exhaustive, with exact numbers):
> - [research/mechanics.md](research/mechanics.md) — full engine mechanics spec
> - [research/units-and-tech.md](research/units-and-tech.md) — full rosters, stats, tech trees
> - [research/visual-and-architecture.md](research/visual-and-architecture.md) — style guide + technical architecture

## 1. Vision

- **Genre:** classic base-building RTS. Harvest → build → tech → destroy the enemy.
- **Presentation:** full 3D (Unity, URP), fixed isometric-style camera with modern zoom, in a dark post-apocalyptic world being consumed by an alien crystal ("Tiberium" in research docs; shipping name **Veridium** — final naming in locale files).
- **Two asymmetric factions:**
  - **Dominion (GDI-analog):** slab geometry, gunmetal + amber, walker mechs, sonic and orbital tech, firestorm defense.
  - **Serpent Order (Nod-analog):** blades and scorpion silhouettes, black + crimson, lasers, stealth, subterranean warfare, cyborgs, chemical weapons.
- **Targets:** Skirmish vs AI first; then broad multiplayer (2–8, lockstep). Bilingual UI (Hebrew RTL + English) from day one.

## 2. Core loop

1. Deploy MCV → Construction Yard.
2. Build power → refinery → harvest Veridium fields (green = common, blue = valuable but explosive).
3. Expand base within build radius; manage power budget (low power slows construction, disables defenses/radar).
4. Climb the tech tree → unlock units, defenses, superweapons.
5. Scout (shroud + optional fog), raid, defend, out-tech and destroy all enemy structures/units.

## 3. Simulation model (authoritative summary)

| Aspect | Rule |
|---|---|
| Tick rate | 15 sim ticks = 1 game second; render interpolates |
| Grid | square cells; positions in **leptons**, 256×256 per cell; heights in levels (~128 leptons) |
| Determinism | integer/fixed-point math only, seeded RNG, order-based input, state hash every N ticks |
| Terrain | height levels, ramps, cliffs; per-land-type speed table per locomotor; deformation craters; destructible/repairable bridges; cracking ice |
| Locomotors | foot, tracked, wheeled, hover, amphibious, subterranean, walker (mech), jumpjet, aircraft |
| Economy | green 25/bail grows+spreads, blue 40/bail explosive; harvester 28 bails; refinery stores 80, silo 60; weed → chemical missile |
| Construction | 4 parallel queues (structures/infantry/vehicles/aircraft), up to 4 queued + 1 building; adjacency placement; sell 50%; repair 20% of cost pro-rata |
| Combat | 5 armor classes × warhead Verses %; projectiles (direct/arcing/homing/beam/rail); veterancy 2 ranks (+25% dmg/armor, +30% speed, +20% ROF) |
| Special | EMP (duration=damage frames, affects vehicles+cyborgs+powered defenses), stealth+sensors, ion storms, crates, tiberium lifeforms |
| Superweapons | Ion Cannon 8.5min/751dmg · Multi-Missile 10min · Chem Missile weed-fueled · EMP Cannon 4.5min (both sides) · Firestorm barrier (charge/drain) · Hunter-Seeker 12min |
| Win | destroy all enemy units+structures (skirmish); options: MCV redeploy, crates, shroud regrow |

Full numeric tables (movement %, warhead Verses, weapon stats, costs, prerequisites): see research docs — those values are the **baseline balance data** and live in `/data/*.json`.

## 4. Factions (full rosters in research/units-and-tech.md)

### Dominion (GDI-analog)
Infantry: rifle, disc grenadier, medic, engineer, jumpjet trooper, railgun hero (mutant).
Vehicles: scout mech (anti-inf), main battle mech, amphibious APC, hover missile platform, sonic disruptor, super-heavy quad mech (limit 1), harvester, MCV, mobile sensor array.
Air: VTOL fighter, heavy bomber, carryall.
Defense: modular component tower (autocannon/rocket/AA plugs), concrete walls, gates, firestorm barrier.
Superweapons: orbital ion beam, hunter-seeker, (expansion: drop pods).

### Serpent Order (Nod-analog)
Infantry: rifle, rocket trooper, engineer, cyborg (heals in crystal, EMP-vulnerable), cyborg commando (limit 1), vehicle hijacker.
Vehicles: fast buggy, attack cycle, entrenching tank (deploys → concrete armor), mobile artillery (deploys to fire), subterranean flame tank, subterranean APC, stealth tank, mobile repair, weed harvester.
Air: anti-infantry helicopter, plasma stealth bomber.
Defense: laser turret, SAM, obelisk beam tower, laser fence, stealth generator (12-cell cloak field).
Superweapons: cluster missile, chemical missile (weed-fueled), hunter-seeker, EMP cannon.

Tech trees: both factions ConYard → Power → Barracks/Refinery → Factory → Radar → Tech Center → superweapon tier, with faction-unique branches. Exact dependency graphs in research doc §6.

## 5. Visual identity (full guide in research/visual-and-architecture.md §A)

**The one rule:** the world is cold and desaturated; only light sources are saturated — green/blue crystal glow, lasers, fires, muzzle flashes, obelisk charge. 

- Terrain: blue-grey/olive temperate + snow themes, layered cliffs, persistent craters/scorch.
- Camera: orthographic-feel at 45° yaw, ~40° pitch, zoom via frustum, no free rotation (optional 90° steps).
- Units: low-poly (300–1500 tris), chunky voxel-descendant silhouettes, hull/turret split, slope-conforming tilt, stepped retro animation (12–15 fps sampling on walk cycles).
- UI: right-side sidebar — radar top (noise when unpowered), two cameo columns with radial build-sweep, segmented power bar, ticking credit counter; phosphor-green terminal typography over dark gunmetal; EVA-style synthetic announcer (original TTS+bandpass).
- Audio: dark ambient / electro-industrial score (original), servo footsteps, concussive cannons, crisp lasers, crystal shimmer near fields.

## 6. Modernizations (deliberate deviations from the original)

Tracked in `docs/balance-changes.md` as they land. Initial list:
1. 3D rendering with real lighting/bloom; optional 90° camera rotation steps.
2. Tabbed sidebar (structures/defense/infantry/vehicles/air) — an RA2-style evolution.
3. Multi-select production, rally points everywhere, control-group UX, attack-move, smart-cast superweapons.
4. Flow-field group movement (original A* felt clumpy); formation move preserved-speed.
5. Modern MP: reconnect grace, replays, observers, ranked-ready lockstep.
6. Accessibility: colorblind-safe team colors, scalable UI, remappable keys, he/en.

## 7. Technical architecture (full details in research/visual-and-architecture.md §B + docs/architecture.md)

- `sim/` — pure C# netstandard2.1 deterministic simulation, no UnityEngine. Tested headless with `dotnet test` (unit + full AI-vs-AI determinism runs).
- `data/` — all unit/structure/weapon/warhead/techtree definitions as JSON (component/trait composition, OpenRA-style).
- `unity/` — Unity 6 LTS + URP client: renders sim state with interpolation, produces Orders only. Targets: Desktop + WebGL.
- `server/` — (Phase 9) C# WebSocket lockstep relay reusing the sim assembly for validation.
- Input → `Order` queue → executed at scheduled tick → sim mutates → view interpolates. Single-player uses the same pipeline (localhost loop) so replays/MP are structural, not bolted on.

## 8. Roadmap

See [roadmap.md](roadmap.md) — 10 phases from foundations to WebGL release, each with a definition-of-done and a ready-to-run prompt.
