#!/usr/bin/env python3
"""Assemble the 3D (WebGL/Three.js) build of the demo from the 2D source:
strips sprite blobs + 2D renderer, injects the 3D view + model builders."""
import re, sys

SP = "/tmp/claude-0/-home-user-RedAlert/84746fb6-337c-560d-9167-231af8584ff6/scratchpad"
src = open(f"{SP}/tiberium-dusk-preview.html").read()

def cut(s, start_marker, end_marker, label):
    a = s.index(start_marker)
    b = s.index(end_marker, a)
    print(f"cut {label}: {b - a} chars")
    return s[:a] + s[b:]

# 1) sprite blobs (each is a single long line)
for name in ["KAYKIT_RAW", "SPRITES_RAW", "DECOR_RAW", "SOLDIER_RAW", "DEAD_RAW", "TANK_RAW"]:
    a = src.index(f"const {name} = ")
    b = src.index("\n", a)
    print(f"blob {name}: {b - a} chars")
    src = src[:a] + src[b + 1:]

# 2) sprite table builders — contiguous span from the first table to isoX
tables = ["const TANKS = {};", "const SPRITES = {};", "const DEADS = {};",
          "const DECOR = {", "const SOLDS = {};", "const KSPRITES = {"]
first = min(src.index(t) for t in tables if t in src)
b = src.index("function isoX")
print(f"cut sprite tables: {b - first} chars")
src = src[:first] + src[b:]

# 3) old projection + 2D draw pipeline (keep drawMinimap)
src = cut(src, "function isoX(x, y)", "function drawMinimap()", "2D draw pipeline")

# 4) old icon renderer
src = cut(src, "const ICONS = {};", "function makeBuildButton", "old iconFor")

# 5) the 2D context grab (would break WebGL context creation)
src = src.replace('let ctx = canvas.getContext("2d");\n', "")

# 6) vehicle predicates instead of sprite-table lookups
src = src.replace("if (TANKS[e.type]) e.recoil = 0.14;", "if (IS_VEH(e.type)) e.recoil = 0.14;")
src = src.replace("const barrel = TANKS[e.type] ? 0.6 : 0.3;", "const barrel = IS_VEH(e.type) ? 0.6 : 0.3;")
src = src.replace("if ((TANKS[e.type] || b.harvester) && e.path && rnd() < 0.35) {",
                  "if (IS_VEH(e.type) && e.path && rnd() < 0.35) {")
src = src.replace("if (UNITS[e.type].infantry && DEADS[e.type])",
                  "if (UNITS[e.type].infantry)")
src = src.replace("const WEAPONS = {",
                  "const IS_VEH = t => UNITS[t] && !UNITS[t].infantry && !UNITS[t].flying;\nconst WEAPONS = {")

# 7) world rebuild signal for the 3D terrain
src = src.replace("let credits, powerOut, powerUse, queues, placing;",
                  "let credits, powerOut, powerUse, queues, placing;\nlet worldVersion = 0;")
src = src.replace("  camX = 7; camY = MAP - 8; zoom = 1;",
                  "  camX = 7; camY = MAP - 8; zoom = 1;\n  worldVersion++;")

# 8) camera-relative pan (mouse middle-drag + touch drag) via ground unproject
src = src.replace("""  if (panning && panLast) {
    const dx = (ev.clientX - panLast[0]) / zoom, dy = (ev.clientY - panLast[1]) / zoom;
    camX -= (dx / (TW / 2) + dy / (TH / 2)) / 2;
    camY -= (dy / (TH / 2) - dx / (TW / 2)) / 2;
    panLast = [ev.clientX, ev.clientY];
  }""",
"""  if (panning && panLast) {
    const w1 = toWorld(panLast[0], panLast[1]), w2 = toWorld(ev.clientX, ev.clientY);
    camX -= w2[0] - w1[0]; camY -= w2[1] - w1[1];
    panLast = [ev.clientX, ev.clientY];
  }""")
src = src.replace("""    if (touch1.moved) {
      camX -= ((dx / zoom) / (TW / 2) + (dy / zoom) / (TH / 2)) / 2;
      camY -= ((dy / zoom) / (TH / 2) - (dx / zoom) / (TW / 2)) / 2;
    }""",
"""    if (touch1.moved) {
      const w1 = toWorld(touch1.x, touch1.y), w2 = toWorld(t.clientX, t.clientY);
      camX -= w2[0] - w1[0]; camY -= w2[1] - w1[1];
    }""")

