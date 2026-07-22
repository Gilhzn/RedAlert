#!/usr/bin/env python3
# Tiberium Dusk — high-quality Blender render of the demo map.
# Faithfully reconstructs the game's procedural terrain (same seed / noise /
# river / mountains / hills / crystal fields) and adds lush meadows along the
# water, plus bridges. Rendered at Full HD with a dusk atmosphere.
import bpy, bmesh, math
from mathutils import Vector

# ----------------------------------------------------------------------------
# 0. render config (fast preview vs full)  --  set via env-ish globals
# ----------------------------------------------------------------------------
import os
PREVIEW = os.environ.get("PREVIEW", "0") == "1"
RES     = os.environ.get("RES", "1080")          # 1080 | 4k
CAM     = os.environ.get("CAM", "hero")          # hero | dunes | topdown
RENDER      = os.environ.get("RENDER", "1") == "1"
EXPORT_GLTF = os.environ.get("EXPORT_GLTF", "0") == "1"
GLB_OUT     = os.path.abspath(os.environ.get("GLB_OUT", "terrain.glb"))
if PREVIEW:      RES_X, RES_Y, SAMPLES = 960, 540, 24
elif RES == "4k": RES_X, RES_Y, SAMPLES = 3840, 2160, int(os.environ.get("SAMPLES", "200"))
else:            RES_X, RES_Y, SAMPLES = 1920, 1080, int(os.environ.get("SAMPLES", "200"))
OUT = os.path.abspath(os.environ.get("OUT", "tiberium_dusk_map.png"))  # Blender needs abs

# ----------------------------------------------------------------------------
# 1. reconstruct the game world  (ports makeWorld() from the JS source)
# ----------------------------------------------------------------------------
MAP = int(os.environ.get("MAPSIZE", "64"))  # 64 = game size; 128 = 4x area
S   = MAP / 64.0                            # feature-size scale (keep grandeur when bigger)
ZS  = float(os.environ.get("ZS", "1.7"))   # height exaggeration (renders); 1.0 for glTF

seed = 1337                     # waveNum == 0
def rnd():
    global seed
    seed = (seed * 1103515245 + 12345) & 0x7fffffff
    return seed / 0x7fffffff

def idx(x, y): return y * MAP + x
def inMap(x, y): return 0 <= x < MAP and 0 <= y < MAP

terrain = [0]*(MAP*MAP)         # 0 sand, 1 rock/mtn, 2 dark sand, 3 water, 4 meadow
crystal = [0]*(MAP*MAP)         # 0 none, 1..4 green, 9 blue
blocked = [0]*(MAP*MAP)
height  = [0.0]*(MAP*MAP)

def homeClear(x, y):
    return (x < 13 and y > MAP-14) or (x > MAP-14 and y < 13)

# value-noise field (seeded) — identical to the game
NG = 10
nGrid = [rnd() for _ in range((NG+1)*(NG+1))]
def vnoise(x, y):
    gx, gy = x/MAP*NG, y/MAP*NG
    x0, y0 = math.floor(gx), math.floor(gy)
    fx, fy = gx-x0, gy-y0
    w = lambda i: ((i % NG) + NG) % NG
    g = lambda ix, iy: nGrid[w(iy)*(NG+1)+w(ix)]
    return (g(x0,y0)*(1-fx)*(1-fy) + g(x0+1,y0)*fx*(1-fy) +
            g(x0,y0+1)*(1-fx)*fy + g(x0+1,y0+1)*fx*fy)

# rolling desert dunes
for y in range(MAP):
    for x in range(MAP):
        dune = vnoise(x,y)*0.7 + vnoise(x*1.9+40, y*1.9+40)*0.3
        height[idx(x,y)] = dune*0.9
        if dune > 0.62: terrain[idx(x,y)] = 2

