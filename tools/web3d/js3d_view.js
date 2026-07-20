/* Tiberium Dusk — real-time 3D view (Three.js r128, Z-up).
   Replaces the 2D sprite renderer: provides toScreen/toWorld/draw()/iconFor
   over the SAME game state and input code. */
"use strict";

/* ---------- renderer / scene / camera ---------- */
const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;
renderer.outputEncoding = THREE.sRGBEncoding;
renderer.toneMapping = THREE.ACESFilmicToneMapping;   // filmic response curve
renderer.toneMappingExposure = 1.05;
const scene = new THREE.Scene();
scene.background = new THREE.Color(0x100d09);
scene.fog = new THREE.Fog(0x100d09, 60, 195);

/* dusk sky dome + image-based lighting: every metal surface picks up the
   horizon glow via a PMREM environment map generated from the same sky */
const skyCanvas = document.createElement("canvas");
skyCanvas.width = 16; skyCanvas.height = 256;
{
  const g = skyCanvas.getContext("2d");
  const gr = g.createLinearGradient(0, 0, 0, 256);
  gr.addColorStop(0, "#04060c");
  gr.addColorStop(0.45, "#0c1120");
  gr.addColorStop(0.60, "#221c17");
  gr.addColorStop(0.68, "#54371f");
  gr.addColorStop(0.74, "#1a1510");
  gr.addColorStop(1, "#0d0b08");
  g.fillStyle = gr; g.fillRect(0, 0, 16, 256);
}
const skyTex = new THREE.CanvasTexture(skyCanvas);
skyTex.mapping = THREE.EquirectangularReflectionMapping;
skyTex.encoding = THREE.sRGBEncoding;   // canvas pixels are sRGB — avoid double-brighten
const skyDome = new THREE.Mesh(
  new THREE.SphereGeometry(230, 24, 18),
  new THREE.MeshBasicMaterial({ map: skyTex, side: THREE.BackSide, fog: false }));
skyDome.position.set(MAP / 2, MAP / 2, -4);
scene.add(skyDome);
const pmrem = new THREE.PMREMGenerator(renderer);
scene.environment = pmrem.fromEquirectangular(skyTex).texture;

/* multi-octave value-noise detail texture: ground grain + water ripples */
const noiseCanvas = document.createElement("canvas");
noiseCanvas.width = noiseCanvas.height = 256;
{
  const g = noiseCanvas.getContext("2d");
  g.fillStyle = "#b4b0a6"; g.fillRect(0, 0, 256, 256);
  for (const [n, a] of [[13, 0.42], [29, 0.3], [61, 0.2], [137, 0.13]]) {
    g.globalAlpha = a;
    const cell = 256 / n;
    for (let y = 0; y < n; y++) for (let x = 0; x < n; x++) {
      const v = 140 + (Math.random() * 90 | 0);
      g.fillStyle = `rgb(${v},${v - 5},${v - 12})`;
      g.fillRect(x * cell - 0.5, y * cell - 0.5, cell + 1, cell + 1);
    }
  }
  g.globalAlpha = 1;
}
const groundTex = new THREE.CanvasTexture(noiseCanvas);
groundTex.wrapS = groundTex.wrapT = THREE.RepeatWrapping;
groundTex.encoding = THREE.sRGBEncoding;
const waterBump = groundTex.clone();
waterBump.needsUpdate = true;

/* soft radial blob for grounding shadows under objects */
const blobTex = (() => {
  const c = document.createElement("canvas"); c.width = c.height = 128;
  const g = c.getContext("2d");
  const rg = g.createRadialGradient(64, 64, 6, 64, 64, 62);
  rg.addColorStop(0, "rgba(0,0,0,0.55)");
  rg.addColorStop(0.7, "rgba(0,0,0,0.28)");
  rg.addColorStop(1, "rgba(0,0,0,0)");
  g.fillStyle = rg; g.fillRect(0, 0, 128, 128);
  return new THREE.CanvasTexture(c);
})();
const blobGeo = new THREE.PlaneGeometry(1, 1);
function blobMesh(size, opacity) {
  const m = new THREE.Mesh(blobGeo, new THREE.MeshBasicMaterial({
    map: blobTex, transparent: true, opacity, depthWrite: false }));
  m.scale.set(size, size, 1);
  m.renderOrder = 4;
  return m;
}

const camera = new THREE.PerspectiveCamera(38, 1, 0.5, 400);
camera.up.set(0, 0, 1);
let camYaw = Math.PI / 4;             // Q/E orbit
let camPitch = 0.86;                  // R/F tilt (radians above horizon)

const hud = document.getElementById("hud");
const hudctx = hud.getContext("2d");

