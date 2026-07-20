"""Aircraft + new structures + grenadier: Blender models, Cycles sprite
renders (demo) and selection-only GLB exports (Unity ModelLibrary ids)."""
import bpy, math, os, json, sys
from mathutils import Vector
SP = "/tmp/claude-0/-home-user-RedAlert/84746fb6-337c-560d-9167-231af8584ff6/scratchpad"
UNITY = "/home/user/RedAlert/unity/Assets/Resources/Models"
sys.path.insert(0, SP)
src = open(f"{SP}/cycles_pipeline.py").read()
head = src[:src.index("# ---------------- 1) KayKit buildings")]
ns = {}
exec(compile(head, "rig", "exec"), ns)
render_current = ns["render_current"]; place = ns["place"]; clear_models = ns["clear_models"]
emis_mat = ns["mat"]
vsrc = open(f"{SP}/blender_vehicles.py").read()
vns = {"bpy": bpy, "math": math, "os": os}
exec(compile(vsrc.split("VEHICLES = [")[0], "h", "exec"), vns)
tail = vsrc[vsrc.index("def soldier("):vsrc.index("for uid, kind, fac in DM:")]
exec(compile(tail, "s", "exec"), vns)
vns["clear"] = clear_models
box, rod, ball, join, tracks = vns["box"], vns["rod"], vns["ball"], vns["join"], vns["tracks"]
DM_ARMOR, DM_DARK, SO_ARMOR, SO_DARK = vns["DM_ARMOR"], vns["DM_DARK"], vns["SO_ARMOR"], vns["SO_DARK"]
GUNMETAL, TIRE, BOOTS, BLUEGREY = vns["GUNMETAL"], vns["TIRE"], vns["BOOTS"], vns["BLUEGREY"]
WHITE, RED, YELLOW, TRACK = vns["WHITE"], vns["RED"], vns["YELLOW"], vns["TRACK"]
CYAN = (0.35, 0.95, 1.0, 1)
CONCRETE = (0.52, 0.50, 0.45, 1)

def emis(o, color):
    o.data.materials.clear()
    o.data.materials.append(emis_mat(color, emissive=True))
    return o

def export_glb(objs, path):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.gltf(filepath=path, export_format="GLB", export_yup=True,
                              use_selection=True)
    bpy.ops.object.select_all(action="DESELECT")

# ---------------- aircraft ----------------
def orca():
    clear_models()
    h = []
    h.append(box(0, 0.1, 0.62, 0.34, 1.05, 0.30, DM_ARMOR))          # fuselage
    h.append(box(0, 0.52, 0.66, 0.26, 0.34, 0.20, BLUEGREY, rx=14))  # canopy
    h.append(box(0, -0.52, 0.72, 0.10, 0.34, 0.26, DM_DARK, rx=-16)) # tail boom
    h.append(box(0, -0.66, 0.90, 0.04, 0.22, 0.24, DM_ARMOR))        # fin
    for s in (-1, 1):
        h.append(box(s*0.42, 0.05, 0.64, 0.52, 0.24, 0.07, DM_DARK)) # stub wing
        h.append(rod(s*0.66, 0.05, 0.64, 0.20, 0.16, GUNMETAL, rx=0, v=14))  # ducted fan
        h.append(rod(s*0.66, 0.05, 0.73, 0.23, 0.045, DM_ARMOR, rx=0, v=14)) # fan rim
        h.append(box(s*0.38, 0.30, 0.52, 0.10, 0.34, 0.10, GUNMETAL))        # rocket pod
        h.append(rod(s*0.30, 0.02, 0.30, 0.03, 0.55, GUNMETAL, rx=90))       # skid
        h.append(rod(s*0.30, 0.02, 0.40, 0.025, 0.22, GUNMETAL, rx=0))
    return join(h, "hull")

