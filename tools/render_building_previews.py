#!/usr/bin/env python3
"""Software iso renderer for BuildingRecipes.json: builds the same shapes the
Unity BuildingFactory will build, flat-shades faces with the atlas region's
average color, painter-sorts, and writes a contact sheet of all structures."""
import json, math
from PIL import Image, ImageDraw, ImageFont

ROOT = "/home/user/RedAlert"
SP = "tools"
data = json.load(open(f"{ROOT}/unity/Assets/Resources/BuildingRecipes.json"))
atlas_img = Image.open(f"{ROOT}/unity/Assets/Resources/Textures/structure_atlas.png")
A = data["atlas"]; CELL = A["cell"]

def region_avg(name):
    ix, iy = A["regions"][name]
    crop = atlas_img.crop((ix * CELL + 8, iy * CELL + 8, (ix + 1) * CELL - 8, (iy + 1) * CELL - 8))
    px = crop.resize((8, 8))
    cols = list(px.getdata())
    return tuple(sum(c[i] for c in cols) // len(cols) for i in range(3))

AVG = {name: region_avg(name) for name in A["regions"]}

def owner_tex(struct_id):
    return "red" if struct_id.startswith("so_") else "gold"

def euler(rx, ry, rz):
    rx, ry, rz = (math.radians(v) for v in (rx, ry, rz))
    cx, sx = math.cos(rx), math.sin(rx)
    cy, sy = math.cos(ry), math.sin(ry)
    cz, sz = math.cos(rz), math.sin(rz)
    # Unity order: Z, X, Y (applied right-to-left on column vectors => v' = Ry*Rx*Rz*v)
    def mul(m, v):
        return tuple(sum(m[i][j] * v[j] for j in range(3)) for i in range(3))
    Rz = ((cz, -sz, 0), (sz, cz, 0), (0, 0, 1))
    Rx = ((1, 0, 0), (0, cx, -sx), (0, sx, cx))
    Ry = ((cy, 0, sy), (0, 1, 0), (-sy, 0, cy))
    def apply(v):
        return mul(Ry, mul(Rx, mul(Rz, v)))
    return apply

def box_faces(size):
    sx, sy, sz = size[0] / 2, size[1], size[2] / 2
    v = [(-sx, 0, -sz), (sx, 0, -sz), (sx, 0, sz), (-sx, 0, sz),
         (-sx, sy, -sz), (sx, sy, -sz), (sx, sy, sz), (-sx, sy, sz)]
    faces = [([0, 1, 5, 4], "side"), ([1, 2, 6, 5], "side"), ([2, 3, 7, 6], "side"),
             ([3, 0, 4, 7], "side"), ([4, 5, 6, 7], "top")]
    return v, faces

def wedge_faces(size):
    sx, sy, sz = size[0] / 2, size[1], size[2] / 2
    # slope rises toward +Z: front edge (z=-sz) at y=0, back at y=sy
    v = [(-sx, 0, -sz), (sx, 0, -sz), (sx, 0, sz), (-sx, 0, sz),
         (sx, sy, sz), (-sx, sy, sz)]
    faces = [([3, 2, 4, 5], "top"), ([0, 1, 2, 3], "side"),
             ([1, 2, 4], "side"), ([0, 3, 5], "side"), ([0, 1, 4, 5], "top")]
    return v, faces

def cyl_faces(r, h, n=12):
    v = []
    for i in range(n):
        a = i / n * 2 * math.pi
        v.append((math.cos(a) * r, 0, math.sin(a) * r))
    for i in range(n):
        a = i / n * 2 * math.pi
        v.append((math.cos(a) * r, h, math.sin(a) * r))
    faces = []
    for i in range(n):
        j = (i + 1) % n
        faces.append(([i, j, n + j, n + i], "side"))
    faces.append((list(range(n, 2 * n)), "top"))
    return v, faces

LIGHT = (0.55, 0.8, 0.25)
ln = math.sqrt(sum(c * c for c in LIGHT)); LIGHT = tuple(c / ln for c in LIGHT)

def render(struct_id, parts, size_px=260):
    polys = []
    for p in parts:
        shape = p["shape"]
        if shape == "box":
            verts, faces = box_faces(p["size"])
        elif shape == "wedge":
            verts, faces = wedge_faces(p["size"])
        else:
            verts, faces = cyl_faces(p["r"], p["h"])
        rot = p.get("rot", [0, 0, 0])
        ap = euler(*rot)
        px, py, pz = p["pos"]
        world = [(x + px, y + py, z + pz) for (x, y, z) in (ap(v) for v in verts)]
        tex = p.get("tex", "metal")
        if tex == "owner": tex = owner_tex(struct_id)
        top = p.get("top", tex)
        emis = p.get("emissive")
        for fi, kind in faces:
            pts = [world[i] for i in fi]
            # face normal
            ux = tuple(pts[1][i] - pts[0][i] for i in range(3))
            uy = tuple(pts[2][i] - pts[0][i] for i in range(3))
            nx = ux[1] * uy[2] - ux[2] * uy[1]
            ny = ux[2] * uy[0] - ux[0] * uy[2]
            nz = ux[0] * uy[1] - ux[1] * uy[0]
            nl = math.sqrt(nx * nx + ny * ny + nz * nz) or 1
            nrm = (nx / nl, ny / nl, nz / nl)
            if emis:
                col = tuple(int(emis[i:i + 2], 16) for i in (1, 3, 5))
            else:
                base = AVG[top if kind == "top" else tex]
                d = abs(sum(nrm[i] * LIGHT[i] for i in range(3)))
                f = 0.45 + 0.6 * d
                col = tuple(min(255, int(c * f)) for c in base)
            depth = sum((p_[0] - p_[2]) * 0 + p_[1] * 0.001 + (p_[0] + p_[2]) for p_ in pts) / len(pts) \
                    + sum(p_[1] for p_ in pts) / len(pts) * 0.001
            polys.append((depth, pts, col))
    # painter: far (low x+z) first
    polys.sort(key=lambda t: t[0])
    img = Image.new("RGB", (size_px, size_px), (8, 10, 9))
    d = ImageDraw.Draw(img)
    scale = size_px / 4.6
    cx, cy = size_px / 2, size_px * 0.72
    def proj(p):
        x, y, z = p
        return (cx + (x - z) * scale * 0.86, cy - (x + z) * scale * 0.5 - y * scale * 0.82)
    for _, pts, col in polys:
        d.polygon([proj(p) for p in pts], fill=col, outline=tuple(int(c * 0.75) for c in col))
    return img

structs = data["structures"]
order = list(structs.keys())
cols, rows = 6, math.ceil(len(order) / 6)
tile = 260
sheet = Image.new("RGB", (cols * tile, rows * (tile + 22)), (5, 8, 6))
dr = ImageDraw.Draw(sheet)
for i, sid in enumerate(order):
    img = render(sid, structs[sid])
    x, y = (i % cols) * tile, (i // cols) * (tile + 22)
    sheet.paste(img, (x, y))
    dr.text((x + 8, y + tile + 4), sid, fill=(120, 200, 140))
sheet.save(f"{SP}/building_previews.png")
print("previews rendered:", len(order))