const sun = new THREE.DirectionalLight(0xffe6b8, 1.0);
sun.castShadow = true;
sun.shadow.mapSize.set(2048, 2048);
sun.shadow.bias = -0.0004;
sun.shadow.camera.near = 5; sun.shadow.camera.far = 160;
scene.add(sun); scene.add(sun.target);
scene.add(new THREE.HemisphereLight(0xbcd8ee, 0x6a5a3d, 0.38));
const amb = new THREE.AmbientLight(0x4a4136, 0.42); scene.add(amb);

function updateCamera() {
  const dist = 30 / zoom;
  const cp = Math.cos(camPitch), sp = Math.sin(camPitch);
  const dx = Math.cos(camYaw) * cp, dy = Math.sin(camYaw) * cp;
  camera.position.set(camX + dx * dist, camY + dy * dist, sp * dist);
  camera.lookAt(camX, camY, 0);
  camera.updateMatrixWorld();
  // sun follows the camera target so the shadow frustum stays tight
  sun.position.set(camX - 18, camY - 26, 34);
  sun.target.position.set(camX, camY, 0);
  const S = Math.min(58, 36 / Math.max(zoom, 0.62));
  sun.shadow.camera.left = -S; sun.shadow.camera.right = S;
  sun.shadow.camera.top = S; sun.shadow.camera.bottom = -S;
  sun.shadow.camera.updateProjectionMatrix();
}

/* ---------- projection (same contract as the old 2D code) ---------- */
const _v3 = new THREE.Vector3();
function toScreen(x, y, z) {
  _v3.set(x, y, z || 0).project(camera);
  return [(_v3.x + 1) / 2 * vw, (1 - _v3.y) / 2 * vh];
}
const _ray = new THREE.Raycaster();
const _groundPlane = new THREE.Plane(new THREE.Vector3(0, 0, 1), 0);
const _hitP = new THREE.Vector3();
function toWorld(sx, sy) {
  _ray.setFromCamera({ x: sx / vw * 2 - 1, y: 1 - sy / vh * 2 }, camera);
  _ray.ray.intersectPlane(_groundPlane, _hitP);
  return [_hitP.x, _hitP.y];
}

/* ---------- static world: terrain, water, rocks, props, crystals ---------- */
let worldGroup = null, waterMat = null, crystalGroup = null, crystalTimer = 0;
const cellObjs = [];                  // [object3d, cellIdx] — hidden while unexplored
let builtWorldVersion = -1;
const fogCanvas = document.createElement("canvas");
fogCanvas.width = fogCanvas.height = MAP;
const fogTex = new THREE.CanvasTexture(fogCanvas);
fogTex.magFilter = THREE.LinearFilter;
let fogMesh = null, fogTimer = 0;

