#!/usr/bin/env python3
"""Render KayKit glTF models (the exact ones mapped into Unity) as demo
sprites: buildings at the demo iso projection, the spacetruck harvester at
8 yaw angles. Emits kaykit_sprites.js (KAYKIT_RAW) with data URIs."""
import json, math, base64, io, struct, os
from PIL import Image, ImageDraw

import sys
K = sys.argv[1] if len(sys.argv) > 1 else "/workspace/kaykit-space-base-bits-1.0/addons/kaykit_space_base_bits/Assets/gltf"
SP = "."
TEX = Image.open(f"{K}/spacebits_texture.png").convert("RGB")
TW_, TH_ = TEX.size

COMP = {5120: ("b", 1), 5121: ("B", 1), 5122: ("h", 2), 5123: ("H", 2), 5125: ("I", 4), 5126: ("f", 4)}
NCOMP = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}

def read_accessor(g, buf, idx):
    a = g["accessors"][idx]
    bv = g["bufferViews"][a["bufferView"]]
    fmt, size = COMP[a["componentType"]]
    n = NCOMP[a["type"]]
    off = bv.get("byteOffset", 0) + a.get("byteOffset", 0)
    stride = bv.get("byteStride", size * n)
    out = []
    for i in range(a["count"]):
        vals = struct.unpack_from("<" + fmt * n, buf, off + i * stride)
        out.append(vals if n > 1 else vals[0])
    return out