# river + two lakes  (fords left dry)
def riverY(x): return MAP*0.5 + math.sin(x*0.13)*MAP*0.16 + math.cos(x*0.05)*MAP*0.06
fords = [round(MAP*0.32), round(MAP*0.66)]
for x in range(MAP):
    if any(abs(x-fx) <= 2 for fx in fords): continue
    cy2 = riverY(x); half = (1.6 + vnoise(x, cy2)*1.4)*S
    for y in range(math.floor(cy2-half), math.ceil(cy2+half)+1):
        if not inMap(x,y) or homeClear(x,y): continue
        terrain[idx(x,y)] = 3; blocked[idx(x,y)] = 1; height[idx(x,y)] = 0
for lx, ly, lr in [[MAP*0.22, MAP*0.3, 5.5*S], [MAP*0.8, MAP*0.7, 6*S]]:
    for y in range(MAP):
        for x in range(MAP):
            if homeClear(x,y): continue
            d = math.hypot(x-lx, y-ly)/lr
            if d < 1 + (vnoise(x+7, y+7)-0.5)*0.5:
                terrain[idx(x,y)] = 3; blocked[idx(x,y)] = 1; height[idx(x,y)] = 0

# mountains (noise-shaped ranges)
ranges = [[MAP*0.28, MAP*0.2, MAP*0.5, MAP*0.12],
          [MAP*0.75, MAP*0.85, MAP*0.55, MAP*0.9]]
for ax, ay, bx, by in ranges:
    steps = 40
    for s in range(steps+1):
        t = s/steps; mx = ax+(bx-ax)*t; my = ay+(by-ay)*t
        wd = (2.6 + vnoise(mx*2, my*2)*2.2)*S
        for y in range(math.floor(my-wd), math.ceil(my+wd)+1):
            for x in range(math.floor(mx-wd), math.ceil(mx+wd)+1):
                if not inMap(x,y) or homeClear(x,y) or terrain[idx(x,y)]==3: continue
                d = math.hypot(x-mx, y-my)/wd
                if d < 1:
                    terrain[idx(x,y)] = 1; blocked[idx(x,y)] = 1
                    peak = (2.6 + vnoise(x,y)*2.4)*(0.4 + 0.6*math.cos(d*math.pi*0.5))*S
                    if peak > height[idx(x,y)]: height[idx(x,y)] = peak

# walkable hills
hills = [[MAP*0.5, MAP*0.5, 10, 2.2], [MAP*0.32, MAP*0.6, 7, 1.6],
         [MAP*0.68, MAP*0.4, 7, 1.7], [MAP*0.6, MAP*0.72, 6, 1.4]]
for hx, hy, hr, hh in hills:
    for y in range(MAP):
        for x in range(MAP):
            if terrain[idx(x,y)]==3 or blocked[idx(x,y)] or homeClear(x,y): continue
            wob = 1 + (vnoise(x*1.5, y*1.5)-0.5)*0.5
            d = math.hypot(x-hx, y-hy)/(hr*S*wob)
            if d < 1:
                rise = hh*(0.5 + 0.5*math.cos(d*math.pi))
                if rise > height[idx(x,y)]: height[idx(x,y)] = rise

# crystal fields
def F(fx, fy, r, kind): return [round(MAP*fx), round(MAP*fy), round(r*S), kind]
def clearCentre(cx, cy):
    if inMap(cx,cy) and not blocked[idx(cx,cy)]: return (cx,cy)
    for rad in range(1,10):
        for dy in range(-rad,rad+1):
            for dx in range(-rad,rad+1):
                nx, ny = cx+dx, cy+dy
                if inMap(nx,ny) and not blocked[idx(nx,ny)]: return (nx,ny)
    return (cx,cy)
fields = [F(.18,.74,6,1), F(.82,.14,6,1), F(.28,.84,6,1), F(.72,.16,6,1),
          F(.12,.38,6,1), F(.88,.62,6,9), F(.4,.86,6,1), F(.6,.14,6,9),
          F(.5,.76,6,1), F(.5,.24,6,1), F(.18,.55,5,9), F(.82,.45,5,1),
          F(.34,.62,5,1), F(.66,.38,5,1)]
