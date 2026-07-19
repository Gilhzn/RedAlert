#!/usr/bin/env python3
"""Generate the structure texture atlas (8x8 grid of 128px regions) in a
late-90s RTS style: saturated painted metal, brick, hazard stripes, big doors.
Output: unity/Assets/Resources/Textures/structure_atlas.png (+ regions map)."""
import json, math, random, os, sys
from PIL import Image, ImageDraw

CELL = 128
GRID = 8
random.seed(42)

img = Image.new("RGB", (CELL * GRID, CELL * GRID), (20, 20, 20))
d = ImageDraw.Draw(img)

def clamp(v): return max(0, min(255, int(v)))
def shade(c, f): return tuple(clamp(x * f) for x in c)
def jitter(c, a): return tuple(clamp(x + random.uniform(-a, a)) for x in c)

def region(ix, iy):
    return ix * CELL, iy * CELL

def noise_fill(x0, y0, base, amp=8, step=4):
    for y in range(y0, y0 + CELL, step):
        for x in range(x0, x0 + CELL, step):
            d.rectangle([x, y, x + step - 1, y + step - 1], fill=jitter(base, amp))

def seams(x0, y0, color, n=4, horiz=True, vert=True):
    if vert:
        for i in range(1, n):
            x = x0 + i * CELL // n
            d.line([x, y0, x, y0 + CELL - 1], fill=color, width=2)
    if horiz:
        for i in range(1, n):
            y = y0 + i * CELL // n
            d.line([x0, y, x0 + CELL - 1, y], fill=color, width=2)

def rivets(x0, y0, color, n=4):
    for i in range(n + 1):
        for j in range(n + 1):
            x = x0 + 6 + i * (CELL - 12) // n
            y = y0 + 6 + j * (CELL - 12) // n
            d.ellipse([x - 2, y - 2, x + 2, y + 2], fill=color)