def mat_identity(): return [[1,0,0,0],[0,1,0,0],[0,0,1,0],[0,0,0,1]]
def mat_mul(a, b):
    return [[sum(a[i][k]*b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]
def mat_from_trs(nd):
    m = mat_identity()
    if "matrix" in nd:
        c = nd["matrix"]
        return [[c[0],c[4],c[8],c[12]],[c[1],c[5],c[9],c[13]],[c[2],c[6],c[10],c[14]],[c[3],c[7],c[11],c[15]]]
    t = nd.get("translation", [0,0,0]); s = nd.get("scale", [1,1,1]); q = nd.get("rotation", [0,0,0,1])
    x,y,z,w = q
    R = [[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]]
    for i in range(3):
        for j in range(3):
            m[i][j] = R[i][j]*s[j]
        m[i][3] = t[i]
    return m
def mat_apply(m, v):
    return tuple(m[i][0]*v[0]+m[i][1]*v[1]+m[i][2]*v[2]+m[i][3] for i in range(3))

def load_model(name):
    g = json.load(open(f"{K}/{name}.gltf"))
    buf = open(f"{K}/{g['buffers'][0]['uri']}", "rb").read()
    tris = []  # (v0,v1,v2, uv0,uv1,uv2)
    def walk(node_idx, parent_m):
        nd = g["nodes"][node_idx]
        m = mat_mul(parent_m, mat_from_trs(nd))
        if "mesh" in nd:
            mesh = g["meshes"][nd["mesh"]]
            for prim in mesh["primitives"]:
                pos = read_accessor(g, buf, prim["attributes"]["POSITION"])
                uv = read_accessor(g, buf, prim["attributes"]["TEXCOORD_0"])
                ind = read_accessor(g, buf, prim["indices"])
                pos = [mat_apply(m, p) for p in pos]
                for k in range(0, len(ind), 3):
                    a, b, c = ind[k], ind[k+1], ind[k+2]
                    tris.append((pos[a], pos[b], pos[c], uv[a], uv[b], uv[c]))
        for ch in nd.get("children", []):
            walk(ch, m)
    scene = g["scenes"][g.get("scene", 0)]
    for n in scene["nodes"]:
        walk(n, mat_identity())
    return tris

def tex_sample(uvs):
    u = sum(p[0] for p in uvs) / 3 % 1.0
    v = sum(p[1] for p in uvs) / 3 % 1.0
    return TEX.getpixel((min(TW_-1, int(u * TW_)), min(TH_-1, int(v * TH_))))

LIGHT = (0.55, 0.8, 0.25)
ln = math.sqrt(sum(c*c for c in LIGHT)); LIGHT = tuple(c/ln for c in LIGHT)

def proj(p):
    x, y, z = p
    return ((x - z) * 26.0, (x + z) * 13.0 - y * 21.0)

def render(tris, target_cells, yaw_deg=0.0):
    # fit: scale so footprint max(x,z) == target_cells, ground at y=0, centered
    xs = [v[i][0] for v in tris for i in range(3)]
    ys = [v[i][1] for v in tris for i in range(3)]
    zs = [v[i][2] for v in tris for i in range(3)]
    fp = max(max(xs)-min(xs), max(zs)-min(zs)) or 1
    s = target_cells / fp
    cx, cz = (max(xs)+min(xs))/2, (max(zs)+min(zs))/2
    y0 = min(ys)
    a = math.radians(yaw_deg)
    ca, sa = math.cos(a), math.sin(a)
    def xf(p):
        x, y, z = (p[0]-cx)*s, (p[1]-y0)*s, (p[2]-cz)*s
        return (x*ca + z*sa, y, -x*sa + z*ca)
    polys = []
    for t in tris:
        pts = [xf(t[i]) for i in range(3)]
        ux = tuple(pts[1][i]-pts[0][i] for i in range(3))
        uy = tuple(pts[2][i]-pts[0][i] for i in range(3))
        n = (ux[1]*uy[2]-ux[2]*uy[1], ux[2]*uy[0]-ux[0]*uy[2], ux[0]*uy[1]-ux[1]*uy[0])
        nl = math.sqrt(sum(c*c for c in n)) or 1
        nrm = tuple(c/nl for c in n)
        base = tex_sample(t[3:6])
        f = 0.48 + 0.55 * abs(sum(nrm[i]*LIGHT[i] for i in range(3)))
        col = tuple(min(255, int(c*f)) for c in base) + (255,)
        depth = sum(p[0]+p[2] for p in pts)/3 + sum(p[1] for p in pts)/3*0.003
        polys.append((depth, pts, col))
    polys.sort(key=lambda t: t[0])
    pts2 = [proj(p) for _, face, _ in polys for p in face]
    xs2 = [p[0] for p in pts2]; ys2 = [p[1] for p in pts2]
    pad = 3
    x0, x1 = math.floor(min(xs2))-pad, math.ceil(max(xs2))+pad
    y0_, y1 = math.floor(min(ys2))-pad, math.ceil(max(ys2))+pad
    img = Image.new("RGBA", (x1-x0, y1-y0_), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for _, face, col in polys:
        pp = [(p[0]-x0, p[1]-y0_) for p in (proj(pt) for pt in face)]
        d.polygon(pp, fill=col)
    return img, -x0, -y0_

def pack(img, ax, ay):
    buf = io.BytesIO(); img.save(buf, "PNG", optimize=True)
    return {"src": "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode(),
            "ax": ax, "ay": ay, "w": img.width, "h": img.height}

BUILDINGS = {
    "nx_conyard":    ("basemodule_garage", 2.9),
    "nx_refinery":   ("drill_structure",  2.8),
    "dm_power_plant":("solarpanel",       1.9),
    "so_power_plant":("windturbine_low",  1.9),
    "dm_barracks":   ("structure_low",    1.9),
    "dm_factory":    ("basemodule_A",     2.9),
    "so_factory":    ("basemodule_B",     2.9),
}

out = {"structures": {}, "harvester": []}
total = 0
for sid, (model, size) in BUILDINGS.items():
    tris = load_model(model)
    img, ax, ay = render(tris, size)
    p = pack(img, ax, ay)
    out["structures"][sid] = p
    total += len(p["src"])
    print(sid, "<-", model, img.size)

truck = load_model("spacetruck")
for k in range(8):
    img, ax, ay = render(truck, 1.15, yaw_deg=k*45)
    p = pack(img, ax, ay)
    out["harvester"].append(p)
    total += len(p["src"])
print("truck 8 dirs done; total KB ~", total*3//4//1024)

with open(f"{SP}/kaykit_sprites.js", "w") as f:
    f.write("const KAYKIT_RAW = " + json.dumps(out) + ";\n")
# contact sheet for my own review
sheet = Image.new("RGB", (170*8, 190*2), (6, 9, 7))
i = 0
for sid in BUILDINGS:
    img = Image.open(io.BytesIO(base64.b64decode(out["structures"][sid]["src"].split(",")[1])))
    sheet.paste(img, (10 + (i%8)*170, 10), img); i += 1
for k in range(8):
    img = Image.open(io.BytesIO(base64.b64decode(out["harvester"][k]["src"].split(",")[1])))
    sheet.paste(img, (10 + k*170, 200), img)
sheet.save(f"{SP}/kaykit_sheet.png")
print("sheet saved")
