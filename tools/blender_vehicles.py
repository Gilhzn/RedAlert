#!/usr/bin/env python3
"""Model the vehicle roster in Blender (bpy headless): tracked tanks and a
buggy, each with a separate 'turret' object (Unity spins it via ModelLibrary).
Also adds a fallen 'death' pose for every soldier.
Exports:
  scratchpad/vehicles/<id>_hull.gltf / <id>_turret.gltf  — sprite renderer
  unity/Assets/Resources/Models/<id>.glb                 — full model + turret child
  scratchpad/soldiers/<id>_p2.gltf                       — soldier death pose
"""
import bpy, math, os

import sys
OUT = "./vehicles"
SOLD = "./soldiers"
UNITY = "unity/Assets/Resources/Models"
os.makedirs(OUT, exist_ok=True)

DM_ARMOR = (0.545, 0.455, 0.20, 1)
DM_DARK  = (0.30, 0.27, 0.17, 1)
SO_ARMOR = (0.43, 0.14, 0.16, 1)
SO_DARK  = (0.16, 0.10, 0.11, 1)
TRACK    = (0.11, 0.11, 0.10, 1)
GUNMETAL = (0.23, 0.24, 0.27, 1)
TIRE     = (0.08, 0.08, 0.08, 1)
SKIN     = (0.85, 0.68, 0.52, 1)
BOOTS    = (0.14, 0.13, 0.12, 1)
YELLOW   = (0.95, 0.75, 0.15, 1)
WHITE    = (0.92, 0.93, 0.90, 1)
RED      = (0.85, 0.20, 0.22, 1)
BLUEGREY = (0.42, 0.48, 0.60, 1)

_mats = {}
def mat(color):
    key = tuple(round(c, 3) for c in color)
    if key not in _mats:
        m = bpy.data.materials.new("m"); m.use_nodes = True
        b = m.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = color
        b.inputs["Roughness"].default_value = 0.8
        _mats[key] = m
    return _mats[key]

def clear():
    bpy.ops.object.select_all(action="SELECT"); bpy.ops.object.delete()

def box(x, y, z, sx, sy, sz, color, rx=0, ry=0, rz=0):
    bpy.ops.mesh.primitive_cube_add(location=(x, y, z))
    o = bpy.context.active_object
    o.scale = (sx/2, sy/2, sz/2)
    o.rotation_euler = (math.radians(rx), math.radians(ry), math.radians(rz))
    o.data.materials.append(mat(color)); return o

def rod(x, y, z, r, length, color, rx=90, ry=0, rz=0, v=10):
    bpy.ops.mesh.primitive_cylinder_add(location=(x, y, z), radius=r, depth=length, vertices=v)
    o = bpy.context.active_object
    o.rotation_euler = (math.radians(rx), math.radians(ry), math.radians(rz))
    o.data.materials.append(mat(color)); return o

def ball(x, y, z, r, color, squash=1.0):
    bpy.ops.mesh.primitive_uv_sphere_add(location=(x, y, z), radius=r, segments=12, ring_count=8)
    o = bpy.context.active_object
    o.scale = (1, 1, squash)
    o.data.materials.append(mat(color)); return o

def join(parts, name):
    for p in parts: p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    o = bpy.context.active_object; o.name = name
    bpy.ops.object.select_all(action="DESELECT")
    return o

def tracks(parts, hw, ln, color, wheel_r=0.09):
    """two track assemblies at +-hw, length ln along Y"""
    for side in (-1, 1):
        parts.append(box(side*hw, 0, 0.16, 0.20, ln, 0.20, color))
        parts.append(box(side*hw, 0, 0.28, 0.16, ln*0.92, 0.08, GUNMETAL))
        n = 4
        for i in range(n):
            yy = -ln/2 + ln*0.15 + i*(ln*0.7/(n-1))
            parts.append(rod(side*hw, yy, 0.12, wheel_r, 0.24, TIRE, rx=0, ry=90))

