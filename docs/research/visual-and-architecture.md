
# Style & Architecture Research: Tiberian Sun-Inspired Browser 3D RTS

## PART A — VISUAL IDENTITY OF C&C: TIBERIAN SUN (1999)

### A1. Overall tone & art direction

- **Setting**: 2030, Earth mid-collapse from Tiberium infestation. Westwood deliberately pivoted from RA1's WWII-alternate pulp to a **dark, oppressive, post-apocalyptic techno-gothic** look — closer to *Blade Runner*-meets-*Aliens* militarism than classic mil-sim.
- **Mood keywords for a style guide**: cold, toxic, overcast, industrial decay, alien encroachment, perpetual twilight. The world feels *sick* — Tiberium is visually "beautiful poison": the only saturated, glowing thing in an otherwise desaturated world.
- **Faction identity**: GDI = blocky, functional, gunmetal/gold-tan, honest military hardware (Titan/Wolverine walkers, disc-thrower, hover MLRS). Nod = angular, insectoid, black/crimson, stealth and lasers and underground (subterranean APC, stealth tank, Obelisk, cyborgs). Recreate this dichotomy: **GDI = slab geometry, right angles, exposed hydraulics; Nod = swept blades, scorpion-tail silhouettes, glowing red accents.**
- Cutscene/menu framing: green-on-black terminal typography, scanlines, holographic globes, Kane's red iconography — the whole game pretends to be a military command terminal.

### A2. Color palette (usable hex targets — original-but-faithful, not sampled from EA assets)

| Element | Direction | Suggested range |
|---|---|---|
| Temperate terrain | Desaturated blue-grey/olive earth; never warm green grass | `#4a5548`, `#5a6258`, `#3d4a52` |
| Snow theme | Blue-white with grey-cyan shadows, dirty ice | `#aebfc9`, `#7d93a3` |
| Green Tiberium (Riparius) | Emissive acid green, the signature accent | `#39ff6a` core, `#1d7a3a` base |
| Blue Tiberium (Vinifera, volatile/valuable) | Emissive cyan-blue crystal | `#4dc8ff` / `#2a6bd8` |
| Veins/veinholes | Sickly brown-red organic pulsing mass | `#6b3a2a` |
| Fires/explosions | Hot orange-white against cold ambient — key contrast pair | `#ff9a2a` vs ambient `#26303a` |
| Nod accents | Crimson + black | `#c1121f`, `#111` |
| GDI accents | Gold/amber + gunmetal | `#d9a441`, `#5c636b` |
| Night/ion-storm ambient | Deep blue-grey global tint, lightning flashes to white-cyan | ambient `#1a2230` |
| UI/EVA green | Phosphor terminal green | `#33ff55` on near-black |

**Core rule**: the world is low-saturation and cool; *light sources* (tiberium, lasers, fires, muzzle flashes, obelisk charge) are the only high-saturation elements. This single rule reproduces 80% of the TS look in a modern renderer.

### A3. Rendering tech of the original (what to emulate in 3D)

- **Isometric 2.5D engine** ("Voxel/2.5D" era): diamond-shaped cells, height-mapped terrain giving true elevation — ramps, cliffs, bridges you could destroy, units occluded behind cliffs. Camera fixed-angle, no rotation.
- **Voxel vehicles**: tanks/aircraft were tiny voxel models rendered with per-facing normals — they visibly tilt to conform to slopes (harvester cresting a hill was a showcase). In Three.js: low-poly meshes with **slope-conforming orientation** and slightly chunky, "stepped" silhouettes give the voxel-era feel; optionally a light quantization/pixelation post-effect.
- **Sprite infantry**: hand-animated 2D sprites (8 facings). Modern equivalent: either low-poly rigged minis, or billboarded sprite-style shading (flat lighting, strong rim) to keep the hybrid feel.
- **Dynamic colored lighting**: the engine's headline feature — day/night cycle, colored point lights baked into cell lighting: green glow radius around tiberium fields, red obelisk glow, white-blue ion storm lightning strobes, orange fire flicker, muzzle flashes lighting nearby ground. This maps 1:1 onto real point lights + emissive/bloom in Three.js and is the highest-value fidelity target.
- **Ion storms**: periodic weather events — sky darkens to deep blue, EVA warns, radar goes down, aircraft grounded, random lightning bolts strike (can kill units). Visual recipe: global ambient drop + blue tint, animated cloud layer, screen-space rain streaks, randomized branched-lightning meshes with 2-frame white flash of the whole scene.
- **Terrain features**: layered cliffs (grey rock strata), scorch marks and craters that persist from explosions and deform pathability, destructible city ruins (grey-brown shattered concrete, girders), burnable trees, ice that cracks under weight, tiberium fields creeping/regrowing, veinholes. Themes: **temperate** and **snow** (plus urban sub-theme). No desert in vanilla TS.

