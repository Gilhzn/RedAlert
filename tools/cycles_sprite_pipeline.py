#!/usr/bin/env python3
"""Re-render every demo sprite with real Blender Cycles: sun + sky light,
soft contact shadows (shadow catcher), AO, anti-aliasing, denoising.
Outputs PNGs + meta to cycles_out/; resumable (skips existing files)."""
import bpy, math, os, json, sys
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view as w2cv

SP = "/tmp/claude-0/-home-user-RedAlert/84746fb6-337c-560d-9167-231af8584ff6/scratchpad"
OUT = f"{SP}/cycles_out"
os.makedirs(OUT, exist_ok=True)
sys.path.insert(0, SP)
from PIL import Image

PPU = 78.0            # px per world-x unit at supersample 3 (26*3)
ZSQUASH = 21.0 / 31.84  # squash heights so sprites match the demo's 21px/unit

# ---------------- scene rig ----------------
def rig():
    bpy.ops.object.select_all(action="SELECT"); bpy.ops.object.delete()
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 24
    sc.cycles.use_denoising = True
    sc.render.film_transparent = True
    sc.view_settings.view_transform = "Standard"
    # world fill light
    sc.world.use_nodes = True
    bg = sc.world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (0.75, 0.8, 0.78, 1)
    bg.inputs[1].default_value = 0.55
    # sun from screen upper-left
    bpy.ops.object.light_add(type="SUN")
    sun = bpy.context.active_object
    sun.data.energy = 3.2
    sun.data.angle = math.radians(12)     # soft shadows
    sun.rotation_euler = (math.radians(48), 0, math.radians(200))
    # shadow catcher ground
    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, 0))
    ground = bpy.context.active_object
    ground.is_shadow_catcher = True
    # camera
    bpy.ops.object.camera_add()
    cam = bpy.context.active_object
    cam.data.type = "ORTHO"
    cam.rotation_euler = (math.radians(60), 0, math.radians(135))
    cam.location = cam.rotation_euler.to_matrix() @ Vector((0, 0, 40))
    sc.camera = cam
    return sc, cam, ground

SC, CAM, GROUND = rig()
KEEP = {GROUND.name, CAM.name}
for o in bpy.data.objects:
    KEEP.add(o.name) if o.type == "LIGHT" else None

def clear_models():
    for o in list(bpy.data.objects):
        if o.name not in KEEP:
            bpy.data.objects.remove(o, do_unlink=True)

_mats = {}
def mat(color, emissive=False):
    key = (tuple(round(c, 3) for c in color), emissive)
    if key in _mats: return _mats[key]
    m = bpy.data.materials.new("m"); m.use_nodes = True
    nt = m.node_tree
    if emissive:
        for n in list(nt.nodes):
            if n.type == "BSDF_PRINCIPLED": nt.nodes.remove(n)
        em = nt.nodes.new("ShaderNodeEmission")
        em.inputs[0].default_value = color
        em.inputs[1].default_value = 4.0
        nt.links.new(em.outputs[0], nt.nodes["Material Output"].inputs[0])
    else:
        b = nt.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = color
        b.inputs["Roughness"].default_value = 0.75
    _mats[key] = m
    return m

# ---------------- render one framing ----------------
def render_current(tag, extra_yaw_objs, view_units, ss_meta):
    """Renders current model objects; returns packed sprite meta."""
    path = f"{OUT}/{tag}.png"
    ortho = max(2.2, view_units)
    res = int(ortho * (PPU / 0.7071) * 1.02) + 8   # guarantee >= PPU after rescale
    res = min(res, 1400)
    SC.render.resolution_x = SC.render.resolution_y = res
    CAM.data.ortho_scale = ortho
    bpy.context.view_layer.update()
    if not os.path.exists(path):
        SC.render.filepath = path
        bpy.ops.render.render(write_still=True)
    # measure projection for exact rescale + anchor
    o = w2cv(SC, CAM, Vector((0, 0, 0)))
    px_ = w2cv(SC, CAM, Vector((1, 0, 0)))
    ux = abs(px_.x - o.x) * res            # px per world-x-unit horizontally
    img = Image.open(path)
    img = img.transpose(Image.FLIP_LEFT_RIGHT)   # rz=135 mirror fix
    f = PPU * 0.7071 / ux                  # target: unit x spans 26*3*... (u component)
    img = img.resize((max(1, int(img.width * f)), max(1, int(img.height * f))), Image.LANCZOS)
    ax = (1 - o.x) * res * f               # origin px after flip+rescale
    ay = (1 - o.y) * res * f
    bbox = img.getbbox()
    if bbox is None: bbox = (0, 0, img.width, img.height)
    pad = 2
    bbox = (max(0, bbox[0]-pad), max(0, bbox[1]-pad), min(img.width, bbox[2]+pad), min(img.height, bbox[3]+pad))
    img = img.crop(bbox)
    img.save(f"{OUT}/{tag}_final.png")
    return {"file": f"{tag}_final.png", "ax": ax - bbox[0], "ay": ay - bbox[1],
            "w": img.width, "h": img.height, "s": ss_meta}

