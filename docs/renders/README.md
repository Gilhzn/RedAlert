# Renders

High-quality offline renders of *Tiberium Dusk* content, produced with Blender
(Cycles). These are promotional / reference stills — the live game runs in the
browser on Three.js; these images are **not** loaded at runtime.

The Blender scene is a faithful reconstruction of the game's own procedural
terrain generator (`makeWorld()` in the preview build): identical seed,
value-noise field, the snaking river with its fords, the lakes, the rock
mountain ranges, the climbable hills and all the tiberium fields. On top of
that it adds lush green meadow banks along the water and bridges spanning the
river, lit as a warm desert dusk with a bloom pass over the glowing tiberium.

These stills are rendered at **4× map size** (`MAPSIZE=128`, four times the
playable area), with feature sizes scaled to keep the mountains, fields and
meadows grand rather than sparse.

## Images

| file | what |
|------|------|
| `tiberium-dusk-map-4k.png`       | 3840×2160 hero shot |
| `tiberium-dusk-map-1080p.png`    | 1920×1080 hero shot |
| `tiberium-dusk-map-dunes.png`    | low "in the dunes" dusk vista |
| `tiberium-dusk-map-tactical.png` | top-down orthographic tactical map / minimap |

## Regenerate

```bash
# hero (1080p). CAM = hero | dunes | topdown ; RES = 1080 | 4k
MAPSIZE=128 CAM=hero OUT=map.png python3 tools/blender_demo_map.py
python3 tools/postprocess_render.py map.png map_final.png   # bloom + dusk grade + vignette
```

- `MAPSIZE` — 64 (game size) or 128 (the 4× map)
- `RES=4k` for 3840×2160; `CAM=topdown` renders a square frame
- `PREVIEW=1` for a fast low-res/low-sample preview while iterating
- `SAMPLES=N` overrides Cycles samples

`tools/postprocess_render.py` needs numpy + Pillow. No third-party or
copyrighted assets are used — the geometry, materials and lighting are all
generated procedurally by the script.

## glTF export

`gltf-viewer/` contains the same terrain exported as a Draco-compressed `.glb`
plus a self-contained Three.js viewer — see its README.