### A4. UI / HUD (the futuristic command-terminal aesthetic)

- **Right-side sidebar**, full height, metallic dark-grey panel with beveled machined edges:
  - **Top: radar/minimap** (square), rendered as greenish tactical display; static/noise when power is low or radar destroyed; unexplored = black (shroud).
  - Below radar: small buttons (options, diplomacy, sidebar repair/sell/power toggles).
  - **Build area: two side-by-side columns of "cameo" icons** — left column structures, right column units — with chunky up/down scroll arrows. **Important correction: vanilla TS has NO tabs.** The 4-tab sidebar (Buildings/Defense/Infantry/Vehicles) is Red Alert 2; the community Vinifera patch retrofits RA2-style tabs onto TS. For a "modern take" a tabbed sidebar is a defensible evolution — just know it's RA2-canon, not TS-canon.
  - **Cameo icon style**: ~60×48 rendered 3D snapshots of the unit/structure on dark backgrounds, thin bevel frame; build progress shown as a **radial clock-sweep darkening** over the cameo + "ON HOLD"/"READY" text; queue count numeral in a corner.
- **Power meter**: thin vertical bar on the sidebar's left edge — segmented LEDs, green (surplus) → yellow → red (deficit); drain indicator vs output.
- **Credit ticker**: numeric readout at top, **ticks up/down incrementally** with an audible tick-tick-tick rather than jumping — a tiny detail with huge nostalgic payoff.
- **In-world UI**: green health bars above selected units (green/yellow/red), white selection brackets (four corners, not a full box), rally-point lines, waypoint mode.
- **Aesthetic language**: dark gunmetal panels, phosphor-green monochrome text (small all-caps bitmap font), scanline/CRT flavor, amber warning accents, subtle animated static. For an original font: any square techno/OCR-style face (e.g., open licensed "Orbitron"/"Share Tech Mono" flavor) reads correctly.

### A5. EVA & sound design

- **EVA** (Electronic Video Agent): calm, clipped female synthetic voice announcing game state: "Construction complete", "Unit ready", "Insufficient funds", "Base under attack", "Ion storm approaching", "Unit lost", "Silos needed". Design cue: emotionless delivery over chaos = core C&C feel. Record original lines with light vocoder/radio band-pass.
- **Unit voices**: confirmation barks per faction (GDI gruff professional, Nod fanatic whispery, cyborgs monotone), radio-filtered.
- **SFX palette**: heavy mechanical servo footsteps for walkers, deep concussive cannon thuds, crisp laser zaps (Nod), Tesla-like ion crackle, tiberium ambient shimmer/chime near fields, rain + thunder during storms.
- **Music (Klepacki + Jarrid Mendelson)**: verified — a deliberate departure from RA1's rock/industrial toward **dark, moody electro-industrial / dark ambient / atmospheric drum-n-bass**; Westwood's direction was "very dark, moody, not upbeat at all". Signature tracks: *Lone Trooper*, *Mad Rap*, *Dusk Hour*, *Infrared*, *What Lurks*, *Approach*, *Mutants*. Recipe for original music: slow tempos (~80–110 BPM) or skittering DnB breaks, metallic percussion loops, detuned pads, distant industrial clangs, sparse melodic motifs, occasional processed vocal fragments.

### A6. Animation style