for fx0, fy0, r, kind in fields:
    cx, cy = clearCentre(fx0, fy0)
    for y in range(cy-r, cy+r+1):
        for x in range(cx-r, cx+r+1):
            if not inMap(x,y) or blocked[idx(x,y)] or terrain[idx(x,y)]==3: continue
            d = math.hypot(x-cx, y-cy)
            if d <= r and rnd() < 1.15 - d/(r+2):
                crystal[idx(x,y)] = 9 if kind==9 else 1 + int(rnd()*4)
for y in range(MAP):
    for x in range(MAP):
        if homeClear(x,y): crystal[idx(x,y)] = 0

# ---- MEADOWS: lush green banks near water (your request) -------------------
# distance-to-water via simple multi-pass relaxation on the 64 grid
INF = 999
dw = [0 if terrain[i]==3 else INF for i in range(MAP*MAP)]
for _ in range(int(5*S)+2):               # passes ~ meadow belt radius (scales with map)
    for y in range(MAP):
        for x in range(MAP):
            best = dw[idx(x,y)]
            for ddx, ddy in ((1,0),(-1,0),(0,1),(0,-1)):
                nx, ny = x+ddx, y+ddy
                if inMap(nx,ny) and dw[idx(nx,ny)]+1 < best: best = dw[idx(nx,ny)]+1
            dw[idx(x,y)] = best
meadow = [0]*(MAP*MAP)
for y in range(MAP):
    for x in range(MAP):
        i = idx(x,y)
        if terrain[i] in (0,2) and not blocked[i] and not crystal[i]:
            moist = 0
            if dw[i] <= 3*S: moist = 1
            # a broad verdant belt on the wetter noise pockets, too
            if dw[i] <= 5*S and vnoise(x*1.3+11, y*1.3+11) > 0.62: moist = 1
            if moist and height[i] < 1.3:
                meadow[i] = 1
                terrain[i] = 4

# ----------------------------------------------------------------------------
# 2. clear scene
# ----------------------------------------------------------------------------
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()
for coll in (bpy.data.meshes, bpy.data.materials, bpy.data.images):
    for b in list(coll):
        try: coll.remove(b)
        except Exception: pass

# a mesh-only height sampler used for the fine render mesh: carve riverbeds
rheight = list(height)
for i in range(MAP*MAP):
    if terrain[i] == 3: rheight[i] = -0.55        # depth for the water channel
def sampleH(gx, gy):                              # bilinear, matches heightAt()
    x = min(max(gx-0.5, 0.0), MAP-1.001)
    y = min(max(gy-0.5, 0.0), MAP-1.001)
    x0, y0 = int(x), int(y); fx, fy = x-x0, y-y0
    h00 = rheight[idx(x0,y0)]; h10 = rheight[idx(x0+1,y0)]
    h01 = rheight[idx(x0,y0+1)]; h11 = rheight[idx(x0+1,y0+1)]
    return (h00*(1-fx)*(1-fy) + h10*fx*(1-fy) + h01*(1-fx)*fy + h11*fx*fy)*ZS
def cellAt(gx, gy):
    return idx(min(int(gx),MAP-1), min(int(gy),MAP-1))

# ----------------------------------------------------------------------------
# 3. terrain mesh (fine grid) with a vertex-colour biome blend
# ----------------------------------------------------------------------------
DIV = int(os.environ.get("DIV", "3"))     # sub-cells per game cell (2 = lighter glb)
NX = MAP*DIV                              # verts per side
def col_for(i, h):
    t = terrain[i]
    hn = h/ZS
    if t == 1:                            # rock / mountain — warm desert stone
        lo = (0.30, 0.20, 0.15)           # ruddy base
        hi = (0.40, 0.29, 0.23)           # weathered upper rock
        k = min(1.0, hn/4.5)
        base = tuple(lo[j]*(1-k) + hi[j]*k for j in range(3))
        if hn > 4.4*S:                    # only the very tips catch snow
            s = min(1.0, (hn-4.4*S)/(1.2*S))*0.5
            base = tuple(base[j]*(1-s) + (0.84,0.84,0.9)[j]*s for j in range(3))
        return base
    if t == 4:                            # meadow / grassland
        return (0.18, 0.40, 0.14)
    if t == 2:                            # dark sand crest
        return (0.50, 0.38, 0.22)
    return (0.76, 0.62, 0.37)            # sand

