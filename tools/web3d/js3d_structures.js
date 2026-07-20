/* Tiberium Dusk — structure + prop builders (Three.js r128, Z-up, front=+Y).
   Evaluated after global H (see js3d_core.js). Defines STRUCT_BUILDERS and
   PROP_BUILDERS. Ports of the Blender builders keep coordinates 1:1.
   (No "use strict": the file is loaded via eval and must create globals.) */

var STRUCT_BUILDERS = {

  /* ================= ported 1:1 from blender_air_structures.py ========== */

  dm_helipad: (H) => {
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.06, 1.9, 1.9, 0.12, C.CONCRETE);              // pad slab
    H.rod(r, 0, 0, 0.125, 0.62, 0.015, C.DM_DARK, { rx: 0, v: 24 }); // landing circle
    H.box(r, 0, 0, 0.135, 0.34, 0.06, 0.012, C.WHITE);             // 'H'
    H.box(r, -0.14, 0, 0.135, 0.06, 0.30, 0.012, C.WHITE);
    H.box(r, 0.14, 0, 0.135, 0.06, 0.30, 0.012, C.WHITE);
    for (const [sx, sy] of [[-0.85, -0.85], [0.85, -0.85], [-0.85, 0.85], [0.85, 0.85]])
      H.box(r, sx, sy, 0.16, 0.08, 0.08, 0.08, C.YELLOW, { e: true }); // corner lights
    H.box(r, 0.72, -0.62, 0.34, 0.4, 0.55, 0.55, C.DM_ARMOR);      // control kiosk
    H.box(r, 0.72, -0.62, 0.64, 0.44, 0.6, 0.06, C.DM_DARK);
    H.rod(r, 0.72, -0.78, 0.85, 0.02, 0.4, C.GUNMETAL, { rx: 0 }); // antenna
    return { root: r, fit: 1.9 };
  },

  dm_aa_tower: (H) => {
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.15, 0.85, 0.85, 0.3, C.CONCRETE);             // base
    H.rod(r, 0, 0, 0.45, 0.16, 0.35, C.DM_DARK, { rx: 0, v: 12 }); // pedestal
    H.box(r, 0, 0, 0.66, 0.34, 0.3, 0.14, C.DM_ARMOR);             // mount
    for (const s of [-1, 1]) {
      H.box(r, s * 0.20, -0.05, 0.86, 0.16, 0.5, 0.16, C.GUNMETAL, { rx: -42 }); // missile boxes
      for (let i = 0; i < 2; i++)
        H.rod(r, s * 0.20, 0.10 - i * 0.14, 0.86 + i * 0.12, 0.035, 0.5, C.WHITE, { rx: 48 });
    }
    H.rod(r, 0, 0.22, 0.72, 0.015, 0.30, C.GUNMETAL, { rx: 0 });   // sensor rod
    H.ball(r, 0, 0.22, 0.88, 0.035, C.RED, { e: true });
    return { root: r, fit: 0.95 };
  },

  dm_service_depot: (H) => {
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.05, 1.9, 1.9, 0.1, C.CONCRETE);               // floor
    H.box(r, 0, 0, 0.105, 1.3, 0.16, 0.012, C.YELLOW);             // stripes
    H.box(r, 0, 0.5, 0.105, 1.3, 0.10, 0.012, C.YELLOW);
    H.box(r, 0, -0.5, 0.105, 1.3, 0.10, 0.012, C.YELLOW);
    for (const s of [-1, 1])
      H.box(r, s * 0.8, 0, 0.55, 0.16, 1.5, 1.0, C.DM_DARK);       // side posts
    H.box(r, 0, 0, 1.06, 1.8, 1.4, 0.12, C.DM_ARMOR);              // gantry beam roof
    H.box(r, 0, 0, 0.92, 0.2, 0.9, 0.14, C.GUNMETAL);              // crane rail
    H.rod(r, 0.1, 0.2, 0.72, 0.03, 0.4, C.GUNMETAL, { rx: 0 });    // crane hook arm
    H.box(r, 0.1, 0.2, 0.50, 0.14, 0.14, 0.08, C.YELLOW);
    return { root: r, fit: 1.9 };
  },

  nx_emp_cannon: (H) => {
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.2, 1.7, 1.7, 0.4, C.CONCRETE);                // bunker base
    H.box(r, 0, -0.45, 0.5, 1.0, 0.7, 0.3, C.DM_ARMOR);            // housing
    H.rod(r, 0, 0, 0.62, 0.30, 0.35, C.GUNMETAL, { rx: 0, v: 14 }); // turntable
    for (let i = 0; i < 4; i++) {                                  // coil rings up the barrel
      const t = 0.15 + i * 0.22;
      H.rod(r, 0, 0.18 + t * 0.55, 0.80 + t * 0.75, 0.13 - i * 0.012, 0.05, C.BLUEGREY, { rx: -54, v: 12 });
    }
    H.rod(r, 0, 0.18 + 0.5 * 0.55, 0.80 + 0.5 * 0.75, 0.055, 1.05, C.GUNMETAL, { rx: -54, v: 12 }); // core barrel
    H.ball(r, 0, 0.18 + 1.0 * 0.55, 0.80 + 1.0 * 0.75, 0.09, C.CYAN, { e: true });
    return { root: r, fit: 1.9 };
  },

  dm_ion_uplink: (H) => {
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.25, 1.8, 1.8, 0.5, C.DM_ARMOR);               // main block
    H.box(r, 0, 0.55, 0.62, 1.2, 0.6, 0.28, C.DM_DARK);            // front annex
    for (const s of [-1, 1])                                        // struts
      H.rod(r, s * 0.5, -0.25, 0.85, 0.045, 0.75, C.GUNMETAL, { rx: s * 28, ry: 0 });
    H.rod(r, 0, -0.25, 1.15, 0.62, 0.16, C.WHITE, { rx: 0, v: 18 }); // dish (up)
    H.rod(r, 0, -0.25, 1.24, 0.5, 0.06, C.BLUEGREY, { rx: 0, v: 18 }); // dish inner
    H.rod(r, 0, -0.25, 1.30, 0.14, 0.10, C.CYAN, { rx: 0, v: 14, e: true });
    H.rod(r, 0.62, 0.62, 0.95, 0.02, 0.55, C.GUNMETAL, { rx: 0 }); // antennas
    H.rod(r, 0.45, 0.72, 0.88, 0.02, 0.4, C.GUNMETAL, { rx: 0 });
    H.ball(r, 0.62, 0.62, 1.24, 0.04, C.RED, { e: true });
    return { root: r, fit: 1.9 };
  },

  /* ============================ new designs ============================= */

  nx_conyard: (H) => {                       // construction yard, 3x3
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.06, 2.8, 2.8, 0.12, C.CONCRETE);              // base slab
    H.box(r, 0, -0.35, 0.75, 2.2, 1.8, 1.3, C.DM_ARMOR);           // garage hall
    H.box(r, 0, -0.35, 1.46, 2.3, 1.9, 0.12, C.DM_DARK);           // roof
    H.box(r, 0, 0.56, 0.62, 1.3, 0.06, 1.0, C.TRACK);              // dark doorway
    H.box(r, 0, 0.56, 1.18, 1.5, 0.08, 0.14, C.YELLOW);            // stripe lintel
    H.box(r, -0.82, 0.56, 0.62, 0.14, 0.08, 1.05, C.DM_DARK);      // door posts
    H.box(r, 0.82, 0.56, 0.62, 0.14, 0.08, 1.05, C.DM_DARK);
    H.box(r, 0, 1.05, 0.125, 1.6, 0.14, 0.013, C.YELLOW);          // apron warning stripes
    H.box(r, 0, 1.30, 0.125, 1.6, 0.10, 0.013, C.YELLOW);
    for (const s of [-1, 1]) {                                     // crane gantry legs
      H.box(r, s * 1.22, 0.95, 0.98, 0.16, 0.16, 1.85, C.DM_DARK);
      H.box(r, s * 1.22, 0.95, 0.18, 0.3, 0.3, 0.12, C.CONCRETE);  // feet
      H.ball(r, s * 1.22, 0.95, 2.06, 0.05, C.YELLOW, { e: true }); // blinkers
    }
    H.box(r, 0, 0.95, 1.95, 2.6, 0.22, 0.18, C.DM_ARMOR);          // gantry beam
    H.box(r, 0.35, 0.95, 1.78, 0.32, 0.28, 0.16, C.GUNMETAL);      // trolley
    H.rod(r, 0.35, 0.95, 1.5, 0.02, 0.42, C.GUNMETAL, { rx: 0 });  // cable
    H.box(r, 0.35, 0.95, 1.24, 0.16, 0.10, 0.10, C.YELLOW);        // hook block
    H.box(r, 0.35, 0.95, 1.13, 0.05, 0.14, 0.12, C.GUNMETAL);      // hook
    H.box(r, -0.6, -0.35, 1.6, 0.4, 0.4, 0.18, C.GUNMETAL);        // roof vent
    H.box(r, 0.55, -0.35, 1.62, 0.32, 0.32, 0.22, C.DM_DARK);      // roof cabin
    H.rod(r, 0.9, -1.05, 1.85, 0.02, 0.8, C.GUNMETAL, { rx: 0 });  // antenna
    H.ball(r, 0.9, -1.05, 2.27, 0.045, C.RED, { e: true });
    return { root: r, fit: 2.9 };
  },

  nx_refinery: (H) => {                      // veridium refinery
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.06, 2.6, 2.6, 0.12, C.CONCRETE);              // base slab
    H.box(r, -0.55, -0.55, 0.7, 1.35, 1.35, 1.2, C.DM_ARMOR);      // processing block
    H.box(r, -0.55, -0.55, 1.42, 0.85, 0.85, 0.55, C.DM_DARK);     // tower
    H.rod(r, -0.55, -0.55, 1.83, 0.3, 0.3, C.GUNMETAL, { rx: 0, v: 14 }); // tower cap
    H.ball(r, -0.55, -0.55, 2.0, 0.06, C.GREEN, { e: true });      // process light
    H.rod(r, -1.0, -1.0, 1.5, 0.11, 1.4, C.GUNMETAL, { rx: 0 });   // chimney
    H.rod(r, -1.0, -1.0, 2.18, 0.14, 0.08, C.TRACK, { rx: 0 });    // chimney lip
    for (const ty of [-0.65, 0.25]) {                              // storage tanks
      H.rod(r, 0.78, ty, 0.62, 0.4, 1.0, C.BLUEGREY, { rx: 0, v: 16 });
      H.ball(r, 0.78, ty, 1.12, 0.4, C.BLUEGREY, { squash: 0.45 });
      H.rod(r, 0.78, ty, 0.28, 0.44, 0.1, C.DM_DARK, { rx: 0, v: 16 });
    }
    H.rod(r, 0.15, -0.65, 1.02, 0.055, 1.15, C.GUNMETAL, { rx: 0, ry: 90 }); // pipes to tanks
    H.rod(r, 0.15, 0.25, 0.9, 0.055, 1.15, C.GUNMETAL, { rx: 0, ry: 90 });
    H.ball(r, 0.78, -0.65, 1.02, 0.075, C.GUNMETAL);               // pipe elbows
    H.ball(r, 0.78, 0.25, 0.9, 0.075, C.GUNMETAL);
    H.box(r, 0.25, 0.78, 0.11, 1.5, 0.9, 0.06, C.GREEN, { e: true }); // glowing intake pit
    H.box(r, 0.25, 0.28, 0.17, 1.7, 0.1, 0.14, C.CONCRETE);        // pit rims
    H.box(r, 0.25, 1.26, 0.17, 1.7, 0.1, 0.14, C.CONCRETE);
    H.box(r, -0.55, 0.78, 0.17, 0.1, 1.06, 0.14, C.CONCRETE);
    H.box(r, 1.05, 0.78, 0.17, 0.1, 1.06, 0.14, C.CONCRETE);
    H.box(r, -0.35, 0.55, 0.75, 0.55, 0.7, 0.3, C.DM_DARK);        // intake conveyor arm
    H.rod(r, -0.35, 0.9, 0.55, 0.05, 0.5, C.GUNMETAL, { rx: 25 }); // slurping duct
    return { root: r, fit: 2.7 };
  },

  nx_silo: (H) => {                          // veridium silo
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.04, 0.8, 0.8, 0.08, C.CONCRETE);              // base slab
    H.rod(r, 0, 0, 0.14, 0.34, 0.14, C.DM_DARK, { rx: 0, v: 16 }); // base ring
    H.rod(r, 0, 0, 0.6, 0.3, 0.85, C.DM_ARMOR, { rx: 0, v: 16 });  // tank
    H.ball(r, 0, 0, 1.02, 0.3, C.DM_DARK, { squash: 0.5 });        // dome
    H.rod(r, 0, 0, 0.72, 0.315, 0.07, C.GREEN, { rx: 0, v: 16, e: true }); // fill-level ring
    H.rod(r, 0, 0, 0.5, 0.315, 0.045, C.DM_DARK, { rx: 0, v: 16 }); // bands
    H.rod(r, 0, 0, 0.92, 0.315, 0.045, C.DM_DARK, { rx: 0, v: 16 });
    H.box(r, -0.05, 0.32, 0.6, 0.025, 0.03, 0.8, C.GUNMETAL);      // ladder rails
    H.box(r, 0.05, 0.32, 0.6, 0.025, 0.03, 0.8, C.GUNMETAL);
    for (let i = 0; i < 4; i++)
      H.box(r, 0, 0.32, 0.32 + i * 0.2, 0.11, 0.025, 0.025, C.GUNMETAL); // rungs
    H.rod(r, 0.16, -0.16, 1.22, 0.03, 0.32, C.GUNMETAL, { rx: 0 }); // vent pipe
    H.ball(r, 0, 0, 1.18, 0.07, C.DM_ARMOR);                       // top valve
    return { root: r, fit: 0.85 };
  },

  dm_power_plant: (H) => {                   // solar array
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.05, 1.8, 1.8, 0.1, C.CONCRETE);               // base slab
    for (const y of [-0.55, 0.05, 0.65]) {                         // 3 panel rows
      H.box(r, -0.35, y, 0.42, 1.05, 0.46, 0.05, C.BLUEGREY, { rx: -32 }); // panel
      H.box(r, -0.35, y, 0.24, 0.9, 0.08, 0.32, C.DM_DARK);        // frame spine
      H.box(r, -0.75, y, 0.2, 0.06, 0.3, 0.25, C.GUNMETAL);        // end legs
      H.box(r, 0.05, y, 0.2, 0.06, 0.3, 0.25, C.GUNMETAL);
    }
    H.box(r, 0.62, -0.3, 0.33, 0.5, 0.55, 0.55, C.DM_ARMOR);       // transformer box
    H.box(r, 0.62, -0.3, 0.64, 0.55, 0.6, 0.08, C.DM_DARK);
    H.rod(r, 0.52, -0.3, 0.78, 0.05, 0.22, C.GUNMETAL, { rx: 0 }); // insulators
    H.rod(r, 0.72, -0.3, 0.78, 0.05, 0.22, C.GUNMETAL, { rx: 0 });
    H.box(r, 0.62, -0.01, 0.45, 0.14, 0.05, 0.12, C.YELLOW, { e: true }); // status light
    H.rod(r, 0.62, 0.35, 0.13, 0.05, 0.65, C.GUNMETAL);            // conduit (along Y)
    H.box(r, 0.62, 0.72, 0.16, 0.22, 0.22, 0.2, C.DM_DARK);        // junction box
    return { root: r, fit: 1.9 };
  },

  so_power_plant: (H) => {                   // vortex turbine (vertical axis)
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.06, 1.7, 1.7, 0.12, C.SO_DARK);               // base slab
    H.box(r, 0, 0, 0.3, 1.0, 1.0, 0.36, C.SO_ARMOR);               // machine bunker
    H.box(r, 0, 0.55, 0.32, 0.45, 0.3, 0.4, C.SO_DARK);            // front intake
    H.box(r, 0, 0.71, 0.4, 0.3, 0.04, 0.08, C.RED, { e: true });   // red slit
    for (const s of [-1, 1])
      H.box(r, s * 0.42, -0.3, 0.52, 0.06, 0.3, 0.08, C.RED, { e: true }); // side slits
    H.rod(r, 0, 0, 1.0, 0.13, 1.1, C.SO_DARK, { rx: 0, v: 12 });   // tower
    H.rod(r, 0, 0, 1.9, 0.05, 0.85, C.GUNMETAL, { rx: 0 });        // spindle
    H.ball(r, 0, 0, 2.36, 0.06, C.RED, { e: true });               // tip beacon
    const spin = H.group(r);                                       // rotor (engine spins)
    spin.position.set(0, 0, 1.95);
    H.rod(spin, 0, 0, 0.4, 0.16, 0.06, C.SO_ARMOR, { rx: 0, v: 12 }); // rings
    H.rod(spin, 0, 0, -0.4, 0.16, 0.06, C.SO_ARMOR, { rx: 0, v: 12 });
    for (let k = 0; k < 3; k++) {                                  // 3 vertical blades
      const a = k * 120, x = Math.cos(a * Math.PI / 180) * 0.3, y = Math.sin(a * Math.PI / 180) * 0.3;
      H.box(spin, x, y, 0, 0.07, 0.28, 0.8, C.SO_ARMOR, { rz: a });
      H.box(spin, x * 0.5, y * 0.5, 0.4, 0.34, 0.05, 0.05, C.GUNMETAL, { rz: a }); // arms
      H.box(spin, x * 0.5, y * 0.5, -0.4, 0.34, 0.05, 0.05, C.GUNMETAL, { rz: a });
    }
    return { root: r, fit: 1.9, spin };
  },

  dm_barracks: (H) => {                      // quonset barracks
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.06, 1.8, 1.8, 0.12, C.CONCRETE);              // base slab
    H.rod(r, 0, -0.1, 0.68, 0.6, 1.5, C.DM_ARMOR, { v: 16 });      // quonset hull (along Y)
    H.box(r, 0, 0.68, 0.62, 1.05, 0.1, 1.1, C.DM_DARK);            // front cap
    H.box(r, 0, -0.88, 0.62, 1.05, 0.1, 1.1, C.DM_DARK);           // rear cap
    H.box(r, 0, 0.74, 0.4, 0.4, 0.06, 0.6, C.TRACK);               // door
    H.box(r, 0, 0.74, 0.88, 0.12, 0.05, 0.07, C.YELLOW, { e: true }); // door lamp
    H.box(r, 0, -0.35, 1.28, 0.18, 0.3, 0.1, C.DM_DARK);           // roof vents
    H.box(r, 0, 0.2, 1.28, 0.18, 0.3, 0.1, C.DM_DARK);
    H.rod(r, 0.78, 0.55, 0.66, 0.025, 1.2, C.GUNMETAL, { rx: 0 }); // flag pole
    H.ball(r, 0.78, 0.55, 1.27, 0.035, C.YELLOW);                  // pole cap
    H.box(r, 0.9, 0.55, 1.18, 0.22, 0.03, 0.15, C.DM_ARMOR);       // flag
    H.box(r, -0.55, 0.8, 0.17, 0.35, 0.18, 0.14, C.SAND);          // sandbags
    H.box(r, -0.62, 0.62, 0.17, 0.3, 0.18, 0.14, C.SAND);
    H.box(r, -0.5, 0.71, 0.29, 0.3, 0.16, 0.12, C.SAND);
    H.rod(r, -0.35, -0.85, 1.4, 0.015, 0.35, C.GUNMETAL, { rx: 0 }); // whip antenna
    return { root: r, fit: 1.9 };
  },

  dm_factory: (H) => {                       // war factory
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.06, 2.8, 2.8, 0.12, C.CONCRETE);              // base slab
    H.box(r, -0.15, -0.15, 0.85, 2.3, 2.3, 1.5, C.DM_ARMOR);       // hangar hall
    H.box(r, -0.15, -0.15, 1.68, 2.4, 2.4, 0.16, C.DM_DARK);       // roof
    H.box(r, -0.15, 1.0, 0.72, 1.5, 0.08, 1.2, C.TRACK);           // dark doorway
    H.box(r, -0.15, 1.01, 1.42, 1.7, 0.08, 0.16, C.YELLOW);        // lintel stripe
    H.box(r, -0.95, 1.01, 0.72, 0.14, 0.1, 1.25, C.DM_DARK);       // door posts
    H.box(r, 0.65, 1.01, 0.72, 0.14, 0.1, 1.25, C.DM_DARK);
    H.ball(r, -0.95, 1.05, 1.4, 0.045, C.YELLOW, { e: true });     // door lights
    H.ball(r, 0.65, 1.05, 1.4, 0.045, C.YELLOW, { e: true });
    for (let i = 0; i < 3; i++)                                    // roof vents
      H.box(r, -0.7 + i * 0.55, -0.55, 1.84, 0.3, 0.5, 0.18, C.GUNMETAL);
    H.rod(r, -0.9, -1.0, 2.0, 0.05, 0.55, C.GUNMETAL, { rx: 0 });  // stack
    H.rod(r, -0.9, -1.0, 2.28, 0.065, 0.06, C.TRACK, { rx: 0 });
    H.box(r, 1.25, -0.15, 1.35, 0.14, 2.2, 0.14, C.GUNMETAL);      // side crane rail
    H.box(r, 1.25, -1.05, 0.7, 0.12, 0.12, 1.3, C.DM_DARK);        // rail supports
    H.box(r, 1.25, 0.75, 0.7, 0.12, 0.12, 1.3, C.DM_DARK);
    H.box(r, 1.25, 0.2, 1.24, 0.2, 0.3, 0.12, C.YELLOW);           // trolley
    H.rod(r, 1.25, 0.2, 1.05, 0.02, 0.28, C.GUNMETAL, { rx: 0 });  // cable
    H.box(r, 1.25, 0.2, 0.9, 0.1, 0.08, 0.08, C.GUNMETAL);         // hook
    return { root: r, fit: 2.9 };
  },

  so_factory: (H) => {                       // Serpent war factory
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.06, 2.8, 2.8, 0.12, C.SO_DARK);               // base slab
    H.box(r, 0, -0.1, 0.8, 2.3, 2.3, 1.4, C.SO_ARMOR);             // hall
    H.box(r, 0, -0.7, 1.6, 2.4, 1.35, 0.14, C.SO_DARK, { rx: 22 }); // sloped roof panes
    H.box(r, 0, 0.5, 1.6, 2.4, 1.35, 0.14, C.SO_DARK, { rx: -22 });
    H.box(r, 0, -0.1, 1.86, 2.45, 0.3, 0.12, C.SO_ARMOR);          // ridge cap
    H.box(r, 0, 1.06, 0.7, 1.5, 0.08, 1.15, C.TRACK);              // dark doorway
    H.box(r, 0, 1.07, 1.35, 1.6, 0.05, 0.07, C.RED, { e: true });  // slit above door
    for (const s of [-1, 1]) {
      H.box(r, s * 1.16, 0.3, 1.0, 0.05, 0.7, 0.07, C.RED, { e: true }); // side slits
      H.box(r, s * 0.85, 1.07, 0.7, 0.14, 0.1, 1.2, C.SO_DARK);    // door posts
      H.box(r, s * 1.2, -0.8, 0.5, 0.3, 0.22, 1.0, C.SO_DARK, { ry: s * 14 }); // buttresses
      H.box(r, s * 1.2, 0.2, 0.5, 0.3, 0.22, 1.0, C.SO_DARK, { ry: s * 14 });
    }
    H.rod(r, -0.8, -0.9, 2.25, 0.03, 1.0, C.GUNMETAL, { rx: 0 });  // spike antennas
    H.ball(r, -0.8, -0.9, 2.77, 0.05, C.RED, { e: true });
    H.rod(r, 0.8, -0.9, 2.1, 0.025, 0.7, C.GUNMETAL, { rx: 0 });
    return { root: r, fit: 2.9 };
  },

  dm_guard_tower: (H) => {                   // vulcan watchtower
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.04, 0.75, 0.75, 0.08, C.CONCRETE);            // base slab
    for (const sx of [-1, 1]) for (const sy of [-1, 1])
      H.box(r, sx * 0.24, sy * 0.24, 0.42, 0.11, 0.11, 0.75, C.CONCRETE); // legs
    H.box(r, 0, 0, 0.55, 0.62, 0.62, 0.07, C.CONCRETE);            // collar
    H.box(r, 0, 0, 0.84, 0.72, 0.72, 0.08, C.DM_DARK);             // platform
    H.box(r, 0, 0, 1.06, 0.55, 0.55, 0.4, C.DM_ARMOR);             // cabin
    H.box(r, 0, 0, 1.16, 0.57, 0.57, 0.1, C.TRACK);                // window band
    H.box(r, 0, 0, 1.3, 0.64, 0.64, 0.07, C.DM_DARK);              // roof
    H.box(r, 0, 0.3, 1.06, 0.22, 0.18, 0.14, C.GUNMETAL);          // gun mount
    for (const s of [-1, 1]) {
      H.rod(r, s * 0.055, 0.5, 1.06, 0.028, 0.34, C.GUNMETAL);     // vulcan barrels (+Y)
      H.rod(r, s * 0.055, 0.68, 1.06, 0.04, 0.04, C.TRACK);        // muzzle rings
    }
    H.box(r, 0, 0.3, 1.36, 0.1, 0.08, 0.07, C.YELLOW, { e: true }); // searchlight
    H.rod(r, -0.24, -0.24, 1.52, 0.015, 0.4, C.GUNMETAL, { rx: 0 }); // antenna
    return { root: r, fit: 0.8 };
  },

  dm_rocket_tower: (H) => {                  // rocket rack tower
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.04, 0.75, 0.75, 0.08, C.CONCRETE);            // base slab
    for (const sx of [-1, 1]) for (const sy of [-1, 1])
      H.box(r, sx * 0.22, sy * 0.22, 0.55, 0.1, 0.1, 1.0, C.CONCRETE); // tall legs
    H.box(r, 0, 0, 0.7, 0.55, 0.55, 0.06, C.CONCRETE);             // collar
    H.box(r, 0, 0, 1.1, 0.65, 0.65, 0.08, C.DM_DARK);              // platform
    H.rod(r, 0, 0, 1.24, 0.12, 0.22, C.DM_ARMOR, { rx: 0, v: 12 }); // pedestal
    H.box(r, 0, -0.05, 1.42, 0.32, 0.3, 0.18, C.DM_ARMOR);         // rack mount
    H.box(r, 0, 0.08, 1.62, 0.46, 0.55, 0.46, C.GUNMETAL, { rx: -38 }); // 2x2 rocket box
    for (const sx of [-1, 1]) for (const sj of [-1, 1])            // rocket tubes
      H.rod(r, sx * 0.11, 0.08 - sj * 0.07, 1.62 + sj * 0.1, 0.055, 0.62, C.WHITE, { rx: 52 });
    H.ball(r, 0, -0.28, 1.52, 0.04, C.RED, { e: true });           // sensor eye
    H.box(r, 0.26, 0.26, 1.17, 0.08, 0.08, 0.06, C.YELLOW, { e: true }); // platform lamp
    H.rod(r, -0.26, -0.26, 1.75, 0.015, 0.5, C.GUNMETAL, { rx: 0 }); // antenna
    return { root: r, fit: 0.8 };
  },

  dm_radar: (H) => {                         // radar center, dish spins
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.06, 1.8, 1.8, 0.12, C.CONCRETE);              // base slab
    H.box(r, 0, -0.25, 0.42, 1.6, 1.2, 0.65, C.DM_ARMOR);          // bunker
    H.box(r, 0, -0.25, 0.79, 1.68, 1.28, 0.1, C.DM_DARK);          // bunker roof
    H.box(r, 0, 0.55, 0.3, 1.0, 0.5, 0.45, C.DM_DARK);             // front annex
    H.box(r, 0, 0.81, 0.28, 0.4, 0.04, 0.4, C.TRACK);              // door
    H.box(r, 0.32, 0.81, 0.55, 0.08, 0.04, 0.06, C.YELLOW, { e: true }); // door lamp
    H.box(r, 0, 0.36, 0.6, 1.2, 0.05, 0.12, C.TRACK);              // window band
    H.rod(r, 0, -0.25, 1.06, 0.14, 0.45, C.GUNMETAL, { rx: 0, v: 12 }); // pylon
    H.rod(r, -0.62, 0.4, 0.85, 0.02, 0.55, C.GUNMETAL, { rx: 0 }); // corner antenna
    H.ball(r, -0.62, 0.4, 1.14, 0.035, C.GREEN, { e: true });
    const spin = H.group(r);                                       // dish assembly
    spin.position.set(0, -0.25, 1.3);
    H.box(spin, 0, 0, 0.05, 0.4, 0.22, 0.18, C.DM_ARMOR);          // yoke
    for (const s of [-1, 1])
      H.box(spin, s * 0.2, 0.02, 0.28, 0.08, 0.14, 0.42, C.GUNMETAL, { rx: -22 }); // arms
    H.rod(spin, 0, 0.1, 0.5, 0.6, 0.12, C.WHITE, { rx: -55, v: 18 }); // dish
    H.rod(spin, 0, 0.14, 0.53, 0.48, 0.05, C.BLUEGREY, { rx: -55, v: 18 }); // dish inner
    H.rod(spin, 0, 0.26, 0.62, 0.025, 0.4, C.GUNMETAL, { rx: 35 }); // feed boom
    H.ball(spin, 0, 0.36, 0.76, 0.05, C.RED, { e: true });         // feed tip
    return { root: r, fit: 1.9, spin };
  },

  dm_tech_center: (H) => {                   // research center
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.06, 1.8, 1.8, 0.12, C.CONCRETE);              // base slab
    H.box(r, 0, -0.1, 0.5, 1.65, 1.5, 0.85, C.DM_ARMOR);           // main block
    H.box(r, 0, -0.1, 0.96, 1.7, 1.55, 0.08, C.DM_DARK);           // trim
    H.box(r, 0, 0.75, 0.35, 1.1, 0.35, 0.55, C.DM_DARK);           // front annex
    H.box(r, 0, 0.93, 0.3, 0.4, 0.04, 0.45, C.TRACK);              // door
    H.box(r, 0.32, 0.93, 0.62, 0.08, 0.04, 0.06, C.YELLOW, { e: true }); // door lamp
    H.rod(r, 0, -0.15, 0.97, 0.6, 0.08, C.CYAN, { rx: 0, v: 18, e: true }); // glowing core ring
    H.ball(r, 0, -0.15, 1.02, 0.58, C.BLUEGREY, { squash: 0.65 }); // dome
    H.rod(r, -0.65, -0.6, 1.35, 0.03, 0.8, C.GUNMETAL, { rx: 0 }); // antenna cluster
    H.rod(r, -0.5, -0.45, 1.25, 0.02, 0.55, C.GUNMETAL, { rx: 0 });
    H.rod(r, -0.78, -0.42, 1.2, 0.02, 0.45, C.GUNMETAL, { rx: 0 });
    H.ball(r, -0.65, -0.6, 1.77, 0.05, C.RED, { e: true });        // mast beacon
    H.rod(r, -0.5, -0.45, 1.54, 0.12, 0.04, C.WHITE, { rx: 0, v: 12 }); // mini dish
    H.rod(r, 0.72, -0.1, 0.5, 0.05, 1.3, C.GUNMETAL);              // side conduit (along Y)
    H.box(r, 0.6, -0.75, 0.35, 0.35, 0.3, 0.45, C.DM_DARK);        // rear utility box
    return { root: r, fit: 1.9 };
  },

  so_laser_turret: (H) => {                  // Serpent laser turret
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.04, 0.65, 0.65, 0.08, C.SO_DARK);             // base slab
    H.box(r, 0, 0, 0.14, 0.4, 0.4, 0.16, C.SO_ARMOR);              // pedestal base
    H.rod(r, 0, 0, 0.32, 0.13, 0.28, C.SO_DARK, { rx: 0, v: 12 }); // pedestal
    H.box(r, 0, -0.02, 0.52, 0.34, 0.3, 0.2, C.SO_ARMOR);          // mount
    H.ball(r, 0, -0.2, 0.56, 0.11, C.SO_DARK);                     // capacitor
    for (const s of [-1, 1]) {
      H.box(r, s * 0.1, 0.12, 0.55, 0.09, 0.4, 0.12, C.GUNMETAL);  // housings
      H.rod(r, s * 0.1, 0.3, 0.55, 0.035, 0.45, C.RED, { e: true }); // laser rods (+Y)
      H.ball(r, s * 0.1, 0.52, 0.55, 0.05, C.RED, { e: true });    // emitter tips
    }
    H.rod(r, 0, 0, 0.68, 0.015, 0.15, C.GUNMETAL, { rx: 0 });      // sensor mast
    H.ball(r, 0, 0, 0.77, 0.03, C.RED, { e: true });
    return { root: r, fit: 0.7 };
  },

  so_obelisk: (H) => {                       // the Serpent obelisk
    const C = H.C, r = H.group();
    H.box(r, 0, 0, 0.05, 0.75, 0.75, 0.1, C.SO_DARK);              // base slab
    for (const sx of [-1, 1]) for (const sy of [-1, 1])
      H.box(r, sx * 0.26, sy * 0.26, 0.25, 0.14, 0.14, 0.4, C.SO_ARMOR, { rz: 45 }); // buttresses
    H.box(r, 0, 0, 0.5, 0.5, 0.5, 0.75, C.SO_DARK);                // tier 1
    H.box(r, 0, 0, 1.15, 0.38, 0.38, 0.65, C.SO_ARMOR, { rz: 45 }); // tier 2 (twisted)
    H.box(r, 0, 0, 1.75, 0.27, 0.27, 0.6, C.SO_DARK);              // tier 3
    H.box(r, 0, 0, 2.25, 0.18, 0.18, 0.45, C.SO_ARMOR, { rz: 45 }); // tier 4 (twisted)
    H.box(r, 0, 0, 2.6, 0.15, 0.15, 0.4, C.RED, { rz: 45, e: true }); // crystal shaft
    H.ball(r, 0, 0, 2.84, 0.1, C.RED, { e: true });                // crystal tip
    H.box(r, 0, 0.255, 0.6, 0.05, 0.02, 0.4, C.RED, { e: true });  // charge slits
    H.box(r, 0, 0.14, 1.75, 0.04, 0.02, 0.35, C.RED, { e: true });
    H.box(r, 0.255, 0, 0.6, 0.02, 0.05, 0.4, C.RED, { e: true });
    H.box(r, -0.255, 0, 0.6, 0.02, 0.05, 0.4, C.RED, { e: true });
    return { root: r, fit: 0.8 };
  },
};