- **Walker gait**: Titan/Wolverine mechs with deliberate, weighty two-legged stride — piston compression, foot-plant pauses, slight torso bob; turret/torso twists independently to track targets. In 3D: keyframed 2-bone-leg IK walk cycle at low frame quantization (12–15 fps stepped sampling) reads pleasingly retro.
- **Harvester loop**: drive to field → deploy scoop animation over crystals (crystals visibly disappear cell by cell) → return → **dock into refinery, tilt/lift, glowing green dump animation** while credits tick up.
- **Deploy transforms**: MCV unpacks into construction yard (multi-stage build-up animation); tick tank deploys; subterranean units burrow with dirt-mound particle effect.
- **Construction**: buildings appear via scaffold "build-up" animation (frame-by-frame rising structure), sell = reverse.
- **Damage states**: buildings show 2 damage tiers (smoke, then fire + rubble texture); flames are big, orange, sprite-flipbook style.
- General rule: chunky, readable, slightly stepped animation > smooth mocap. Exaggerated recoil and muzzle flash lights.

---

## PART B — TECHNICAL ARCHITECTURE

### B1. Deterministic lockstep simulation

**Why lockstep for RTS** (canon: Terrano & Bettner, "1500 Archers on a 28.8", GDC 2001 — AoE):
- State-sync bandwidth scales with unit count; AoE calculated that syncing even minimal per-unit state capped them at ~250 units. Lockstep sends **only player commands** (~a few dozen bytes each), so 1500+ units run over a 28.8k modem. For a C&C-like with hundreds of units, lockstep is the right default.
- Model: every client runs the **identical simulation**; inputs are the only thing networked; identical inputs + deterministic sim ⇒ identical state everywhere.

**Command delay / turn scheduling**:
- AoE: "communication turns" (~200 ms default) decoupled from render frames; a command issued during turn N executes at turn N+2 — giving the network 2 turns to deliver it to everyone. UI hides latency with immediate acknowledgment barks + visual feedback.
- OpenRA does the same: orders are stamped for a future net-frame; modern refinements use adaptive delay based on measured latency.
- Typical numbers: sim tick 10–30 Hz (OpenRA ~25 ms world ticks in bundles; AoE 200 ms turns subdivided; StarCraft ran ~24 fps sim with orders delayed several frames). A good modern choice: **sim at 15–20 Hz fixed ticks, order execution delay 2–4 ticks (~100–250 ms), render at display rate with interpolation**.

**Determinism requirements**:
- Floating point is the classic desync source: different CPUs/compilers/JS engines can differ in transcendental functions (sin/cos), FMA contraction, SIMD. AoE2 HD/DE achieves float determinism only via strict compiler flags and per-platform testing — **not available to you across browsers**.
- Browser reality: IEEE-754 basic ops (+,−,×,÷,sqrt) are correctly rounded and *should* be identical across JS engines, but `Math.sin/cos/atan2` etc. are **not specified to be bit-identical** across engines. Safe strategy for a web RTS: **integer/fixed-point math throughout the sim** (e.g., 32-bit ints with 10–12 fractional bits, positions in "leptons"-style subcell units like the original C&C engines), lookup tables or polynomial approximations for trig, integer square root, and a **seeded deterministic PRNG** (OpenRA uses a synced RNG; any client using the shared RNG outside the sim causes desync). Avoid `Math.random`, iteration-order-dependent hashmaps (JS object/Map insertion order is actually specified — usable, but be disciplined), and any wall-clock reads inside the sim.
- **Desync detection**: every N ticks, each client hashes salient game state (positions, HP, ownership, RNG state) and exchanges hashes; mismatch ⇒ desync detected (OpenRA does exactly this via `[Sync]`-attributed fields hashed per frame; it can dump full sync reports for debugging). You cannot *recover* easily — typical handling is: report + end game, or resync by state snapshot from an authoritative peer [resync-on-desync is uncommon in shipped RTS; StarCraft/AoE just drop].
- Cheating note: lockstep reveals full state to every client (maphack risk) but prevents unit-command forgery only via server validation of order legality. Fog-of-war secrecy fundamentally can't be enforced client-side in pure lockstep — accept it (OpenRA does) or add server-side sim later.

**Rollback considerations**: GGPO-style rollback (predict remote inputs, re-simulate on mispredict) eliminates input delay but requires cheap state snapshot/restore and re-simulating K ticks — expensive with thousands of units. RTS practice: **don't rollback; use delay-based lockstep** — RTS commands tolerate 100–250 ms latency because acknowledgment audio/visuals mask it. Rollback is for fighting games/small state. A pragmatic middle ground: rollback only for local-player responsiveness cosmetics (immediate visual order feedback outside the sim).