def tank(uid, tier, fac):
    """tier: light / mbt / heavy ; fac dm/so. Facing +Y."""
    clear()
    armor = DM_ARMOR if fac == "dm" else SO_ARMOR
    dark = DM_DARK if fac == "dm" else SO_DARK
    hull = []
    if tier == "light":
        tracks(hull, 0.30, 0.85, TRACK)
        hull.append(box(0, 0, 0.36, 0.52, 0.80, 0.18, armor))
        hull.append(box(0, 0.28, 0.44, 0.46, 0.28, 0.10, dark, rx=18))
        turret_h = 0.46; bar_l = 0.5; t_s = 0.75
    elif tier == "mbt":
        tracks(hull, 0.36, 1.05, TRACK)
        hull.append(box(0, 0, 0.38, 0.62, 1.0, 0.22, armor))
        hull.append(box(0, 0.36, 0.50, 0.56, 0.34, 0.12, dark, rx=22))
        hull.append(box(0, -0.42, 0.50, 0.50, 0.16, 0.10, dark))
        turret_h = 0.50; bar_l = 0.72; t_s = 1.0
    else:  # heavy: wide twin-barrel monster
        tracks(hull, 0.46, 1.25, TRACK, wheel_r=0.11)
        hull.append(box(0, 0, 0.42, 0.80, 1.2, 0.26, armor))
        hull.append(box(0, 0.44, 0.56, 0.72, 0.38, 0.14, dark, rx=20))
        hull.append(box(0, -0.5, 0.58, 0.6, 0.2, 0.14, dark))
        turret_h = 0.56; bar_l = 0.85; t_s = 1.3
    hull_o = join(hull, "hull")

    tur = []
    tur.append(rod(0, 0, 0.02, 0.26*t_s, 0.10, dark, rx=0))
    tur.append(box(0, -0.02, 0.10, 0.44*t_s, 0.46*t_s, 0.16, armor))
    tur.append(box(0, 0.1*t_s, 0.20, 0.3*t_s, 0.24*t_s, 0.07, dark))
    if tier == "heavy":
        for side in (-1, 1):
            tur.append(rod(side*0.11, 0.30 + bar_l/2, 0.12, 0.045, bar_l, GUNMETAL, rx=90))
            tur.append(box(side*0.11, 0.30 + bar_l, 0.12, 0.10, 0.10, 0.10, BOOTS))
    else:
        tur.append(rod(0, 0.24 + bar_l/2, 0.12, 0.05 if tier == "mbt" else 0.04, bar_l, GUNMETAL, rx=90))
        tur.append(box(0, 0.24 + bar_l, 0.12, 0.09, 0.10, 0.09, BOOTS))
    tur_o = join(tur, "turret")
    tur_o.location = (0, 0, turret_h)
    return hull_o, tur_o, turret_h

def buggy(uid):
    clear()
    hull = []
    for sy in (-0.32, 0.32):
        for sx in (-0.3, 0.3):
            hull.append(rod(sx, sy, 0.14, 0.13, 0.12, TIRE, rx=0, ry=90, v=12))
    hull.append(box(0, 0, 0.28, 0.44, 0.85, 0.14, SO_ARMOR))
    hull.append(box(0, 0.22, 0.40, 0.36, 0.30, 0.12, SO_DARK, rx=14))
    hull.append(rod(-0.2, -0.1, 0.36, 0.03, 0.5, GUNMETAL, rx=68))   # roll bar
    hull.append(rod(0.2, -0.1, 0.36, 0.03, 0.5, GUNMETAL, rx=68))
    hull_o = join(hull, "hull")
    tur = [rod(0, 0, 0.02, 0.06, 0.16, SO_DARK, rx=0),
           box(0, 0.12, 0.10, 0.08, 0.34, 0.06, GUNMETAL)]
    tur_o = join(tur, "turret")
    tur_o.location = (0, -0.15, 0.52)
    return hull_o, tur_o, 0.52

VEHICLES = [
    ("dm_wolverine", "light", "dm"), ("dm_mbt_walker", "mbt", "dm"),
    ("dm_mammoth", "heavy", "dm"), ("so_tick_tank", "mbt", "so"),
]

def export_pair(uid, build):
    # combined GLB for Unity (hull + turret child)
    r = build()
    bpy.ops.export_scene.gltf(filepath=f"{UNITY}/{uid}.glb", export_format="GLB", export_yup=True)
    # hull-only gltf
    r = build()
    bpy.data.objects["turret"].select_set(True)
    bpy.ops.object.delete()
    bpy.ops.export_scene.gltf(filepath=f"{OUT}/{uid}_hull.gltf", export_format="GLTF_SEPARATE", export_yup=True)
    # turret-only gltf, moved to origin so its pivot is the anchor
    hull_o, tur_o, th = build()
    hull_o.select_set(True); bpy.ops.object.delete()
    t = bpy.data.objects["turret"]; t.location = (0, 0, 0)
    bpy.ops.export_scene.gltf(filepath=f"{OUT}/{uid}_turret.gltf", export_format="GLTF_SEPARATE", export_yup=True)
    return th

META = {}
for uid, tier, fac in VEHICLES:
    META[uid] = export_pair(uid, lambda t=tier, f=fac: tank(uid, t, f))
    print("vehicle", uid, "ok", flush=True)
META["so_scout_buggy"] = export_pair("so_scout_buggy", lambda: buggy("so_scout_buggy"))
print("buggy ok", flush=True)
import json
json.dump(META, open(f"{OUT}/meta.json", "w"))