def place(objs, yaw_deg, fit=None, ground_z=True):
    """Parent objs to an empty, squash Z, rotate, sit on ground; returns view size."""
    bpy.ops.object.empty_add()
    root = bpy.context.active_object
    for ob in objs:
        if ob.parent is None: ob.parent = root
    root.scale = (1, 1, ZSQUASH)
    root.rotation_euler = (0, 0, math.radians(-yaw_deg))
    bpy.context.view_layer.update()
    # bounds in world
    pts = []
    for ob in objs:
        if ob.type != "MESH": continue
        for c in ob.bound_box:
            pts.append(ob.matrix_world @ Vector(c))
    if not pts: return root, 3
    minz = min(p.z for p in pts)
    if ground_z: root.location.z -= minz
    minx = min(p.x for p in pts); maxx = max(p.x for p in pts)
    miny = min(p.y for p in pts); maxy = max(p.y for p in pts)
    fpx = maxx - minx; fpy = maxy - miny
    if fit:
        s = fit / max(fpx, fpy, 0.001)
        root.scale = (s, s, s * ZSQUASH)
        fpx *= s; fpy *= s
    # center on origin
    bpy.context.view_layer.update()
    pts = [ob.matrix_world @ Vector(c) for ob in objs if ob.type == "MESH" for c in ob.bound_box]
    cx = (min(p.x for p in pts) + max(p.x for p in pts)) / 2
    cy = (min(p.y for p in pts) + max(p.y for p in pts)) / 2
    root.location.x -= cx; root.location.y -= cy
    if ground_z:
        minz2 = min(p.z for p in pts)
        root.location.z -= minz2
    bpy.context.view_layer.update()
    maxdim = max(fpx, fpy)
    zext = max(p.z for p in pts) - min(p.z for p in pts)
    return root, maxdim * 1.6 + zext + 1.2

META = {}
def J(section, key, val):
    META.setdefault(section, {})[key] = val
    json.dump(META, open(f"{OUT}/meta.json", "w"))

# ---------------- 1) KayKit buildings + truck ----------------
KAYK = "/workspace/kaykit-space-base-bits-1.0/addons/kaykit_space_base_bits/Assets/gltf"
KB = {"nx_conyard": ("basemodule_garage", 2.9), "nx_refinery": ("drill_structure", 2.8),
      "dm_power_plant": ("solarpanel", 1.9), "so_power_plant": ("windturbine_low", 1.9),
      "dm_barracks": ("structure_low", 1.9), "dm_factory": ("basemodule_A", 2.9),
      "so_factory": ("basemodule_B", 2.9)}
