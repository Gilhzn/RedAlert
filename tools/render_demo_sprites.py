#!/usr/bin/env python3
"""Render the browser demo's structures as tight transparent sprites from
BuildingRecipes.json, projection-matched to the demo canvas (2:1 iso,
26px/half-cell at zoom 1). Emits sprites.js with data-URI images + anchors."""
import json, math, base64, io
from PIL import Image, ImageDraw

ROOT = "/home/user/RedAlert"
SP = "/tmp/claude-0/-home-user-RedAlert/84746fb6-337c-560d-9167-231af8584ff6/scratchpad"
data = json.load(open(f"{ROOT}/unity/Assets/Resources/BuildingRecipes.json"))
atlas_img = Image.open(f"{ROOT}/unity/Assets/Resources/Textures/structure_atlas.png")
A = data["atlas"]; CELL = A["cell"]

WANT = ["nx_conyard", "nx_refinery", "dm_power_plant", "dm_barracks", "dm_factory", "dm_guard_tower",
        "so_power_plant", "so_factory", "so_laser_turret",
        "nx_silo", "dm_radar", "dm_tech_center", "dm_rocket_tower", "so_obelisk"]

def region_avg(name):
    ix, iy = A["regions"][name]
    crop = atlas_img.crop((ix * CELL + 8, iy * CELL + 8, (ix + 1) * CELL - 8, (iy + 1) * CELL - 8))
    px = list(crop.resize((8, 8)).getdata())
    return tuple(sum(c[i] for c in px) // len(px) for i in range(3))

AVG = {name: region_avg(name) for name in A["regions"]}

def owner_tex(sid): return "red" if sid.startswith("so_") else "gold"

def euler(rx, ry, rz):
    rx, ry, rz = (math.radians(v) for v in (rx, ry, rz))
    cx, sx = math.cos(rx), math.sin(rx); cy, sy = math.cos(ry), math.sin(ry)
    cz, sz = math.cos(rz), math.sin(rz)
    def mul(m, v): return tuple(sum(m[i][j] * v[j] for j in range(3)) for i in range(3))
    Rz = ((cz, -sz, 0), (sz, cz, 0), (0, 0, 1))
    Rx = ((1, 0, 0), (0, cx, -sx), (0, sx, cx))
    Ry = ((cy, 0, sy), (0, 1, 0), (-sy, 0, cy))
    return lambda v: mul(Ry, mul(Rx, mul(Rz, v)))

def box_faces(size):
    sx, sy, sz = size[0] / 2, size[1], size[2] / 2
    v = [(-sx, 0, -sz), (sx, 0, -sz), (sx, 0, sz), (-sx, 0, sz),
         (-sx, sy, -sz), (sx, sy, -sz), (sx, sy, sz), (-sx, sy, sz)]
    return v, [([0, 1, 5, 4], "side"), ([1, 2, 6, 5], "side"), ([2, 3, 7, 6], "side"),
               ([3, 0, 4, 7], "side"), ([4, 5, 6, 7], "top")]

def wedge_faces(size):
    sx, sy, sz = size[0] / 2, size[1], size[2] / 2
    v = [(-sx, 0, -sz), (sx, 0, -sz), (sx, 0, sz), (-sx, 0, sz), (sx, sy, sz), (-sx, sy, sz)]
    return v, [([0, 1, 4, 5], "top"), ([2, 3, 5, 4], "side"), ([1, 2, 4], "side"), ([3, 0, 5], "side")]

def cyl_faces(r, h, n=14):
    v = [(math.cos(i / n * 2 * math.pi) * r, 0, math.sin(i / n * 2 * math.pi) * r) for i in range(n)]
    v += [(x, h, z) for (x, y, z) in v]
    faces = [([i, (i + 1) % n, n + (i + 1) % n, n + i], "side") for i in range(n)]
    faces.append((list(range(n, 2 * n)), "top"))
    return v, faces

LIGHT = (0.55, 0.8, 0.25)
ln = math.sqrt(sum(c * c for c in LIGHT)); LIGHT = tuple(c / ln for c in LIGHT)

# demo projection at zoom 1: cell = 52x26 px
SS = 3
def proj(p):
    x, y, z = p
    return ((x - z) * 26.0 * SS, (x + z) * 13.0 * SS - y * 21.0 * SS)

def render(sid, parts):
    polys = []
    for p in parts:
        shape = p["shape"]
        if shape == "box": verts, faces = box_faces(p["size"])
        elif shape == "wedge": verts, faces = wedge_faces(p["size"])
        else: verts, faces = cyl_faces(p["r"], p["h"])
        ap = euler(*(p.get("rot", [0, 0, 0])))
        px_, py_, pz_ = p["pos"]
        world = [(x + px_, y + py_, z + pz_) for (x, y, z) in (ap(v) for v in verts)]
        tex = p.get("tex", "metal"); tex = owner_tex(sid) if tex == "owner" else tex
        top = p.get("top", tex); emis = p.get("emissive")
        for fi, kind in faces:
            pts = [world[i] for i in fi]
            ux = tuple(pts[1][i] - pts[0][i] for i in range(3))
            uy = tuple(pts[2][i] - pts[0][i] for i in range(3))
            n = (ux[1] * uy[2] - ux[2] * uy[1], ux[2] * uy[0] - ux[0] * uy[2], ux[0] * uy[1] - ux[1] * uy[0])
            nl = math.sqrt(sum(c * c for c in n)) or 1
            nrm = tuple(c / nl for c in n)
            if emis:
                col = tuple(int(emis[i:i + 2], 16) for i in (1, 3, 5)) + (255,)
            else:
                base = AVG[top if kind == "top" else tex]
                f = 0.5 + 0.55 * abs(sum(nrm[i] * LIGHT[i] for i in range(3)))
                col = tuple(min(255, int(c * f)) for c in base) + (255,)
            depth = sum(pt[0] + pt[2] for pt in pts) / len(pts) + sum(pt[1] for pt in pts) / len(pts) * 0.002
            polys.append((depth, pts, col))
    polys.sort(key=lambda t: t[0])

    pts2 = [proj(pt) for _, face, _ in polys for pt in face]
    xs = [p[0] for p in pts2]; ys = [p[1] for p in pts2]
    pad = 3
    x0, x1 = math.floor(min(xs)) - pad, math.ceil(max(xs)) + pad
    y0, y1 = math.floor(min(ys)) - pad, math.ceil(max(ys)) + pad
    img = Image.new("RGBA", (x1 - x0, y1 - y0), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for _, face, col in polys:
        pp = [(p[0] - x0, p[1] - y0) for p in (proj(pt) for pt in face)]
        outline = tuple(int(c * 0.72) for c in col[:3]) + (255,)
        d.polygon(pp, fill=col, outline=outline)
    # anchor: projection of world origin (ground center) inside the image
    ax, ay = -x0, -y0
    return img, ax, ay

out = {}
for sid in WANT:
    img, ax, ay = render(sid, data["structures"][sid])
    buf = io.BytesIO(); img.save(buf, "PNG", optimize=True)
    out[sid] = {"src": "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode(),
                "ax": ax, "ay": ay, "w": img.width, "h": img.height, "s": 1.0/SS}
    print(sid, img.size, len(buf.getvalue()) // 1024, "KB")

with open(f"{SP}/sprites.js", "w") as f:
    f.write("const SPRITES_RAW = " + json.dumps(out) + ";\n")
print("total KB:", sum(len(v["src"]) for v in out.values()) * 3 // 4 // 1024)