mesh = bpy.data.meshes.new("Terrain")
bm = bmesh.new()
verts = {}
for j in range(NX+1):
    for i in range(NX+1):
        gx = i/DIV; gy = j/DIV
        z = sampleH(gx, gy)
        ci = cellAt(gx, gy)
        if terrain[ci] == 1:              # craggy displacement on rock
            z += ((vnoise(gx*4.1+3, gy*4.1+7)-0.5)*1.15 +
                  (vnoise(gx*8.7+1, gy*8.7+5)-0.5)*0.45)*ZS
        v = bm.verts.new((gx, gy, z))
        verts[(i,j)] = v
bm.verts.ensure_lookup_table()
col_layer = bm.loops.layers.color.new("Col")
for j in range(NX):
    for i in range(NX):
        f = bm.faces.new((verts[(i,j)], verts[(i+1,j)], verts[(i+1,j+1)], verts[(i,j+1)]))
        for loop in f.loops:
            co = loop.vert.co
            c = col_for(cellAt(co.x, co.y), co.z)
            # subtle per-vertex tonal noise so materials don't look flat
            n = (vnoise(co.x*2.0, co.y*2.0)-0.5)*0.12
            loop[col_layer] = (max(0,c[0]+n), max(0,c[1]+n), max(0,c[2]+n), 1.0)
bm.normal_update()
bm.to_mesh(mesh)
bm.free()
terr_obj = bpy.data.objects.new("Terrain", mesh)
bpy.context.collection.objects.link(terr_obj)
# smooth shading
for p in mesh.polygons: p.use_smooth = True

# terrain material — Principled driven by vertex colour, roughness from colour
mat = bpy.data.materials.new("TerrainMat"); mat.use_nodes = True
nt = mat.node_tree; nt.nodes.clear()
out = nt.nodes.new("ShaderNodeOutputMaterial")
bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
vc = nt.nodes.new("ShaderNodeVertexColor"); vc.layer_name = "Col"
# roughen with a noise + bump for grit
noiset = nt.nodes.new("ShaderNodeTexNoise"); noiset.inputs["Scale"].default_value = 45.0
bump = nt.nodes.new("ShaderNodeBump"); bump.inputs["Strength"].default_value = 0.18
nt.links.new(vc.outputs["Color"], bsdf.inputs["Base Color"])
nt.links.new(noiset.outputs["Fac"], bump.inputs["Height"])
nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
bsdf.inputs["Roughness"].default_value = 0.92
nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
mesh.materials.append(mat)

# ----------------------------------------------------------------------------
# 4. water surface (only over water cells) — glassy dusk pool
# ----------------------------------------------------------------------------
WZ = 0.10*ZS
wm = bpy.data.meshes.new("Water"); wbm = bmesh.new()
wv = {}
def wvert(i, j):
    key=(i,j)
    if key not in wv: wv[key] = wbm.verts.new((i, j, WZ))
    return wv[key]
for y in range(MAP):
    for x in range(MAP):
        if terrain[idx(x,y)]==3:
            wbm.faces.new((wvert(x,y), wvert(x+1,y), wvert(x+1,y+1), wvert(x,y+1)))