const T_COLORS = [0x5a4c31, 0x4a3d27, 0x4e4129, 0x1d5a72];
function buildWorld() {
  if (worldGroup) scene.remove(worldGroup);
  if (crystalGroup) scene.remove(crystalGroup);
  worldGroup = new THREE.Group(); scene.add(worldGroup);

  // terrain: one quad per cell, per-cell color × tiling detail noise
  const pos = [], col = [], nrm = [], uvs = [], idxA = [];
  const c3 = new THREE.Color();
  let vi = 0;
  for (let y = 0; y < MAP; y++) for (let x = 0; x < MAP; x++) {
    const t = terrain[idx(x, y)];
    const water = t === 3;
    c3.setHex(T_COLORS[t]);
    const shade = 0.9 + ((x * 31 + y * 17) % 7) * 0.022;    // deterministic patchwork
    const zb = water ? -0.28 : 0;
    for (const [ox, oy] of [[0, 0], [1, 0], [1, 1], [0, 1]]) {
      pos.push(x + ox, y + oy, zb);
      col.push(c3.r * shade, c3.g * shade, c3.b * shade);
      nrm.push(0, 0, 1);
      uvs.push((x + ox) * 0.34, (y + oy) * 0.34);
    }
    idxA.push(vi, vi + 1, vi + 2, vi, vi + 2, vi + 3);
    vi += 4;
  }
  const tg = new THREE.BufferGeometry();
  tg.setAttribute("position", new THREE.Float32BufferAttribute(pos, 3));
  tg.setAttribute("color", new THREE.Float32BufferAttribute(col, 3));
  tg.setAttribute("normal", new THREE.Float32BufferAttribute(nrm, 3));
  tg.setAttribute("uv", new THREE.Float32BufferAttribute(uvs, 2));
  tg.setIndex(idxA);
  const terr = new THREE.Mesh(tg, new THREE.MeshStandardMaterial({
    vertexColors: true, map: groundTex, roughness: 1, metalness: 0 }));
  terr.receiveShadow = true;
  worldGroup.add(terr);

  // water surface (animated emissive pulse)
  const wpos = [], widx = [];
  let wi = 0;
  for (let y = 0; y < MAP; y++) for (let x = 0; x < MAP; x++) {
    if (terrain[idx(x, y)] !== 3) continue;
    for (const [ox, oy] of [[0, 0], [1, 0], [1, 1], [0, 1]]) wpos.push(x + ox, y + oy, -0.08);
    widx.push(wi, wi + 1, wi + 2, wi, wi + 2, wi + 3);
    wi += 4;
  }
  if (wpos.length) {
    const wg = new THREE.BufferGeometry();
    wg.setAttribute("position", new THREE.Float32BufferAttribute(wpos, 3));
    wg.computeVertexNormals(); wg.setIndex(widx);
    waterMat = new THREE.MeshStandardMaterial({ color: 0x2779a3, roughness: 0.12,
      metalness: 0.55, transparent: true, opacity: 0.9, emissive: 0x0d3a52, emissiveIntensity: 0.5,
      bumpMap: waterBump, bumpScale: 0.05 });
    const wm = new THREE.Mesh(wg, waterMat);
    worldGroup.add(wm);
  }

  // rock-ridge cells: mesa block + rock prop (fog hides them until explored)
  cellObjs.length = 0;
  for (let y = 0; y < MAP; y++) for (let x = 0; x < MAP; x++) {
    if (terrain[idx(x, y)] !== 1) continue;
    const h = 0.22 + ((x * 7 + y * 13) % 5) * 0.05;
    const mesa = H.box(worldGroup, x + 0.5, y + 0.5, h / 2, 0.96, 0.96, h, 0x3d3322);
    cellObjs.push([mesa, idx(x, y)]);
    const pr = PROP_BUILDERS.rock(H, (x * 3 + y) % 8);
    H.fitTo(pr.root, pr.fit * (0.8 + ((x + y) % 3) * 0.15));
    pr.root.position.x += x + 0.5; pr.root.position.y += y + 0.5; pr.root.position.z += h;
    worldGroup.add(pr.root);
    cellObjs.push([pr.root, idx(x, y)]);
  }
  // decor props (cacti / bushes / rocks from the map's decor list)
  for (const pr of decor) {
    const kind = pr.flora ? (pr.v % 3 === 0 ? "bush" : "cactus") : "rock";
    const b = PROP_BUILDERS[kind](H, pr.v % 8);
    H.fitTo(b.root, b.fit);
    b.root.position.x += pr.x; b.root.position.y += pr.y;
    worldGroup.add(b.root);
    cellObjs.push([b.root, idx(Math.min(MAP - 1, pr.x | 0), Math.min(MAP - 1, pr.y | 0))]);
  }

  // map rim: dark cliff skirt so the world doesn't float in space
  H.box(worldGroup, MAP / 2, MAP / 2, -0.62, MAP + 2.4, MAP + 2.4, 1.2, 0x171310);

  // fog-of-war overlay decal
  if (fogMesh) scene.remove(fogMesh);
  const fmat = new THREE.MeshBasicMaterial({ map: fogTex, transparent: true, depthWrite: false });
  fogMesh = new THREE.Mesh(new THREE.PlaneGeometry(MAP, MAP), fmat);
  fogMesh.position.set(MAP / 2, MAP / 2, 0.05);
  fogMesh.renderOrder = 5;
  scene.add(fogMesh);
  fogTex.center.set(0.5, 0.5); fogTex.rotation = 0; fogTex.flipY = false;

  crystalGroup = new THREE.Group(); scene.add(crystalGroup);
  rebuildCrystals();
  builtWorldVersion = worldVersion;
}

const crysGeo = new THREE.ConeGeometry(1, 1, 5);
crysGeo.translate(0, 0.5, 0); crysGeo.rotateX(Math.PI / 2);
let crysMeshG = null, crysMeshB = null, crysGlow = null;
function rebuildCrystals() {
  if (crysMeshG) crystalGroup.remove(crysMeshG);
  if (crysMeshB) crystalGroup.remove(crysMeshB);
  const g = [], b = [];
  for (let y = 0; y < MAP; y++) for (let x = 0; x < MAP; x++) {
    const c = crystal[idx(x, y)];
    if (!c || !explored[idx(x, y)]) continue;   // hidden until scouted
    (c === 9 ? b : g).push([x, y, c === 9 ? 3 : Math.min(c, 4)]);
  }
  const mk = (list, color) => {
    let count = 0;
    for (const [, , n] of list) count += n;
    if (!count) return null;
    const im = new THREE.InstancedMesh(crysGeo,
      new THREE.MeshStandardMaterial({ color, emissive: color, emissiveIntensity: 0.5, roughness: 0.3 }), count);
    const m4 = new THREE.Matrix4(), q = new THREE.Quaternion(), s = new THREE.Vector3(), p = new THREE.Vector3();
    let i = 0;
    for (const [x, y, n] of list) for (let k = 0; k < n; k++) {
      const ox = ((k * 53 + x * 31) % 20 - 10) / 26;
      const oy = ((k * 37 + y * 17) % 16 - 8) / 22;
      const h = 0.22 + ((k * 29 + x * 7) % 6) * 0.05;
      p.set(x + 0.5 + ox, y + 0.5 + oy, 0);
      q.setFromEuler(new THREE.Euler(((k * 11 + x) % 10 - 5) * 0.05, ((k * 7 + y) % 10 - 5) * 0.05, (k * 47 + x * 3) % 7));
      s.set(h * 0.42, h * 0.42, h);
      m4.compose(p, q, s); im.setMatrixAt(i++, m4);
    }
    im.castShadow = true;
    crystalGroup.add(im);
    return im;
  };
  crysMeshG = mk(g, 0x2fae57);
  crysMeshB = mk(b, 0x2b7fb3);
  // fake-bloom halos over each crystal cluster
  if (crysGlow) crystalGroup.remove(crysGlow);
  crysGlow = new THREE.Group();
  for (const [list, color] of [[g, 0x2fae57], [b, 0x2b7fb3]]) {
    for (const [x, y] of list) {
      const s = glowSprite(color, 1.25, 0.13);
      s.position.set(x + 0.5, y + 0.5, 0.4);
      crysGlow.add(s);
    }
  }
  crystalGroup.add(crysGlow);
}

