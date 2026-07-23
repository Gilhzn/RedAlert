/* Tiberium Dusk 3D core DSL — mirrors the Blender python helpers (Z-up).
   Evaluated with a global THREE in scope; defines global H. */
"use strict";
var TEAM_COLOR = 0xd7a838;   // player team-colour accent (changeable in Settings)
var H = (function () {
  const mats = new Map();
  function mat(color) {
    const k = "m" + color;
    if (!mats.has(k)) mats.set(k, new THREE.MeshStandardMaterial({
      color, roughness: 0.8, metalness: 0.12 }));
    return mats.get(k);
  }
  function emis(color) {
    const k = "e" + color;
    if (!mats.has(k)) mats.set(k, new THREE.MeshStandardMaterial({
      color, emissive: color, emissiveIntensity: 1.6, roughness: 0.5 }));
    return mats.get(k);
  }
  const R = Math.PI / 180;
  function finish(mesh, parent, x, y, z, o) {
    mesh.position.set(x, y, z);
    mesh.rotation.order = "XYZ";
    mesh.rotation.set((o.rx || 0) * R, (o.ry || 0) * R, (o.rz || 0) * R);
    mesh.castShadow = true;
    mesh.receiveShadow = true;
    if (parent) parent.add(mesh);
    return mesh;
  }
  const boxGeo = new THREE.BoxGeometry(1, 1, 1);
  function box(parent, x, y, z, sx, sy, sz, color, o = {}) {
    const m = new THREE.Mesh(boxGeo, o.e ? emis(color) : mat(color));
    m.scale.set(sx, sy, sz);
    return finish(m, parent, x, y, z, o);
  }
  const rodGeos = new Map();
  function rodGeo(v) {
    if (!rodGeos.has(v)) {
      const g = new THREE.CylinderGeometry(1, 1, 1, v);
      g.rotateX(Math.PI / 2);          // cylinder along Z (Blender-style)
      rodGeos.set(v, g);
    }
    return rodGeos.get(v);
  }
  function rod(parent, x, y, z, r, len, color, o = {}) {
    if (o.rx === undefined) o = Object.assign({ rx: 90 }, o);
    const m = new THREE.Mesh(rodGeo(o.v || 10), o.e ? emis(color) : mat(color));
    m.scale.set(r, r, len);
    return finish(m, parent, x, y, z, o);
  }
  const ballGeos = new Map();
  function ballGeo(seg) {
    if (!ballGeos.has(seg)) ballGeos.set(seg, new THREE.SphereGeometry(1, seg, Math.max(6, seg * 2 / 3 | 0)));
    return ballGeos.get(seg);
  }
  function ball(parent, x, y, z, r, color, o = {}) {
    const m = new THREE.Mesh(ballGeo(o.v || 12), o.e ? emis(color) : mat(color));
    m.scale.set(r, r, r * (o.squash || 1));
    return finish(m, parent, x, y, z, o);
  }
  function group(parent) {
    const g = new THREE.Group();
    if (parent) parent.add(g);
    return g;
  }
  /** Rescale so max(XY extent) === fit and ground minZ at 0. */
  function fitTo(root, fit) {
    root.updateMatrixWorld(true);
    const bb = new THREE.Box3().setFromObject(root);
    const w = Math.max(bb.max.x - bb.min.x, bb.max.y - bb.min.y, 0.001);
    const s = fit / w;
    root.scale.multiplyScalar(s);
    root.updateMatrixWorld(true);
    const bb2 = new THREE.Box3().setFromObject(root);
    root.position.z -= bb2.min.z;
    root.position.x -= (bb2.min.x + bb2.max.x) / 2;
    root.position.y -= (bb2.min.y + bb2.max.y) / 2;
    return root;
  }
  const C = {
    DM_ARMOR: 0x8b7433, DM_DARK: 0x4d452b, SO_ARMOR: 0x6e2429, SO_DARK: 0x291a1c,
    TRACK: 0x1c1c1a, GUNMETAL: 0x3b3d45, TIRE: 0x141414, SKIN: 0xd9ad85,
    BOOTS: 0x24211f, YELLOW: 0xf2bf26, WHITE: 0xebede6, RED: 0xd93338,
    BLUEGREY: 0x6b7a99, CYAN: 0x59f2ff, CONCRETE: 0x858073, SAND: 0x9a8a5e,
    GREEN: 0x49ff7a, CACTUS: 0x4a7a3a,
  };
  return { box, rod, ball, group, mat, emis, fitTo, C };
})();
if (typeof module !== "undefined") module.exports = H;