wbm.normal_update(); wbm.to_mesh(wm); wbm.free()
water_obj = bpy.data.objects.new("Water", wm)
bpy.context.collection.objects.link(water_obj)
wmat = bpy.data.materials.new("WaterMat"); wmat.use_nodes = True
wnt = wmat.node_tree; b = wnt.nodes["Principled BSDF"]
b.inputs["Base Color"].default_value = (0.02, 0.09, 0.13, 1)
b.inputs["Roughness"].default_value = 0.06
b.inputs["Transmission Weight"].default_value = 0.85 if "Transmission Weight" in b.inputs else 0
b.inputs["IOR"].default_value = 1.33
if "Metallic" in b.inputs: b.inputs["Metallic"].default_value = 0.0
# gentle ripples
wnoise = wnt.nodes.new("ShaderNodeTexNoise"); wnoise.inputs["Scale"].default_value = 12.0
wbump = wnt.nodes.new("ShaderNodeBump"); wbump.inputs["Strength"].default_value = 0.08
wnt.links.new(wnoise.outputs["Fac"], wbump.inputs["Height"])
wnt.links.new(wbump.outputs["Normal"], b.inputs["Normal"])
wm.materials.append(wmat)
for p in wm.polygons: p.use_smooth = True

# ----------------------------------------------------------------------------
# 5. tiberium crystals — emissive shards (green + blue)
# ----------------------------------------------------------------------------
def shard_bm(target, cx, cy, h0, r, tall, jitter):
    # a small faceted spike
    base_z = h0
    tip = target.verts.new((cx, cy, base_z + tall))
    ring = []
    for k in range(5):
        a = k/5*2*math.pi
        ring.append(target.verts.new((cx+math.cos(a)*r, cy+math.sin(a)*r, base_z)))
    for k in range(5):
        target.faces.new((ring[k], ring[(k+1)%5], tip))

def build_crystals(kind_match, name, color, strength):
    cbm = bmesh.new(); cnt = 0
    for y in range(MAP):
        for x in range(MAP):
            k = crystal[idx(x,y)]
            if k == 0: continue
            isblue = (k == 9)
            if kind_match != isblue: continue
            dens = 3 if isblue else k
            gh = sampleH(x+0.5, y+0.5)
            for s in range(min(dens,3)):
                jx = (rnd()-0.5)*0.7; jy = (rnd()-0.5)*0.7
                r = 0.10 + rnd()*0.10
                tall = (0.5 + rnd()*0.7)*(1.2 if isblue else 1.0)
                shard_bm(cbm, x+0.5+jx, y+0.5+jy, gh, r, tall, 0)
                cnt += 1
    if cnt == 0: cbm.free(); return
    cm = bpy.data.meshes.new(name); cbm.to_mesh(cm); cbm.free()
    ob = bpy.data.objects.new(name, cm); bpy.context.collection.objects.link(ob)
    m = bpy.data.materials.new(name+"Mat"); m.use_nodes = True
    mnt = m.node_tree; bb = mnt.nodes["Principled BSDF"]
    em = "Emission Color" if "Emission Color" in bb.inputs else "Emission"
    bb.inputs["Base Color"].default_value = color
    bb.inputs[em].default_value = color
    bb.inputs["Emission Strength"].default_value = strength
    bb.inputs["Roughness"].default_value = 0.25
    cm.materials.append(m)
    for p in cm.polygons: p.use_smooth = False
    return ob

build_crystals(False, "TibGreen", (0.13, 1.0, 0.22, 1), 3.4)
build_crystals(True,  "TibBlue",  (0.18, 0.52, 1.0, 1), 4.8)

# ----------------------------------------------------------------------------
# 6. bridges across the river (spanning real water)
# ----------------------------------------------------------------------------
def water_span(bx):
    ys = [y for y in range(MAP) if terrain[idx(bx,y)]==3]
    return (min(ys), max(ys)) if ys else None
