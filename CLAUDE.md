# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Two parallel tracks — know which one you're touching

This repo contains **two independent implementations** of "Tiberium Dusk," an
original-IP 3D RTS inspired by late-90s isometric strategy games:

1. **The Unity/C# simulation** (`sim/`, `unity/`, `data/`, `server/`) — the
   originally-designed architecture: a deterministic C# sim core, a Unity 6
   client that renders it, JSON game data, and a lockstep relay server. This is
   what `README.md`, `docs/GDD.md`, `docs/architecture.md`, and the CI workflow
   describe. It builds/tests with `dotnet`.

2. **The shipped browser game** (`docs/preview/index.html`) — a single-file,
   self-contained Three.js RTS. **This is what is actually live** at
   https://gilhzn.github.io/RedAlert/ and where essentially all recent active
   development happens. It shares none of the C# code; it is its own complete
   game (economy, combat, AI, fog, menu, progression, cosmetics, campaign,
   tutorial) written in JS.

Most day-to-day feature work (control schemes, economy tweaks, menu, cosmetics,
etc.) targets track 2. Don't assume a change to `sim/` affects the live game.

## Original-IP constraint (hard rule)

No EA/Westwood assets, names, unit names, art, audio, or trademarks anywhere in
committed code or on-screen text — everything is original ("Serpent Order,"
"veridium," "Tiberium Dusk," etc.). Original C&C `.ini`/reference files are
git-ignored (`reference/`) and must never be committed; numeric values may be
re-derived into our own JSON/JS, never the source files.

## Track 2 — the browser game (primary)

### Build pipeline (important, non-obvious)

The live file `docs/preview/index.html` is a **built artifact**, not hand-edited
source. The pipeline is:

```
tiberium-dusk-preview.html   (2D sim + all game/UI logic — THE source)
        + js3d_view.js       (Three.js render layer)
   │  tools/web3d/assemble3d.py   (strips the 2D sprite/draw pipeline, injects the 3D view)
   ▼
tiberium-dusk-3d.html
   │  manual wrap: prepend <!doctype html><html><head>… with a
   │  <meta name="viewport"> and an <!-- build: <tag> --> comment …</head><body>
   ▼
docs/preview/index.html      (committed; pushing it triggers deploy)
```

**Critical gotcha:** the sim/UI source `tiberium-dusk-preview.html` is **not
tracked in the repo** — it lives in the session working scratchpad, and
`assemble3d.py` reads it from a hard-coded absolute `SP = ".../scratchpad"`
path. Only the *built* `docs/preview/index.html` and the *render* source under
`tools/web3d/` are committed. If you need to edit game logic and no scratchpad
preview exists, the committed `docs/preview/index.html` is the effective
source of truth to work from. When you change the render layer, sync your
edited `js3d_view.js` back into `tools/web3d/js3d_view.js`.

The viewport `<meta>` added during the wrap is what makes the mobile
`@media (max-width: 760px)` rules apply — the raw `tiberium-dusk-3d.html` lacks
it, so test mobile against the wrapped `docs/preview/index.html`, not the raw
build.

### Render layer contract (`tools/web3d/`)

`js3d_view.js` (+ `js3d_core.js`, `js3d_units.js`, `js3d_structures.js`) build
all 3D models procedurally through the `H.*` DSL documented in
`tools/web3d/js3d_spec.md`. The world is **Z-up** (ground = XY, height = +Z),
1 unit = 1 map tile, model front faces +Y — matching the Blender builders in
`tools/blender_*.py`. `setTeamColor(hex)` retints the player's units by
rebuilding views.

### Browser-game internal architecture (single file)

- **Sim/view split:** the sim owns state and logic; `js3d_view.js` only reads
  `ents` each frame and renders. `entHidden(e)` gates rendering by the fog
  arrays (`explored`/`visible`).
- **Grid:** `MAP = 128`, cell index `idx(x,y) = y*MAP + x`. Terrain/fog are flat
  typed arrays (`terrain`, `crystal`, `blocked`, `explored`, `visible`,
  `height`).
