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
renderer.toneMappingExposure = 1.12;
renderer.physicallyCorrectLights = false;
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
noiseCanvas.width = noiseCanvas.height = 512;
{
  // smooth multi-octave detail: each octave is drawn coarse then blurred,
  // so the grain reads as soft natural soil, not blocky pixels
  const g = noiseCanvas.getContext("2d");
  g.fillStyle = "#b6b1a4"; g.fillRect(0, 0, 512, 512);
  const tmp = document.createElement("canvas"); tmp.width = tmp.height = 512;
  const tg = tmp.getContext("2d");
  for (const [n, a, blur] of [[10, 0.4, 10], [22, 0.28, 6], [48, 0.2, 3], [110, 0.13, 1.5]]) {
    tg.clearRect(0, 0, 512, 512);
    const cell = 512 / n;
    for (let y = -1; y <= n; y++) for (let x = -1; x <= n; x++) {
      const v = 132 + (Math.random() * 96 | 0);
      tg.fillStyle = `rgb(${v},${v - 6},${v - 14})`;
      tg.fillRect(x * cell, y * cell, cell + 1, cell + 1);
    }
    g.globalAlpha = a;
    g.filter = `blur(${blur}px)`;
    g.drawImage(tmp, 0, 0);
  }
  g.filter = "none"; g.globalAlpha = 1;
}
const maxAniso = renderer.capabilities.getMaxAnisotropy();
const groundTex = new THREE.CanvasTexture(noiseCanvas);
groundTex.wrapS = groundTex.wrapT = THREE.RepeatWrapping;
groundTex.encoding = THREE.sRGBEncoding;
groundTex.anisotropy = maxAniso;
groundTex.generateMipmaps = true;
groundTex.minFilter = THREE.LinearMipmapLinearFilter;
// realistic water: a seamless, animated ripple NORMAL map built from layered
// sine waves (finite-difference normals). Two copies scroll in different
// directions for a convincing living surface.
function makeRippleNormal() {
  const S = 256, c = document.createElement("canvas"); c.width = c.height = S;
  const g = c.getContext("2d"), img = g.createImageData(S, S), TAU = Math.PI * 2;
  const hAt = (x, y) =>
    Math.sin((x * 3 + y * 2) / S * TAU) +
    Math.sin((x * 5 - y * 4) / S * TAU) * 0.7 +
    Math.sin((x * 9 + y * 8) / S * TAU) * 0.4 +
    Math.sin((x * 14 - y * 11) / S * TAU) * 0.22;
  for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) {
    const dx = hAt(x + 1, y) - hAt(x - 1, y);
    const dy = hAt(x, y + 1) - hAt(x, y - 1);
    let nx = -dx * 0.5, ny = -dy * 0.5, nz = 1;
    const L = Math.hypot(nx, ny, nz);
    const o = (y * S + x) * 4;
    img.data[o] = (nx / L * 0.5 + 0.5) * 255;
    img.data[o + 1] = (ny / L * 0.5 + 0.5) * 255;
    img.data[o + 2] = (nz / L * 0.5 + 0.5) * 255;
    img.data[o + 3] = 255;
  }
  g.putImageData(img, 0, 0);
  const t = new THREE.CanvasTexture(c);
  t.wrapS = t.wrapT = THREE.RepeatWrapping;
  t.anisotropy = maxAniso;
  return t;
}
const waterNorm = makeRippleNormal();

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

/* Merge every mesh in a group into one mesh per material (transforms
   baked in). Slashes draw calls — the difference between 15 and 60 FPS
   on mobile GPUs. */
function mergeGroupStatic(root) {
  root.updateMatrixWorld(true);
  const byMat = new Map();
  root.traverse(o => {
    if (!o.isMesh) return;
    const arr = byMat.get(o.material) || [];
    arr.push(o);
    byMat.set(o.material, arr);
  });
  const merged = new THREE.Group();
  const v = new THREE.Vector3(), n = new THREE.Vector3(), nm = new THREE.Matrix3();
  for (const [mat, meshes] of byMat) {
    const posArr = [], nrmArr = [], idxArr = [];
    let base = 0;
    for (const m of meshes) {
      const g = m.geometry, p = g.attributes.position, no = g.attributes.normal;
      nm.getNormalMatrix(m.matrixWorld);
      for (let i = 0; i < p.count; i++) {
        v.fromBufferAttribute(p, i).applyMatrix4(m.matrixWorld);
        posArr.push(v.x, v.y, v.z);
        n.fromBufferAttribute(no, i).applyMatrix3(nm).normalize();
        nrmArr.push(n.x, n.y, n.z);
      }
      const idx = g.index;
      if (idx) for (let i = 0; i < idx.count; i++) idxArr.push(idx.getX(i) + base);
      else for (let i = 0; i < p.count; i++) idxArr.push(i + base);
      base += p.count;
    }
    const gg = new THREE.BufferGeometry();
    gg.setAttribute("position", new THREE.Float32BufferAttribute(posArr, 3));
    gg.setAttribute("normal", new THREE.Float32BufferAttribute(nrmArr, 3));
    gg.setIndex(idxArr);
    const mm = new THREE.Mesh(gg, mat);
    mm.castShadow = true; mm.receiveShadow = true;
    merged.add(mm);
  }
  return merged;
}

