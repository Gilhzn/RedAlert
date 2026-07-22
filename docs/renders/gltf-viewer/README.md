# glTF terrain viewer

A self-contained, in-browser viewer for the demo map exported from Blender as
glTF — proof that the pre-baked terrain mesh loads and renders in **Three.js**,
the same engine the game uses. This is the bridge from the game's procedural
terrain to an offline-authored mesh.

## Run it

Serve this folder over HTTP (the glTF is fetched with XHR, which `file://`
blocks) and open `index.html`:

```bash
cd docs/renders/gltf-viewer
python3 -m http.server 8099
# → http://127.0.0.1:8099/index.html
```

Drag to orbit, wheel to zoom.

## What's here

- `index.html` — the viewer (Three.js + GLTFLoader + DRACOLoader + OrbitControls)
- `terrain.glb` — the map: terrain (with vertex colours), water, two tiberium
  fields, two bridges. Z-up, in game units (1 unit = 1 cell), Draco-compressed
  (~0.5 MB, down from ~12 MB uncompressed).
- `draco/` — the Draco WASM decoder used to unpack the mesh

## Regenerate the mesh

From the repo root, with Blender available as the `bpy` module:

```bash
EXPORT_GLTF=1 EXPORT_DRACO=1 RENDER=0 MAPSIZE=128 ZS=1.0 DIV=2 \
  GLB_OUT=docs/renders/gltf-viewer/terrain.glb python3 tools/blender_demo_map.py
```

- `MAPSIZE` — 64 (game size) or 128 (the 4× map)
- `ZS=1.0` keeps real game proportions (renders use 1.7 for drama)
- `DIV=2` is a lighter mesh for realtime; drop `EXPORT_DRACO` for a plain glb

## Bringing it into the game

The game's Three.js is Z-up, matching this export, so the mesh drops in without
axis juggling. Sketch:

```js
const draco = new THREE.DRACOLoader(); draco.setDecoderPath('draco/');
const loader = new THREE.GLTFLoader(); loader.setDRACOLoader(draco);
loader.load('terrain.glb', g => { worldGroup.add(g.scene); });
```

Full gameplay integration (aligning the mesh to the sim grid, fog-of-war
sampling, unit placement on the baked heights) is a follow-up beyond this
viewer.

## Third-party components

Vendored for offline use, unmodified:

- **Three.js** r128 (`three.min.js`, `GLTFLoader.js`, `DRACOLoader.js`,
  `OrbitControls.js`) — MIT License, © three.js authors.
- **Draco** decoder (`draco/`) — Apache License 2.0, © The Draco Authors.