def orca_bomber():
    clear_models()
    h = []
    h.append(box(0, 0, 0.70, 0.56, 1.5, 0.38, DM_ARMOR))             # heavy fuselage
    h.append(box(0, 0.72, 0.72, 0.36, 0.36, 0.24, BLUEGREY, rx=16))  # cockpit
    h.append(box(0, -0.05, 0.48, 0.42, 1.0, 0.12, DM_DARK))          # bomb bay
    h.append(box(0, -0.80, 0.88, 0.05, 0.26, 0.30, DM_ARMOR))        # fin
    h.append(box(0, -0.78, 1.00, 0.5, 0.20, 0.05, DM_DARK))          # tailplane
    for s in (-1, 1):
        h.append(box(s*0.58, 0.1, 0.72, 0.62, 0.30, 0.08, DM_DARK))
        h.append(rod(s*0.86, 0.1, 0.72, 0.25, 0.20, GUNMETAL, rx=0, v=14))
        h.append(rod(s*0.86, 0.1, 0.83, 0.28, 0.05, DM_ARMOR, rx=0, v=14))
        h.append(rod(s*0.34, 0.0, 0.32, 0.035, 0.8, GUNMETAL, rx=90))
        h.append(rod(s*0.34, 0.0, 0.44, 0.03, 0.26, GUNMETAL, rx=0))
    return join(h, "hull")

def harpy():
    clear_models()
    h = []
    h.append(box(0, 0.05, 0.52, 0.26, 0.85, 0.26, SO_ARMOR))         # slim body
    h.append(box(0, 0.42, 0.56, 0.20, 0.26, 0.16, SO_DARK, rx=18))   # canopy
    h.append(box(0, -0.55, 0.60, 0.07, 0.40, 0.10, SO_DARK, rx=-8))  # tail
    h.append(box(0, -0.74, 0.70, 0.03, 0.14, 0.16, SO_ARMOR))        # fin
    h.append(rod(0, 0.0, 0.72, 0.035, 0.14, GUNMETAL, rx=0))         # rotor mast
    h.append(box(0, 0.0, 0.80, 0.09, 1.35, 0.02, TRACK))             # rotor blades
    h.append(box(0, 0.0, 0.80, 1.35, 0.09, 0.02, TRACK))
    h.append(rod(0, 0.30, 0.36, 0.05, 0.22, GUNMETAL, rx=0))         # chin gun
    h.append(rod(0, 0.42, 0.30, 0.028, 0.20, TRACK, rx=90))
    for s in (-1, 1):
        h.append(rod(s*0.20, 0.02, 0.26, 0.025, 0.5, GUNMETAL, rx=90))
        h.append(rod(s*0.20, 0.02, 0.34, 0.02, 0.16, GUNMETAL, rx=0))
    return join(h, "hull")

# ---------------- structures ----------------
def helipad():
    clear_models()
    h = []
    h.append(box(0, 0, 0.06, 1.9, 1.9, 0.12, CONCRETE))              # pad slab
    h.append(rod(0, 0, 0.125, 0.62, 0.015, DM_DARK, rx=0, v=24))     # landing circle
    h.append(box(0, 0, 0.135, 0.34, 0.06, 0.012, WHITE))             # 'H'
    h.append(box(-0.14, 0, 0.135, 0.06, 0.30, 0.012, WHITE))
    h.append(box(0.14, 0, 0.135, 0.06, 0.30, 0.012, WHITE))
    for sx, sy in ((-0.85, -0.85), (0.85, -0.85), (-0.85, 0.85), (0.85, 0.85)):
        h.append(emis(box(sx, sy, 0.16, 0.08, 0.08, 0.08, YELLOW), YELLOW))  # corner lights
    h.append(box(0.72, -0.62, 0.34, 0.4, 0.55, 0.55, DM_ARMOR))      # control kiosk
    h.append(box(0.72, -0.62, 0.64, 0.44, 0.6, 0.06, DM_DARK))
    h.append(rod(0.72, -0.78, 0.85, 0.02, 0.4, GUNMETAL, rx=0))      # antenna
    return join(h, "model")

