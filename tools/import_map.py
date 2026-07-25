#!/usr/bin/env python3
"""Import a Blender map export (Sirocco Flats) into the browser game's 128x128
grid model. Reads assets_src/map-export/ and writes data/maps/sirocco.json.

The gameplay-critical layer is `blocked[]`, derived from passability.json
(the wadis are barriers; the six bridges are the only vehicle crossings).
terrain/height/bridges/roads/spawns are derived alongside.

If terrain.bin (+terrain.json) is present it supplies the REAL Blender
heightfield and precise water/rock typing; otherwise heights are derived
from the wadi network so the map still reads as sunken channels + benches.
"""
import json, os, struct, zlib, math, collections, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC  = os.path.join(ROOT, "assets_src", "map-export")
OUT  = os.path.join(ROOT, "data", "maps")
MAP  = 128                                   # game grid
def gidx(x, y): return y * MAP + x

def load(name):
    with open(os.path.join(SRC, name)) as f: return json.load(f)

pas = load("passability.json")
N, SIZE, CELL = pas["N"], pas["size"], pas["cell_m"]      # 256, 400m, 1.5686
grid = pas["grid"]                                        # row-major, x_fastest, 1=passable
def spass(col, row):                                      # source passability, clamped
    col = min(N - 1, max(0, col)); row = min(N - 1, max(0, row))
    return grid[row * N + col]

# world(m, centre origin) -> game grid (0..127). +X east -> +gx ; +Y north -> +gy
def w2g(wx, wy):
    return (wx + SIZE / 2) / SIZE * MAP, (wy + SIZE / 2) / SIZE * MAP
# game grid -> source col/row (256)
def g2s(gx, gy):
    return int((gx + 0.5) / MAP * N), int((gy + 0.5) / MAP * N)

