/* Tiberium Dusk — unit model builders, ported 1:1 from the Blender python
   builders (blender_vehicles.py, blender_gdi_units.py, blender_air_structures.py,
   blender_new_vehicles_snippet.py). Z-up, front = +Y. Defines global UNIT_BUILDERS.
   Each builder: (H, pose) => ({ root, turret, fit }); engine calls fitTo. */
"use strict";
var UNIT_BUILDERS = (function () {

  /* python tracks(parts, hw, ln, color, wheel_r=0.09) — two track assemblies. */
  function tracks(H, g, hw, ln, color, wheelR) {
    wheelR = wheelR || 0.09;
    for (const side of [-1, 1]) {
      H.box(g, side * hw, 0, 0.16, 0.20, ln, 0.20, color);
      H.box(g, side * hw, 0, 0.28, 0.16, ln * 0.92, 0.08, H.C.GUNMETAL);
      const n = 4;
      for (let i = 0; i < n; i++) {
        const yy = -ln / 2 + ln * 0.15 + i * (ln * 0.7 / (n - 1));
        H.rod(g, side * hw, yy, 0.12, wheelR, 0.24, H.C.TIRE, { rx: 0, ry: 90 });
      }
    }
  }

  /* python tank(uid, tier, fac): tier light/mbt/heavy, fac dm/so. */
  function tank(H, tier, fac) {
    const C = H.C;
    const armor = fac === "dm" ? C.DM_ARMOR : C.SO_ARMOR;
    const dark = fac === "dm" ? C.DM_DARK : C.SO_DARK;
    const root = H.group();
    let turretH, barL, ts;
    if (tier === "light") {
      tracks(H, root, 0.30, 0.85, C.TRACK);
      H.box(root, 0, 0, 0.36, 0.52, 0.80, 0.18, armor);
      H.box(root, 0, 0.28, 0.44, 0.46, 0.28, 0.10, dark, { rx: 18 });
      turretH = 0.46; barL = 0.5; ts = 0.75;
    } else if (tier === "mbt") {
      tracks(H, root, 0.36, 1.05, C.TRACK);
      H.box(root, 0, 0, 0.38, 0.62, 1.0, 0.22, armor);
      H.box(root, 0, 0.36, 0.50, 0.56, 0.34, 0.12, dark, { rx: 22 });
      H.box(root, 0, -0.42, 0.50, 0.50, 0.16, 0.10, dark);
      turretH = 0.50; barL = 0.72; ts = 1.0;
    } else { // heavy: wide twin-barrel monster
      tracks(H, root, 0.46, 1.25, C.TRACK, 0.11);
      H.box(root, 0, 0, 0.42, 0.80, 1.2, 0.26, armor);
      H.box(root, 0, 0.44, 0.56, 0.72, 0.38, 0.14, dark, { rx: 20 });
      H.box(root, 0, -0.5, 0.58, 0.6, 0.2, 0.14, dark);
      turretH = 0.56; barL = 0.85; ts = 1.3;
    }
    const accent = fac === "dm" ? TEAM_COLOR : 0xc0303a;   // faction team-colour band
    const tur = H.group(root);
    tur.position.set(0, 0, turretH);
    H.rod(tur, 0, 0, 0.02, 0.26 * ts, 0.10, dark, { rx: 0 });
    H.box(tur, 0, -0.02, 0.10, 0.44 * ts, 0.46 * ts, 0.16, armor);
    H.box(tur, 0, 0.1 * ts, 0.20, 0.3 * ts, 0.24 * ts, 0.07, dark);
    H.box(tur, 0, -0.22 * ts, 0.15, 0.34 * ts, 0.05, 0.09, accent);   // rear turret team stripe
    if (tier === "heavy") {
      for (const s of [-1, 1]) {
        H.rod(tur, s * 0.11, 0.30 + barL / 2, 0.12, 0.045, barL, C.GUNMETAL, { rx: 90 });
        H.box(tur, s * 0.11, 0.30 + barL, 0.12, 0.10, 0.10, 0.10, C.BOOTS);
      }
    } else {
      H.rod(tur, 0, 0.24 + barL / 2, 0.12, tier === "mbt" ? 0.05 : 0.04, barL, C.GUNMETAL, { rx: 90 });
      H.box(tur, 0, 0.24 + barL, 0.12, 0.09, 0.10, 0.09, C.BOOTS);
    }
    return { root, turret: tur };
  }

  /* python soldier(kind, faction, pose) — parts added into g. */
  function soldier(H, g, kind, faction, pose) {
    const C = H.C;
    const armor = faction === "dm" ? C.DM_ARMOR : C.SO_ARMOR;
    const dark = faction === "dm" ? C.DM_DARK : C.SO_DARK;
    // bright faction accent for a clear team-colour read from the iso camera
    const accent = faction === "dm" ? TEAM_COLOR : 0xc0303a;
    const heavy = kind === "heavy";
    const w = heavy ? 1.15 : 1.0;
    const la = pose === 1 ? 22 : 6;
    H.box(g, -0.10 * w, 0, 0.26, 0.13 * w, 0.15, 0.42, dark, { rx: la });
    H.box(g, 0.10 * w, 0, 0.26, 0.13 * w, 0.15, 0.42, dark, { rx: -la });
    const lo = 0.09 * Math.sin(la * Math.PI / 180);
    H.box(g, -0.10 * w, lo * 2.4, 0.045, 0.15 * w, 0.22, 0.09, C.BOOTS);
    H.box(g, 0.10 * w, -lo * 2.4, 0.045, 0.15 * w, 0.22, 0.09, C.BOOTS);
    H.box(g, 0, 0, 0.40, 0.40 * w, 0.24, 0.10, dark);
    H.box(g, 0, 0, 0.62, 0.46 * w, 0.29 * w, 0.36, armor);              // torso (a touch bulkier)
    H.box(g, 0, 0.15, 0.66, 0.30 * w, 0.05, 0.20, accent);             // chest team-colour plate
    H.box(g, -0.27 * w, 0, 0.77, 0.16, 0.22, 0.15, accent);           // team-colour shoulder pads
    H.box(g, 0.27 * w, 0, 0.77, 0.16, 0.22, 0.15, accent);
    H.ball(g, 0, 0, 0.93, 0.13, C.SKIN);                              // heroic head (bigger)
    const helmet = kind === "engineer" ? C.YELLOW : kind === "medic" ? C.WHITE : armor;
    H.ball(g, 0, 0, 0.99, 0.155, helmet, { squash: 0.78 });          // bigger helmet reads from above
    H.box(g, 0, 0, 1.10, 0.09, 0.24, 0.05, accent);                   // helmet team crest (reads top-down)
    H.box(g, 0, 0.13, 0.93, 0.18, 0.06, 0.06, dark);                  // visor
    if (kind === "rocket") {
      H.rod(g, -0.30 * w, 0.05, 0.72, 0.05, 0.34, armor, { rx: 70 });
      H.rod(g, 0.24 * w, 0.10, 0.80, 0.05, 0.30, armor, { rx: 95 });
      H.rod(g, 0.16, -0.05, 0.95, 0.075, 0.62, C.BLUEGREY, { rx: 88 });
      H.rod(g, 0.16, 0.28, 0.95, 0.09, 0.10, C.RED, { rx: 88 });
    } else if (kind === "engineer") {
      H.rod(g, -0.30 * w, 0.10, 0.66, 0.05, 0.32, armor, { rx: 55 });
      H.rod(g, 0.30 * w, 0.10, 0.66, 0.05, 0.32, armor, { rx: 55 });
      H.box(g, 0.30, 0.26, 0.42, 0.20, 0.12, 0.16, C.YELLOW);
      H.box(g, 0.30, 0.26, 0.51, 0.06, 0.12, 0.04, C.GUNMETAL);
    } else if (kind === "medic") {
      H.rod(g, -0.30 * w, 0.10, 0.66, 0.05, 0.32, C.WHITE, { rx: 55 });
      H.rod(g, 0.30 * w, 0.10, 0.66, 0.05, 0.32, C.WHITE, { rx: 55 });
      H.box(g, 0, -0.20, 0.68, 0.30, 0.12, 0.34, C.WHITE);
      H.box(g, 0, -0.27, 0.70, 0.16, 0.02, 0.05, C.RED);
      H.box(g, 0, -0.27, 0.70, 0.05, 0.02, 0.16, C.RED);
    } else { // rifle-style arms + gun (also heavy / grenadier base)
      H.rod(g, -0.28 * w, 0.16, 0.70, 0.05, 0.30, armor, { rx: 35 });
      H.rod(g, 0.28 * w, 0.16, 0.70, 0.05, 0.30, armor, { rx: 35 });
      if (heavy) {
        H.rod(g, 0.02, 0.30, 0.62, 0.085, 0.55, C.GUNMETAL, { rx: 90 });
        H.rod(g, 0.02, 0.60, 0.62, 0.05, 0.16, C.BOOTS, { rx: 90 });
        H.box(g, 0.02, 0.12, 0.52, 0.16, 0.18, 0.16, C.GUNMETAL);
      } else {
        H.box(g, 0.02, 0.30, 0.66, 0.09, 0.50, 0.12, C.GUNMETAL);     // chunkier rifle reads at a glance
        H.rod(g, 0.02, 0.58, 0.68, 0.03, 0.18, C.BOOTS, { rx: 90 });
        H.box(g, 0.02, 0.14, 0.60, 0.05, 0.14, 0.10, dark);           // magazine
      }
    }
    return g;
  }

  const B = {};

  // ---- tanks (python VEHICLES list) ----
  const TANKS = {
    dm_wolverine: ["light", "dm", 0.75], dm_mbt_walker: ["mbt", "dm", 0.95],
    dm_mammoth: ["heavy", "dm", 1.2], so_tick_tank: ["mbt", "so", 0.95],
  };
  for (const id of Object.keys(TANKS)) {
    const [tier, fac, fit] = TANKS[id];
    B[id] = (H) => { const { root, turret } = tank(H, tier, fac); return { root, turret, fit }; };
  }

  // ---- buggy ----
  B.so_scout_buggy = (H) => {
    const C = H.C, root = H.group();
    for (const sy of [-0.32, 0.32])
      for (const sx of [-0.3, 0.3])
        H.rod(root, sx, sy, 0.14, 0.13, 0.12, C.TIRE, { rx: 0, ry: 90, v: 12 });
    H.box(root, 0, 0, 0.28, 0.44, 0.85, 0.14, C.SO_ARMOR);
    H.box(root, 0, 0.22, 0.40, 0.36, 0.30, 0.12, C.SO_DARK, { rx: 14 });
    H.rod(root, -0.2, -0.1, 0.36, 0.03, 0.5, C.GUNMETAL, { rx: 68 }); // roll bar
    H.rod(root, 0.2, -0.1, 0.36, 0.03, 0.5, C.GUNMETAL, { rx: 68 });
    const tur = H.group(root);
    tur.position.set(0, -0.15, 0.52);
    H.rod(tur, 0, 0, 0.02, 0.06, 0.16, C.SO_DARK, { rx: 0 });
    H.box(tur, 0, 0.12, 0.10, 0.08, 0.34, 0.06, C.GUNMETAL);
    return { root, turret: tur, fit: 0.8 };
  };

  // ---- stealth tank ----
  B.so_stealth_tank = (H) => {
    const C = H.C, root = H.group();
    tracks(H, root, 0.30, 0.9, C.TRACK);
    H.box(root, 0, 0, 0.34, 0.55, 0.85, 0.16, C.SO_DARK);
    H.box(root, 0, 0.1, 0.46, 0.42, 0.55, 0.12, C.SO_ARMOR, { rx: 8 });
    H.box(root, 0, 0.38, 0.42, 0.34, 0.25, 0.10, C.SO_DARK, { rx: 30 });
    H.box(root, 0, -0.35, 0.44, 0.36, 0.22, 0.10, C.SO_DARK, { rx: -25 });
    const tur = H.group(root);
    tur.position.set(0, 0, 0.50);
    H.box(tur, 0, 0, 0.04, 0.30, 0.34, 0.10, C.SO_DARK);
    H.rod(tur, -0.09, 0.26, 0.08, 0.035, 0.32, C.GUNMETAL, { rx: 90 });
    H.rod(tur, 0.09, 0.26, 0.08, 0.035, 0.32, C.GUNMETAL, { rx: 90 });
    return { root, turret: tur, fit: 0.9 };
  };

  // ---- hover MLRS ----
  B.dm_hover_mlrs = (H) => {
    const C = H.C, root = H.group();
    H.box(root, 0, 0, 0.14, 0.62, 0.95, 0.10, C.GUNMETAL);
    H.box(root, 0, 0, 0.24, 0.55, 0.88, 0.14, C.DM_ARMOR);
    H.box(root, 0, 0.32, 0.34, 0.45, 0.25, 0.10, C.DM_DARK, { rx: 16 });
    for (const s of [-1, 1])
      H.box(root, s * 0.30, 0, 0.16, 0.06, 0.8, 0.14, C.DM_DARK);
    const tur = H.group(root);
    tur.position.set(0, -0.08, 0.40);
    H.box(tur, 0, 0, 0.06, 0.40, 0.5, 0.24, C.DM_DARK, { rx: -18 });
    for (let i = 0; i < 3; i++)
      for (let j = 0; j < 2; j++)
        H.rod(tur, -0.12 + i * 0.12, 0.24, 0.10 + j * 0.11, 0.035, 0.1, C.BOOTS, { rx: 72 });
    return { root, turret: tur, fit: 0.9 };
  };

  // ---- hull-only GDI vehicles ----
  B.dm_apc = (H) => {
    const C = H.C, root = H.group();
    for (const sy of [-0.35, 0, 0.35])
      for (const sx of [-0.28, 0.28])
        H.rod(root, sx, sy, 0.13, 0.12, 0.12, C.TIRE, { rx: 0, ry: 90, v: 12 });
    H.box(root, 0, 0, 0.30, 0.5, 1.0, 0.22, C.DM_ARMOR);
    H.box(root, 0, 0.30, 0.44, 0.42, 0.4, 0.14, C.DM_DARK, { rx: 18 });    // sloped bow
    H.box(root, 0, -0.2, 0.47, 0.4, 0.5, 0.12, C.DM_ARMOR);                // troop bay
    H.box(root, 0, -0.44, 0.36, 0.44, 0.1, 0.2, C.GUNMETAL, { rx: -20 });  // rear ramp
    return { root, turret: null, fit: 0.9 };
  };

  B.dm_disruptor = (H) => {
    const C = H.C, root = H.group();
    tracks(H, root, 0.36, 1.05, C.TRACK);
    H.box(root, 0, 0, 0.40, 0.6, 1.0, 0.24, C.DM_ARMOR);
    H.box(root, 0, -0.25, 0.58, 0.5, 0.4, 0.16, C.DM_DARK);
    H.rod(root, 0, 0.42, 0.58, 0.19, 0.18, C.BLUEGREY, { rx: 90, v: 14 }); // sonic dish
    H.rod(root, 0, 0.53, 0.58, 0.23, 0.06, C.GUNMETAL, { rx: 90, v: 14 });
    H.box(root, 0, 0.15, 0.56, 0.2, 0.4, 0.12, C.GUNMETAL);
    return { root, turret: null, fit: 1.0 };
  };

  B.dm_sensor = (H) => {
    const C = H.C, root = H.group();
    for (const sy of [-0.3, 0.3])
      for (const sx of [-0.26, 0.26])
        H.rod(root, sx, sy, 0.12, 0.11, 0.12, C.TIRE, { rx: 0, ry: 90, v: 12 });
    H.box(root, 0, 0, 0.28, 0.46, 0.85, 0.2, C.DM_ARMOR);
    H.box(root, 0, 0.25, 0.42, 0.36, 0.3, 0.12, C.DM_DARK);
    H.rod(root, 0, -0.15, 0.38, 0.05, 0.5, C.GUNMETAL);
    H.rod(root, 0, -0.15, 0.74, 0.22, 0.05, C.WHITE, { rx: 28, v: 14 });   // dish
    return { root, turret: null, fit: 0.85 };
  };

  // ---- aircraft ----
  B.dm_orca = (H) => {
    const C = H.C, root = H.group();
    H.box(root, 0, 0.1, 0.62, 0.34, 1.05, 0.30, C.DM_ARMOR);               // fuselage
    H.box(root, 0, 0.52, 0.66, 0.26, 0.34, 0.20, C.BLUEGREY, { rx: 14 });  // canopy
    H.box(root, 0, -0.52, 0.72, 0.10, 0.34, 0.26, C.DM_DARK, { rx: -16 }); // tail boom
    H.box(root, 0, -0.66, 0.90, 0.04, 0.22, 0.24, C.DM_ARMOR);             // fin
    for (const s of [-1, 1]) {
      H.box(root, s * 0.42, 0.05, 0.64, 0.52, 0.24, 0.07, C.DM_DARK);      // stub wing
      H.rod(root, s * 0.66, 0.05, 0.64, 0.20, 0.16, C.GUNMETAL, { rx: 0, v: 14 });  // ducted fan
      H.rod(root, s * 0.66, 0.05, 0.73, 0.23, 0.045, C.DM_ARMOR, { rx: 0, v: 14 }); // fan rim
      H.box(root, s * 0.38, 0.30, 0.52, 0.10, 0.34, 0.10, C.GUNMETAL);     // rocket pod
      H.rod(root, s * 0.30, 0.02, 0.30, 0.03, 0.55, C.GUNMETAL, { rx: 90 }); // skid
      H.rod(root, s * 0.30, 0.02, 0.40, 0.025, 0.22, C.GUNMETAL, { rx: 0 });
    }
    return { root, turret: null, fit: 1.15 };
  };

  B.dm_orca_bomber = (H) => {
    const C = H.C, root = H.group();
    H.box(root, 0, 0, 0.70, 0.56, 1.5, 0.38, C.DM_ARMOR);                  // heavy fuselage
    H.box(root, 0, 0.72, 0.72, 0.36, 0.36, 0.24, C.BLUEGREY, { rx: 16 });  // cockpit
    H.box(root, 0, -0.05, 0.48, 0.42, 1.0, 0.12, C.DM_DARK);               // bomb bay
    H.box(root, 0, -0.80, 0.88, 0.05, 0.26, 0.30, C.DM_ARMOR);             // fin
    H.box(root, 0, -0.78, 1.00, 0.5, 0.20, 0.05, C.DM_DARK);               // tailplane
    for (const s of [-1, 1]) {
      H.box(root, s * 0.58, 0.1, 0.72, 0.62, 0.30, 0.08, C.DM_DARK);
      H.rod(root, s * 0.86, 0.1, 0.72, 0.25, 0.20, C.GUNMETAL, { rx: 0, v: 14 });
      H.rod(root, s * 0.86, 0.1, 0.83, 0.28, 0.05, C.DM_ARMOR, { rx: 0, v: 14 });
      H.rod(root, s * 0.34, 0.0, 0.32, 0.035, 0.8, C.GUNMETAL, { rx: 90 });
      H.rod(root, s * 0.34, 0.0, 0.44, 0.03, 0.26, C.GUNMETAL, { rx: 0 });
    }
    return { root, turret: null, fit: 1.45 };
  };

  B.so_harpy = (H) => {
    const C = H.C, root = H.group();
    H.box(root, 0, 0.05, 0.52, 0.26, 0.85, 0.26, C.SO_ARMOR);              // slim body
    H.box(root, 0, 0.42, 0.56, 0.20, 0.26, 0.16, C.SO_DARK, { rx: 18 });   // canopy
    H.box(root, 0, -0.55, 0.60, 0.07, 0.40, 0.10, C.SO_DARK, { rx: -8 });  // tail
    H.box(root, 0, -0.74, 0.70, 0.03, 0.14, 0.16, C.SO_ARMOR);             // fin
    // rotor assembly (returned as `turret` so the engine spins it)
    const rotor = H.group(root);
    rotor.position.set(0, 0, 0.80);
    H.rod(rotor, 0, 0, -0.08, 0.035, 0.14, C.GUNMETAL, { rx: 0 });         // rotor mast
    H.box(rotor, 0, 0, 0, 0.09, 1.35, 0.02, C.TRACK);                      // rotor blades
    H.box(rotor, 0, 0, 0, 1.35, 0.09, 0.02, C.TRACK);
    H.rod(root, 0, 0.30, 0.36, 0.05, 0.22, C.GUNMETAL, { rx: 0 });         // chin gun
    H.rod(root, 0, 0.42, 0.30, 0.028, 0.20, C.TRACK, { rx: 90 });
    for (const s of [-1, 1]) {
      H.rod(root, s * 0.20, 0.02, 0.26, 0.025, 0.5, C.GUNMETAL, { rx: 90 });
      H.rod(root, s * 0.20, 0.02, 0.34, 0.02, 0.16, C.GUNMETAL, { rx: 0 });
    }
    return { root, turret: rotor, fit: 1.0 };
  };

  // ---- infantry ----
  const INFANTRY = {
    dm_rifle: ["rifle", "dm", 0.50], dm_rocket: ["rocket", "dm", 0.50],
    dm_heavy: ["heavy", "dm", 0.58], dm_engineer: ["engineer", "dm", 0.50],
    dm_medic: ["medic", "dm", 0.50], so_rifle: ["rifle", "so", 0.50],
    so_rocket: ["rocket", "so", 0.50],
  };
  for (const id of Object.keys(INFANTRY)) {
    const [kind, fac, fit] = INFANTRY[id];
    B[id] = (H, pose) =>
      ({ root: soldier(H, H.group(), kind, fac, pose ? 1 : 0), turret: null, fit });
  }

  // soldier_variant("jump") — jumpjet trooper
  B.dm_jumptrooper = (H, pose) => {
    const C = H.C, root = soldier(H, H.group(), "rifle", "dm", pose ? 1 : 0);
    H.rod(root, -0.10, -0.20, 0.62, 0.055, 0.34, C.GUNMETAL);
    H.rod(root, 0.10, -0.20, 0.62, 0.055, 0.34, C.GUNMETAL);
    H.rod(root, -0.10, -0.20, 0.42, 0.035, 0.08, C.RED);
    H.rod(root, 0.10, -0.20, 0.42, 0.035, 0.08, C.RED);
    return { root, turret: null, fit: 0.50 };
  };

  // soldier_variant("hero") — railgun hero
  B.dm_railhero = (H, pose) => {
    const C = H.C, root = soldier(H, H.group(), "rifle", "dm", pose ? 1 : 0);
    H.rod(root, 0.05, 0.30, 0.70, 0.05, 0.55, C.BLUEGREY, { rx: 90 });
    H.box(root, 0.05, 0.50, 0.70, 0.10, 0.12, 0.10, C.RED);
    H.box(root, -0.28, 0, 0.80, 0.18, 0.24, 0.16, C.DM_DARK);
    return { root, turret: null, fit: 0.50 };
  };

  // grenadier — rifle base + disc launcher tube + back rack
  B.dm_grenadier = (H, pose) => {
    const C = H.C, root = soldier(H, H.group(), "grenadier", "dm", pose ? 1 : 0);
    H.rod(root, 0.05, 0.28, 0.66, 0.075, 0.42, C.GUNMETAL, { rx: 90, v: 12 }); // disc tube
    H.rod(root, 0.05, 0.50, 0.66, 0.09, 0.06, C.DM_DARK, { rx: 90, v: 12 });   // muzzle ring
    for (let i = 0; i < 3; i++)                                                // disc rack
      H.rod(root, 0, -0.17 - i * 0.045, 0.62, 0.11, 0.03, C.BLUEGREY, { rx: 90, v: 12 });
    return { root, turret: null, fit: 0.50 };
  };

  // dm_transport — heavy-lift transport helicopter (carries vehicles + troops).
  // The big main rotor is returned as `turret` so the engine spins it.
  B.dm_transport = (H) => {
    const C = H.C, root = H.group();
    H.box(root, 0, 0, 0.62, 0.62, 1.95, 0.52, C.DM_ARMOR);              // fuselage
    H.box(root, 0, 0.95, 0.66, 0.52, 0.42, 0.42, C.BLUEGREY, { rx: 14 }); // cockpit glass
    H.box(root, 0, -0.9, 0.5, 0.58, 0.32, 0.42, C.DM_DARK, { rx: -20 }); // rear cargo ramp
    H.box(root, 0, 0.1, 0.92, 0.52, 1.5, 0.1, C.DM_DARK);               // roof deck
    for (const s of [-1, 1]) {                                          // sponsons + skids
      H.box(root, s * 0.44, 0, 0.5, 0.16, 1.0, 0.26, C.DM_DARK);
      H.rod(root, s * 0.38, 0.5, 0.26, 0.045, 0.72, C.GUNMETAL, { rx: 90 });
      H.rod(root, s * 0.38, 0.5, 0.4, 0.045, 0.28, C.GUNMETAL, { rx: 0 });
      H.rod(root, s * 0.38, -0.5, 0.26, 0.045, 0.72, C.GUNMETAL, { rx: 90 });
      H.rod(root, s * 0.38, -0.5, 0.4, 0.045, 0.28, C.GUNMETAL, { rx: 0 });
    }
    H.box(root, 0, -1.18, 0.92, 0.08, 0.3, 0.52, C.DM_ARMOR);           // tail fin
    H.box(root, 0, 0.1, 0.72, 0.66, 0.06, 0.16, TEAM_COLOR);             // GDI team stripe
    H.ball(root, 0, 1.0, 0.7, 0.05, C.RED, { e: true });               // nose beacon
    const rotor = H.group(root);                                        // main rotor (spun by engine)
    rotor.position.set(0, 0.05, 1.18);
    H.rod(rotor, 0, 0, -0.06, 0.07, 0.16, C.GUNMETAL, { rx: 0 });       // mast
    for (let i = 0; i < 4; i++) H.box(rotor, 0, 0, 0, 0.13, 2.7, 0.03, C.TRACK, { rz: i * 45 });
    H.ball(rotor, 0, 0, 0, 0.11, C.DM_DARK);                           // hub
    return { root, turret: rotor, fit: 1.75 };
  };

  // phantom — stealth trooper with an AT launcher; dark hooded cloak so the
  // silhouette reads as a covert unit even when decloaked near the enemy
  B.dm_phantom = (H, pose) => {
    const C = H.C, root = soldier(H, H.group(), "rocket", "dm", pose ? 1 : 0);
    // hooded cloak over the shoulders/back (dark, faint cyan trim)
    H.box(root, 0, -0.06, 0.62, 0.5, 0.34, 0.5, 0x1a2230);        // cloak body
    H.box(root, 0, -0.02, 0.98, 0.28, 0.28, 0.2, 0x11161f);       // hood
    H.box(root, 0, 0.12, 1.0, 0.16, 0.05, 0.06, C.CYAN, { e: true }); // visor glow
    H.box(root, -0.24, -0.2, 0.5, 0.06, 0.04, 0.5, 0x0e2a33);     // cloak seams
    H.box(root, 0.24, -0.2, 0.5, 0.06, 0.04, 0.5, 0x0e2a33);
    return { root, turret: null, fit: 0.50 };
  };

  // artillery — tracked howitzer: long high-elevation barrel on a rotating
  // mount, fires in a high arc at long range (returned turret aims/recoils)
  B.dm_artillery = (H) => {
    const C = H.C, root = H.group();
    tracks(H, root, 0.40, 1.15, C.TRACK, 0.11);
    H.box(root, 0, 0, 0.40, 0.68, 1.05, 0.22, C.DM_ARMOR);        // hull
    H.box(root, 0, -0.42, 0.52, 0.5, 0.3, 0.16, C.DM_DARK);       // rear engine deck
    for (const s of [-1, 1])                                       // recoil spades
      H.box(root, s * 0.3, -0.6, 0.34, 0.1, 0.22, 0.1, C.GUNMETAL, { rx: 28 });
    const tur = H.group(root);
    tur.position.set(0, 0, 0.52);
    H.box(tur, 0, -0.06, 0.06, 0.46, 0.5, 0.2, C.DM_DARK);        // gun cradle
    H.rod(tur, 0, 0.02, 0.16, 0.16, 0.22, C.GUNMETAL, { rx: 0, v: 12 }); // trunnion
    // long barrel, angled up for the arcing shot
    H.rod(tur, 0, 0.34, 0.42, 0.07, 1.15, C.GUNMETAL, { rx: 52 });
    H.rod(tur, 0, 0.62, 0.78, 0.09, 0.16, C.BOOTS, { rx: 52 });   // muzzle brake
    H.box(tur, 0, -0.24, 0.18, 0.34, 0.22, 0.16, C.DM_ARMOR);     // breech
    H.box(tur, 0, -0.2, 0.30, 0.2, 0.05, 0.06, C.YELLOW, { e: true }); // aim light
    return { root, turret: tur, fit: 1.05 };
  };

  // ---- nx_harvester: original design (python used an external KayKit model).
  // Six-wheeled mining dump truck: DM_ARMOR cab forward (+Y), GUNMETAL chassis,
  // open hopper at the back holding emissive tiberium shards.
  B.nx_harvester = (H) => {
    const C = H.C, root = H.group();
    for (const sy of [-0.42, 0, 0.42])
      for (const sx of [-0.36, 0.36])
        H.rod(root, sx, sy, 0.16, 0.15, 0.16, C.TIRE, { rx: 0, ry: 90, v: 12 });
    H.box(root, 0, 0, 0.30, 0.60, 1.30, 0.16, C.GUNMETAL);                 // chassis
    H.box(root, 0, 0.50, 0.52, 0.50, 0.32, 0.30, C.DM_ARMOR);              // cab
    H.box(root, 0, 0.62, 0.56, 0.44, 0.08, 0.16, C.BLUEGREY, { rx: 12 });  // windshield
    H.box(root, 0, 0.70, 0.34, 0.56, 0.10, 0.18, C.GUNMETAL);              // front scoop
    H.rod(root, 0.20, 0.30, 0.70, 0.035, 0.26, C.GUNMETAL, { rx: 0 });     // exhaust stack
    H.box(root, 0, -0.34, 0.42, 0.70, 0.86, 0.06, C.DM_DARK);              // hopper floor
    H.box(root, -0.33, -0.34, 0.56, 0.08, 0.86, 0.30, C.DM_DARK);          // hopper walls
    H.box(root, 0.33, -0.34, 0.56, 0.08, 0.86, 0.30, C.DM_DARK);
    H.box(root, 0, -0.73, 0.56, 0.72, 0.08, 0.30, C.DM_DARK);
    H.box(root, 0, 0.05, 0.56, 0.72, 0.08, 0.30, C.DM_DARK);
    H.box(root, -0.12, -0.20, 0.60, 0.10, 0.10, 0.22, C.GREEN, { rx: 12, rz: 25, e: true });
    H.box(root, 0.14, -0.38, 0.62, 0.12, 0.12, 0.26, C.GREEN, { rx: -8, rz: -30, e: true });
    H.box(root, -0.02, -0.55, 0.58, 0.09, 0.09, 0.18, C.GREEN, { rx: 6, ry: 10, rz: 50, e: true });
    return { root, turret: null, fit: 1.15 };
  };

  // ---- nx_mcv: Mobile Construction Vehicle — heavy 8-wheel hauler with a
  // folded crane boom; deploys into the construction yard.
  B.nx_mcv = (H) => {
    const C = H.C, root = H.group();
    for (const sy of [-0.52, -0.18, 0.18, 0.52])
      for (const sx of [-0.3, 0.3])
        H.rod(root, sx, sy, 0.14, 0.13, 0.13, C.TIRE, { rx: 0, ry: 90, v: 12 });
    H.box(root, 0, 0, 0.32, 0.56, 1.5, 0.2, C.GUNMETAL);                // chassis
    H.box(root, 0, 0.55, 0.55, 0.5, 0.4, 0.3, C.DM_ARMOR);              // cab
    H.box(root, 0, 0.72, 0.6, 0.44, 0.06, 0.16, C.BLUEGREY);            // windshield
    H.box(root, 0, -0.15, 0.52, 0.52, 0.9, 0.22, C.DM_ARMOR);           // hull body
    H.box(root, 0, -0.15, 0.66, 0.42, 0.8, 0.08, C.DM_DARK);            // roof plate
    H.box(root, 0, -0.5, 0.72, 0.1, 0.55, 0.1, C.YELLOW, { rx: -22 });  // folded crane boom
    H.box(root, 0, -0.75, 0.62, 0.14, 0.12, 0.12, C.GUNMETAL);          // crane base
    H.box(root, 0.2, 0.1, 0.68, 0.08, 0.3, 0.05, C.YELLOW);             // hazard stripes
    H.box(root, -0.2, 0.1, 0.68, 0.08, 0.3, 0.05, C.YELLOW);
    H.ball(root, 0, 0.62, 0.78, 0.045, C.RED, { e: true });             // beacon
    return { root, turret: null, fit: 1.25 };
  };

  return B;
})();
if (typeof module !== "undefined") module.exports = UNIT_BUILDERS;