def import_gltf(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    return [o for o in bpy.data.objects if o not in before]

for sid, (model, size) in KB.items():
    if f"kk_{sid}" not in META.get("kaykit", {}):
        clear_models()
        objs = import_gltf(f"{KAYK}/{model}.gltf")
        place(objs, 0, fit=size)
        J("kaykit", sid, render_current(f"kk_{sid}", None, size * 2.2, 1/3))
        print("kaykit", sid, flush=True)

for k in range(8):
    clear_models()
    objs = import_gltf(f"{KAYK}/spacetruck.gltf")
    place(objs, k * 45, fit=1.15)
    J("truck", str(k), render_current(f"truck_{k}", None, 3.2, 1/3))
    print("truck", k, flush=True)

# ---------------- 2) soldiers (all poses) + tanks from builder scripts ----------------
import importlib.util
def load_builders():
    # blender_vehicles.py contains soldier(), tank(), buggy() etc. — reuse
    src = open(f"{SP}/blender_vehicles.py").read()
    src = src.split("VEHICLES = [")[0] + "\n"    # helpers + tank/buggy defs only? need soldier too
    src2 = src + open(f"{SP}/blender_vehicles.py").read().split("# ---------------- soldier death pose (p2) ----------------")[1]
    ns = {"bpy": bpy, "math": math, "os": os}
    exec(compile(src, "helpers", "exec"), ns)
    # soldier() from the death-pose section references helpers in same ns
    tail = open(f"{SP}/blender_vehicles.py").read()
    s_start = tail.index("def soldier(")
    s_end = tail.index("for uid, kind, fac in DM:")
    exec(compile(tail[s_start:s_end], "soldier", "exec"), ns)
    return ns

NS = load_builders()
# neutralize the builders' clear() so they don't delete rig objects
def safe_clear():
    clear_models()
NS["clear"] = safe_clear

SOLDIERS = [("dm_rifle","rifle","dm"),("dm_rocket","rocket","dm"),("dm_heavy","heavy","dm"),
            ("dm_engineer","engineer","dm"),("dm_medic","medic","dm"),
            ("so_rifle","rifle","so"),("so_rocket","rocket","so")]
for uid, kind, fac in SOLDIERS:
    for pose in (0, 1):
        for k in range(8):
            tag = f"sold_{uid}_p{pose}_{k}"
            if os.path.exists(f"{OUT}/{tag}_final.png") and META.get("soldiers",{}).get(f"{uid}_p{pose}_{k}"): continue
            clear_models()
            body = NS["soldier"](kind, fac, pose)
            bpy.context.view_layer.update()
            bpts = [body.matrix_world @ Vector(c) for c in body.bound_box]
            fh = 0.55 / max(0.001, max(q.z for q in bpts) - min(q.z for q in bpts))
            body.scale = tuple(v * fh for v in body.scale)
            place([body], k * 45)
            J("soldiers", f"{uid}_p{pose}_{k}", render_current(tag, None, 2.4, 1/3))
    # death pose
    for k in range(8):
        tag = f"sold_{uid}_p2_{k}"
        if os.path.exists(f"{OUT}/{tag}_final.png") and META.get("soldiers",{}).get(f"{uid}_p2_{k}"): continue
        clear_models()
        body = NS["soldier"](kind, fac, 0)
        bpy.context.view_layer.update()
        bpts = [body.matrix_world @ Vector(c) for c in body.bound_box]
        fh = 0.52 / max(0.001, max(q.z for q in bpts) - min(q.z for q in bpts))
        body.scale = tuple(v * fh for v in body.scale)
        body.rotation_euler = (math.radians(-82), 0, math.radians(18))
        place([body], k * 45)
        J("soldiers", f"{uid}_p2_{k}", render_current(tag, None, 2.4, 1/3))
    print("soldier", uid, flush=True)

VEH = {"dm_wolverine": ("light","dm",0.8), "dm_mbt_walker": ("mbt","dm",0.95),
       "dm_mammoth": ("heavy","dm",1.25), "so_tick_tank": ("mbt","so",0.9)}
def build_tank(uid):
    if uid in VEH:
        tier, fac, size = VEH[uid]
        return NS["tank"](uid, tier, fac), size
    if uid == "so_scout_buggy": return NS["buggy"](uid), 0.75
    if uid == "so_stealth_tank":
        exec(open(f"{SP}/blender_new_vehicles_snippet.py").read(), NS)
        return NS["stealth_tank"](), 0.85
    if uid == "dm_hover_mlrs":
        exec(open(f"{SP}/blender_new_vehicles_snippet.py").read(), NS)
        return NS["hover_mlrs"](), 0.9

ALLV = list(VEH.keys()) + ["so_scout_buggy", "so_stealth_tank", "dm_hover_mlrs"]
for uid in ALLV:
    for k in range(8):
        # hull
        tag = f"veh_{uid}_hull_{k}"
        if not (os.path.exists(f"{OUT}/{tag}_final.png") and META.get("veh",{}).get(f"{uid}_hull_{k}")):
            clear_models()
            (hull, tur, mount), size = build_tank(uid)
            bpy.data.objects.remove(tur, do_unlink=True)
            root, view = place([hull], k * 45, fit=size)
            J("veh", f"{uid}_hull_{k}", render_current(tag, None, 3.4, 1/3))
        # turret (no ground snap: pivot anchored, rendered floating)
        tag = f"veh_{uid}_tur_{k}"
        if not (os.path.exists(f"{OUT}/{tag}_final.png") and META.get("veh",{}).get(f"{uid}_tur_{k}")):
            clear_models()
            (hull, tur, mount), size = build_tank(uid)
            # measure hull footprint scale factor like the hull pass
            pts = [hull.matrix_world @ Vector(c) for c in hull.bound_box]
            fp = max(max(p.x for p in pts) - min(p.x for p in pts),
                     max(p.y for p in pts) - min(p.y for p in pts))
            s = size / max(fp, 0.001)
            bpy.data.objects.remove(hull, do_unlink=True)
            tur.location = (0, 0, 0)
            bpy.ops.object.empty_add()
            root = bpy.context.active_object
            tur.parent = root
            root.scale = (s, s, s * ZSQUASH)
            root.rotation_euler = (0, 0, math.radians(-k * 45))
            bpy.context.view_layer.update()
            m = render_current(tag, None, 3.0, 1/3)
            m["mount"] = mount * s
            J("veh", f"{uid}_tur_{k}", m)
    print("vehicle", uid, flush=True)

print("ALL DONE", flush=True)