# ---------------- soldier death pose (p2) ----------------
DM = [("dm_rifle","rifle","dm"),("dm_rocket","rocket","dm"),("dm_heavy","heavy","dm"),
      ("dm_engineer","engineer","dm"),("dm_medic","medic","dm"),
      ("so_rifle","rifle","so"),("so_rocket","rocket","so")]

def soldier(kind, faction, pose):
    clear()
    armor = DM_ARMOR if faction == "dm" else SO_ARMOR
    dark = DM_DARK if faction == "dm" else SO_DARK
    parts = []
    heavy = kind == "heavy"
    w = 1.15 if heavy else 1.0
    la = 22 if pose == 1 else 6
    parts.append(box(-0.10*w, 0, 0.26, 0.13*w, 0.15, 0.42, dark, rx=la))
    parts.append(box( 0.10*w, 0, 0.26, 0.13*w, 0.15, 0.42, dark, rx=-la))
    lo = 0.09*math.sin(math.radians(la))
    parts.append(box(-0.10*w,  lo*2.4, 0.045, 0.15*w, 0.22, 0.09, BOOTS))
    parts.append(box( 0.10*w, -lo*2.4, 0.045, 0.15*w, 0.22, 0.09, BOOTS))
    parts.append(box(0, 0, 0.40, 0.40*w, 0.24, 0.10, dark))
    parts.append(box(0, 0, 0.62, 0.44*w, 0.27*w, 0.34, armor))
    parts.append(box(-0.26*w, 0, 0.76, 0.14, 0.20, 0.12, armor))
    parts.append(box( 0.26*w, 0, 0.76, 0.14, 0.20, 0.12, armor))
    parts.append(ball(0, 0, 0.92, 0.115, SKIN))
    helmet = YELLOW if kind == "engineer" else WHITE if kind == "medic" else armor
    parts.append(ball(0, 0, 0.97, 0.135, helmet, squash=0.75))
    parts.append(box(0, 0.10, 0.92, 0.16, 0.06, 0.05, dark))
    if kind == "rocket":
        parts.append(rod(-0.30*w, 0.05, 0.72, 0.05, 0.34, armor, rx=70))
        parts.append(rod(0.24*w, 0.10, 0.80, 0.05, 0.30, armor, rx=95))
        parts.append(rod(0.16, -0.05, 0.95, 0.075, 0.62, BLUEGREY, rx=88))
        parts.append(rod(0.16, 0.28, 0.95, 0.09, 0.10, RED, rx=88))
    elif kind == "engineer":
        parts.append(rod(-0.30*w, 0.10, 0.66, 0.05, 0.32, armor, rx=55))
        parts.append(rod(0.30*w, 0.10, 0.66, 0.05, 0.32, armor, rx=55))
        parts.append(box(0.30, 0.26, 0.42, 0.20, 0.12, 0.16, YELLOW))
        parts.append(box(0.30, 0.26, 0.51, 0.06, 0.12, 0.04, GUNMETAL))
    elif kind == "medic":
        parts.append(rod(-0.30*w, 0.10, 0.66, 0.05, 0.32, WHITE, rx=55))
        parts.append(rod(0.30*w, 0.10, 0.66, 0.05, 0.32, WHITE, rx=55))
        parts.append(box(0, -0.20, 0.68, 0.30, 0.12, 0.34, WHITE))
        parts.append(box(0, -0.27, 0.70, 0.16, 0.02, 0.05, RED))
        parts.append(box(0, -0.27, 0.70, 0.05, 0.02, 0.16, RED))
    else:
        parts.append(rod(-0.28*w, 0.16, 0.70, 0.05, 0.30, armor, rx=35))
        parts.append(rod(0.28*w, 0.16, 0.70, 0.05, 0.30, armor, rx=35))
        if heavy:
            parts.append(rod(0.02, 0.30, 0.62, 0.085, 0.55, GUNMETAL, rx=90))
            parts.append(rod(0.02, 0.60, 0.62, 0.05, 0.16, BOOTS, rx=90))
            parts.append(box(0.02, 0.12, 0.52, 0.16, 0.18, 0.16, GUNMETAL))
        else:
            parts.append(box(0.02, 0.30, 0.66, 0.07, 0.46, 0.10, GUNMETAL))
            parts.append(rod(0.02, 0.56, 0.68, 0.025, 0.16, BOOTS, rx=90))
    return join(parts, "soldier")

for uid, kind, fac in DM:
    body = soldier(kind, fac, 0)
    body.rotation_euler = (math.radians(-82), 0, math.radians(18))  # fallen on back
    body.location = (0, 0, 0.06)
    bpy.ops.export_scene.gltf(filepath=f"{SOLD}/{uid}_p2.gltf", export_format="GLTF_SEPARATE", export_yup=True)
    print("death pose", uid, flush=True)
print("done")