def make_bridge(bx):
    span = water_span(bx)
    if not span: return
    y0, y1 = span[0]-1.5, span[1]+1.5
    deck_z = WZ + 0.28
    hw = 1.5                              # half width in x
    bb = bmesh.new()
    def q(x0,x1,ya,yb,z):
        vs=[bb.verts.new((x0,ya,z)),bb.verts.new((x1,ya,z)),
            bb.verts.new((x1,yb,z)),bb.verts.new((x0,yb,z))]
        bb.faces.new(vs)
    # deck
    v=[bb.verts.new((bx-hw,y0,deck_z)),bb.verts.new((bx+hw,y0,deck_z)),
       bb.verts.new((bx+hw,y1,deck_z)),bb.verts.new((bx-hw,y1,deck_z))]
    bb.faces.new(v)
    # side rails
    for sx in (bx-hw, bx+hw):
        r=[bb.verts.new((sx,y0,deck_z)),bb.verts.new((sx,y1,deck_z)),
           bb.verts.new((sx,y1,deck_z+0.35)),bb.verts.new((sx,y0,deck_z+0.35))]
        bb.faces.new(r)
    # support pillars down to bed
    for yy in (y0+1, (y0+y1)/2, y1-1):
        for sx in (bx-hw+0.2, bx+hw-0.2):
            cz=-0.5
            p=[bb.verts.new((sx-0.15,yy-0.15,cz)),bb.verts.new((sx+0.15,yy-0.15,cz)),
               bb.verts.new((sx+0.15,yy-0.15,deck_z)),bb.verts.new((sx-0.15,yy-0.15,deck_z))]
            bb.faces.new(p)
    m = bpy.data.meshes.new("Bridge%d"%bx); bb.to_mesh(m); bb.free()
    ob = bpy.data.objects.new("Bridge%d"%bx, m); bpy.context.collection.objects.link(ob)
    mat = bpy.data.materials.new("BridgeMat%d"%bx); mat.use_nodes=True
    bs = mat.node_tree.nodes["Principled BSDF"]
    bs.inputs["Base Color"].default_value=(0.30,0.28,0.26,1)
    bs.inputs["Roughness"].default_value=0.8
    m.materials.append(mat)
