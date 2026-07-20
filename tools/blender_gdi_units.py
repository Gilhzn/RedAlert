import bpy, math, os, json, sys
from mathutils import Vector
SP = "/tmp/claude-0/-home-user-RedAlert/84746fb6-337c-560d-9167-231af8584ff6/scratchpad"
UNITY = "/home/user/RedAlert/unity/Assets/Resources/Models"
sys.path.insert(0, SP)
src = open(f"{SP}/cycles_pipeline.py").read()
head = src[:src.index("# ---------------- 1) KayKit buildings")]
ns = {}
exec(compile(head, "rig", "exec"), ns)
render_current = ns["render_current"]; place = ns["place"]; clear_models = ns["clear_models"]; mat = ns["mat"]
vsrc = open(f"{SP}/blender_vehicles.py").read()
vns = {"bpy": bpy, "math": math, "os": os}
exec(compile(vsrc.split("VEHICLES = [")[0], "h", "exec"), vns)
tail = vsrc[vsrc.index("def soldier("):vsrc.index("for uid, kind, fac in DM:")]
exec(compile(tail, "s", "exec"), vns)
vns["clear"] = clear_models
for k in ["box","rod","ball","join","tracks","DM_ARMOR","DM_DARK","GUNMETAL","TRACK","TIRE","BOOTS","BLUEGREY","WHITE","RED","SKIN","YELLOW"]:
    ns[k] = vns[k]
box, rod, ball, join, tracks = vns["box"], vns["rod"], vns["ball"], vns["join"], vns["tracks"]
DM_ARMOR, DM_DARK, GUNMETAL, TIRE, BOOTS, BLUEGREY = vns["DM_ARMOR"], vns["DM_DARK"], vns["GUNMETAL"], vns["TIRE"], vns["BOOTS"], vns["BLUEGREY"]

def apc():
    clear_models()
    h = []
    for sy in (-0.35, 0, 0.35):
        for sx in (-0.28, 0.28):
            h.append(rod(sx, sy, 0.13, 0.12, 0.12, TIRE, rx=0, ry=90, v=12))
    h.append(box(0, 0, 0.30, 0.5, 1.0, 0.22, DM_ARMOR))
    h.append(box(0, 0.30, 0.44, 0.42, 0.4, 0.14, DM_DARK, rx=18))    # sloped bow
    h.append(box(0, -0.2, 0.47, 0.4, 0.5, 0.12, DM_ARMOR))           # troop bay
    h.append(box(0, -0.44, 0.36, 0.44, 0.1, 0.2, GUNMETAL, rx=-20))  # rear ramp
    return join(h, "hull")

def disruptor():
    clear_models()
    h = []
    tracks(h, 0.36, 1.05, vns["TRACK"])
    h.append(box(0, 0, 0.40, 0.6, 1.0, 0.24, DM_ARMOR))
    h.append(box(0, -0.25, 0.58, 0.5, 0.4, 0.16, DM_DARK))
    # big sonic emitter dish forward
    h.append(rod(0, 0.42, 0.58, 0.19, 0.18, BLUEGREY, rx=90, v=14))
    h.append(rod(0, 0.53, 0.58, 0.23, 0.06, GUNMETAL, rx=90, v=14))
    h.append(box(0, 0.15, 0.56, 0.2, 0.4, 0.12, GUNMETAL))
    return join(h, "hull")

def sensor():
    clear_models()
    h = []
    for sy in (-0.3, 0.3):
        for sx in (-0.26, 0.26):
            h.append(rod(sx, sy, 0.12, 0.11, 0.12, TIRE, rx=0, ry=90, v=12))
    h.append(box(0, 0, 0.28, 0.46, 0.85, 0.2, DM_ARMOR))
    h.append(box(0, 0.25, 0.42, 0.36, 0.3, 0.12, DM_DARK))
    h.append(rod(0, -0.15, 0.38, 0.05, 0.5, GUNMETAL))
    h.append(rod(0, -0.15, 0.74, 0.22, 0.05, WHITE := vns["WHITE"], rx=28, v=14))  # dish
    return join(h, "hull")

VEH = [("dm_apc", apc, 0.9), ("dm_disruptor", disruptor, 1.0), ("dm_sensor", sensor, 0.85)]
meta = json.load(open(f"{SP}/cycles_out/meta.json"))
for uid, fn, size in VEH:
    fn(); place([bpy.context.active_object], 0, fit=size)
    # Unity GLB (whole model, no turret)
    bpy.ops.export_scene.gltf(filepath=f"{UNITY}/{uid}.glb", export_format="GLB", export_yup=True)
    for k in range(8):
        fn()
        place([bpy.context.active_object], k * 45, fit=size)
        m = render_current(f"veh_{uid}_hull_{k}", None, 3.4, 1/3)
        meta.setdefault("veh", {})[f"{uid}_hull_{k}"] = m
        json.dump(meta, open(f"{SP}/cycles_out/meta.json", "w"))
    print("veh", uid, flush=True)

# --- soldier variants: jumpjet trooper + railgun hero ---
def soldier_variant(kind, pose):
    body = vns["soldier"]("rifle", "dm", pose)
    extras = []
    if kind == "jump":
        extras.append(rod(-0.10, -0.20, 0.62, 0.055, 0.34, GUNMETAL))
        extras.append(rod(0.10, -0.20, 0.62, 0.055, 0.34, GUNMETAL))
        extras.append(rod(-0.10, -0.20, 0.42, 0.035, 0.08, vns["RED"]))
        extras.append(rod(0.10, -0.20, 0.42, 0.035, 0.08, vns["RED"]))
    else:  # hero: long railgun + heavy pauldron
        extras.append(rod(0.05, 0.30, 0.70, 0.05, 0.55, BLUEGREY, rx=90))
        extras.append(box(0.05, 0.50, 0.70, 0.10, 0.12, 0.10, vns["RED"]))
        extras.append(box(-0.28, 0, 0.80, 0.18, 0.24, 0.16, DM_DARK))
    for e in extras:
        e.select_set(True)
    body.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.join()
    return bpy.context.active_object

for uid, kind in [("dm_jumptrooper", "jump"), ("dm_railhero", "hero")]:
    for pose in (0, 1, 2):
        for k in range(8):
            clear_models()
            body = soldier_variant(kind, 0 if pose == 2 else pose)
            bpy.context.view_layer.update()
            pts = [body.matrix_world @ Vector(c) for c in body.bound_box]
            fh = (0.52 if pose == 2 else 0.57) / max(0.001, max(q.z for q in pts) - min(q.z for q in pts))
            body.scale = tuple(v * fh for v in body.scale)
            if pose == 2:
                body.rotation_euler = (math.radians(-82), 0, math.radians(18))
            place([body], k * 45)
            m = render_current(f"sold_{uid}_p{pose}_{k}", None, 2.4, 1/3)
            meta.setdefault("soldiers", {})[f"{uid}_p{pose}_{k}"] = m
            json.dump(meta, open(f"{SP}/cycles_out/meta.json", "w"))
    clear_models()
    body = soldier_variant(kind, 0)
    bpy.ops.export_scene.gltf(filepath=f"{UNITY}/{uid}.glb", export_format="GLB", export_yup=True)
    print("soldier", uid, flush=True)
print("GDI DONE", flush=True)