function updateFog() {
  const fctx = fogCanvas.getContext("2d");
  const img = fctx.createImageData(MAP, MAP);
  for (let y = 0; y < MAP; y++) for (let x = 0; x < MAP; x++) {
    const i = idx(x, y), o = i * 4;
    img.data[o] = 5; img.data[o + 1] = 4; img.data[o + 2] = 3;
    img.data[o + 3] = !explored[i] ? 252 : (!visible[i] ? 110 : 0);
  }
  fctx.putImageData(img, 0, 0);
  fogTex.needsUpdate = true;
  for (const [o, cell] of cellObjs) o.visible = !!explored[cell];
}

/* ---------- entity view objects ---------- */
const views = new Map();     // ent id -> view record
const selRingGeo = new THREE.RingGeometry(0.68, 0.8, 24);
function makeSelRing(color) {
  const m = new THREE.Mesh(selRingGeo, new THREE.MeshBasicMaterial({
    color, transparent: true, opacity: 0.85, depthWrite: false }));
  m.renderOrder = 6;
  return m;
}
function glowSprite(color, scale, opacity) {
  const s = new THREE.Sprite(new THREE.SpriteMaterial({
    map: flashTex, color, transparent: true, opacity,
    blending: THREE.AdditiveBlending, depthWrite: false }));
  s.scale.set(scale, scale, 1);
  s.renderOrder = 7;
  return s;
}
function makeUnitView(e) {
  const spec = UNITS[e.type];
  const wrap = new THREE.Group();
  let rec;
  if (spec.infantry) {
    const p0 = UNIT_BUILDERS[e.type](H, 0), p1 = UNIT_BUILDERS[e.type](H, 1);
    H.fitTo(p0.root, p0.fit); H.fitTo(p1.root, p1.fit);
    wrap.add(p0.root); wrap.add(p1.root);
    p1.root.visible = false;
    rec = { wrap, p0: p0.root, p1: p1.root, turret: null };
  } else {
    const b = UNIT_BUILDERS[e.type](H, 0);
    H.fitTo(b.root, b.fit);
    wrap.add(b.root);
    rec = { wrap, turret: b.turret || null };
  }
  const ring = makeSelRing(0x37e86e);
  ring.visible = false; wrap.add(ring);
  rec.ring = ring;
  const blob = blobMesh(spec.infantry ? 0.5 : 1.15, 0.4);   // grounding contact shadow
  wrap.add(blob);
  rec.blob = blob;
  rec.kind = "unit"; rec.type = e.type;
  scene.add(wrap);
  return rec;
}
function makeStructView(e) {
  const wrap = new THREE.Group();
  const b = STRUCT_BUILDERS[e.type](H);
  H.fitTo(b.root, b.fit);
  b.root.rotation.z = Math.PI;          // fronts face the camera side
  wrap.add(b.root);
  const ring = makeSelRing(0x37e86e); ring.visible = false;
  ring.scale.setScalar(Math.max(e.fw, e.fh) * 0.8); wrap.add(ring);
  const blob = blobMesh(Math.max(e.fw, e.fh) * 1.5, 0.42);
  blob.position.z = 0.035; wrap.add(blob);
  // fake-bloom halos on strongly emissive structures
  const bb = new THREE.Box3().setFromObject(b.root);
  const topZ = bb.max.z;
  const glows = { so_obelisk: [0xff3540, 1.1, 0.55], so_laser_turret: [0xff3540, 0.6, 0.4],
    dm_ion_uplink: [0x59f2ff, 0.9, 0.45], nx_refinery: [0x49ff7a, 0.9, 0.3],
    nx_emp_cannon: [0x59f2ff, 0.55, 0.35] };
  if (glows[e.type]) {
    const [gc, gs, go] = glows[e.type];
    const gl = glowSprite(gc, gs, go);
    gl.position.set(0, 0, topZ * 0.92);
    wrap.add(gl);
  }
  scene.add(wrap);
  return { wrap, spin: b.spin || null, ring, kind: "struct", type: e.type };
}