def grime(x0, y0, color, count=10):
    for _ in range(count):
        x = x0 + random.randint(0, CELL - 1)
        y = y0 + random.randint(0, CELL // 2)
        h = random.randint(8, 40)
        d.line([x, y, x, y + h], fill=color, width=random.randint(1, 3))

def panel(ix, iy, base, seam_f=0.55, rivet_f=1.35, n=4):
    x0, y0 = region(ix, iy)
    noise_fill(x0, y0, base)
    seams(x0, y0, shade(base, seam_f), n)
    rivets(x0, y0, shade(base, rivet_f), n)
    grime(x0, y0, shade(base, 0.7), 8)
    # top edge highlight per panel row
    for i in range(n):
        y = y0 + i * CELL // n + 2
        d.line([x0, y, x0 + CELL - 1, y], fill=shade(base, 1.2), width=1)

def brick(ix, iy, base, mortar):
    x0, y0 = region(ix, iy)
    noise_fill(x0, y0, mortar, amp=5)
    bh, bw = 16, 32
    for row in range(CELL // bh):
        off = (row % 2) * (bw // 2)
        for col in range(-1, CELL // bw + 1):
            x = x0 + col * bw + off
            y = y0 + row * bh
            d.rectangle([x + 2, y + 2, x + bw - 2, y + bh - 2], fill=jitter(base, 14))
            d.line([x + 2, y + 2, x + bw - 2, y + 2], fill=jitter(shade(base, 1.25), 8), width=1)
    grime(x0, y0, shade(base, 0.55), 6)

def hazard(ix, iy, c1=(226, 178, 32), c2=(28, 28, 30)):
    x0, y0 = region(ix, iy)
    tile = Image.new("RGB", (CELL, CELL), c1)
    td = ImageDraw.Draw(tile)
    for y in range(0, CELL, 4):
        for x in range(0, CELL, 4):
            td.rectangle([x, y, x + 3, y + 3], fill=jitter(c1, 6))
    w = 24
    for i in range(-3, CELL // w + 2):
        x = i * w * 2
        td.polygon([(x, CELL), (x + w, CELL), (x + w + CELL, 0), (x + CELL, 0)], fill=jitter(c2, 4))
    for _ in range(6):
        gx = random.randint(0, CELL - 1); gy = random.randint(0, CELL // 2)
        td.line([gx, gy, gx, gy + random.randint(8, 30)], fill=(60, 55, 40), width=2)
    img.paste(tile, (x0, y0))

def door(ix, iy, base, trim):
    x0, y0 = region(ix, iy)
    noise_fill(x0, y0, shade(base, 0.8), amp=5)
    # horizontal slats
    for i in range(8):
        y = y0 + 8 + i * 14
        d.rectangle([x0 + 8, y, x0 + CELL - 8, y + 10], fill=jitter(base, 8))
        d.line([x0 + 8, y, x0 + CELL - 8, y], fill=shade(base, 1.3), width=1)
        d.line([x0 + 8, y + 10, x0 + CELL - 8, y + 10], fill=shade(base, 0.5), width=2)
    d.rectangle([x0, y0, x0 + CELL - 1, y0 + 6], fill=trim)
    d.rectangle([x0, y0 + CELL - 7, x0 + CELL - 1, y0 + CELL - 1], fill=trim)
    d.rectangle([x0, y0, x0 + 5, y0 + CELL - 1], fill=trim)
    d.rectangle([x0 + CELL - 6, y0, x0 + CELL - 1, y0 + CELL - 1], fill=trim)

def roof(ix, iy, base):
    x0, y0 = region(ix, iy)
    noise_fill(x0, y0, base, amp=6)
    for i in range(0, CELL, 10):  # corrugation
        d.line([x0 + i, y0, x0 + i, y0 + CELL - 1], fill=shade(base, 0.7), width=2)
        d.line([x0 + i + 4, y0, x0 + i + 4, y0 + CELL - 1], fill=shade(base, 1.2), width=1)
    d.rectangle([x0, y0, x0 + CELL - 1, y0 + 3], fill=shade(base, 1.25))

def vents(ix, iy, base):
    x0, y0 = region(ix, iy)
    noise_fill(x0, y0, base, amp=6)
    for i in range(2):
        for j in range(2):
            vx = x0 + 12 + i * 60
            vy = y0 + 12 + j * 60
            d.rectangle([vx, vy, vx + 44, vy + 44], fill=shade(base, 0.55))
            for k in range(5):
                d.line([vx + 3, vy + 5 + k * 9, vx + 41, vy + 5 + k * 9], fill=shade(base, 1.15), width=3)

def glow(ix, iy, core, edge):
    x0, y0 = region(ix, iy)
    for y in range(CELL):
        for x in range(0, CELL, 2):
            dx = (x - CELL / 2) / (CELL / 2)
            dy = (y - CELL / 2) / (CELL / 2)
            r = min(1, math.hypot(dx, dy))
            c = tuple(clamp(core[i] * (1 - r) + edge[i] * r + random.uniform(-6, 6)) for i in range(3))
            d.rectangle([x0 + x, y0 + y, x0 + x + 1, y0 + y + 1], fill=c)
    # crystal facets
    for _ in range(7):
        cx = x0 + random.randint(20, CELL - 20)
        cy = y0 + random.randint(20, CELL - 20)
        s = random.randint(6, 16)
        d.polygon([(cx, cy - s), (cx + s, cy + s), (cx - s, cy + s)], outline=shade(core, 1.2))

def grate(ix, iy, base):
    x0, y0 = region(ix, iy)
    noise_fill(x0, y0, shade(base, 0.5), amp=4)
    for i in range(0, CELL, 16):
        d.line([x0 + i, y0, x0 + i, y0 + CELL - 1], fill=base, width=5)
        d.line([x0, y0 + i, x0 + CELL - 1, y0 + i], fill=base, width=5)

# ---- palette ----
CONCRETE   = (128, 124, 116)
STEEL      = (150, 152, 158)
STEEL_DARK = (86, 88, 94)
GOLD       = (196, 150, 62)     # Dominion painted metal
GOLD_DARK  = (140, 104, 42)
CRIMSON    = (172, 40, 46)      # Serpent painted metal
BLACKP     = (52, 50, 54)
BRICK_C    = (158, 84, 58)
RUST       = (122, 78, 50)

REGIONS = {}
def R(name, ix, iy): REGIONS[name] = [ix, iy]

# row 0: neutral surfaces
panel(0, 0, CONCRETE, n=2);            R("concrete", 0, 0)
panel(1, 0, shade(CONCRETE, 0.7), n=2);R("concrete_dark", 1, 0)
panel(2, 0, STEEL);                    R("metal", 2, 0)
panel(3, 0, STEEL_DARK);               R("metal_dark", 3, 0)
brick(4, 0, BRICK_C, (92, 74, 62));    R("brick", 4, 0)
brick(5, 0, shade(BRICK_C, 0.65), (70, 58, 50)); R("brick_dark", 5, 0)
panel(6, 0, RUST, n=3);                R("rust", 6, 0)
grate(7, 0, STEEL);                    R("grate", 7, 0)
# row 1: faction paint
panel(0, 1, GOLD);                     R("gold", 0, 1)
panel(1, 1, GOLD_DARK);                R("gold_dark", 1, 1)
panel(2, 1, CRIMSON);                  R("red", 2, 1)
panel(3, 1, shade(CRIMSON, 0.62));     R("red_dark", 3, 1)
panel(4, 1, BLACKP);                   R("black", 4, 1)
hazard(5, 1);                          R("hazard", 5, 1)
hazard(6, 1, (188, 40, 40), (240, 235, 225)); R("hazard_red", 6, 1)
roof(7, 1, (98, 100, 104));            R("roof", 7, 1)
# row 2: features
door(0, 2, (110, 112, 118), GOLD_DARK);R("door", 0, 2)
door(1, 2, (70, 62, 66), shade(CRIMSON, 0.8)); R("door_red", 1, 2)
vents(2, 2, STEEL_DARK);               R("vent", 2, 2)
roof(3, 2, shade(GOLD, 0.8));          R("roof_gold", 3, 2)
roof(4, 2, (60, 52, 56));              R("roof_dark", 4, 2)
glow(5, 2, (110, 255, 140), (20, 60, 30));   R("glow_green", 5, 2)
glow(6, 2, (255, 90, 96), (60, 16, 18));     R("glow_red", 6, 2)
glow(7, 2, (110, 190, 255), (18, 40, 66));   R("glow_blue", 7, 2)

out_dir = sys.argv[1] if len(sys.argv) > 1 else "."
os.makedirs(f"{out_dir}/unity/Assets/Resources/Textures", exist_ok=True)
img.save(f"{out_dir}/unity/Assets/Resources/Textures/structure_atlas.png")
with open(f"{out_dir}/tools/atlas_regions.json", "w") as f:
    json.dump({"cell": CELL, "grid": GRID, "regions": REGIONS}, f, indent=1)
print("atlas written,", len(REGIONS), "regions")