# choose two x columns that actually cross water and aren't fords
bridge_xs=[x for x in range(8,MAP-8) if water_span(x) and all(abs(x-fx)>4 for fx in fords)]
if bridge_xs:
    make_bridge(bridge_xs[len(bridge_xs)//4])
    make_bridge(bridge_xs[3*len(bridge_xs)//4])

# ----------------------------------------------------------------------------
# 7. lighting + dusk world
# ----------------------------------------------------------------------------
world = bpy.data.worlds.new("Dusk"); bpy.context.scene.world = world
world.use_nodes = True
wn = world.node_tree; wn.nodes.clear()
wout = wn.nodes.new("ShaderNodeOutputWorld")
sky = wn.nodes.new("ShaderNodeTexSky")
try:
    sky.sky_type = 'NISHITA'
    sky.sun_elevation = math.radians(3.2)     # sun near the horizon — dusk
    sky.sun_rotation = math.radians(-58.0)
    sky.altitude = 150.0
    sky.air_density = 1.8
    sky.dust_density = 5.5                     # thick warm dusk haze
    sky.ozone_density = 3.0
except Exception:
    pass
bg = wn.nodes.new("ShaderNodeBackground")
bg.inputs["Strength"].default_value = 0.18    # very dim sky so dusk mood holds
wn.links.new(sky.outputs["Color"], bg.inputs["Color"])
wn.links.new(bg.outputs["Background"], wout.inputs["Surface"])

sun_data = bpy.data.lights.new("Sun", 'SUN')
sun_data.energy = 6.0
sun_data.angle = math.radians(3.0)            # soft-edged low sun
sun_data.color = (1.0, 0.44, 0.19)            # deep amber-gold dusk
sun = bpy.data.objects.new("Sun", sun_data)
bpy.context.collection.objects.link(sun)
# low raking sun from the left for long dramatic shadows
sun.rotation_euler = (math.radians(80), math.radians(3), math.radians(115))

# cool twilight fill from the opposite side (sky bounce)
fill_data = bpy.data.lights.new("Fill", 'SUN'); fill_data.energy = 0.30
fill_data.color = (0.26, 0.38, 0.72)
fill = bpy.data.objects.new("Fill", fill_data)
bpy.context.collection.objects.link(fill)
fill.rotation_euler = (math.radians(48), math.radians(-12), math.radians(-52))

# ----------------------------------------------------------------------------
# 8. camera — hero 3/4 iso, low "in the dunes", or top-down tactical
# ----------------------------------------------------------------------------
cam_data = bpy.data.cameras.new("Cam")
cam_data.clip_end = MAP * 8                     # keep the whole board in the frustum
cam = bpy.data.objects.new("Cam", cam_data)
bpy.context.collection.objects.link(cam)
C = MAP/2
M = MAP                                         # camera offsets scale with map size
def look_at(obj, target):
    d = Vector(target) - obj.location
    obj.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
if CAM == "topdown":
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = MAP * 1.03          # frame the whole board
    cam.location = (C, C, MAP * 2.2)
    cam.rotation_euler = (0.0, 0.0, 0.0)       # straight down, north (+Y) up
elif CAM == "dunes":
    cam_data.lens = 40                         # dramatic low 3/4
    cam.location = (C - 0.53*M, C - 0.75*M, 0.30*M)   # low over the sand
    look_at(cam, (C + 0.12*M, C + 0.12*M, 2.5))       # across the glowing valley
else:  # hero
    cam_data.lens = 52
    cam.location = (C - 0.72*M, C - 0.94*M, 0.90*M)
    look_at(cam, (C + 0.03*M, C + 0.06*M, 3))
bpy.context.scene.camera = cam

# ----------------------------------------------------------------------------
# 9. render settings
# ----------------------------------------------------------------------------
sc = bpy.context.scene
sc.render.engine = 'CYCLES'
sc.cycles.device = 'CPU'
sc.cycles.samples = SAMPLES
sc.cycles.use_adaptive_sampling = True
sc.cycles.adaptive_threshold = 0.01
try:
    sc.cycles.use_denoising = True
    sc.cycles.denoiser = 'OPENIMAGEDENOISE'
except Exception: pass
if CAM == "topdown":                    # tactical map wants a square frame
    RES_X = RES_Y = RES_Y
sc.render.resolution_x = RES_X
sc.render.resolution_y = RES_Y
sc.render.resolution_percentage = 100
sc.render.image_settings.file_format = 'PNG'
sc.render.filepath = OUT
looks = [l.name for l in bpy.types.ColorManagedViewSettings.bl_rna.properties['look'].enum_items]
vts   = [v.name for v in bpy.types.ColorManagedViewSettings.bl_rna.properties['view_transform'].enum_items]
sc.view_settings.view_transform = 'AgX' if 'AgX' in vts else 'Filmic'
for cand in ('AgX - Punchy', 'AgX - High Contrast', 'AgX - Medium High Contrast',
             'High Contrast', 'Medium High Contrast'):
    if cand in looks: sc.view_settings.look = cand; break
sc.view_settings.exposure = 0.5
sc.view_settings.gamma = 1.02

# (bloom + grade + vignette are applied as a numpy/PIL post-process — see post.py)

# ----------------------------------------------------------------------------
# 10. optional glTF export (game-ready mesh for Three.js)
# ----------------------------------------------------------------------------
if EXPORT_GLTF:
    for o in bpy.data.objects:
        o.select_set(o.type == 'MESH')
    kw = dict(filepath=GLB_OUT, export_format='GLB', use_selection=True,
              export_apply=True, export_yup=False)   # keep Z-up (matches the game)
    if os.environ.get("EXPORT_DRACO", "0") == "1":
        kw['export_draco_mesh_compression_enable'] = True
        kw['export_draco_mesh_compression_level'] = 6
    try:
        bpy.ops.export_scene.gltf(**kw)
    except TypeError:
        for k in ('export_yup', 'export_draco_mesh_compression_enable',
                  'export_draco_mesh_compression_level'):
            kw.pop(k, None)
        bpy.ops.export_scene.gltf(**kw)
    print("GLTF ->", GLB_OUT)

if RENDER:
    print("Rendering %dx%d  samples=%d  cam=%s  ->  %s" % (RES_X, RES_Y, SAMPLES, CAM, OUT))
    bpy.ops.render.render(write_still=True)
    print("DONE", OUT)
