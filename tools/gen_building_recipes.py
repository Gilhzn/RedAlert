#!/usr/bin/env python3
"""Emit unity/Assets/Resources/BuildingRecipes.json — per-structure part lists.
Units: 1 = one map cell. pos = [x, yBottom, z] of the part's base center,
structure centered at origin. Shapes: box (size), cyl (r,h), wedge (size —
slope rises toward +Z). Optional rot=[rx,ry,rz] Euler degrees, top=roof
texture, emissive="#rrggbb" (part rendered as glow, tex ignored).
tex "owner" resolves at runtime: dominion→gold, serpent→red."""
import json, sys

def box(pos, size, tex, top=None, rot=None, emissive=None):
    p = {"shape": "box", "pos": pos, "size": size, "tex": tex}
    if top: p["top"] = top
    if rot: p["rot"] = rot
    if emissive: p["emissive"] = emissive
    return p

def cyl(pos, r, h, tex, top=None, rot=None, emissive=None):
    p = {"shape": "cyl", "pos": pos, "r": r, "h": h, "tex": tex}
    if top: p["top"] = top
    if rot: p["rot"] = rot
    if emissive: p["emissive"] = emissive
    return p

def wedge(pos, size, tex, rot=None):
    p = {"shape": "wedge", "pos": pos, "size": size, "tex": tex}
    if rot: p["rot"] = rot
    return p

GREEN = "#49ff7a"; RED = "#ff4b55"; BLUE = "#41b9ff"

R = {}

# ---------- shared ----------
R["nx_conyard"] = [
    box([0, 0, 0], [2.9, 0.12, 2.9], "concrete", top="concrete"),
    box([-0.35, 0.12, 0.15], [2.0, 1.1, 2.1], "metal", top="roof"),
    box([-0.35, 1.22, 0.15], [2.06, 0.14, 2.16], "hazard", top="roof"),
    box([-0.35, 0.17, -0.93], [1.2, 0.85, 0.1], "door"),
    box([-0.85, 1.36, 0.6], [0.55, 0.16, 0.55], "vent", top="vent"),
    box([1.05, 0.12, 0.8], [0.7, 1.85, 0.7], "owner", top="metal_dark"),
    box([1.05, 1.75, -0.25], [0.28, 0.24, 1.9], "gold_dark", top="metal_dark"),
    cyl([1.02, 0.12, -0.85], 0.26, 0.6, "rust", top="metal_dark"),
    cyl([0.45, 0.12, -0.98], 0.2, 0.45, "metal_dark", top="metal_dark"),
    wedge([-0.35, 0.02, -1.22], [1.3, 0.32, 0.45], "hazard"),
]
R["nx_refinery"] = [
    box([0, 0, 0], [2.9, 0.12, 2.9], "concrete", top="concrete"),
    box([-0.55, 0.12, 0.35], [1.7, 1.15, 2.0], "metal", top="roof"),
    box([-0.55, 1.27, 0.35], [1.76, 0.12, 2.06], "owner", top="roof"),
    cyl([-1.0, 1.39, 0.9], 0.15, 0.95, "metal_dark", top="metal_dark"),
    cyl([0.95, 0.12, 0.55], 0.55, 1.05, "metal_dark", top="glow_green"),
    cyl([0.95, 0.84, 0.55], 0.57, 0.16, "metal", emissive=GREEN),
    box([0.2, 0.72, 0.5], [0.9, 0.18, 0.22], "rust"),
    box([-0.5, 0.17, -0.7], [1.0, 0.8, 0.1], "door"),
    wedge([0.35, 0.02, -1.1], [1.5, 0.4, 0.65], "hazard"),
    box([0.35, 0.12, -0.35], [1.5, 0.06, 0.9], "grate", top="grate"),
]
R["nx_silo"] = [
    box([0, 0, 0], [0.9, 0.14, 0.9], "concrete", top="concrete_dark"),
    cyl([0, 0.14, 0], 0.34, 0.85, "metal", top="metal_dark"),
    cyl([0, 0.55, 0], 0.36, 0.14, "metal", emissive=GREEN),
    cyl([0, 0.99, 0], 0.25, 0.14, "metal_dark", top="metal_dark"),
]
R["nx_emp_cannon"] = [
    box([0, 0, 0], [1.9, 0.12, 1.9], "concrete", top="concrete_dark"),
    box([0, 0.12, 0], [1.25, 0.5, 1.25], "metal", top="metal_dark"),
    cyl([0, 0.62, 0], 0.42, 0.24, "grate", top="metal_dark"),
    cyl([0, 0.86, 0], 0.34, 0.24, "metal_dark", top="metal_dark"),
    cyl([0, 1.10, 0], 0.27, 0.24, "grate", top="metal_dark"),
    cyl([0, 1.36, 0], 0.2, 0.3, "metal", emissive=BLUE),
    box([0.75, 0.12, 0.75], [0.35, 0.7, 0.35], "hazard", top="metal_dark"),
]

