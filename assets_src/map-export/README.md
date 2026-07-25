# Sirocco Flats — map export

A Red Alert–style desert map: rolling badland terrain cut by water-filled wadis,
crossed by a road network with six single-span bridges. 400 × 400 m.

## Coordinate system
Right-handed, metres. **+X east, +Y north, +Z up**, origin at the map **centre**
(so world coords run −200..+200 on X and Y). Still-water surface at **z = -1.6 m**.
Terrain height ranges -12.7..26.5 m.
(The `.glb` is exported Y-up per glTF convention — Z-up maps to Y-up there.)

## Files

### Data (engine-agnostic, lossless — drives gameplay and procedural rendering)
- **terrain.bin** — 1025×1025 grid, row-major, X fastest, `float32`, **8 channels
  per vertex** in this order:
  `height, road, wash, pav, fill, bank, apron, bench`.
  height is metres; the rest are 0..1 masks. See terrain.json for the header.
- **terrain.json** — `{N, size, water, hmin, hmax, channels[], stride}`.
- **passability.png / passability.json** — 256×256 vehicle pathfinding grid;
  white / `1` = passable. Derived: dry AND slope < 22° , OR road, OR embankment.
- **roads.json** — road centre-lines as world-space polylines.
- **bridges.json** — six bridge sites: endpoints `a`,`b`, `deck` elevation,
  span `len`, orientation `ang` (deg), abutment ground `hA`/`hB`, channel `depth`.

### Geometry (drop-in for any glTF viewer/engine)
- **sirocco_flats.glb** — the whole map. Props (rocks, saguaro, ocotillo,
  creosote, bursage) are **GPU-instanced** (prototype mesh + transforms).
  Terrain albedo is baked to a COLOR_0 vertex attribute; props carry
  representative constant colours. For the full material look, render the
  terrain from terrain.bin with your own shader — the mask channels are all
  there.

## Channel meanings (for a procedural ground shader)
- **wash** — active channel sand: the brightest ground, lowest elevation.
- **pav**  — desert pavement: dark varnished gravel on the stable benches.
- **bank / apron** — freshly-cut bank face / colluvial toe below it.
- **bench** — flat enough for a true interlocking pavement (vs coarse colluvium).
- **fill** — engineered embankment (bridge approaches): graded borrow material.
- **road** — the graded roadway.

## Gameplay notes
- The wadis are the barriers; the **six bridges are the only vehicle crossings**
  (passability already encodes this). Bridge decks sit at `deck` metres and are
  reached by graded approach ramps baked into the terrain.
- Water is a still plane at z = -1.6; anything below is submerged.