const camera = new THREE.PerspectiveCamera(38, 1, 0.5, 400);
camera.up.set(0, 0, 1);
let camYaw = Math.PI / 4;             // Q/E orbit
let camPitch = 0.86;                  // R/F tilt (radians above horizon)

const hud = document.getElementById("hud");
const hudctx = hud.getContext("2d");

const IS_MOBILE = window.matchMedia && matchMedia("(pointer: coarse)").matches;
const sun = new THREE.DirectionalLight(0xffe6b8, 1.0);
sun.castShadow = true;
sun.shadow.mapSize.set(IS_MOBILE ? 1024 : 2048, IS_MOBILE ? 1024 : 2048);
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

/* ---------- edge-scroll: push the mouse to a screen edge to pan the camera
   (classic RTS). Respects the top bar, bottom bar and the right sidebar so
   the playfield edges — not the panels — are what trigger the scroll. ---------- */
let edgePanOn = false;
canvas.addEventListener("mousemove", () => { edgePanOn = true; });
canvas.addEventListener("mouseleave", () => { edgePanOn = false; });
function edgeScroll(dt) {
  if (IS_MOBILE || !edgePanOn || typeof running === "undefined" || !running || over) return;
  const M = 22;                                   // edge trigger band (px)
  const collapsed = document.body.classList.contains("sb-collapsed");
  const sbW = collapsed ? 0 : (vw <= 760 ? 178 : 216);
  const top = 42, bot = 34;                        // top/bottom HUD bars
  const rightEdge = vw - sbW;
  let ex = 0, ey = 0;
  if (mouseX < M) ex = -1; else if (mouseX > rightEdge - M && mouseX <= rightEdge) ex = 1;
  if (mouseY < top + M && mouseY > top - 4) ey = -1; else if (mouseY > vh - bot - M) ey = 1;
  if (!ex && !ey) return;
  const pan = 24 * dt / Math.max(zoom, 0.7);
  const cy = Math.cos(camYaw), sy = Math.sin(camYaw);
  if (ey < 0) { camX -= cy * pan; camY -= sy * pan; }        // up = camera-forward
  else if (ey > 0) { camX += cy * pan; camY += sy * pan; }   // down
  if (ex < 0) { camX -= sy * pan; camY += cy * pan; }        // left
  else if (ex > 0) { camX += sy * pan; camY -= cy * pan; }   // right
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
let waterMesh = null, waterGeoBase = null;
let propDefs = [], propMesh = null, builtExplored = -1, exploredCount = 0;
function rebuildProps() {
  if (propMesh) {
    worldGroup.remove(propMesh);
    propMesh.traverse(o => { if (o.isMesh) o.geometry.dispose(); });
  }
  const g = new THREE.Group();
  for (const d of propDefs) {
    if (!explored[d.cell]) continue;
    if (d.mesa) {
      H.box(g, d.x, d.y, d.mesa / 2, 0.96, 0.96, d.mesa, 0x3d3322);
      continue;
    }
    const b = PROP_BUILDERS[d.kind](H, d.v);
    H.fitTo(b.root, b.fit * (typeof d.fit === "number" ? d.fit : 1));
    b.root.position.x += d.x; b.root.position.y += d.y;
    b.root.position.z += (d.z || 0) + heightAt(d.x, d.y);
    g.add(b.root);
  }
  propMesh = mergeGroupStatic(g);
  worldGroup.add(propMesh);
  builtExplored = exploredCount;
}
let builtWorldVersion = -1;
// fog is authored at cell resolution (fogSrc) then upsampled + blurred onto a
// larger canvas so the shroud boundary is a soft feather, never jagged pixels
const fogSrc = document.createElement("canvas");
fogSrc.width = fogSrc.height = MAP;
const fogCanvas = document.createElement("canvas");
fogCanvas.width = fogCanvas.height = Math.min(1024, MAP * 6);
const fogTex = new THREE.CanvasTexture(fogCanvas);
fogTex.magFilter = THREE.LinearFilter;
fogTex.minFilter = THREE.LinearFilter;
fogTex.generateMipmaps = false;
let fogMesh = null, fogTimer = 0;

const T_COLORS = [0x5a4c31, 0x4a3d27, 0x4e4129, 0x1d5a72];

// The reachable map is an island of light: everything beyond the play field
// sinks into darkness (a dark void floor + a low, dark, distant mountain
// silhouette) so the world never looks like reachable terrain that's cut off.
let surround = null;
function buildSurroundings() {
  if (surround) { scene.remove(surround); surround.traverse(o => { if (o.isMesh) o.geometry.dispose(); }); }
  surround = new THREE.Group();
  const C = MAP / 2, EXT = MAP * 3;

  // A clean dark void beyond the reachable map — nothing unreachable is drawn,
  // so the playable field reads as an island of light bounded by darkness
  // (the same navy as the fog shroud, blended into the scene fog at distance).
  const outer = new THREE.Mesh(
    new THREE.PlaneGeometry(EXT * 2, EXT * 2, 1, 1),
    new THREE.MeshBasicMaterial({ color: 0x070a10, fog: true }));
  outer.position.set(C, C, -0.34);             // just below the play field
  surround.add(outer);

  scene.add(surround);
}

function buildWorld() {
  if (worldGroup) scene.remove(worldGroup);
  if (crystalGroup) scene.remove(crystalGroup);
  worldGroup = new THREE.Group(); scene.add(worldGroup);

  // deterministic grass meadows: dirt cells that are near water or fall in
  // a few seeded patches read as green grassland (view-only, no sim effect)
  const grassCell = new Uint8Array(MAP * MAP);
  // distance (in cells, capped) to the nearest water tile — drives a lush,
  // graded green belt that hugs the river and lakes
  const waterDist = (x, y, rad) => {
    for (let r = 1; r <= rad; r++)
      for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++) {
        if (Math.max(Math.abs(dx), Math.abs(dy)) !== r) continue;   // ring only
        if (inMap(x + dx, y + dy) && terrain[idx(x + dx, y + dy)] === 3) return r;
      }
    return 99;
  };
  for (let y = 0; y < MAP; y++) for (let x = 0; x < MAP; x++) {
    const t = terrain[idx(x, y)];
    if (t !== 0 && t !== 2) continue;
    const wd = waterDist(x, y, 3);
    const oasis = wd <= 2;                                        // wide green belt hugging the water
    const patch = (Math.sin(x * 0.28) + Math.cos(y * 0.24) + Math.sin((x + y) * 0.13)) > 1.55;
    if (oasis || patch) grassCell[idx(x, y)] = oasis && wd === 1 ? 2 : 1;   // 2 = lush bank, 1 = meadow
  }

  // ---- per-cell base colour (soil/grass/rock/water-bed), later blended ----
  const cellCol = new Float32Array(MAP * MAP * 3);
  const cc = new THREE.Color();
  for (let y = 0; y < MAP; y++) for (let x = 0; x < MAP; x++) {
    const i = idx(x, y), t = terrain[i], hc = height[i];
    let hex = grassCell[i] === 2 ? 0x4f8f2e : grassCell[i] ? 0x527f34 : T_COLORS[t];   // lush bank / meadow / soil
    if (t === 3) hex = 0x24485a;                    // submerged bed (water drawn on top)
    else if (hc > 1.6) hex = 0x6d6455;              // rocky peaks
    else if (hc > 0.7) hex = grassCell[i] ? 0x5b6a3a : 0x5f5334;   // upper slopes
    cc.setHex(hex);
    const shade = 0.94 + ((x * 13 + y * 29) % 11) / 11 * 0.12;     // gentle variation
    // edge fade: the last few cells darken toward the void so the map reads
    // as an island of light, not terrain sliced off by a hard border
    const edge = Math.min(x, y, MAP - 1 - x, MAP - 1 - y);
    const ef = edge >= 4 ? 1 : 0.42 + 0.58 * (edge / 4);   // gentle vignette into the void
    const s = shade * ef;
    cellCol[i * 3] = cc.r * s; cellCol[i * 3 + 1] = cc.g * s; cellCol[i * 3 + 2] = cc.b * s;
  }
  // bilinear colour sample at any continuous point → smooth transitions
  const sampleCol = (wx, wy, out) => {
    const x = Math.max(0, Math.min(MAP - 1.001, wx - 0.5));
    const y = Math.max(0, Math.min(MAP - 1.001, wy - 0.5));
    const x0 = x | 0, y0 = y | 0, fx = x - x0, fy = y - y0;
    for (let c = 0; c < 3; c++) {
      const a = cellCol[(y0 * MAP + x0) * 3 + c], b = cellCol[(y0 * MAP + x0 + 1) * 3 + c];
      const d = cellCol[((y0 + 1) * MAP + x0) * 3 + c], e = cellCol[((y0 + 1) * MAP + x0 + 1) * 3 + c];
      out[c] = a * (1 - fx) * (1 - fy) + b * fx * (1 - fy) + d * (1 - fx) * fy + e * fx * fy;
    }
  };
  const waterAt = (wx, wy) => terrain[idx(Math.min(MAP - 1, wx | 0), Math.min(MAP - 1, wy | 0))] === 3;
  const hnoise = (x, y) => (Math.sin(x * 1.7 + y * 0.6) + Math.sin(x * 0.5 - y * 2.1)
    + Math.sin((x + y) * 1.1)) * 0.5;

  // ---- SMOOTH high-detail terrain: subdivided mesh, blended heights+colours ----
  const SUB = MAP >= 96 ? 2 : 3, GN = MAP * SUB, out3 = [0, 0, 0];   // coarser subdiv on big maps (perf)
  const pos = [], col = [], uvs = [], idxA = [];
  for (let iy = 0; iy <= GN; iy++) for (let ix = 0; ix <= GN; ix++) {
    const wx = ix / SUB, wy = iy / SUB;
    const water = waterAt(wx, wy);
    const z = (water ? -0.3 : 0) + heightAt(wx, wy) + (water ? 0 : hnoise(wx, wy) * 0.035);
    pos.push(wx, wy, z);
    sampleCol(wx, wy, out3);
    col.push(out3[0], out3[1], out3[2]);
    uvs.push(wx * 0.42, wy * 0.42);
  }
  const row = GN + 1;
  for (let iy = 0; iy < GN; iy++) for (let ix = 0; ix < GN; ix++) {
    const a = iy * row + ix, b = a + 1, c = a + row, d = c + 1;
    idxA.push(a, b, d, a, d, c);
  }
  const tg = new THREE.BufferGeometry();
  tg.setAttribute("position", new THREE.Float32BufferAttribute(pos, 3));
  tg.setAttribute("color", new THREE.Float32BufferAttribute(col, 3));
  tg.setAttribute("uv", new THREE.Float32BufferAttribute(uvs, 2));
  tg.setIndex(idxA);
  tg.computeVertexNormals();                                // smooth slope shading
  const terr = new THREE.Mesh(tg, new THREE.MeshStandardMaterial({
    vertexColors: true, map: groundTex, roughness: 0.95, metalness: 0 }));
  terr.receiveShadow = true;
  worldGroup.add(terr);

  // water surface: a smooth, realistic body of water. The mesh is DILATED one
  // cell past the shoreline and tucked UNDER the (higher, smoothly-subdivided)
  // banks, so the visible waterline follows the smooth terrain instead of hard
  // cell steps; depth is sampled per-vertex for a seamless shallow→deep gradient.
  const isWater = (x, y) => inMap(x, y) && terrain[idx(x, y)] === 3;
  const nearWater = (x, y) => {
    for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++)
      if (isWater(x + dx, y + dy)) return true;
    return false;
  };
  // smooth depth field: cells to the nearest shore (0 at/over land), for
  // per-vertex bilinear sampling → no concentric "contour ring" banding
  const depthField = new Float32Array(MAP * MAP);
  for (let y = 0; y < MAP; y++) for (let x = 0; x < MAP; x++) {
    if (!isWater(x, y)) continue;
    let d = 6;
    for (let r = 1; r <= 5; r++) {
      let hit = false;
      for (let dy = -r; dy <= r && !hit; dy++) for (let dx = -r; dx <= r; dx++) {
        if (Math.max(Math.abs(dx), Math.abs(dy)) !== r) continue;
        if (!isWater(x + dx, y + dy)) { hit = true; break; }
      }
      if (hit) { d = r; break; }
    }
    depthField[idx(x, y)] = d;
  }
  const sampleDepth = (wx, wy) => {
    const x = Math.max(0, Math.min(MAP - 1.001, wx - 0.5)), y = Math.max(0, Math.min(MAP - 1.001, wy - 0.5));
    const x0 = x | 0, y0 = y | 0, fx = x - x0, fy = y - y0;
    const a = depthField[y0 * MAP + x0], b = depthField[y0 * MAP + x0 + 1];
    const c = depthField[(y0 + 1) * MAP + x0], e = depthField[(y0 + 1) * MAP + x0 + 1];
    return a * (1 - fx) * (1 - fy) + b * fx * (1 - fy) + c * (1 - fx) * fy + e * fx * fy;
  };
  const wpos = [], wuv = [], wcol = [], widx = [];
  const WSUB = 2; let wv = 0;
  const cShallow = new THREE.Color(0x49b6d0), cDeep = new THREE.Color(0x114063),
        cFoam = new THREE.Color(0xd6eef2), _wc = new THREE.Color();
  for (let y = 0; y < MAP; y++) for (let x = 0; x < MAP; x++) {
    if (!nearWater(x, y)) continue;            // dilated one cell under the banks
    const base = wv;
    for (let sy = 0; sy <= WSUB; sy++) for (let sx = 0; sx <= WSUB; sx++) {
      const wx = x + sx / WSUB, wy = y + sy / WSUB;
      const dep = Math.min(1, sampleDepth(wx, wy) / 4);
      _wc.copy(cShallow).lerp(cDeep, dep);
      if (dep < 0.14) _wc.lerp(cFoam, (0.14 - dep) / 0.14 * 0.5);     // soft foam at the waterline
      const edge = Math.min(wx, wy, MAP - wx, MAP - wy);
      if (edge < 4) _wc.multiplyScalar(0.35 + 0.65 * (edge / 4));
      wpos.push(wx, wy, -0.05);
      wuv.push(wx * 0.5, wy * 0.5);
      wcol.push(_wc.r, _wc.g, _wc.b);
      wv++;
    }
    const r = WSUB + 1;
    for (let sy = 0; sy < WSUB; sy++) for (let sx = 0; sx < WSUB; sx++) {
      const a = base + sy * r + sx;
      widx.push(a, a + 1, a + r + 1, a, a + r + 1, a + r);
    }
  }
  if (wpos.length) {
    waterGeoBase = new Float32Array(wpos);
    const wg = new THREE.BufferGeometry();
    wg.setAttribute("position", new THREE.Float32BufferAttribute(wpos.slice(), 3));
    wg.setAttribute("uv", new THREE.Float32BufferAttribute(wuv, 2));
    wg.setAttribute("color", new THREE.Float32BufferAttribute(wcol, 3));
    wg.setIndex(widx); wg.computeVertexNormals();
    waterMat = new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.05,
      metalness: 0.45, transparent: true, opacity: 0.9, emissive: 0x0c3a58, emissiveIntensity: 0.3,
      normalMap: waterNorm, normalScale: new THREE.Vector2(0.55, 0.55), envMapIntensity: 1.9 });
    waterMesh = new THREE.Mesh(wg, waterMat);
    waterMesh.renderOrder = 1;
    worldGroup.add(waterMesh);
  } else { waterMesh = null; waterGeoBase = null; }

  // props are collected as build recipes and rebuilt as ONE merged mesh
  // per material whenever new terrain is scouted (fog-aware + fast)
  propDefs = [];
  // rocky mountains: the heightfield already raises them; crown them with
  // boulders (bigger on taller cells) for craggy detail
  for (let y = 0; y < MAP; y++) for (let x = 0; x < MAP; x++) {
    if (terrain[idx(x, y)] !== 1) continue;
    if ((x * 5 + y * 7) % 2 !== 0) continue;                          // thin out for perf
    const hc = height[idx(x, y)];
    propDefs.push({ kind: "rock", v: (x * 3 + y) % 8,
      fit: 0.7 + Math.min(1.4, hc * 0.4) + ((x + y) % 3) * 0.12,
      x: x + 0.4 + ((x * 7 + y) % 4) * 0.06, y: y + 0.4 + ((y * 7 + x) % 4) * 0.06,
      z: 0, cell: idx(x, y) });
  }
  for (const pr of decor) {
    const kind = pr.flora ? (pr.v % 3 === 0 ? "bush" : "cactus") : "rock";
    propDefs.push({ kind, v: pr.v % 8, x: pr.x, y: pr.y, z: 0,
      cell: idx(Math.min(MAP - 1, pr.x | 0), Math.min(MAP - 1, pr.y | 0)) });
  }
  // grass tufts scattered across the meadow cells
  for (let y = 0; y < MAP; y++) for (let x = 0; x < MAP; x++) {
    if (!grassCell[idx(x, y)]) continue;
    if ((x * 7 + y * 13) % 3 !== 0) continue;                       // ~1/3 density
    propDefs.push({ kind: "grass", v: (x * 5 + y * 3) % 8,
      x: x + 0.3 + ((x * 11 + y) % 5) * 0.1, y: y + 0.3 + ((y * 11 + x) % 5) * 0.1,
      z: 0, cell: idx(x, y) });
  }
  builtExplored = -1;   // force a props rebuild

  // ---- surroundings: no square edge — the world blends into an endless
  // desert ringed by distant mountains, with a large lake off to one side.
  buildSurroundings();

  // fog-of-war overlay: a grid that DRAPES over the terrain relief so unexplored
  // ground is SOLID black — no mountains/hills/meadows/water leak through. The
  // grid matches the terrain subdivision and each vertex is lifted to the MAX
  // height of its neighbourhood so even sharp peaks stay fully covered.
  if (fogMesh) scene.remove(fogMesh);
  const fmat = new THREE.MeshBasicMaterial({ map: fogTex, transparent: true, depthWrite: false });
  const FSEG = MAP * SUB;                       // match the terrain mesh resolution
  const fgeo = new THREE.PlaneGeometry(MAP, MAP, FSEG, FSEG);
  const fp = fgeo.attributes.position;
  const maxH = (wx, wy) => {                    // highest terrain within ~1 cell → covers peaks
    let m = 0;
    for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++)
      m = Math.max(m, heightAt(wx + dx, wy + dy));
    return m;
  };
  for (let i = 0; i < fp.count; i++) {
    const wx = fp.getX(i) + MAP / 2, wy = fp.getY(i) + MAP / 2;
    fp.setZ(i, maxH(wx, wy) + 0.35);
  }
  fp.needsUpdate = true; fgeo.computeVertexNormals();
  fogMesh = new THREE.Mesh(fgeo, fmat);
  fogMesh.position.set(MAP / 2, MAP / 2, 0);
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
      if ((x * 7 + y * 5) % 3 !== 0) continue;   // thin the halos (~1/3) — keeps the glow field cheap on big maps
      const s = glowSprite(color, 1.6, 0.16);
      s.position.set(x + 0.5, y + 0.5, 0.4);
      crysGlow.add(s);
    }
  }
  crystalGroup.add(crysGlow);
}