/* ============================ desert props ============================== */

var PROP_BUILDERS = {

  cactus: (H, v) => {                        // saguaro
    const C = H.C, r = H.group();
    const h = 0.7 + (v % 4) * 0.12;                                // trunk height
    H.rod(r, 0, 0, h / 2, 0.07, h, C.CACTUS, { rx: 0, v: 8 });
    H.ball(r, 0, 0, h, 0.07, C.CACTUS, { v: 8 });
    const side = (v % 2) ? 1 : -1;
    const arm = (s, z, ah) => {                                    // elbow arm
      H.rod(r, s * 0.14, 0, z, 0.05, 0.18, C.CACTUS, { rx: 0, ry: 90, v: 8 });
      H.rod(r, s * 0.22, 0, z + ah / 2, 0.05, ah, C.CACTUS, { rx: 0, v: 8 });
      H.ball(r, s * 0.22, 0, z + ah, 0.05, C.CACTUS, { v: 8 });
    };
    arm(side, h * 0.45, 0.26 + (v % 3) * 0.04);
    if (v >= 4) arm(-side, h * 0.62, 0.2);                         // second arm
    return { root: r, fit: 0.5 };
  },

  rock: (H, v) => {                          // boulder cluster
    const r = H.group();
    const n = 2 + (v % 3), k = 0.8 + (v % 4) * 0.1;
    const D = [
      [0, 0, 0.15, 0.42, 0.34, 0.32, 0x6b675c, 18],
      [0.22, -0.15, 0.1, 0.26, 0.22, 0.2, 0x77705f, -12],
      [-0.2, 0.14, 0.08, 0.2, 0.24, 0.16, 0x7a6e4b, 30],
      [0.05, 0.22, 0.07, 0.16, 0.14, 0.14, 0x6b675c, -25],
    ];
    for (let i = 0; i < n; i++) {
      const d = D[i];
      H.box(r, d[0] * k, d[1] * k, d[2] * k, d[3] * k, d[4] * k, d[5] * k, d[6], { rz: d[7] + v * 9 });
    }
    return { root: r, fit: 0.6 };
  },

  bush: (H, v) => {                          // scrub bush
    const C = H.C, r = H.group();
    const n = 3 + (v % 3), k = 0.75 + (v % 4) * 0.1;
    const cols = [C.CACTUS, 0x5c6b34, 0x6e6440, C.CACTUS, 0x5c6b34];
    const P = [
      [0, 0, 0.1, 0.13], [0.12, 0.06, 0.08, 0.1], [-0.11, 0.05, 0.08, 0.1],
      [0.03, -0.12, 0.07, 0.09], [-0.06, -0.1, 0.09, 0.08],
    ];
    for (let i = 0; i < n; i++) {
      const p = P[i];
      H.ball(r, p[0] * k, p[1] * k, p[2] * k, p[3] * k, cols[i], { squash: 0.5, v: 8 });
    }
    return { root: r, fit: 0.4 };
  },
};

if (typeof module !== "undefined") module.exports = { STRUCT_BUILDERS, PROP_BUILDERS };
