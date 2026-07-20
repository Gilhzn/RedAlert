#!/usr/bin/env python3
"""Model the infantry roster in Blender (bpy, headless): stylized low-poly
soldiers with class gear, two walk poses. Exports:
  scratchpad/soldiers/<id>_<pose>.gltf (+bin)  — for the demo sprite renderer
  unity/Assets/Resources/Models/<id>.glb        — for the Unity client
"""
import bpy, math, os, sys

import sys
OUT = sys.argv[1] if len(sys.argv) > 1 else "./soldiers"
UNITY = "unity/Assets/Resources/Models"
os.makedirs(OUT, exist_ok=True)

# palette
DM_ARMOR = (0.545, 0.455, 0.20, 1)   # dominion gold-olive
DM_DARK  = (0.30, 0.27, 0.17, 1)
SO_ARMOR = (0.43, 0.14, 0.16, 1)     # serpent crimson
SO_DARK  = (0.16, 0.10, 0.11, 1)
SKIN     = (0.85, 0.68, 0.52, 1)
BOOTS    = (0.14, 0.13, 0.12, 1)
GUNMETAL = (0.23, 0.24, 0.27, 1)
YELLOW   = (0.95, 0.75, 0.15, 1)
WHITE    = (0.92, 0.93, 0.90, 1)
RED      = (0.85, 0.20, 0.22, 1)
BLUEGREY = (0.42, 0.48, 0.60, 1)

_mats = {}
def mat(color):
    key = tuple(round(c, 3) for c in color)
    if key in _mats: return _mats[key]
    m = bpy.data.materials.new("m")
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = 0.8
    _mats[key] = m
    return m

def clear():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()

def box(x, y, z, sx, sy, sz, color, rx=0, ry=0, rz=0):
    bpy.ops.mesh.primitive_cube_add(location=(x, y, z))
    o = bpy.context.active_object
    o.scale = (sx / 2, sy / 2, sz / 2)
    o.rotation_euler = (math.radians(rx), math.radians(ry), math.radians(rz))
    o.data.materials.append(mat(color))
    return o

def ball(x, y, z, r, color, squash=1.0):
    bpy.ops.mesh.primitive_uv_sphere_add(location=(x, y, z), radius=r, segments=12, ring_count=8)
    o = bpy.context.active_object
    o.scale = (1, 1, squash)
    o.data.materials.append(mat(color))
    return o

def rod(x, y, z, r, length, color, rx=90, ry=0, rz=0):
    bpy.ops.mesh.primitive_cylinder_add(location=(x, y, z), radius=r, depth=length, vertices=10)
    o = bpy.context.active_object
    o.rotation_euler = (math.radians(rx), math.radians(ry), math.radians(rz))
    o.data.materials.append(mat(color))
    return o