function updateFog() {
  // author the tri-state shroud at cell resolution: unexplored (opaque),
  // explored-but-unseen (dim haze), currently-visible (clear)
  const sctx = fogSrc.getContext("2d");
  const img = sctx.createImageData(MAP, MAP);
  let count = 0;
  for (let y = 0; y < MAP; y++) for (let x = 0; x < MAP; x++) {
    const i = idx(x, y), o = i * 4;
    if (explored[i]) count++;
    // unexplored = solid black (heights, water, everything hidden until scouted);
    // explored-but-unseen = dimmed; currently-visible = clear
    img.data[o] = 3; img.data[o + 1] = 4; img.data[o + 2] = 6;
    img.data[o + 3] = !explored[i] ? 255 : (!visible[i] ? 120 : 0);
  }
  exploredCount = count;
  sctx.putImageData(img, 0, 0);
  // upsample + blur onto the display canvas → a soft feathered edge, no jaggies
  const fctx = fogCanvas.getContext("2d");
  const D = fogCanvas.width;
  fctx.clearRect(0, 0, D, D);
  fctx.imageSmoothingEnabled = true;
  fctx.imageSmoothingQuality = "high";
  fctx.filter = "blur(" + (D / MAP * 0.85).toFixed(2) + "px)";     // feather ~1 cell wide
  fctx.drawImage(fogSrc, 0, 0, D, D);
  fctx.filter = "none";
  fogTex.needsUpdate = true;
}