**Sim/render separation**:
- Sim: fixed-tick, integer, headless-runnable (crucial for tests + server verification + replays). Replays = initial state + command log, nearly free with lockstep. 
- Render: reads sim state, **interpolates entity transforms between the last two ticks** (`renderPos = lerp(prevTick, currTick, alpha)`), runs particles/audio/UI at full frame rate, never writes back into sim state. Turret angles, walk-cycle phase etc. can be render-side cosmetic.
- Structure as two packages with a one-way boundary: `@game/sim` (pure TS, no three.js imports, deterministic) and `@game/client` (three.js view + input → produces Orders only).

### B2. OpenRA as the reference architecture

- **Actors + Traits**: every entity is an `Actor` = bag of **traits** (components) declared in YAML (`Mobile`, `Health`, `Armament`, `AttackFrontal`, `Harvester`, `RenderSprites`...). Each trait has a paired `TraitInfo` (immutable per-type config parsed from YAML) and a runtime trait instance per actor. Traits communicate via interfaces (`ITick`, `INotifyDamage`, `IIssueOrder`, `IResolveOrder`) — this is data-driven ECS-lite, and it's why OpenRA supports TD/RA/D2K as *mods* on one engine. Directly transplantable to TS: define units in JSON/YAML, compose behavior components.
- **Order system**: input never mutates the world. UI produces `Order` objects (subject actor, order string, target, extra data) → orders go to the network layer → scheduled onto a future frame → executed identically on all clients by each actor's `IResolveOrder` traits. Even single-player runs through the same pipeline (localhost "network") — adopt this: it makes replays, spectators, and MP free.
- **Sync checking**: fields/properties marked `[Sync]` are hashed each frame; hashes exchanged; mismatch triggers "Out of sync" with dumped sync reports. OpenRA also uses fixed-point (`WDist`, `WPos`, `WAngle` — integer world units, 1024 units/cell, 1024 angle units/circle) — a proven blueprint for your integer sim types.
- **OpenRA Tiberian Sun status**: TS support is OpenRA's long-standing next-gen goal; the in-tree TS mod is still **not shipped** — GitHub milestone "Tiberian Sun first 'full' (beta) release" sits ~48% complete (blockers list ongoing as of late 2025). Playable **unofficial community builds** exist on ModDB ("OpenRA Tiberian Sun Unofficial Release", incl. a "Full Version" build dated March 2026) [uncertain: exact feature-completeness of those community builds]. Studying their trait YAML for TS units is legitimate and useful.
- **Dawn of the Tiberium Age (DTA)**: standalone freeware total conversion running TD+RA content **on the actual (patched) Tiberian Sun engine** via the XNA-based CnCNet client + the **Vinifera** open-source TS engine extension (reverse-engineering/extending TS.exe; adds RA2-style sidebar tabs etc.). DTA/Vinifera/CnCNet's TS client code (GitHub: `Vinifera-Developers/Vinifera`, `CnCNet/xna-cncnet-client`) is currently the best window into real TS engine behavior.
- **EA source releases — verified**:
  - **May 2020**: *Tiberian Dawn* and *Red Alert 1* engine code released under **GPL v3** (alongside the Remastered Collection).
  - **Feb 27, 2025**: EA released GPL source for **four titles: C&C (Tiberian Dawn), Red Alert, Renegade, and Generals + Zero Hour** (github.com/electronicarts), plus Steam Workshop support and a SAGE modding asset pack. 
  - **Tiberian Sun and Red Alert 2 source were NOT released** — explicitly noted as conspicuously absent (community speculates remasters are why). As of this research (July 2026) no TS/RA2 source release found [uncertain: could change; re-check EA's GitHub before relying on it].
  - **Practical implication**: you can legally study TD/RA1 GPL code (direct ancestors — the lepton coordinate system, cell model, harvester logic, and build-queue logic carry into TS largely intact), plus OpenRA (GPLv3) and Vinifera (GPLv3, but derived via reverse engineering) for TS-specific mechanics. Note GPL study ≠ freely copying code into a non-GPL project; either keep your project GPL-compatible or use these strictly as behavioral references.

### B3. Three.js specifics for an RTS

**Rendering many units**
- `InstancedMesh` per unit-type (per mesh+material): one draw call for all Titans; per-instance matrix + `instanceColor` for faction tint/team color; write custom shader chunks (`onBeforeCompile`) for per-instance damage tint/selection glow. Hundreds–thousands of units is trivial this way; animation via (a) rigid sub-part instancing (hull + turret as separate instanced meshes — very TS-appropriate), or (b) baked vertex-animation-textures for walk cycles if you need skinned crowds. Also: merged static geometry for terrain doodads, `BatchedMesh` (newer three.js) for mixed static meshes, frustum culling by chunk.

**Pathfinding**
- Grid, not navmesh: TS is cell-based; a grid keeps sim integer-deterministic and makes crushing/terrain destruction/wall placement trivial. Navmesh (Recast) shines for irregular 3D spaces but complicates determinism (float geometry) and dynamic building placement.
- **Hierarchical A\* on the grid** for single orders + **flow fields** for group moves to shared destinations (the Supreme Commander 2 approach): compute one integration field per destination, all units in the group read direction vectors — O(1) per unit per tick, handles chokepoints and shifting obstacles gracefully. Combine with local steering/boids-lite for unit separation. All in integer math inside the sim.

**Picking**
- CPU raycasting (three.js `Raycaster`) is fine for click-selects if you raycast against simplified bounding volumes / a spatial hash rather than full meshes; `InstancedMesh` supports raycasting with instance IDs.
- **GPU picking** (render entity IDs as colors into a small render target, read pixel) is the robust option for pixel-perfect selection of many instanced/animated units and costs O(1) per click; drag-box selection is best done in sim space (project box, test unit positions) not against meshes.

**Fog of war**
- Classic technique: maintain a low-res **visibility texture** (e.g., 1 texel per cell or 2×2 per cell, `DataTexture` updated from the sim's shroud/explored bitfields) with 3 states: unexplored (black), explored-but-unseen (dimmed, no units), visible.
- Render: sample the fog texture in the terrain shader (world XZ → UV) to darken terrain; blur/smoothstep for soft edges; hide enemy actors whose cells aren't visible (sim-side check, not shader). Alternative compositing approach: draw a full-screen ground-plane-projected fog quad, or render the fog texture to a render target with blur post-pass and multiply over the scene. TS-flavored touch: unexplored = pure black shroud (TS had hard black shroud that regrows for Nod shroud regenerator [uncertain: regrowth was via GDI/Nod "shroud" superweapon mechanics]).
- The minimap **reuses the same fog texture** multiplied over a top-down representation.

**Minimap**
- Cheapest & most reliable: don't re-render the scene — draw a 2D canvas/offscreen texture from sim data: terrain color-coded base image (generated once from the map), + per-frame unit dots (faction colors), + fog texture overlay, + camera frustum trapezoid. Optionally a secondary orthographic top-down render-to-target at low res and low frequency (every ~10 frames) for prettier terrain. Radar-jammed/low-power = replace with animated noise shader (very TS).

**Camera**
- For faithful TS feel: **OrthographicCamera** pitched ~35–45° down [TS's projection ≈ classic 2:1 isometric, ~30° elevation look], yaw fixed at 45°; no player rotation. Modern take: **perspective camera with long FOV (15–25°) at high altitude** approximates isometric flatness while giving subtle parallax and making bloom/DoF nicer; allow optional 90° rotation steps. Orthographic simplifies fog projection, minimap math, and edge-scroll consistency — recommended default, with zoom = frustum scale.

**ECS for TypeScript**
- `bitecs`: SoA typed-array ECS, extremely fast, data-oriented — best perf, spartan API; good fit since typed-array state also makes state hashing/serialization for desync checks trivial.
- `miniplex`: entity-object ECS, ergonomic TS types, queries as archetypes — friendlier, fast enough for RTS-scale.
- Custom: given lockstep constraints (deterministic iteration order, snapshot/hash, integer components), a small bespoke ECS over preallocated typed arrays is a legitimate choice and what many lockstep projects do; determinism requirements often outweigh library convenience. Recommendation: **bitecs or custom typed-array store for the sim; keep render-side scene graph separate** (map: entityId → InstancedMesh slot).
- Note: OpenRA-style *traits* (per-actor component objects with interfaces) is an alternative to archetypal ECS that maps more naturally onto data-driven unit definitions; a hybrid (YAML/JSON unit defs → component sets) works well.

**Networking**
- **socket.io / plain WebSocket**: TCP, ordered/reliable. For *lockstep* this is actually acceptable — lockstep needs reliable ordered delivery anyway, and head-of-line blocking only adds latency spikes that command delay absorbs. Simplest ops story (works everywhere, trivial hosting).
- **geckos.io**: WebRTC DataChannels (unordered/unreliable, UDP-like) client↔Node.js server; socket.io-like API. Wins for state-sync/action games; for lockstep its benefit is mainly avoiding TCP retransmit stalls. Cost: needs UDP ports/TURN, harder to host behind PaaS/CDNs.
- **Colyseus**: room-based Node framework with schema-based *state synchronization* over WebSocket — its core value (authoritative state diffing) is largely what lockstep *avoids*; still useful purely for rooms/matchmaking/lobby, but you'd bypass its state sync.
- **Recommended**: **Node.js authoritative relay over WebSocket** — server collects orders per tick, assigns tick numbers, broadcasts the ordered order-bundle ("turn") to all clients (star topology, like OpenRA's dedicated server). Server also: validates order legality, stores the command log (= replay), runs desync-hash comparison, handles late-join via snapshot [uncertain: snapshot join requires sim state serialization — plan for it early], and can optionally run a headless sim instance for server-side verification/anti-cheat. Add geckos.io later only if latency spikes prove problematic. Room/lobby layer: trivial to hand-roll or use Colyseus rooms without its state sync.

### B4. Asset pipeline

- **Format**: glTF 2.0 (.glb) end-to-end; Draco or Meshopt compression (`gltfpack`); KTX2/BasisU textures for GPU-compressed loading; three.js `GLTFLoader` + `MeshoptDecoder`. Author in Blender (free) with a shared scale (1 unit = 1 m; a cell ≈ 2–3 m).
- **Style target**: low-poly (300–1500 tris/unit), flat-ish shading with strong emissive maps (tiberium, engines, Nod trim), single small texture atlas or vertex colors + gradient atlas (Quaternius/Synty-style) — this reads "modern voxel-descendant" and keeps you far from EA's assets while faithful in silhouette language (walkers, angular Nod, slab GDI).
- **CC0/free sources**:
  - **Kenney.nl** (CC0): sci-fi kits, tower-defense kit, particle packs, UI packs (his "Sci-Fi RTS" 2D pack is a great cameo/UI reference), crosshair/interface audio.
  - **Quaternius** (CC0): low-poly mech/robot packs, sci-fi buildings, ultimate vehicles — closest ready-made match for walker mechs.
  - **OpenGameArt.org**: filter CC0/CC-BY; sci-fi buildings, explosions flipbooks, terrain textures; quality varies.
  - Also: Poly Haven (CC0 textures/HDRIs for terrain + lighting), ambientCG (CC0 PBR terrain textures), Kay Lousberg/KayKit (CC0 low-poly), Sonniss GDC audio bundles (free game-audio SFX) [check per-pack licenses].
  - Music/EVA must be original: Klepacki tracks and the real EVA voice are copyrighted — synthesize an original EVA (TTS + band-pass + subtle vocoder works well) and commission/compose dark-ambient-industrial originals.
- **Procedural terrain texturing**: splat-map approach — heightmap-derived terrain mesh (chunked), 3–5 tiling textures (rock/dirt/grass-grey/snow/tiberium-soil) blended by a control texture painted from map data + slope/height rules in the shader; add macro-variation noise to kill tiling; decal system for craters/scorch (projected quads or texture-array stamps into the splat map — TS's persistent scorch marks are just runtime writes into that control texture); cliff faces as separate meshes/kit pieces with strata texture, or tri-planar mapping on steep slopes. Tiberium fields: instanced crystal clusters + emissive + point light per field (not per crystal) + a green tint painted into the terrain control map that grows/recedes with the sim's tiberium cells.

---

### Bottom-line recommendations
1. **Sim**: TypeScript, headless, integer/fixed-point (OpenRA-style WPos/WAngle, 1024 sub-units per cell), 15–20 Hz fixed tick, seeded RNG, `[Sync]`-style state hashing every N ticks; orders-only mutation.
2. **Net**: delay-based lockstep (2–4 tick command delay) over a Node WebSocket relay; replays = command log; desync = report + abort.
3. **Render**: three.js orthographic 45°-yaw camera, InstancedMesh per unit type with hull/turret split, interpolated transforms, emissive+bloom for tiberium/lasers/fires, DataTexture fog of war sampled in terrain shader, canvas minimap.
4. **Content**: JSON/YAML unit definitions → component sets (OpenRA trait pattern); study TD/RA GPL code + OpenRA + Vinifera for mechanics — TS/RA2 original source remains unreleased.
5. **Look**: cool desaturated world + saturated emissive light sources; slab-GDI vs blade-Nod; phosphor-green terminal UI with radar-top right sidebar, ticking credits, sweep-fill cameos; dark ambient/electro-industrial score; deadpan synthetic EVA.

Sources: [Engadget — EA releases source code for four C&C games](https://www.engadget.com/gaming/pc/ea-releases-source-code-for-four-command--conquer-games-223425774.html) · [GameSpot — C&C source release](https://www.gamespot.com/articles/ea-releases-full-source-code-for-multiple-command-conquer-titles/1100-6529779/) · [PC Gamer — C&C source + Workshop](https://www.pcgamer.com/games/strategy/ea-just-released-source-code-for-a-bunch-of-old-command-and-conquer-games-and-added-steam-workshop-support-to-bangers-like-c-and-c-3-tiberium-wars/) · [Hacker News — EA open sources Red Alert et al.](https://news.ycombinator.com/item?id=43197131) · [OpenRA TS milestone](https://github.com/OpenRA/OpenRA/milestone/30) · [OpenRA about](https://www.openra.net/about/) · [ModDB — OpenRA Tiberian Sun unofficial](https://www.moddb.com/mods/openra-tiberian-sun) · [DeepWiki — OpenRA order processing & networking](https://deepwiki.com/OpenRA/OpenRA/2.3-order-processing-and-networking) · [OpenRA traits docs](https://docs.openra.net/en/release/traits/) · [OpenRA Hacking wiki](https://github.com/OpenRA/OpenRA/wiki/Hacking) · [Delft SWA — OpenRA architecture](https://delftswa.github.io/chapters/openra/) · [Terrano & Bettner — 1500 Archers (PDF)](https://zoo.cs.yale.edu/classes/cs538/readings/papers/terrano_1500arch.pdf) · [Game Developer — 1500 Archers](https://www.gamedeveloper.com/programming/1500-archers-on-a-28-8-network-programming-in-age-of-empires-and-beyond) · [SnapNet — Netcode Architectures: Lockstep](https://www.snapnet.dev/blog/netcode-architectures-part-1-lockstep/) · [Wikipedia — C&C: Tiberian Sun](https://en.wikipedia.org/wiki/Command_&_Conquer:_Tiberian_Sun) · [C&C Wiki — Sidebar](https://cnc.fandom.com/wiki/Sidebar) · [Vinifera docs — User Interface](https://vinifera.readthedocs.io/en/master/User-Interface.html) · [C&C Wiki — Tiberian Sun soundtrack](https://cnc.fandom.com/wiki/Command_%26_Conquer:_Tiberian_Sun_soundtrack) · [frankklepacki.com — TS OST](https://www.frankklepacki.com/ost/vg/cnc-ts) · [ModDB — Dawn of the Tiberium Age](https://www.moddb.com/mods/the-dawn-of-the-tiberium-age) · [CnCNet — DTA](https://cncnet.org/dawn-of-the-tiberium-age) · [Alex Goldring — Fog of War](https://medium.com/@travnick/fog-of-war-282c8335a355) · [three.js forum — Fog of War](https://discourse.threejs.org/t/fog-of-war-for-games/7988) · [geckos.io GitHub](https://github.com/geckosio/geckos.io) · [Rune — WebRTC vs WebSockets](https://developers.rune.ai/blog/webrtc-vs-websockets-for-multiplayer-games) · [Web Game Dev — WebRTC](https://www.webgamedev.com/backend/webrtc)