function entHidden(e) {
  const cx = Math.min(MAP - 1, e.x | 0), cy = Math.min(MAP - 1, e.y | 0);
  if (e.owner === 1 && !visible[idx(cx, cy)]) return true;
  if (typeof stealthHidden === "function" && stealthHidden(e)) return true;
  if (!explored[idx(cx, cy)]) return true;
  return false;
}

function syncEnts(dt) {
  const seen = new Set();
  for (const e of ents) {
    if (e.dead) continue;
    seen.add(e.id);
    let v = views.get(e.id);
    if (!v) {
      v = e.kind === "struct" ? makeStructView(e) : makeUnitView(e);
      views.set(e.id, v);
    }
    const hidden = entHidden(e);
    v.wrap.visible = !hidden;
    if (hidden) continue;
    // battle damage: heavily wounded things smolder
    if (e.hp < e.maxhp * 0.45 && Math.random() < 0.08)
      particles.push({ x: e.x + (Math.random() - 0.5) * 0.5, y: e.y + (Math.random() - 0.5) * 0.5,
        vx: 0, vy: 0, g: -0.5, t: 0, life: 1.4,
        c: Math.random() < 0.3 ? "#6a6152" : "#2b2b28", s: 3 });
    if (e.kind === "struct") {
      v.wrap.position.set(e.x, e.y, 0);
      if (v.spin) v.spin.rotation.z += dt * 0.7;
      v.ring.visible = !!e.sel;
      continue;
    }
    const b = UNITS[e.type];
    const alt = b.flying ? (b.infantry ? 0.55 : 1.15) + Math.sin(e.anim * 1.6) * 0.07 : 0;
    const bob = b.walker ? Math.abs(Math.sin(e.anim * 4)) * 0.045 : 0;
    v.wrap.position.set(e.x, e.y, alt + bob);
    v.wrap.rotation.z = (e.face || 0) - Math.PI / 2;
    if (v.p0) {   // infantry leg swing
      const moving = !!e.path;
      const p1 = moving && Math.floor(e.anim * 2.4) % 2 === 1;
      v.p0.visible = !p1; v.p1.visible = p1;
    }
    if (v.turret) {
      if (v.type === "so_harpy") v.turret.rotation.z += dt * 26;   // rotor
      else {
        const tface = e.target && !e.target.dead
          ? Math.atan2(e.target.y - e.y, e.target.x - e.x) : (e.face || 0);
        v.turret.rotation.z = tface - Math.PI / 2 - v.wrap.rotation.z;
        if (e.recoil > 0) {
          const k = (e.recoil / 0.14) * 0.09;
          v.turret.position.y = -k / Math.max(0.001, v.wrap.scale.y);
        } else v.turret.position.y = 0;
      }
    }
    v.ring.visible = !!e.sel;
    v.ring.position.z = -alt - bob + 0.03;
    if (v.blob) v.blob.position.z = -alt - bob + 0.025;   // shadow stays on the ground
  }
  for (const [id, v] of views) {
    if (!seen.has(id)) { scene.remove(v.wrap); views.delete(id); }
  }
}

/* ---------- corpses / scorch ---------- */
const corpseViews = new Map();
const scorchGeo = new THREE.CircleGeometry(0.55, 14);
function syncCorpses() {
  const seen = new Set();
  for (const c of corpses) {
    if (!c._vid) c._vid = "c" + (nextId++);
    seen.add(c._vid);
    let v = corpseViews.get(c._vid);
    if (!v) {
      const g = new THREE.Group();
      if (c.type === "ruin") {
        for (let i = 0; i < 6; i++)
          H.box(g, (i % 3 - 1) * 0.35 * c.fw, ((i / 3 | 0) - 0.5) * 0.5 * c.fh,
            0.12 + (i % 2) * 0.1, 0.5 * c.fw, 0.4 * c.fh, 0.24, i % 2 ? 0x22231c : 0x191a14, { rz: i * 31 });
      } else if (c.type === null) {
        const m = new THREE.Mesh(scorchGeo, new THREE.MeshBasicMaterial({
          color: 0x0c0d0b, transparent: true, opacity: 0.85, depthWrite: false }));
        m.renderOrder = 4; g.add(m);
      } else if (UNIT_BUILDERS[c.type]) {
        const b = UNIT_BUILDERS[c.type](H, 0);
        H.fitTo(b.root, b.fit);
        b.root.rotation.x = -Math.PI / 2 * 0.94;    // fallen on its back
        b.root.position.z = 0.06;
        g.add(b.root);
      }
      g.position.set(c.x, c.y, 0.02);
      g.rotation.z = (c.face || 0);
      scene.add(g);
      v = { g }; corpseViews.set(c._vid, v);
    }
    const a = Math.min(1, (c.life - c.t) / 1.5);
    v.g.visible = a > 0.02 && explored[idx(Math.min(MAP - 1, c.x | 0), Math.min(MAP - 1, c.y | 0))];
  }
  for (const [id, v] of corpseViews) if (!seen.has(id)) { scene.remove(v.g); corpseViews.delete(id); }
}