# ---- optional real heightfield from terrain.bin ------------------------------
real_h = None; hmin = hmax = None; water_z = pas.get("water", -1.6)
tj = os.path.join(SRC, "terrain.json"); tb = os.path.join(SRC, "terrain.bin")
if os.path.exists(tj) and os.path.exists(tb):
    th = json.load(open(tj)); TN = th["N"]; stride = th["stride"]; hmin = th["hmin"]; hmax = th["hmax"]
    ch = th["channels"]; hi = ch.index("height")
    buf = open(tb, "rb").read()
    fa = struct.unpack("<%df" % (len(buf) // 4), buf)
    def sheight(col, row):        # source terrain height at 256-space, sampled from TNxTN
        c = min(TN - 1, max(0, int((col + 0.5) / N * TN)))
        r = min(TN - 1, max(0, int((row + 0.5) / N * TN)))
        return fa[(r * TN + c) * stride + hi]
    real_h = sheight
    print("terrain.bin present: %dx%d, height %.1f..%.1f m -> real Blender relief" % (TN, TN, hmin, hmax))
else:
    print("terrain.bin absent: deriving heights from the wadi network (upload it for the exact relief)")

# ---- 1) blocked[] + terrain[] from passability (256->128) --------------------
# Block only cells whose whole 2x2 source block is impassable: this preserves the
# thin passable corridors that make the map traversable at half resolution.
blocked = [0] * (MAP * MAP)
terrain = [0] * (MAP * MAP)          # 0 sand, 1 rock, 2 crest, 3 water
barrier = [0] * (MAP * MAP)          # original wadi/steep barrier (vs. pockets filled later)
for gy in range(MAP):
    for gx in range(MAP):
        c0, r0 = 2 * gx, 2 * gy
        imp = sum(0 if spass(c0 + dc, r0 + dr) else 1 for dc in (0, 1) for dr in (0, 1))
        if imp >= 4:                 # only fully-impassable blocks are barriers
            blocked[gidx(gx, gy)] = 1
            barrier[gidx(gx, gy)] = 1
            terrain[gidx(gx, gy)] = 3   # provisional: barriers read as wadi water (refined by real height)

# ---- 2) bridges: carve passable crossings, remember their cells --------------
bridges_out = []
brg = load("bridges.json")
for b in brg:
    ax, ay = w2g(b["ax"], b["ay"]); bx, by = w2g(b["bx"], b["by"])
    steps = max(2, int(math.hypot(bx - ax, by - ay)) + 1)
    cells = []
    for s in range(steps + 1):
        t = s / steps
        gx = int(round(ax + (bx - ax) * t)); gy = int(round(ay + (by - ay) * t))
        for dx in (-1, 0, 1):        # 3-wide deck so units actually fit
            for dy in (-1, 0, 1):
                x, y = gx + dx, gy + dy
                if 0 <= x < MAP and 0 <= y < MAP:
                    blocked[gidx(x, y)] = 0
                    if terrain[gidx(x, y)] == 3: terrain[gidx(x, y)] = 0
        if 0 <= gx < MAP and 0 <= gy < MAP: cells.append([gx, gy])
    bridges_out.append({"a": [round(ax, 1), round(ay, 1)], "b": [round(bx, 1), round(by, 1)],
                        "deck": b["deck"], "cells": cells})

# ---- 2b) keep only the bridge-connected main region; fill stranded pockets ---
# Sirocco's passable land is (by design) split into basins joined by the bridges.
# We keep the single largest connected region as the play area and turn every
# stranded pocket into blocked rock scenery, so no unit or base is ever marooned.
def label(blk):
    comp = [-1] * (MAP * MAP); n = 0; sz = {}
    for i in range(MAP * MAP):
        if blk[i] or comp[i] != -1: continue
        n += 1; sz[n] = 0; dq = collections.deque([i]); comp[i] = n
        while dq:
            j = dq.popleft(); sz[n] += 1; x, y = j % MAP, j // MAP
            for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < MAP and 0 <= ny < MAP and not blk[gidx(nx, ny)] and comp[gidx(nx, ny)] == -1:
                    comp[gidx(nx, ny)] = n; dq.append(gidx(nx, ny))
    return comp, sz
comp, sizes = label(blocked)
main = max(sizes, key=sizes.get) if sizes else 0
for i in range(MAP * MAP):
    if not blocked[i] and comp[i] != main:      # stranded pocket -> rock scenery
        blocked[i] = 1; terrain[i] = 1
comp, sizes = label(blocked)                    # relabel the clean map
main = max(sizes, key=sizes.get) if sizes else 0
mainfrac = sizes.get(main, 0) / (MAP * MAP); ncomp = len(sizes)

# ---- 3) heights: real Blender field, else derived from distance-to-wadi ------
height = [0.0] * (MAP * MAP)
if real_h:
    lo, span = hmin, max(1e-3, hmax - hmin)
    for gy in range(MAP):
        for gx in range(MAP):
            col, row = g2s(gx, gy)
            h = real_h(col, row)
            # normalise to the game's gentle 0..~5 range; water sits at 0
            height[gidx(gx, gy)] = max(0.0, (h - water_z) / span * 6.0)
            if blocked[gidx(gx, gy)] and h <= water_z + 0.2:
                terrain[gidx(gx, gy)] = 3          # genuine water
            elif blocked[gidx(gx, gy)]:
                terrain[gidx(gx, gy)] = 1; height[gidx(gx, gy)] = max(height[gidx(gx, gy)], 2.5)  # dry steep -> rock
else:
    # BFS distance from every water/barrier cell; banks rise away from the wadi
    INF = 1 << 30; dist = [INF] * (MAP * MAP); dq = collections.deque()
    for i in range(MAP * MAP):
        if terrain[i] == 3: dist[i] = 0; dq.append(i)
    while dq:
        i = dq.popleft(); x, y = i % MAP, i // MAP
        for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < MAP and 0 <= ny < MAP and dist[gidx(nx, ny)] == INF:
                dist[gidx(nx, ny)] = dist[i] + 1; dq.append(gidx(nx, ny))
    for i in range(MAP * MAP):
        d = min(dist[i], 8)
        height[i] = 0.0 if terrain[i] == 3 else 0.4 + 0.35 * d      # sunken wadis, rising benches

# ---- 5) spawns: a clear passable pocket per corner, all in the main region ---
def clear_pocket(cx, cy, rad=3):  # nearest main-region cell with a (2rad+1)^2 clear halo
    best = None; bestd = 1e9
    for gy in range(rad, MAP - rad):
        for gx in range(rad, MAP - rad):
            if comp[gidx(gx, gy)] != main: continue
            ok = all(not blocked[gidx(gx+dx, gy+dy)]
                     for dx in range(-rad, rad+1) for dy in range(-rad, rad+1))
            if not ok: continue
            d = (gx-cx)**2 + (gy-cy)**2
            if d < bestd: bestd = d; best = (gx, gy)
    return best
player = clear_pocket(10, MAP-10)
en1 = clear_pocket(MAP-10, 10); en2 = clear_pocket(MAP-12, MAP-12); en3 = clear_pocket(12, 12)
spawns = {"player": player, "enemies": [e for e in (en1, en2, en3) if e]}

# ---- 6) roads (visual) -> grid polylines -------------------------------------
roads_out = []
for poly in load("roads.json"):
    gp = []
    for wx, wy in poly:
        gx, gy = w2g(wx, wy); gp.append([round(gx, 2), round(gy, 2)])
    roads_out.append(gp)

# ---- write -------------------------------------------------------------------
os.makedirs(OUT, exist_ok=True)
out = {"name": "Sirocco Flats", "MAP": MAP, "hasRealHeight": bool(real_h),
       "blocked": blocked, "terrain": terrain,
       "height": [round(h, 3) for h in height],
       "bridges": bridges_out, "roads": roads_out, "spawns": spawns}
with open(os.path.join(OUT, "sirocco.json"), "w") as f: json.dump(out, f, separators=(",", ":"))

bl = sum(blocked); print("\n128 grid: blocked=%.1f%% water=%.1f%% mainRegion=%.1f%% components=%d"
      % (100*bl/(MAP*MAP), 100*sum(1 for t in terrain if t==3)/(MAP*MAP), 100*mainfrac, ncomp))
print("spawns:", spawns)
print("bridges carved:", len(bridges_out), " roads:", len(roads_out))
# sanity: are all spawns reachable from each other?
ids = {comp[gidx(*s)] for s in [player]+spawns["enemies"] if s}
print("all spawns in one region:", ids == {main})

# ASCII preview of the FINAL game grid
def ch(i):
    t = terrain[i]
    return "~" if t==3 else ("^" if t==1 else ".")
print("\n=== final 128 grid @ step2 (~=water ^=rock .=land, B=bridge, P/E=spawn) ===")
sp = {}
if player: sp[gidx(*player)] = "P"
for e in spawns["enemies"]: sp[gidx(*e)] = "E"
for b in bridges_out:
    for c in b["cells"]: sp[gidx(*c)] = "B"
for gy in range(0, MAP, 2):
    line = ""
    for gx in range(0, MAP, 2):
        i = gidx(gx, gy); line += sp.get(i, ch(i))
    print(line)