/* ---------- entity view objects ---------- */
const views = new Map();     // ent id -> view record
// change the player's team colour: rebuild unit views + sidebar icons so the
// new accent takes effect immediately
function setTeamColor(hex) {
  TEAM_COLOR = hex;
  for (const [, v] of views) scene.remove(v.wrap);
  views.clear();
  for (const k in ICONS3D) delete ICONS3D[k];
  if (typeof buildButtons === "function") buildButtons();
}
// shared faded-gray material used to tint powered-down buildings
const GRAY_MAT = new THREE.MeshStandardMaterial({ color: 0x44443f, roughness: 1, metalness: 0 });
const selRingGeo = new THREE.RingGeometry(0.68, 0.8, 24);
function makeSelRing(color) {
  // depthTest:false → the selection ring is never hidden by terrain or other
  // objects; it always draws on top so the marking stays fully visible
  const m = new THREE.Mesh(selRingGeo, new THREE.MeshBasicMaterial({
    color, transparent: true, opacity: 0.9, depthWrite: false, depthTest: false }));
  m.renderOrder = 12;
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
  // enemies are shown anywhere EXPLORED (so you can see them coming), not just
  // in live sight — but stealth units stay cloaked until detected
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
      v.wrap.position.set(e.x, e.y, STRUCTS[e.type].bridge ? 0 : heightAt(e.x, e.y));
      if (e.hitFlash > 0) e.hitFlash = Math.max(0, e.hitFlash - dt);   // "under attack" red pulse timer
      // faded gray when this building has no power (see updatePower)
      if (v.unpow !== !!e.unpowered) {
        v.unpow = !!e.unpowered;
        v.wrap.traverse(o => {
          if (!o.isMesh || !o.material || !o.material.isMeshStandardMaterial) return;
          if (!o.userData.pm) o.userData.pm = o.material;
          o.material = v.unpow ? GRAY_MAT : o.userData.pm;
        });
      }
      if (v.spin) {
        if (v.type === "dm_gate") {   // barrier slides down into the ground when open
          const tz = e.gateOpen ? -0.62 : 0;
          v.spin.position.z += (tz - v.spin.position.z) * Math.min(1, dt * 5);
        } else if (!e.unpowered) v.spin.rotation.z += dt * 0.7;   // dead buildings stop spinning
      }
      v.ring.visible = !!e.sel;
      continue;
    }
    const b = UNITS[e.type];
    const alt = b.flying ? (b.infantry ? 0.55 : 1.15) + Math.sin(e.anim * 1.6) * 0.07 : 0;
    const bob = b.walker ? Math.abs(Math.sin(e.anim * 4)) * 0.045 : 0;
    const gc = idx(Math.min(MAP - 1, e.x | 0), Math.min(MAP - 1, e.y | 0));
    const gz = bridgeCells[gc] ? 0.5 : heightAt(e.x, e.y);   // ride the bridge deck
    v.wrap.position.set(e.x, e.y, gz + alt + bob);
    v.wrap.rotation.z = (e.face || 0) - Math.PI / 2;
    if (v.p0) {   // infantry leg swing
      const moving = !!e.path;
      const p1 = moving && Math.floor(e.anim * 2.4) % 2 === 1;
      v.p0.visible = !p1; v.p1.visible = p1;
    }
    if (v.turret) {
      if (v.type === "so_harpy" || v.type === "dm_transport") v.turret.rotation.z += dt * (v.type === "dm_transport" ? 20 : 26);   // rotor
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
    if (e.sel) v.ring.material.opacity = 0.6 + Math.sin(tSec * 5.5) * 0.25;
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
      g.position.set(c.x, c.y, 0.02 + heightAt(c.x, c.y));
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
    const p1 = new THREE.Vector3(bm.x1, bm.y1, 0.55 + (bm.thick ? 0.7 : 0) + heightAt(bm.x1, bm.y1));
    const p2 = new THREE.Vector3(bm.x2, bm.y2, 0.35 + heightAt(bm.x2, bm.y2));
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
    m.position.set(p.x, p.y, hh + heightAt(p.x, p.y));
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
    s.position.set(f.x, f.y, 0.55 + heightAt(f.x, f.y));
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
const fireballPool = [];
function drawFireballs() {
  let fi = 0;
  for (const f of fireballs) {
    let rec = fireballPool[fi];
    if (!rec) {
      rec = { glow: new THREE.Sprite(billboardMat(1)), core: glowSprite(0xfff2c0, 1, 1) };
      rec.glow.renderOrder = 8; rec.core.renderOrder = 8;
      scene.add(rec.glow); scene.add(rec.core);
      fireballPool.push(rec);
    }
    fi++;
    const prog = f.t / f.life;
    const s = f.s * (0.8 + prog * 2.1);
    rec.glow.position.set(f.x, f.y, 0.45 + prog * 0.5);
    rec.glow.scale.set(s, s, 1);
    rec.glow.material.opacity = Math.pow(1 - prog, 1.4) * 0.9;
    rec.core.position.set(f.x, f.y, 0.45 + prog * 0.4);
    rec.core.scale.set(s * 0.45, s * 0.45, 1);
    rec.core.material.opacity = Math.pow(1 - prog, 2.2);
    rec.glow.visible = rec.core.visible = true;
  }
  for (let i = fi; i < fireballPool.length; i++) {
    fireballPool[i].glow.visible = false;
    fireballPool[i].core.visible = false;
  }
}
const fsPool = [];
const fsGeo = new THREE.CylinderGeometry(0.34, 0.42, 1.7, 8, 1, true);
fsGeo.rotateX(Math.PI / 2);
function drawFirestorm() {
  let fi = 0;
  if (fsActive > 0) {
    for (const em of ents) {
      if (em.dead || em.kind !== "struct" || !STRUCTS[em.type].fsem || em.owner !== 0) continue;
      let m = fsPool[fi];
      if (!m) {
        m = new THREE.Mesh(fsGeo, addMat(0x8fd8ff, 0.5));
        m.renderOrder = 7; scene.add(m); fsPool.push(m);
      }
      fi++;
      m.position.set(em.x, em.y, 0.95);
      m.material.opacity = 0.35 + Math.abs(Math.sin(tSec * 14 + em.gx * 2.1)) * 0.35;
      m.scale.setScalar(0.92 + Math.sin(tSec * 22 + em.gy) * 0.1);
      m.visible = true;
    }
  }
  for (let i = fi; i < fsPool.length; i++) fsPool[i].visible = false;
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
      if (!m) { m = new THREE.Mesh(ghostGeo, addMat(0x37e86e, 0.4)); scene.add(m); ghostPool.push(m); }
      m.material.depthTest = false; m.renderOrder = 20;   // always drawn ON TOP of the map, never sunk under it
      gi++;
      m.position.set(x + 0.5, y + 0.5, heightAt(x + 0.5, y + 0.5) + 0.18);
      m.material.color.set(ok ? 0x37e86e : 0xd8404a);
      m.visible = true;
    }
    placingSpot = [gx, gy, ok];
    // pending-confirm marker: a cell already clicked once, awaiting the confirm click (gold)
    if (typeof placeLock !== "undefined" && placeLock) {
      const okL = canPlace(placing, placeLock[0], placeLock[1]);
      for (let y = placeLock[1]; y < placeLock[1] + b.fh; y++) for (let x = placeLock[0]; x < placeLock[0] + b.fw; x++) {
        if (!inMap(x, y)) continue;
        let m = ghostPool[gi];
        if (!m) { m = new THREE.Mesh(ghostGeo, addMat(0x37e86e, 0.4)); scene.add(m); ghostPool.push(m); }
        m.material.depthTest = false; m.renderOrder = 21;
        gi++;
        m.position.set(x + 0.5, y + 0.5, heightAt(x + 0.5, y + 0.5) + 0.22);
        m.material.color.set(okL ? 0xf2bf26 : 0xd8404a);   // gold = marked, click again to confirm
        m.visible = true;
      }
    }
  }
  for (let i = gi; i < ghostPool.length; i++) ghostPool[i].visible = false;
}