/* ---------- transient FX ---------- */
function addMat(color, opacity) {
  return new THREE.MeshBasicMaterial({ color, transparent: true, opacity,
    blending: THREE.AdditiveBlending, depthWrite: false });
}
const beamPool = [];
const beamGeo = new THREE.CylinderGeometry(1, 1, 1, 6);
beamGeo.rotateX(Math.PI / 2);
function drawBeams() {
  let bi = 0;
  for (const bm of beams) {
    let m = beamPool[bi];
    if (!m) { m = new THREE.Mesh(beamGeo, addMat(0xffffff, 1)); m.renderOrder = 7; scene.add(m); beamPool.push(m); }
    bi++;
    const a = 1 - bm.t / 0.16;
    const p1 = new THREE.Vector3(bm.x1, bm.y1, 0.55 + (bm.thick ? 0.7 : 0));
    const p2 = new THREE.Vector3(bm.x2, bm.y2, 0.35);
    const mid = p1.clone().add(p2).multiplyScalar(0.5);
    m.position.copy(mid);
    m.scale.set(bm.thick ? 0.09 : 0.035, bm.thick ? 0.09 : 0.035, p1.distanceTo(p2));
    m.lookAt(p2);
    m.material.color.set(bm.c);
    m.material.opacity = a * 0.95;
    m.visible = true;
  }
  for (let i = bi; i < beamPool.length; i++) beamPool[i].visible = false;
}
const projPool = [];
const projGeo = new THREE.BoxGeometry(0.09, 0.28, 0.09);
function drawProjectiles() {
  let pi = 0;
  for (const p of projectiles) {
    if (p.delay > 0) continue;
    let m = projPool[pi];
    if (!m) { m = new THREE.Mesh(projGeo, addMat(0xfff6d8, 1)); m.renderOrder = 7; scene.add(m); projPool.push(m); }
    pi++;
    let hh = 0.5;
    if (p.arc) {
      const prog = Math.min(1, Math.hypot(p.x - p.sx0, p.y - p.sy0) / p.dist0);
      hh = 0.4 + Math.sin(prog * Math.PI) * p.dist0 * 0.35;
    }
    m.position.set(p.x, p.y, hh);
    const t = p.target;
    if (t) m.lookAt(t.x, t.y, 0.4);
    m.material.color.set(p.c);
    m.material.opacity = 1;
    m.visible = true;
  }
  for (let i = pi; i < projPool.length; i++) projPool[i].visible = false;
}
const flashPool = [];
const flashGeo = new THREE.PlaneGeometry(1, 1);
const flashTex = (() => {
  const c = document.createElement("canvas"); c.width = c.height = 64;
  const g = c.getContext("2d");
  const rg = g.createRadialGradient(32, 32, 2, 32, 32, 30);
  rg.addColorStop(0, "rgba(255,244,200,1)");
  rg.addColorStop(0.4, "rgba(255,214,120,0.7)");
  rg.addColorStop(1, "rgba(255,180,70,0)");
  g.fillStyle = rg; g.fillRect(0, 0, 64, 64);
  return new THREE.CanvasTexture(c);
})();
function billboardMat(opacity) {
  return new THREE.SpriteMaterial({ map: flashTex, transparent: true, opacity,
    blending: THREE.AdditiveBlending, depthWrite: false });
}
function drawFlashes() {
  let fi = 0;
  for (const f of flashes) {
    let s = flashPool[fi];
    if (!s) { s = new THREE.Sprite(billboardMat(1)); s.renderOrder = 8; scene.add(s); flashPool.push(s); }
    fi++;
    const a = 1 - f.t / 0.07;
    s.position.set(f.x, f.y, 0.55);
    const sc = f.big ? 1.1 : 0.6;
    s.scale.set(sc, sc, 1);
    s.material.opacity = a;
    s.visible = true;
  }
  for (let i = fi; i < flashPool.length; i++) flashPool[i].visible = false;
}
/* particles via THREE.Points (positions rebuilt per frame) */
const MAXP = 1600;
const partGeo = new THREE.BufferGeometry();
const partPos = new Float32Array(MAXP * 3);
const partCol = new Float32Array(MAXP * 3);
partGeo.setAttribute("position", new THREE.BufferAttribute(partPos, 3));
partGeo.setAttribute("color", new THREE.BufferAttribute(partCol, 3));
const partPts = new THREE.Points(partGeo, new THREE.PointsMaterial({
  size: 0.16, vertexColors: true, transparent: true, opacity: 0.9,
  depthWrite: false, sizeAttenuation: true }));