# ---------- Dominion (gold / brick / grey) ----------
R["dm_power_plant"] = [
    box([0, 0, 0], [1.9, 0.12, 1.9], "concrete", top="concrete"),
    box([-0.35, 0.12, 0], [1.15, 0.85, 1.7], "gold", top="roof_gold"),
    box([-0.35, 0.35, -0.86], [0.8, 0.4, 0.06], "vent"),
    cyl([0.55, 0.12, -0.45], 0.3, 1.3, "metal", top="metal_dark"),
    cyl([0.55, 0.12, 0.45], 0.3, 1.3, "metal", top="metal_dark"),
    box([0.2, 0.72, 0], [0.7, 0.16, 0.16], "rust"),
    box([-0.35, 0.97, 0], [1.2, 0.1, 1.75], "hazard", top="roof"),
]
R["dm_barracks"] = [
    box([0, 0, 0], [1.9, 0.12, 1.9], "concrete", top="concrete"),
    box([-0.15, 0.12, 0.1], [1.5, 0.75, 1.25], "brick", top="roof_gold"),
    box([-0.15, 0.17, -0.55], [0.55, 0.6, 0.08], "door"),
    box([0.45, 0.12, 0.72], [0.75, 0.5, 0.45], "metal", top="roof"),
    cyl([-0.78, 0.12, 0.72], 0.035, 1.35, "metal_dark"),
    box([-0.66, 1.3, 0.72], [0.26, 0.16, 0.03], "owner"),
]
R["dm_factory"] = [
    box([0, 0, 0], [2.9, 0.12, 1.9], "concrete", top="concrete"),
    box([-0.3, 0.12, 0.1], [2.2, 1.25, 1.6], "brick", top="roof"),
    box([-0.3, 0.17, -0.72], [1.5, 0.95, 0.1], "door"),
    box([-0.3, 1.14, -0.73], [1.62, 0.24, 0.1], "hazard"),
    box([-0.3, 1.37, 0.1], [1.4, 0.14, 1.0], "vent", top="vent"),
    cyl([1.15, 0.12, 0.55], 0.17, 1.65, "metal_dark", top="metal_dark"),
    cyl([1.15, 0.12, 0.0], 0.17, 1.45, "metal_dark", top="metal_dark"),
    box([1.15, 0.12, -0.6], [0.5, 0.5, 0.5], "owner", top="metal_dark"),
    wedge([-0.3, 0.02, -1.08], [1.6, 0.35, 0.55], "hazard"),
]
R["dm_radar"] = [
    box([0, 0, 0], [1.9, 0.12, 1.9], "concrete", top="concrete"),
    box([0, 0.12, 0.25], [1.5, 0.7, 1.2], "gold", top="roof_gold"),
    box([0, 0.17, -0.4], [0.5, 0.5, 0.08], "door"),
    cyl([0, 0.82, 0.25], 0.13, 0.5, "metal_dark"),
    cyl([0, 1.35, 0.25], 0.6, 0.1, "metal", top="metal_dark", rot=[35, 0, 0]),
    cyl([0.55, 0.82, -0.3], 0.04, 0.6, "metal_dark"),
]
R["dm_helipad"] = [
    box([0, 0, 0], [1.9, 0.16, 1.9], "concrete", top="concrete_dark"),
    box([0, 0.16, 0], [1.1, 0.03, 1.1], "grate", top="grate"),
    box([0.7, 0.16, 0.7], [0.45, 0.5, 0.45], "owner", top="roof_gold"),
    box([-0.8, 0.16, -0.8], [0.12, 0.12, 0.12], "metal", emissive=GREEN),
    box([0.8, 0.16, -0.8], [0.12, 0.12, 0.12], "metal", emissive=GREEN),
    box([-0.8, 0.16, 0.8], [0.12, 0.12, 0.12], "metal", emissive=GREEN),
]
R["dm_service_depot"] = [
    box([0, 0, 0], [2.8, 0.12, 1.8], "grate", top="grate"),
    box([-1.15, 0.12, 0], [0.28, 1.3, 0.9], "metal", top="metal_dark"),
    box([1.15, 0.12, 0], [0.28, 1.3, 0.9], "metal", top="metal_dark"),
    box([0, 1.42, 0], [2.6, 0.26, 0.7], "hazard", top="metal_dark"),
    cyl([0, 1.0, 0], 0.05, 0.42, "metal_dark"),
    box([0, 0.9, 0], [0.2, 0.12, 0.2], "rust"),
]
R["dm_tech_center"] = [
    box([0, 0, 0], [1.9, 0.12, 1.9], "concrete", top="concrete"),
    box([0, 0.12, 0.05], [1.6, 1.0, 1.45], "gold", top="roof_gold"),
    box([0, 0.68, -0.68], [1.35, 0.24, 0.06], "metal", emissive=BLUE),
    box([0, 1.12, 0.05], [1.0, 0.14, 0.9], "vent", top="vent"),
    cyl([0.55, 1.26, 0.4], 0.04, 0.75, "metal_dark"),
    cyl([0.55, 1.95, 0.4], 0.16, 0.06, "metal", top="metal_dark", rot=[30, 0, 0]),
]
R["dm_guard_tower"] = [
    box([-0.3, 0, -0.3], [0.11, 0.95, 0.11], "metal_dark"),
    box([0.3, 0, -0.3], [0.11, 0.95, 0.11], "metal_dark"),
    box([-0.3, 0, 0.3], [0.11, 0.95, 0.11], "metal_dark"),
    box([0.3, 0, 0.3], [0.11, 0.95, 0.11], "metal_dark"),
    box([0, 0.5, 0], [0.5, 0.06, 0.5], "grate", top="grate"),
    box([0, 0.95, 0], [0.82, 0.14, 0.82], "metal", top="grate"),
    box([0, 1.09, 0], [0.68, 0.42, 0.68], "owner", top="roof"),
    box([0, 1.2, -0.42], [0.22, 0.16, 0.32], "metal_dark"),
]
R["dm_rocket_tower"] = [
    box([0, 0, 0], [0.62, 1.25, 0.62], "concrete", top="concrete_dark"),
    box([0, 0.42, 0], [0.68, 0.14, 0.68], "hazard"),
    box([0, 1.25, 0], [0.75, 0.42, 0.55], "owner", top="metal_dark"),
    cyl([-0.15, 1.4, -0.3], 0.08, 0.35, "metal_dark", rot=[75, 0, 0]),
    cyl([0.15, 1.4, -0.3], 0.08, 0.35, "metal_dark", rot=[75, 0, 0]),
]
R["dm_aa_tower"] = [
    box([0, 0, 0], [0.7, 0.55, 0.7], "metal", top="metal_dark"),
    box([0, 0.55, 0], [0.5, 0.3, 0.5], "owner", top="metal_dark"),
    cyl([-0.12, 0.85, 0.05], 0.05, 0.55, "metal_dark", rot=[35, 0, 0]),
    cyl([0.12, 0.85, 0.05], 0.05, 0.55, "metal_dark", rot=[35, 0, 0]),
    cyl([-0.12, 0.78, -0.14], 0.05, 0.55, "metal_dark", rot=[35, 0, 0]),
    cyl([0.12, 0.78, -0.14], 0.05, 0.55, "metal_dark", rot=[35, 0, 0]),
]
R["dm_ion_uplink"] = [
    box([0, 0, 0], [1.9, 0.12, 1.9], "concrete", top="concrete_dark"),
    cyl([0, 0.12, 0], 0.72, 0.5, "metal", top="metal_dark"),
    wedge([0, 0.62, 0.5], [0.55, 0.55, 0.4], "gold", rot=[0, 0, 0]),
    wedge([0.5, 0.62, 0], [0.55, 0.55, 0.4], "gold", rot=[0, 90, 0]),
    wedge([0, 0.62, -0.5], [0.55, 0.55, 0.4], "gold", rot=[0, 180, 0]),
    wedge([-0.5, 0.62, 0], [0.55, 0.55, 0.4], "gold", rot=[0, 270, 0]),
    cyl([0, 0.62, 0], 0.26, 0.55, "metal", emissive=BLUE),
]

