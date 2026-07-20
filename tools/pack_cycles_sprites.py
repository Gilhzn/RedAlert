#!/usr/bin/env python3
"""Pack cycles_out/ renders into the demo's JS sprite blobs and build a
contact sheet for review. Buildings + truck are downscaled to SS=1 (matching
the existing draw path); soldiers/tanks keep s=1/3 supersample metadata."""
import json, base64, io, os, sys
from PIL import Image

SP = "/tmp/claude-0/-home-user-RedAlert/84746fb6-337c-560d-9167-231af8584ff6/scratchpad"
OUT = f"{SP}/cycles_out"
META = json.load(open(f"{OUT}/meta.json"))
REMAP = json.loads(sys.argv[1]) if len(sys.argv) > 1 else {}   # optional dir remaps

def b64(img):
    q = img.quantize(colors=80, method=Image.FASTOCTREE, dither=Image.NONE)
    b = io.BytesIO(); q.save(b, "PNG", optimize=True)
    return "data:image/png;base64," + base64.b64encode(b.getvalue()).decode()

def pack(meta, downscale=False):
    img = Image.open(f"{OUT}/{meta['file']}")
    if downscale:
        img = img.resize((max(1, img.width // 3), max(1, img.height // 3)), Image.LANCZOS)
        return {"src": b64(img), "ax": meta["ax"] / 3, "ay": meta["ay"] / 3,
                "w": img.width, "h": img.height}
    d = {"src": b64(img), "ax": meta["ax"], "ay": meta["ay"],
         "w": img.width, "h": img.height, "s": 1/3}
    if "mount" in meta: d["mount"] = meta["mount"]
    return d

def reorder(lst, key):
    off = REMAP.get(key, 0)
    flip = REMAP.get(key + "_flip", False)
    idx = [( -k if flip else k) + off for k in range(8)]
    return [lst[(i % 8 + 8) % 8] for i in idx]

total = 0
def sz(js):
    global total; total += len(js); return js

# KayKit buildings + truck
kk = {"structures": {}, "harvester": []}
for sid, m in META["kaykit"].items():
    kk["structures"][sid] = pack(m, downscale=True)
truck = [pack(META["truck"][str(k)], downscale=True) for k in range(8)]
kk["harvester"] = reorder(truck, "truck")
open(f"{SP}/kaykit_sprites.js", "w").write(sz("const KAYKIT_RAW = " + json.dumps(kk) + ";\n"))

# soldiers
S_IDS = ["dm_rifle","dm_rocket","dm_heavy","dm_engineer","dm_medic","so_rifle","so_rocket"]
sold, dead = {}, {}
for uid in S_IDS:
    entry = {}
    for pose in (0, 1):
        entry[f"p{pose}"] = reorder([pack(META["soldiers"][f"{uid}_p{pose}_{k}"]) for k in range(8)], "sold")
    sold[uid] = entry
    dead[uid] = reorder([pack(META["soldiers"][f"{uid}_p2_{k}"]) for k in range(8)], "sold")
open(f"{SP}/soldier_sprites.js", "w").write(sz("const SOLDIER_RAW = " + json.dumps(sold) + ";\n"))
open(f"{SP}/dead_sprites.js", "w").write(sz("const DEAD_RAW = " + json.dumps(dead) + ";\n"))

# vehicles
V_IDS = ["dm_wolverine","dm_mbt_walker","dm_mammoth","so_tick_tank","so_scout_buggy",
         "so_stealth_tank","dm_hover_mlrs"]
tanks = {}
for uid in V_IDS:
    hull = reorder([pack(META["veh"][f"{uid}_hull_{k}"]) for k in range(8)], "veh")
    tur = reorder([pack(META["veh"][f"{uid}_tur_{k}"]) for k in range(8)], "veh")
    tanks[uid] = {"hull": hull, "tur": tur, "mount": META["veh"][f"{uid}_tur_0"]["mount"]}
open(f"{SP}/tank_sprites.js", "w").write(sz("const TANK_RAW = " + json.dumps(tanks) + ";\n"))

print("packed; total KB ~", total * 3 // 4 // 1024)

# contact sheet
rows = []
rows.append([("kk_" + sid) for sid in list(META["kaykit"])[:7]])
rows.append([f"truck_{k}" for k in range(8)])
rows.append([f"sold_dm_rifle_p1_{k}" for k in range(8)])
rows.append([f"sold_dm_heavy_p0_{k}" for k in range(8)])
rows.append([f"sold_so_rifle_p2_{k}" for k in range(8)])
rows.append([f"veh_dm_mammoth_hull_{k}" for k in range(8)])
rows.append([f"veh_so_stealth_tank_hull_{k}" for k in range(8)])
sheet = Image.new("RGB", (8 * 150, len(rows) * 150), (8, 10, 8))
for r, row in enumerate(rows):
    for c, tag in enumerate(row):
        p = f"{OUT}/{tag}_final.png"
        if not os.path.exists(p): continue
        im = Image.open(p)
        if im.width > 140: im = im.resize((140, int(im.height * 140 / im.width)))
        sheet.paste(im, (5 + c * 150, 5 + r * 150), im)
sheet.save(f"{SP}/cycles_sheet.png")
print("sheet saved")