partPts.frustumCulled = false; partPts.renderOrder = 7;
scene.add(partPts);
const _pc = new THREE.Color();
function drawParticles() {
  let n = 0;
  for (const pt of particles) {
    if (n >= MAXP) break;
    const a = 1 - pt.t / pt.life;
    partPos[n * 3] = pt.x; partPos[n * 3 + 1] = pt.y;
    partPos[n * 3 + 2] = 0.25 + (pt.s || 2) * 0.04 + pt.t * (pt.g < 0 ? 0.6 : -0.1);
    _pc.set(pt.c).multiplyScalar(a);
    partCol[n * 3] = _pc.r; partCol[n * 3 + 1] = _pc.g; partCol[n * 3 + 2] = _pc.b;
    n++;
  }
  partGeo.setDrawRange(0, n);
  partGeo.attributes.position.needsUpdate = true;
  partGeo.attributes.color.needsUpdate = true;
}
const ringPool = [];
const fxRingGeo = new THREE.RingGeometry(0.9, 1, 28);
function drawRings() {
  let ri = 0;
  for (const r of rings) {
    let m = ringPool[ri];
    if (!m) { m = new THREE.Mesh(fxRingGeo, addMat(0xffd27a, 1)); m.renderOrder = 6; scene.add(m); ringPool.push(m); }
    ri++;
    const f = r.t / r.max;
    const rad = r.emp ? f * 5 : f * 0.55;
    m.position.set(r.x, r.y, 0.06);
    m.scale.set(rad, rad, 1);
    m.material.color.set(r.emp ? 0x7ad8ff : 0xffd27a);
    m.material.opacity = (1 - f) * 0.85;
    m.visible = rad > 0.01;
  }
  for (let i = ri; i < ringPool.length; i++) ringPool[i].visible = false;
}
const ionPool = [];
function drawIon() {
  let ii = 0;
  for (const st of ionStrikes) {
    let m = ionPool[ii];
    if (!m) {
      m = new THREE.Mesh(new THREE.CylinderGeometry(1, 1, 40, 12, 1, true), addMat(0xbfeaff, 1));
      m.geometry.rotateX(Math.PI / 2);
      m.renderOrder = 8; scene.add(m);
      m.userData.light = new THREE.PointLight(0x9fdcff, 0, 14);
      scene.add(m.userData.light);
      ionPool.push(m);
    }
    ii++;
    const f = Math.min(1, st.t / 0.7);
    const w = st.t < 0.7 ? 0.12 + f * 0.4 : Math.max(0.01, 0.55 * (1 - (st.t - 0.7) / 0.9));
    m.position.set(st.x, st.y, 20);
    m.scale.set(w, w, 1);
    m.material.opacity = st.t < 0.7 ? 0.55 + f * 0.4 : (1 - (st.t - 0.7) / 0.9);
    m.visible = true;
    m.userData.light.position.set(st.x, st.y, 2.5);
    m.userData.light.intensity = st.t < 0.7 ? f * 2.2 : Math.max(0, 2.8 * (1 - (st.t - 0.7) / 0.9));
  }
  for (let i = ii; i < ionPool.length; i++) { ionPool[i].visible = false; ionPool[i].userData.light.intensity = 0; }
}

/* ---------- placement ghost ---------- */
const ghostPool = [];
const ghostGeo = new THREE.PlaneGeometry(0.96, 0.96);
function drawGhost() {
  let gi = 0;
  placingSpot = null;
  if (placing) {
    const [wx, wy] = toWorld(mouseX, mouseY);
    const b = STRUCTS[placing];
    const gx = Math.round(wx - b.fw / 2), gy = Math.round(wy - b.fh / 2);
    const ok = canPlace(placing, gx, gy);
    for (let y = gy; y < gy + b.fh; y++) for (let x = gx; x < gx + b.fw; x++) {
      if (!inMap(x, y)) continue;
      let m = ghostPool[gi];
      if (!m) { m = new THREE.Mesh(ghostGeo, addMat(0x37e86e, 0.4)); m.renderOrder = 6; scene.add(m); ghostPool.push(m); }
      gi++;
      m.position.set(x + 0.5, y + 0.5, 0.07);
      m.material.color.set(ok ? 0x37e86e : 0xd8404a);
      m.visible = true;
    }
    placingSpot = [gx, gy, ok];
  }
  for (let i = gi; i < ghostPool.length; i++) ghostPool[i].visible = false;
}