# ---------- Serpent (black / crimson) ----------
R["so_power_plant"] = [
    box([0, 0, 0], [1.9, 0.12, 1.9], "concrete_dark", top="concrete_dark"),
    box([-0.35, 0.12, 0], [1.15, 0.85, 1.7], "red", top="roof_dark"),
    box([-0.35, 0.35, -0.86], [0.8, 0.4, 0.06], "vent"),
    cyl([0.55, 0.12, -0.45], 0.3, 1.3, "metal_dark", top="black"),
    cyl([0.55, 0.12, 0.45], 0.3, 1.3, "metal_dark", top="black"),
    box([-0.35, 0.97, 0], [1.2, 0.1, 1.75], "black", top="roof_dark"),
]
R["so_adv_power"] = [
    box([0, 0, 0], [1.9, 0.12, 1.9], "concrete_dark", top="concrete_dark"),
    box([-0.4, 0.12, 0], [1.05, 1.0, 1.7], "black", top="roof_dark"),
    box([-0.4, 1.12, 0], [1.1, 0.12, 1.75], "red_dark", top="roof_dark"),
    cyl([0.5, 0.12, -0.55], 0.27, 1.6, "metal_dark", top="black"),
    cyl([0.5, 0.12, 0.0], 0.27, 1.75, "metal_dark", top="black"),
    cyl([0.5, 0.12, 0.55], 0.27, 1.6, "metal_dark", top="black"),
    box([0.5, 0.6, 0], [0.2, 0.14, 1.1], "rust"),
]
R["so_hand"] = [
    box([0, 0, 0], [1.9, 0.12, 1.9], "concrete_dark", top="concrete_dark"),
    box([0, 0.12, 0.05], [1.75, 0.55, 1.6], "black", top="roof_dark"),
    box([0, 0.67, 0.05], [1.35, 0.5, 1.25], "red_dark", top="roof_dark"),
    box([0, 1.17, 0.05], [0.95, 0.45, 0.9], "black", top="roof_dark"),
    box([0, 0.17, -0.74], [0.6, 0.5, 0.08], "door_red"),
    box([0, 1.3, -0.41], [0.5, 0.16, 0.06], "metal", emissive=RED),
    cyl([0, 1.62, 0.05], 0.05, 0.6, "metal_dark"),
]
R["so_factory"] = [
    box([0, 0, 0], [2.9, 0.12, 1.9], "concrete_dark", top="concrete_dark"),
    box([-0.3, 0.12, 0.1], [2.2, 1.25, 1.6], "black", top="roof_dark"),
    box([-0.3, 0.17, -0.72], [1.5, 0.95, 0.1], "door_red"),
    box([-0.3, 1.14, -0.73], [1.62, 0.24, 0.1], "hazard_red"),
    box([-0.3, 1.37, 0.1], [1.4, 0.14, 1.0], "vent", top="vent"),
    cyl([1.15, 0.12, 0.55], 0.17, 1.65, "metal_dark", top="black"),
    cyl([1.15, 0.12, 0.0], 0.17, 1.45, "metal_dark", top="black"),
    box([1.15, 0.12, -0.6], [0.5, 0.5, 0.5], "red_dark", top="black"),
    wedge([-0.3, 0.02, -1.08], [1.6, 0.35, 0.55], "hazard_red"),
]
R["so_radar"] = [
    box([0, 0, 0], [1.9, 0.12, 1.9], "concrete_dark", top="concrete_dark"),
    box([0, 0.12, 0.3], [1.3, 0.6, 1.1], "red_dark", top="roof_dark"),
    box([0, 0.72, 0.3], [0.35, 0.85, 0.35], "black", top="black"),
    cyl([0, 1.62, 0.3], 0.5, 0.09, "metal_dark", top="black", rot=[40, 0, 0]),
    box([0, 0.17, -0.35], [0.45, 0.45, 0.08], "door_red"),
]
R["so_helipad"] = [
    box([0, 0, 0], [1.9, 0.16, 1.9], "concrete_dark", top="concrete_dark"),
    box([0, 0.16, 0], [1.1, 0.03, 1.1], "grate", top="grate"),
    box([0.7, 0.16, 0.7], [0.45, 0.5, 0.45], "red_dark", top="roof_dark"),
    box([-0.8, 0.16, -0.8], [0.12, 0.12, 0.12], "metal", emissive=RED),
    box([0.8, 0.16, -0.8], [0.12, 0.12, 0.12], "metal", emissive=RED),
    box([-0.8, 0.16, 0.8], [0.12, 0.12, 0.12], "metal", emissive=RED),
]
R["so_tech_center"] = [
    box([0, 0, 0], [1.9, 0.12, 1.9], "concrete_dark", top="concrete_dark"),
    box([0, 0.12, 0.05], [1.55, 1.05, 1.4], "black", top="roof_dark"),
    box([0, 0.7, -0.66], [1.3, 0.22, 0.06], "metal", emissive=RED),
    box([0, 1.17, 0.05], [1.0, 0.35, 0.9], "red_dark", top="roof_dark"),
    cyl([0, 1.52, 0.05], 0.05, 0.85, "metal_dark"),
]
R["so_laser_turret"] = [
    box([0, 0, 0], [0.62, 0.25, 0.62], "black", top="black"),
    box([0, 0.25, 0], [0.34, 0.65, 0.34], "black", top="black"),
    box([0, 0.9, 0], [0.26, 0.45, 0.26], "red_dark", top="black"),
    box([0, 1.35, 0], [0.18, 0.3, 0.18], "metal", emissive=RED),
]
R["so_sam"] = [
    box([0, 0, 0], [0.88, 0.4, 0.88], "black", top="roof_dark"),
    box([0, 0.4, 0.15], [0.5, 0.25, 0.5], "red_dark", top="black"),
    cyl([-0.12, 0.62, 0.0], 0.07, 0.6, "metal_dark", rot=[55, 0, 0]),
    cyl([0.12, 0.62, 0.0], 0.07, 0.6, "metal_dark", rot=[55, 0, 0]),
    box([0, 0.14, -0.45], [0.6, 0.2, 0.04], "hazard_red"),
]
R["so_obelisk"] = [
    box([0, 0, 0], [0.72, 0.3, 0.72], "black", top="black"),
    box([0, 0.3, 0.05], [0.46, 0.9, 0.46], "black", top="black"),
    box([0, 1.2, 0.08], [0.32, 0.7, 0.32], "black", top="black"),
    box([0, 1.9, 0.1], [0.2, 0.45, 0.2], "black", top="black"),
    box([0, 1.65, -0.13], [0.11, 0.6, 0.11], "metal", emissive=RED),
]
R["so_stealth_generator"] = [
    box([0, 0, 0], [1.9, 0.12, 1.9], "concrete_dark", top="concrete_dark"),
    cyl([0, 0.12, 0], 0.55, 0.55, "black", top="black"),
    box([-0.7, 0.12, -0.7], [0.16, 1.15, 0.16], "black", top="black"),
    box([0.7, 0.12, -0.7], [0.16, 1.15, 0.16], "black", top="black"),
    box([-0.7, 0.12, 0.7], [0.16, 1.15, 0.16], "black", top="black"),
    box([0.7, 0.12, 0.7], [0.16, 1.15, 0.16], "black", top="black"),
    box([-0.7, 1.27, -0.7], [0.12, 0.12, 0.12], "metal", emissive=BLUE),
    box([0.7, 1.27, -0.7], [0.12, 0.12, 0.12], "metal", emissive=BLUE),
    box([-0.7, 1.27, 0.7], [0.12, 0.12, 0.12], "metal", emissive=BLUE),
    box([0.7, 1.27, 0.7], [0.12, 0.12, 0.12], "metal", emissive=BLUE),
    cyl([0, 0.67, 0], 0.3, 0.3, "metal", emissive=BLUE),
]
R["so_temple"] = [
    box([0, 0, 0], [2.9, 0.12, 2.9], "concrete_dark", top="concrete_dark"),
    box([0, 0.12, 0.1], [2.6, 0.6, 2.4], "red_dark", top="roof_dark"),
    box([0, 0.72, 0.1], [2.0, 0.55, 1.85], "red", top="roof_dark"),
    box([0, 1.27, 0.1], [1.4, 0.5, 1.3], "red_dark", top="roof_dark"),
    cyl([0, 1.77, 0.1], 0.45, 0.35, "black", top="black"),
    box([0, 0.17, -1.12], [0.85, 0.5, 0.08], "door_red"),
    box([0, 1.45, -0.58], [0.55, 0.14, 0.05], "metal", emissive=RED),
    cyl([-1.05, 0.72, -0.85], 0.06, 0.95, "black"),
    cyl([1.05, 0.72, -0.85], 0.06, 0.95, "black"),
]
R["so_missile_silo"] = [
    box([0, 0, 0], [1.85, 0.5, 1.85], "concrete_dark", top="concrete"),
    box([-0.45, 0.5, 0], [0.8, 0.07, 1.45], "hazard_red", top="hazard_red"),
    box([0.45, 0.5, 0], [0.8, 0.07, 1.45], "hazard_red", top="hazard_red"),
    box([0.85, 0.5, -0.85], [0.14, 0.35, 0.14], "metal", emissive=RED),
    box([-0.85, 0.5, -0.85], [0.14, 0.35, 0.14], "black", top="black"),
]
R["so_tick_deployed"] = [
    box([0, 0, 0], [0.9, 0.28, 0.9], "rust", top="concrete_dark"),
    box([0, 0.28, 0.05], [0.55, 0.3, 0.6], "red_dark", top="black"),
    cyl([0, 0.47, -0.35], 0.06, 0.5, "metal_dark", rot=[90, 0, 0]),
]
R["so_artillery_deployed"] = [
    box([0, 0, 0], [0.9, 0.2, 0.9], "metal_dark", top="grate"),
    box([0, 0.2, 0.15], [0.5, 0.35, 0.5], "red_dark", top="black"),
    cyl([0, 0.4, -0.05], 0.08, 1.1, "metal_dark", rot=[55, 0, 0]),
]

out = sys.argv[1] if len(sys.argv) > 1 else "."
regions = json.load(open(f"{out}/tools/atlas_regions.json"))
data = {"atlas": regions, "structures": R}
with open(f"{out}/unity/Assets/Resources/BuildingRecipes.json", "w") as f:
    json.dump(data, f, indent=1)
print("recipes:", len(R), "structures")