def aa_tower():
    clear_models()
    h = []
    h.append(box(0, 0, 0.15, 0.85, 0.85, 0.3, CONCRETE))             # base
    h.append(rod(0, 0, 0.45, 0.16, 0.35, DM_DARK, rx=0, v=12))       # pedestal
    h.append(box(0, 0, 0.66, 0.34, 0.3, 0.14, DM_ARMOR))             # mount
    for s in (-1, 1):
        h.append(box(s*0.20, -0.05, 0.86, 0.16, 0.5, 0.16, GUNMETAL, rx=-42))  # missile boxes
        for i in range(2):
            h.append(rod(s*0.20, 0.10 - i*0.14, 0.86 + i*0.12, 0.035, 0.5, WHITE, rx=48))
    h.append(rod(0, 0.22, 0.72, 0.015, 0.30, GUNMETAL, rx=0))        # sensor rod
    h.append(emis(ball(0, 0.22, 0.88, 0.035, RED), RED))
    return join(h, "model")

def service_depot():
    clear_models()
    h = []
    h.append(box(0, 0, 0.05, 1.9, 1.9, 0.1, CONCRETE))               # floor
    h.append(box(0, 0, 0.105, 1.3, 0.16, 0.012, YELLOW))             # stripes
    h.append(box(0, 0.5, 0.105, 1.3, 0.10, 0.012, YELLOW))
    h.append(box(0, -0.5, 0.105, 1.3, 0.10, 0.012, YELLOW))
    for s in (-1, 1):
        h.append(box(s*0.8, 0, 0.55, 0.16, 1.5, 1.0, DM_DARK))       # side posts
    h.append(box(0, 0, 1.06, 1.8, 1.4, 0.12, DM_ARMOR))              # gantry beam roof
    h.append(box(0, 0, 0.92, 0.2, 0.9, 0.14, GUNMETAL))              # crane rail
    h.append(rod(0.1, 0.2, 0.72, 0.03, 0.4, GUNMETAL, rx=0))         # crane hook arm
    h.append(box(0.1, 0.2, 0.50, 0.14, 0.14, 0.08, YELLOW))
    return join(h, "model")

def emp_cannon():
    clear_models()
    h = []
    h.append(box(0, 0, 0.2, 1.7, 1.7, 0.4, CONCRETE))                # bunker base
    h.append(box(0, -0.45, 0.5, 1.0, 0.7, 0.3, DM_ARMOR))            # housing
    h.append(rod(0, 0, 0.62, 0.30, 0.35, GUNMETAL, rx=0, v=14))      # turntable
    barrel = []
    for i in range(4):                                                # coil rings up the barrel
        t = 0.15 + i * 0.22
        barrel.append(rod(0, 0.18 + t*0.55, 0.80 + t*0.75, 0.13 - i*0.012, 0.05, BLUEGREY, rx=-54, v=12))
    h.append(rod(0, 0.18 + 0.5*0.55, 0.80 + 0.5*0.75, 0.055, 1.05, GUNMETAL, rx=-54, v=12))  # core barrel
    h.append(emis(ball(0, 0.18 + 1.0*0.55, 0.80 + 1.0*0.75, 0.09, CYAN), CYAN))
    h += barrel
    return join(h, "model")

def ion_uplink():
    clear_models()
    h = []
    h.append(box(0, 0, 0.25, 1.8, 1.8, 0.5, DM_ARMOR))               # main block
    h.append(box(0, 0.55, 0.62, 1.2, 0.6, 0.28, DM_DARK))            # front annex
    for s in (-1, 1):                                                 # struts
        h.append(rod(s*0.5, -0.25, 0.85, 0.045, 0.75, GUNMETAL, rx=s*28, ry=0))
    h.append(rod(0, -0.25, 1.15, 0.62, 0.16, WHITE, rx=0, v=18))     # dish (up)
    h.append(rod(0, -0.25, 1.24, 0.5, 0.06, BLUEGREY, rx=0, v=18))   # dish inner
    h.append(emis(rod(0, -0.25, 1.30, 0.14, 0.10, CYAN, rx=0, v=14), CYAN))
    h.append(rod(0.62, 0.62, 0.95, 0.02, 0.55, GUNMETAL, rx=0))      # antennas
    h.append(rod(0.45, 0.72, 0.88, 0.02, 0.4, GUNMETAL, rx=0))
    h.append(emis(ball(0.62, 0.62, 1.24, 0.04, RED), RED))
    return join(h, "model")

meta = json.load(open(f"{SP}/cycles_out/meta.json"))
def save(): json.dump(meta, open(f"{SP}/cycles_out/meta.json", "w"))

