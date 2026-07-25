#!/usr/bin/env python3
"""Compress a Blender map export's terrain.bin into ONE small, upload-friendly
file (assets_src/map-export/terrain_pack.gz, ~1 MB) that the browser game can
turn into a realistic 3D terrain — real heightfield + the ground mask channels
(sand / gravel / rock / road), the same data Blender used for its material.

terrain.bin itself is ~33 MB (over GitHub's 25 MB web-upload limit); this pack
is a fraction of that and carries everything needed to rebuild the look.

Usage (run wherever terrain.bin lives — e.g. your Blender export folder):
    python tools/pack_terrain.py  "E:\\claude\\designer\\map-export"
    # writes terrain_pack.gz next to terrain.bin; upload that ONE file to
    # assets_src/map-export/ in the repo (git push, or GitHub web upload).
"""
import os, sys, json, struct, gzip, array

SRCDIR = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "assets_src", "map-export")

tj = os.path.join(SRCDIR, "terrain.json")
tb = os.path.join(SRCDIR, "terrain.bin")
if not (os.path.exists(tj) and os.path.exists(tb)):
    sys.exit("ERROR: terrain.json / terrain.bin not found in %s\n"
             "Point me at the folder that holds them, e.g.\n"
             "    python tools/pack_terrain.py \"E:\\\\claude\\\\designer\\\\map-export\"" % SRCDIR)

meta = json.load(open(tj))
N = meta["N"]; stride = meta["stride"]; chans = meta["channels"]
hmin = meta["hmin"]; hmax = meta["hmax"]; span = max(1e-6, hmax - hmin)
hi = chans.index("height")
mask_names = [c for c in chans if c != "height"]      # road, wash, pav, fill, bank, apron, bench

raw = open(tb, "rb").read()
flt = struct.unpack("<%df" % (len(raw) // 4), raw)     # N*N*stride float32

# downsample native N (e.g. 1025) -> OUT (513): plenty for a smooth in-game mesh
OUT = 513 if N >= 700 else N
step = N / OUT
def samp(col, row, ch):
    c = min(N - 1, int(col * step)); r = min(N - 1, int(row * step))
    return flt[(r * N + c) * stride + ch]

heights = array.array("H", bytes(2 * OUT * OUT))       # uint16
masks = {m: array.array("B", bytes(OUT * OUT)) for m in mask_names}
for r in range(OUT):
    for c in range(OUT):
        i = r * OUT + c
        h = samp(c, r, hi)
        heights[i] = max(0, min(65535, int((h - hmin) / span * 65535)))
        for m in mask_names:
            v = samp(c, r, chans.index(m))
            masks[m][i] = max(0, min(255, int(v * 255)))

header = {"OUT": OUT, "size": meta["size"], "water": meta.get("water", -1.6),
          "hmin": hmin, "hmax": hmax, "masks": mask_names}
blob = (json.dumps(header) + "\n").encode() + heights.tobytes()
for m in mask_names:
    blob += masks[m].tobytes()

dst = os.path.join(SRCDIR, "terrain_pack.gz")
with gzip.open(dst, "wb", compresslevel=9) as f:
    f.write(blob)
print("WROTE %s  (%.2f MB, %dx%d heights + %d masks: %s)" %
      (dst, os.path.getsize(dst) / 1e6, OUT, OUT, len(mask_names), ", ".join(mask_names)))
print("Now upload terrain_pack.gz to assets_src/map-export/ in the repo.")