def soldier(kind, faction, pose):
    """Build one soldier at origin, facing +Y (Blender ground = XY, up = Z).
    pose: 0 stand, 1 step."""
    clear()
    armor = DM_ARMOR if faction == "dm" else SO_ARMOR
    dark = DM_DARK if faction == "dm" else SO_DARK
    parts = []
    heavy = kind == "heavy"
    w = 1.15 if heavy else 1.0
    la = 22 if pose == 1 else 6         # leg swing degrees

    # legs + boots
    parts.append(box(-0.10 * w, 0, 0.26, 0.13 * w, 0.15, 0.42, dark, rx=la))
    parts.append(box( 0.10 * w, 0, 0.26, 0.13 * w, 0.15, 0.42, dark, rx=-la))
    lo = 0.09 * math.sin(math.radians(la))
    parts.append(box(-0.10 * w,  lo * 2.4, 0.045, 0.15 * w, 0.22, 0.09, BOOTS))
    parts.append(box( 0.10 * w, -lo * 2.4, 0.045, 0.15 * w, 0.22, 0.09, BOOTS))
    # torso armor + belt + shoulders
    parts.append(box(0, 0, 0.40, 0.40 * w, 0.24, 0.10, dark))
    parts.append(box(0, 0, 0.62, 0.44 * w, 0.27 * w, 0.34, armor))
    parts.append(box(-0.26 * w, 0, 0.76, 0.14, 0.20, 0.12, armor))
    parts.append(box( 0.26 * w, 0, 0.76, 0.14, 0.20, 0.12, armor))
    # head + helmet + visor
    parts.append(ball(0, 0, 0.92, 0.115, SKIN))
    helmet = YELLOW if kind == "engineer" else WHITE if kind == "medic" else armor
    parts.append(ball(0, 0, 0.97, 0.135, helmet, squash=0.75))
    parts.append(box(0, 0.10, 0.92, 0.16, 0.06, 0.05, dark))
    # arms
    if kind == "rocket":
        parts.append(rod(-0.30 * w, 0.05, 0.72, 0.05, 0.34, armor, rx=70))
        parts.append(rod(0.24 * w, 0.10, 0.80, 0.05, 0.30, armor, rx=95))
        # shoulder launcher tube
        parts.append(rod(0.16, -0.05, 0.95, 0.075, 0.62, BLUEGREY, rx=88))
        parts.append(rod(0.16, 0.28, 0.95, 0.09, 0.10, RED, rx=88))
    elif kind == "engineer":
        parts.append(rod(-0.30 * w, 0.10, 0.66, 0.05, 0.32, armor, rx=55))
        parts.append(rod(0.30 * w, 0.10, 0.66, 0.05, 0.32, armor, rx=55))
        parts.append(box(0.30, 0.26, 0.42, 0.20, 0.12, 0.16, YELLOW))     # toolbox
        parts.append(box(0.30, 0.26, 0.51, 0.06, 0.12, 0.04, GUNMETAL))
    elif kind == "medic":
        parts.append(rod(-0.30 * w, 0.10, 0.66, 0.05, 0.32, WHITE, rx=55))
        parts.append(rod(0.30 * w, 0.10, 0.66, 0.05, 0.32, WHITE, rx=55))
        parts.append(box(0, -0.20, 0.68, 0.30, 0.12, 0.34, WHITE))        # med pack
        parts.append(box(0, -0.27, 0.70, 0.16, 0.02, 0.05, RED))          # cross
        parts.append(box(0, -0.27, 0.70, 0.05, 0.02, 0.16, RED))
    else:
        # rifle / heavy gunner: two arms forward holding a gun
        parts.append(rod(-0.28 * w, 0.16, 0.70, 0.05, 0.30, armor, rx=35))
        parts.append(rod(0.28 * w, 0.16, 0.70, 0.05, 0.30, armor, rx=35))
        if heavy:
            parts.append(rod(0.02, 0.30, 0.62, 0.085, 0.55, GUNMETAL, rx=90))
            parts.append(rod(0.02, 0.60, 0.62, 0.05, 0.16, BOOTS, rx=90))
            parts.append(box(0.02, 0.12, 0.52, 0.16, 0.18, 0.16, GUNMETAL))  # ammo box
        else:
            parts.append(box(0.02, 0.30, 0.66, 0.07, 0.46, 0.10, GUNMETAL))
            parts.append(rod(0.02, 0.56, 0.68, 0.025, 0.16, BOOTS, rx=90))

    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    body = bpy.context.active_object
    body.name = "soldier"
    return body

ROSTER = [
    ("dm_rifle", "rifle", "dm"), ("dm_rocket", "rocket", "dm"),
    ("dm_heavy", "heavy", "dm"), ("dm_engineer", "engineer", "dm"),
    ("dm_medic", "medic", "dm"),
    ("so_rifle", "rifle", "so"), ("so_rocket", "rocket", "so"),
]

for uid, kind, fac in ROSTER:
    for pose in (0, 1):
        soldier(kind, fac, pose)
        bpy.ops.export_scene.gltf(filepath=f"{OUT}/{uid}_p{pose}.gltf",
                                  export_format="GLTF_SEPARATE", export_yup=True)
    # Unity gets the standing pose as GLB
    soldier(kind, fac, 0)
    bpy.ops.export_scene.gltf(filepath=f"{UNITY}/{uid}.glb", export_format="GLB", export_yup=True)
    print("exported", uid, flush=True)
print("done")