# aircraft: 8 dirs each + GLB
AIR = [("dm_orca", orca, 1.15, 3.4), ("dm_orca_bomber", orca_bomber, 1.45, 3.8),
       ("so_harpy", harpy, 1.0, 3.4)]
for uid, fn, size, view in AIR:
    body = fn(); place([body], 0, fit=size)
    export_glb([body], f"{UNITY}/{uid}.glb")
    for k in range(8):
        body = fn()
        place([body], k * 45, fit=size)
        m = render_current(f"veh_{uid}_hull_{k}", None, view, 1/3)
        meta.setdefault("veh", {})[f"{uid}_hull_{k}"] = m; save()
    print("air", uid, flush=True)

# structures: single dir + GLB
STRUCTS = [("dm_helipad", helipad, 1.9), ("dm_aa_tower", aa_tower, 0.95),
           ("dm_service_depot", service_depot, 1.9), ("nx_emp_cannon", emp_cannon, 1.9),
           ("dm_ion_uplink", ion_uplink, 1.9)]
for sid, fn, size in STRUCTS:
    body = fn(); place([body], 0, fit=size)
    export_glb([body], f"{UNITY}/{sid}.glb")
    body = fn(); place([body], 0, fit=size)
    m = render_current(f"kk_{sid}", None, size * 2.2 + 1.2, 1/3)
    meta.setdefault("kaykit", {})[sid] = m; save()
    print("struct", sid, flush=True)

# grenadier (disc thrower): rifle base + disc launcher tube + back rack
def grenadier(pose):
    body = vns["soldier"]("grenadier", "dm", pose)
    extras = []
    extras.append(rod(0.05, 0.28, 0.66, 0.075, 0.42, GUNMETAL, rx=90, v=12))  # disc tube
    extras.append(rod(0.05, 0.50, 0.66, 0.09, 0.06, DM_DARK, rx=90, v=12))    # muzzle ring
    for i in range(3):                                                         # disc rack on back
        extras.append(rod(0, -0.17 - i*0.045, 0.62, 0.11, 0.03, BLUEGREY, rx=90, v=12))
    for e in extras: e.select_set(True)
    body.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.join()
    return bpy.context.active_object

for pose in (0, 1, 2):
    for k in range(8):
        clear_models()
        body = grenadier(0 if pose == 2 else pose)
        bpy.context.view_layer.update()
        pts = [body.matrix_world @ Vector(c) for c in body.bound_box]
        fh = (0.52 if pose == 2 else 0.57) / max(0.001, max(q.z for q in pts) - min(q.z for q in pts))
        body.scale = tuple(v * fh for v in body.scale)
        if pose == 2:
            body.rotation_euler = (math.radians(-82), 0, math.radians(18))
        place([body], k * 45)
        m = render_current(f"sold_dm_grenadier_p{pose}_{k}", None, 2.4, 1/3)
        meta.setdefault("soldiers", {})[f"dm_grenadier_p{pose}_{k}"] = m; save()
clear_models()
body = grenadier(0)
export_glb([body], f"{UNITY}/dm_grenadier.glb")
print("grenadier done", flush=True)

# re-export earlier GDI GLBs selection-only (previous exports swept in rig objects)
gsrc = open(f"{SP}/blender_gdi_units.py").read()
defs = gsrc[gsrc.index("def apc():"):gsrc.index("VEH = [")]
gns = dict(vns); gns.update({"clear_models": clear_models, "tracks": tracks, "vns": vns})
exec(compile(defs, "g", "exec"), gns)
for uid, fname, size in [("dm_apc", "apc", 0.9), ("dm_disruptor", "disruptor", 1.0), ("dm_sensor", "sensor", 0.85)]:
    body = gns[fname](); place([body], 0, fit=size)
    export_glb([body], f"{UNITY}/{uid}.glb")
sv = gsrc[gsrc.index("def soldier_variant("):gsrc.index("for uid, kind in [")]
exec(compile(sv, "sv", "exec"), gns)
for uid, kind in [("dm_jumptrooper", "jump"), ("dm_railhero", "hero")]:
    clear_models()
    body = gns["soldier_variant"](kind, 0)
    export_glb([body], f"{UNITY}/{uid}.glb")
print("ALL DONE", flush=True)