/* ---------- HUD overlay (hp bars, boxes, reticle, wrench) ---------- */
function drawHud() {
  hudctx.clearRect(0, 0, vw, vh);
  for (const e of ents) {
    if (e.dead || entHidden(e)) continue;
    const show = e.sel || e.hp < e.maxhp;
    const isStruct = e.kind === "struct";
    if (show) {
      const zTop = isStruct ? Math.max(e.fw, e.fh) * 0.9 + 0.4
        : (UNITS[e.type].flying ? 1.9 : (UNITS[e.type].infantry ? 0.85 : 1.0));
      const [sx, sy] = toScreen(e.x, e.y, zTop);
      const w = (isStruct ? 46 : 26) * Math.min(zoom, 1.6);
      const frac = Math.max(0, e.hp / e.maxhp);
      hudctx.fillStyle = "rgba(0,0,0,0.6)";
      hudctx.fillRect(sx - w / 2, sy, w, 4);
      hudctx.fillStyle = frac > 0.55 ? "#37e86e" : frac > 0.25 ? "#ffc94d" : "#d8404a";
      hudctx.fillRect(sx - w / 2, sy, w * frac, 4);
    }
    if (isStruct && e.repairing && ((tSec * 2) | 0) % 2 === 0) {
      const [sx, sy] = toScreen(e.x, e.y, Math.max(e.fw, e.fh) * 0.75);
      hudctx.font = "17px sans-serif";
      hudctx.fillText("🔧", sx - 8, sy);
    }
  }
  if (selBox) {
    hudctx.strokeStyle = "rgba(55,232,110,0.9)"; hudctx.lineWidth = 1;
    hudctx.strokeRect(selBox.x1, selBox.y1, selBox.x2 - selBox.x1, selBox.y2 - selBox.y1);
  }
  if (superAim) {
    const [wx, wy] = toWorld(mouseX, mouseY);
    const r = superAim === "emp" ? 5 : 3.6;
    hudctx.strokeStyle = superAim === "emp" ? "rgba(122,216,255,0.9)" : "rgba(255,240,180,0.9)";
    hudctx.lineWidth = 2; hudctx.setLineDash([7, 5]);
    hudctx.beginPath();
    for (let a = 0; a <= 40; a++) {
      const [px, py] = toScreen(wx + Math.cos(a / 20 * Math.PI) * r, wy + Math.sin(a / 20 * Math.PI) * r, 0);
      a === 0 ? hudctx.moveTo(px, py) : hudctx.lineTo(px, py);
    }
    hudctx.stroke(); hudctx.setLineDash([]);
  }
}

/* ---------- sidebar icons: tiny offline renders of the real models ---------- */
const ICONS3D = {};
let iconRT = null;
function iconFor(type, q) {
  const key = q + ":" + type;
  if (ICONS3D[key]) return ICONS3D[key];
  if (!iconRT) {
    iconRT = { c: document.createElement("canvas"), r: null };
    iconRT.c.width = 120; iconRT.c.height = 96;
    iconRT.r = new THREE.WebGLRenderer({ canvas: iconRT.c, antialias: true, alpha: true, preserveDrawingBuffer: true });
    iconRT.r.outputEncoding = THREE.sRGBEncoding;
    iconRT.scene = new THREE.Scene();
    const dl = new THREE.DirectionalLight(0xffeecb, 1.2); dl.position.set(-2, -3, 4);
    iconRT.scene.add(dl);
    iconRT.scene.add(new THREE.HemisphereLight(0xcfe8ff, 0x7a6a45, 0.8));
    iconRT.cam = new THREE.PerspectiveCamera(32, 120 / 96, 0.1, 50);
    iconRT.cam.up.set(0, 0, 1);
  }
  let built;
  try {
    built = q === "structure" ? STRUCT_BUILDERS[type](H) : UNIT_BUILDERS[type](H, 0);
  } catch (err) { return ""; }
  H.fitTo(built.root, 1);
  iconRT.scene.add(built.root);
  const d = 2.1;
  iconRT.cam.position.set(d * 0.8, -d, d * 0.78);
  iconRT.cam.lookAt(0, 0, 0.3);
  iconRT.r.render(iconRT.scene, iconRT.cam);
  const url = iconRT.c.toDataURL();
  iconRT.scene.remove(built.root);
  ICONS3D[key] = url;
  return url;
}

/* ---------- main per-frame entry (same name as the old 2D draw) ---------- */
let _lastT = performance.now();
function draw() {
  const now = performance.now();
  const dt = Math.min(0.06, (now - _lastT) / 1000);
  _lastT = now;
  if (worldVersion !== builtWorldVersion) buildWorld();
  crystalTimer -= dt;
  if (crystalTimer <= 0) { crystalTimer = 2; rebuildCrystals(); }
  fogTimer -= dt;
  if (fogTimer <= 0) { fogTimer = 0.15; updateFog(); }
  if (waterMat) {
    waterMat.emissiveIntensity = 0.4 + Math.sin(tSec * 1.4) * 0.18;
    waterBump.offset.set(tSec * 0.014, tSec * 0.009);   // drifting ripples
  }
  updateCamera();
  syncEnts(dt);
  syncCorpses();
  drawBeams(); drawProjectiles(); drawFlashes(); drawParticles(); drawRings(); drawIon(); drawGhost();
  renderer.render(scene, camera);
  drawHud();
  drawMinimap();
}