/* ---------- HUD overlay (hp bars, boxes, reticle, wrench) ---------- */
function drawHud() {
  hudctx.clearRect(0, 0, vw, vh);
  // enemies currently marked for attack by any of the player's units
  const marked = new Set();
  for (const u of ents)
    if (!u.dead && u.owner === 0 && u.kind === "unit" && u.target && !u.target.dead && u.target.owner === 1)
      marked.add(u.target);
  // enemy under the cursor while attack-capable units are selected → preview the reticle on hover
  let hovered = null;
  if (typeof placing !== "undefined" && !placing && !superAim &&
      ents.some(u => u.sel && u.owner === 0 && u.kind === "unit" && UNITS[u.type] &&
                     (UNITS[u.type].weapon || UNITS[u.type].capture))) {
    const hv = typeof pickEntity === "function" ? pickEntity(mouseX, mouseY) : null;
    if (hv && !hv.dead && hv.owner === 1 && !entHidden(hv)) hovered = hv;
  }
  for (const e of ents) {
    if (e.dead || entHidden(e)) continue;
    const show = e.sel || e.hp < e.maxhp;
    const isStruct = e.kind === "struct";
    const gz = e.kind === "struct" ? heightAt(e.x, e.y) : heightAt(e.x, e.y);
    if (show) {
      const zTop = (isStruct ? Math.max(e.fw, e.fh) * 0.9 + 0.4
        : (UNITS[e.type].flying ? 1.9 : (UNITS[e.type].infantry ? 0.85 : 1.0))) + gz;
      const [sx, sy] = toScreen(e.x, e.y, zTop);
      const w = (isStruct ? 46 : 26) * Math.min(zoom, 1.6);
      const frac = Math.max(0, e.hp / e.maxhp);
      hudctx.fillStyle = "rgba(0,0,0,0.6)";
      hudctx.fillRect(sx - w / 2, sy, w, 4);
      hudctx.fillStyle = frac > 0.55 ? "#37e86e" : frac > 0.25 ? "#ffc94d" : "#d8404a";
      hudctx.fillRect(sx - w / 2, sy, w * frac, 4);
    }
    if (isStruct && e.repairing && ((tSec * 2) | 0) % 2 === 0) {
      const [sx, sy] = toScreen(e.x, e.y, Math.max(e.fw, e.fh) * 0.75 + gz);
      hudctx.font = "17px sans-serif";
      hudctx.fillText("🔧", sx - 8, sy);
    }
    // subtle red pulse (~3 flashes) when a base building is under attack
    if (isStruct && e.hitFlash > 0) {
      const pulse = Math.abs(Math.sin(e.hitFlash * Math.PI * 4));
      const [sx, sy] = toScreen(e.x, e.y, gz + 0.3);
      const rad = Math.max(e.fw, e.fh) * 22 * Math.min(zoom, 1.7);
      hudctx.strokeStyle = "rgba(255,46,58," + (0.25 + pulse * 0.6).toFixed(2) + ")";
      hudctx.lineWidth = 3;
      hudctx.beginPath(); hudctx.arc(sx, sy, rad, 0, Math.PI * 2); hudctx.stroke();
      hudctx.fillStyle = "rgba(255,46,58," + (pulse * 0.12).toFixed(2) + ")";
      hudctx.fill();
    }
    // RED attack reticle: on any enemy our units are targeting, or the one hovered
    if (marked.has(e) || e === hovered) {
      const [sx, sy] = toScreen(e.x, e.y, gz + (isStruct ? 0.3 : 0.2));
      const rad = (isStruct ? 26 : 15) * Math.min(zoom, 1.7);
      const rot = tSec * 1.6;
      hudctx.strokeStyle = "#ff2e3a"; hudctx.lineWidth = 2.2;
      hudctx.beginPath(); hudctx.arc(sx, sy, rad, 0, Math.PI * 2); hudctx.stroke();
      // rotating corner ticks + crosshair
      hudctx.lineWidth = 2.6;
      for (let k = 0; k < 4; k++) {
        const a = rot + k * Math.PI / 2;
        const ix = sx + Math.cos(a) * rad, iy = sy + Math.sin(a) * rad;
        const ox = sx + Math.cos(a) * (rad + 6), oy = sy + Math.sin(a) * (rad + 6);
        hudctx.beginPath(); hudctx.moveTo(ix, iy); hudctx.lineTo(ox, oy); hudctx.stroke();
      }
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

/* ---------- adaptive resolution: keep mobile GPUs at a smooth frame rate ---------- */
let resScale = 1, fpsEMA = 60, fpsCheck = 0;
function applyRes() {
  const cap = Math.min(IS_MOBILE ? 2 : 2.5, window.devicePixelRatio || 1);
  renderer.setPixelRatio(Math.max(0.55, cap * resScale));
}

/* ---------- main per-frame entry (same name as the old 2D draw) ---------- */
let _lastT = performance.now();
function draw() {
  const now = performance.now();
  const rawDt = (now - _lastT) / 1000;
  const dt = Math.min(0.06, rawDt);
  _lastT = now;
  if (rawDt > 0.0005) fpsEMA = fpsEMA * 0.93 + (1 / rawDt) * 0.07;
  fpsCheck -= dt;
  if (fpsCheck <= 0) {
    fpsCheck = 2.2;
    if (fpsEMA < 34 && resScale > 0.55) { resScale -= 0.15; applyRes(); }
    else if (fpsEMA > 56 && resScale < 1) { resScale += 0.1; applyRes(); }
  }
  if (worldVersion !== builtWorldVersion) buildWorld();
  crystalTimer -= dt;
  if (crystalTimer <= 0) {
    crystalTimer = 2;
    rebuildCrystals();
    if (exploredCount !== builtExplored) rebuildProps();
  }
  fogTimer -= dt;
  if (fogTimer <= 0) { fogTimer = 0.15; updateFog(); }
  if (waterMat) {
    waterMat.emissiveIntensity = 0.3 + Math.sin(tSec * 1.4) * 0.14;
    waterNorm.offset.set(tSec * 0.020, tSec * 0.013);    // ripples drift one way
    waterNorm.repeat.set(3, 3);
    // gentle rolling wave displacement on the water vertices
    if (waterMesh && waterGeoBase) {
      const p = waterMesh.geometry.attributes.position;
      for (let i = 0; i < p.count; i++) {
        const bx = waterGeoBase[i * 3], by = waterGeoBase[i * 3 + 1];
        p.array[i * 3 + 2] = -0.05 + Math.sin(bx * 1.3 + tSec * 1.8) * 0.03
          + Math.cos(by * 1.7 - tSec * 1.4) * 0.025;
      }
      p.needsUpdate = true;    // ripple lighting comes from the animated normal map (no costly normal recompute)
    }
  }
  edgeScroll(dt);
  updateCamera();
  syncEnts(dt);
  syncCorpses();
  drawBeams(); drawProjectiles(); drawFlashes(); drawParticles(); drawRings(); drawIon(); drawFirestorm(); drawFireballs(); drawGhost();
  renderer.render(scene, camera);
  drawHud();
  drawMinimap();
}
