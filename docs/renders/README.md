# Renders

High-quality offline renders of *Tiberium Dusk* content, produced with Blender
(Cycles). These are promotional / reference stills — the live game runs in the
browser on Three.js; these images are **not** loaded at runtime.

## `tiberium-dusk-map-1080p.png`

A Full HD (1920×1080) hero render of the demo map. The Blender scene is a
faithful reconstruction of the game's own procedural terrain generator
(`makeWorld()` in the preview build): identical seed, value-noise field, the
snaking river with its two fords, the two lakes, the two rock mountain ranges,
the four climbable hills and all the tiberium fields. On top of that it adds
lush green meadow banks along the water and two bridges spanning the river,
lit as a warm desert dusk with a bloom pass over the glowing tiberium.

### Regenerate

```bash
# 1. render (Blender 5.x as the `bpy` Python module; CPU Cycles is fine)
OUT=map_render.png python3 tools/blender_demo_map.py
# 2. post-process: bloom + dusk grade + vignette (needs numpy + Pillow)
python3 tools/postprocess_render.py map_render.png tiberium-dusk-map-1080p.png
```

Set `PREVIEW=1` for a fast low-res/low-sample preview while iterating.

No third-party or copyrighted assets are used — the geometry, materials and
lighting are all generated procedurally by the script.