- **Determinism:** a single seeded LCG `rnd()` over the global `seed` drives all
  sim randomness, so a fixed seed reproduces a map/match (this is what makes the
  daily challenge globally identical and would underpin future lockstep).
- **Entities:** `ents` array; `spawnUnit`/`spawnStruct`. `owner` 0 = player,
  1 = enemy (governs economy/UI/victory). **Combat friend-or-foe is decided by
  `team`, not owner** — the player is team 0 and each enemy base gets its own
  team, so opponents fight each other (free-for-all). Victory = no owner-1
  structures remain.
- **Loop:** `step(dt)` advances the sim (call it directly in tests to fast-
  forward); `frame()` runs input/render each rAF; `makeWorld()` fully rebuilds
  the world and is the single reset point (also resets per-match progression
  counters).
- **Progression/menu (localStorage `td.v1.*`):** `profile`, `stats`,
  `achievements`, `scores`, `daily`, `cosmetics`, `campaign` — all reads are
  fault-tolerant. The main menu is the `#menu` overlay rendered by
  `renderMenu()`/`renderPane()`; `launchMatch`/`launchMission`/`launchTutorial`
  start a game; `recordMatch(win)` at `endGame` writes results, XP/ranks,
  achievements, and campaign completion.
- **i18n:** `L = {he, en}` with `T(k)`; menu-specific strings live in `MENUTXT`
  with `MT(k)`. Hebrew is RTL; the menu flips `dir` per language.

### Testing the browser game (headless Playwright)

There is no unit-test harness for the JS game — verify changes by driving the
built page headlessly:

```bash
NODE_PATH=/opt/node22/lib/node_modules node <yourtest>.js
```

Launch Chromium from `/opt/pw-browsers/chromium` with
`--no-sandbox --use-gl=angle --ignore-gpu-blocklist`. Inside the page you have
the full sim in scope: call `makeWorld()`, set `running = true`, drive
`step(0.1)` in a loop to advance time (GL renders at ~1.5fps, so advance the sim
directly and dispatch synthetic Mouse/Touch/KeyboardEvents rather than relying
on real-time frames). Always assert `pageerror` count is zero. Load the wrapped
`docs/preview/index.html` when testing anything mobile/viewport-dependent.

### Deploy (browser game)

`.github/workflows/pages.yml` fires on pushes to
`claude/red-alert-tiberian-sun-bkdixg` that change `docs/preview/index.html`,
and force-mirrors that single file to the `gh-pages` branch (served via "Deploy
from a branch"). So: commit the wrapped `docs/preview/index.html` to the feature
branch and the site updates within ~a minute (hard-refresh to bypass cache).

## Track 1 — the Unity/C# sim

```bash
cd sim && dotnet test TiberiumDusk.sln    # runs headless: game logic, AI-vs-AI
                                          # matches, replay reproduction, real-socket lockstep
dotnet run --project server/TiberiumDusk.Server   # lockstep relay server
```

CI (`.github/workflows/ci.yml`) runs `dotnet test sim/TiberiumDusk.sln` on every
push/PR. Structure: `sim/TiberiumDusk.Sim` (pure deterministic core,
netstandard2.1), `.Sim.Tests`, `.Balance` (JSON loading/validation), `.Net`
(lockstep). All game content is data-driven from `data/*.json` (units,
weapons, warheads, techtree, terrain, superweapons, `locale/he.json`+`en.json`).
The Unity client (`unity/`) only renders sim state and can't be built here
(no editor in this environment); it opens on a developer machine via the
"Tiberium Dusk → Setup Project" menu item.

## Reference docs

`docs/architecture.md` (sim/view split, determinism, lockstep design),
`docs/GDD.md`, `docs/roadmap.md`, `docs/research/` (mechanics, rosters/tech,
visual style), `docs/deploy.md`, `docs/balance-changes.md`, and
`docs/checklists/` (per-phase verification).