# 9) keyboard pan is camera-relative now + Q/E orbit
src = src.replace("""    const pan = 12 * dt / Math.max(zoom, 0.7);
    if (keys["w"] || keys["arrowup"]) camY -= pan, camX -= pan;
    if (keys["s"] || keys["arrowdown"]) camY += pan, camX += pan;
    if (keys["a"] || keys["arrowleft"]) camX -= pan, camY += pan;
    if (keys["d"] || keys["arrowright"]) camX += pan, camY -= pan;""",
"""    const pan = 17 * dt / Math.max(zoom, 0.7);
    const cy = Math.cos(camYaw), sy = Math.sin(camYaw);
    if (keys["w"] || keys["arrowup"]) { camX -= cy * pan; camY -= sy * pan; }
    if (keys["s"] || keys["arrowdown"]) { camX += cy * pan; camY += sy * pan; }
    if (keys["a"] || keys["arrowleft"]) { camX -= sy * pan; camY += cy * pan; }
    if (keys["d"] || keys["arrowright"]) { camX += sy * pan; camY -= cy * pan; }
    if (keys["q"]) camYaw += dt * 1.5;
    if (keys["e"]) camYaw -= dt * 1.5;
    if (keys["r"]) camPitch = Math.min(1.28, camPitch + dt * 0.9);
    if (keys["f"]) camPitch = Math.max(0.52, camPitch - dt * 0.9);""")

# 10) pickEntity: pick against 3D-projected positions
src = src.replace("""      const [ex, ey] = toScreen(e.x, e.y);
      const rad = e.kind === "struct" ? (e.fw * TW / 3) * zoom : 16 * zoom;
      const d = Math.hypot(ex - sx, (ey - 8 * zoom) - sy);""",
"""      const [ex, ey] = toScreen(e.x, e.y, heightAt(e.x, e.y) + (e.kind === "struct" ? 0.7 : 0.35));
      const rad = (e.kind === "struct" ? e.fw * 26 : 20) * Math.min(zoom, 1.8) * (vh / 900);
      const d = Math.hypot(ex - sx, ey - sy);""")

# 11) resize(): drive the WebGL renderer + HUD overlay instead of a 2D canvas
src = src.replace("""function resize() {
  vw = window.innerWidth; vh = window.innerHeight;
  dpr = Math.min(3, window.devicePixelRatio || 1);
  canvas.width = Math.round(vw * dpr);
  canvas.height = Math.round(vh * dpr);
  canvas.style.width = vw + "px";
  canvas.style.height = vh + "px";
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  ctx.imageSmoothingQuality = "high";
  mm.width = Math.round(194 * dpr); mm.height = Math.round(194 * dpr);
  mmctx.setTransform(dpr, 0, 0, dpr, 0, 0);
}""",
"""function resize() {
  vw = window.innerWidth; vh = window.innerHeight;
  dpr = Math.min(2.5, window.devicePixelRatio || 1);
  applyRes();
  renderer.setSize(vw, vh);
  camera.aspect = vw / vh;
  camera.updateProjectionMatrix();
  hud.width = Math.round(vw * dpr); hud.height = Math.round(vh * dpr);
  hud.style.width = vw + "px"; hud.style.height = vh + "px";
  hudctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  mm.width = Math.round(194 * dpr); mm.height = Math.round(194 * dpr);
  mmctx.setTransform(dpr, 0, 0, dpr, 0, 0);
}""")

# 12) HUD canvas element + CSS
src = src.replace('<canvas id="game"></canvas>',
                  '<canvas id="game"></canvas>\n<canvas id="hud"></canvas>')
src = src.replace("  #game { position: fixed; inset: 0; display: block; cursor: crosshair; touch-action: none; }",
                  "  #game { position: fixed; inset: 0; display: block; cursor: crosshair; touch-action: none; }\n"
                  "  #hud { position: fixed; inset: 0; pointer-events: none; z-index: 8; }")

# 13) inject libraries + model builders before the main game script,
#     and the 3D view before the bootstrap section inside it
three = open(f"{SP}/package/build/three.min.js").read()
core = open(f"{SP}/js3d_core.js").read()
units = open(f"{SP}/js3d_units.js").read()
structs = open(f"{SP}/js3d_structures.js").read()
view = open(f"{SP}/js3d_view.js").read()

libs = ("<script>\n" + three + "\n</script>\n"
        "<script>\n" + core + "\n</script>\n"
        "<script>\n" + units + "\n</script>\n"
        "<script>\n" + structs + "\n</script>\n")
src = src.replace('<script>\n"use strict";', libs + '<script>\n"use strict";', 1)

marker = "/* ============================== Main loop ============================== */"
src = src.replace(marker, view + "\n" + marker, 1)

# 14) sanity: no leftovers
for bad in ["TANKS[", "SOLDS[", "DEADS[", "KSPRITES", "SPRITES_RAW", "KAYKIT_RAW",
            "DECOR_RAW", "SOLDIER_RAW", "TANK_RAW", "isoX(", "tilePath("]:
    n = src.count(bad)
    if n: print(f"WARNING leftover {bad}: {n}")
# ctx. must only appear as hudctx./mmctx./fctx.
stray = re.findall(r"(?<![a-zA-Z_])ctx\.", src)
print("stray ctx. count:", len(stray))

open(f"{SP}/tiberium-dusk-3d.html", "w").write(src)
print("WROTE tiberium-dusk-3d.html:", len(src) // 1024, "KB")
