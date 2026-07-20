# Tiberium Dusk — 3D model builder contract (Three.js r128, Z-UP world)

Every model is built procedurally with a tiny DSL that mirrors the Blender
helper functions used in the repo's python builders. The world is **Z-up**
(ground = XY plane, height = +Z), 1 unit = 1 map tile, exactly like the
Blender scripts. Model "front" faces **+Y** (same as the Blender builders).

## Helper API (already implemented by the engine — code against it)

Your file is evaluated AFTER a global `H` exists:

```js
H.group(parent?)                 // new THREE.Group, optionally added to parent
H.box(parent, x, y, z, sx, sy, sz, color, o)
   // axis-aligned box CENTERED at (x,y,z) with FULL sizes sx,sy,sz
   // o = { rx, ry, rz } rotation in DEGREES, XYZ order (bpy-compatible). optional.
H.rod(parent, x, y, z, r, len, color, o)
   // cylinder of radius r, length len, ALONG LOCAL Z before rotation,
   // centered at (x,y,z). o = { rx = 90 by default (→ lies along Y), ry, rz, v }
   // v = radial segments (default 10). NOTE: default rx is 90 — identical to
   // the python `rod()` helper, so ported calls keep their rx values as-is.
H.ball(parent, x, y, z, r, color, o)
   // uv-sphere; o = { squash } scales Z by squash (like python ball()).
H.mat(color)                     // cached MeshStandardMaterial (rough .8)
H.emis(color)                    // cached emissive material (glows)
// pass o.e = true on box/rod/ball to use the emissive material instead.
```

`color` is a hex int. Palette (converted from the python tuples):

```js
H.C = {
  DM_ARMOR: 0x8b7433, DM_DARK: 0x4d452b, SO_ARMOR: 0x6e2429, SO_DARK: 0x291a1c,
  TRACK: 0x1c1c1a, GUNMETAL: 0x3b3d45, TIRE: 0x141414, SKIN: 0xd9ad85,
  BOOTS: 0x24211f, YELLOW: 0xf2bf26, WHITE: 0xebede6, RED: 0xd93338,
  BLUEGREY: 0x6b7a99, CYAN: 0x59f2ff, CONCRETE: 0x858073, SAND: 0x9a8a5e,
  GREEN: 0x49ff7a, CACTUS: 0x4a7a3a,
}
```

All coordinates/sizes/rotations in the python sources port 1:1 — do NOT
convert axes. `tracks(parts, hw, ln, color)` from python: port it as a JS
helper inside your file.

## Deliverable shapes

**Units file** (`js3d_units.js`) must define ONE global:

```js
UNIT_BUILDERS = {
  // id: (H, pose) => ({ root, turret, fit })
  //  - root: THREE.Group holding the whole model (z-up, front = +Y)
  //  - turret: sub-Group that should rotate independently, or null
  //  - fit: number — target footprint (max XY extent) in tiles; the engine
  //    rescales the model so max(width,depth) === fit and grounds minZ to 0.
  //  - pose: 0 or 1, ONLY meaningful for infantry (leg swing); vehicles
  //    ignore it.
  dm_mbt_walker: (H, pose) => { ... },
  ...
}
```

**Structures file** (`js3d_structures.js`) must define TWO globals:

```js
STRUCT_BUILDERS = {
  // id: (H) => ({ root, fit, spin })
  //  - fit: footprint in tiles (3x3 building → ~2.9)
  //  - spin: optional sub-Group the engine slowly rotates (radar dish etc.)
  nx_conyard: (H) => { ... },
  ...
}
PROP_BUILDERS = {
  // kind: (H, variant) => ({ root, fit })  — variant 0..7 varies the shape
  cactus: (H, v) => { ... }, rock: (H, v) => { ... }, bush: (H, v) => { ... },
}
```

## Testing your work (do this before finishing!)

Three.js r128 is at `scratchpad/package/` — in node:

```js
const THREE = require('/tmp/claude-0/.../scratchpad/package/build/three.js');
// build H exactly per spec (copy from js3d_core.js in the scratchpad),
// eval your file, call every builder, assert:
//  - no exceptions, root is a Group with >0 children
//  - new THREE.Box3().setFromObject(root) has sane extents (no NaN, no
//    dimension > 6 or < 0.05 BEFORE fitting)
//  - turret/spin (when present) is a descendant of root
```

`js3d_core.js` in the scratchpad contains the real H implementation — require
three, load it, and use it directly in your node test.
