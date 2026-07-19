
# C&C Tiberian Sun (1999) + Firestorm — Complete Faction Rosters & Tech Trees

**Primary source:** actual `RULES.INI` + `FIRESTRM.INI` game data (Vinifera-Developers archival repo of the original Westwood INIs). All numbers below are exact engine values unless marked [uncertain].

**Reading the stats:**
- **HP** = `Strength`. **Range** in cells. **ROF** = frames between shots (game runs ~15 frames/sec at normal speed → ROF 60 ≈ 4 s). **Speed** = raw engine speed value (relative; ~4 = slow tracked, 10+ = very fast). **Power**: positive = produced, negative = drained.
- **Armor classes** (damage order in warhead `Verses` lists): `none, wood, light, heavy, concrete`.
- **TechLevel** 1–10 = buildable (gated by MP tech slider); **−1 = not normally buildable** (campaign/crate only).
- Prerequisite aliases (from `[General]`): `POWER=GAPOWR|NAPOWR|NAAPWR`, `BARRACKS=GAPILE|NAHAND`, `FACTORY=GAWEAP|NAWEAP`, `RADAR=GARADR|NARADR`, `TECH=GATECH|NATECH`. Firestorm extends factory aliases to include the deployed Mobile War Factories (`DGWEAP`/`DNWEAP`).
- Economy: green Tiberium (Riparius) = 25/bail, blue (Vinifera) = 40, Aboreus 30, Cruentus 70. Harvester holds 28 bails (700 cr green), Refinery stores 80 bails, Silo 60. Refund 50%, repair rate 0.016, BuildSpeed 0.8 min per $1000 [engine formula], low-power build rate 30–75%.
- Veterancy: earn by kills worth `VeteranRatio`× own cost (TS: 10×; **FS: 5×**); 2 ranks. TS bonuses +25% damage/armor; FS buffed to +50% damage/armor, +30% speed/ROF.

---

## 1. STRUCTURES

### 1.1 GDI structures (base game)

| Structure (ID) | Cost | Power | HP | Armor | Prerequisite | Function / notes |
|---|---|---|---|---|---|---|
| Construction Yard (GACNST) | 2500 | 0 | 1000 | heavy | (deploy MCV) | Base builder; undeploys into MCV; capturable |
| Power Plant (GAPOWR) | 300 | **+100** | 750 | wood | none (TL1) | Power. Accepts **2× Power Turbine** add-ons |
| — Power Turbine upgrade (GAPOWRUP) | 100 | **+50** each | — | wood | Power Plant (TL7) | Plug-in; max 2 per plant → 200 total |
| Tiberium Refinery (PROC) | 2000 | −30 | 900 | heavy | any Power (TL1) | Comes with free Harvester; stores 80 bails |
| Tiberium Silo (GASILO) | 150 | −10 | 300 | wood | Refinery (TL1) | +60 bails storage; explodes when destroyed |
| Barracks (GAPILE) | 300 | −20 | 800 | wood | any Power (TL1) | Trains GDI infantry |
| War Factory (GAWEAP) | 2000 | −30 | 1000 | heavy | Refinery + Barracks (TL2) | Builds vehicles |
| Radar (GARADR) | 1000 | −40 | 1000 | wood | Refinery (TL3) | Minimap; unlocks Helipad/SAM/EMP/Tech Center |
| Helipad (GAHPAD) | 500 | −10 | 600 | wood | Radar (TL5) | Builds + rearms Orcas (ReloadRate 0.5 min/ammo) |
| Service Depot (GADEPT) | 1200 | −30 | 1100 | wood | any War Factory (TL7) | Repairs vehicles/aircraft (GDI only) |
| Tech Center (GATECH) | 1500 | −200 | 500 | wood | War Factory + Radar (TL6) | Unlocks tier-3 tech |
| Upgrade Center (GAPLUG) | 1000 | −150 | 1000 | wood | Refinery + Tech Center (TL10) | Has 2 plug sockets for the nodes below |
| — Ion Cannon Uplink (GAPLUG3) | 1500 | −100 | — | wood | Upgrade Ctr + Tech Ctr (TL10) | **Superweapon: Ion Cannon**, recharge 8.5 min, damage 751 |
| — Seeker Control (GAPLUG2) | 1000 | −50 | — | wood | Upgrade Ctr + Tech Ctr + War Factory (TL10) | **Superweapon: Hunter-Seeker**, recharge 12 min (drone: 500 HP, speed 25, 11000-dmg suicide bomb) |
| — Threat Rating Node (GAPLUG1) | 500 | −20 | — | wood | Upgrade Ctr | **TL−1: disabled/unbuildable in release** (cut logic) |
| Firestorm Generator (GAFIRE) | 2000 | −200 | 800 | heavy | Tech Center (TL9) | **Superweapon: Firestorm Defense** — 3 min charge, charge-drain toggle; powers Firestorm walls |
| — Firestorm Wall Section (GAFSDF) | 50/cell | −2 each | 200 | concrete | Firestorm Generator (TL9) | Impassable energy wall while active; blocks everything incl. subterranean [wall emitters themselves are normal buildings] |
| Component Tower (GACTWR) | 200 | −10 | 500 | light | Barracks (TL2) | Empty turret base; takes one weapon plug: |
| — Vulcan Cannon upgrade (GAVULC) | 150 | −20 | — | — | Comp. Tower + Barracks (TL2) | Anti-inf: dmg 18, ROF 26, rng 6 (SA warhead) |
| — RPG upgrade (GAROCK) | 600 | −20 | — | — | Comp. Tower + Barracks (TL9) | Anti-armor lobbed RPG: dmg 110, ROF 80, rng 8, min rng 2 |
| — SAM upgrade (GACSAM) | 300 | −30 | — | — | Comp. Tower + Radar (TL5) | AA: RedEye2 missile, dmg 33, rng 15 |
| EMP Cannon (NAPULS) | 1000 | −150 | 500 | heavy | Radar (TL6) — **both factions** | **Superweapon: EM Pulse**, recharge 4.5 min; 1200 "damage" EMP warhead, range 40, disables vehicles/cyborgs/defenses in radius (Spread 11) |
| Concrete Wall (GAWALL) | 50 | 0 | 150 (FS: 225) | concrete | Barracks (TL6) | Blocks fire/movement |
| Gate (GAGATE_A/B) | 250 | 0 | 350 | heavy | Barracks (TL6) | Auto-opens for friendlies; 2 orientations |
| Sandbags (GASAND) | 25 | 0 | 250 | light | Barracks | **TL−1 — not buildable in release**; crushable |
| Mobile Sensor Array, deployed (GADPSA) | — | 0 | 600 | wood | deploy LPST | `SensorArray=yes`, detects cloaked/subterranean, radius 25 cells |

### 1.2 Nod structures (base game)

| Structure (ID) | Cost | Power | HP | Armor | Prerequisite | Function / notes |
|---|---|---|---|---|---|---|
| Construction Yard (GACNST) | 2500 | 0 | 1000 | heavy | (deploy MCV) | Shared building type |
| Power Plant (NAPOWR) | 300 | **+100** | 750 | wood | none (TL1) | No turbine upgrade |
| Advanced Power Plant (NAAPWR) | 500 | **+200** | 750 | wood | War Factory (TL7) | Nod's power scaling |
| Tiberium Refinery (PROC) | 2000 | −30 | 900 | heavy | Power (TL1) | Shared; free Harvester |
| Tiberium Silo (GASILO) | 150 | −10 | 300 | wood | Refinery | Shared |
| Hand of Nod (NAHAND) | 300 | −20 | 800 | wood | Power (TL1) | Trains Nod infantry |
| War Factory (NAWEAP) | 2000 | −30 | 1000 | heavy | Refinery + Hand of Nod (TL2) | Builds vehicles |
| Radar (NARADR) | 1000 | −40 | 1000 | wood | Refinery (TL3) | |
| Helipad (NAHPAD) | 500 | −10 | 600 | wood | Radar (TL5) | Harpy/Banshee production & rearm |
| Tech Center (NATECH) | 1500 | **−100** | 500 | wood | War Factory + Radar (TL6) | (drains less than GDI's) |
| Laser turret (NALASR) | 300 | −40 | 500 | wood | Hand of Nod (TL2) | dmg 30, ROF 40, rng 5.5, instant-hit; no charge-up |
| SAM Site (NASAM) | 500 | −30 | 600 | wood | Radar (TL5) | RedEye2 AA missile, dmg 33, rng 15 |
| Obelisk of Light (NAOBEL) | 1500 | −150 | 725 | wood | Tech Center (TL9) | LaserFire: **dmg 250**, rng 10.5, ROF 120, `Charges=yes` (charge-up), 100% vs all armor |
| Stealth Generator (NASTLH) | 2500 | **−350** | 600 | wood | Refinery + Tech Center (TL9) | Cloaks everything in **12-cell radius** |
| Temple of Nod (NATMPL) | 2000 | −200 | 1000 | wood | Tech Center (TL10) | **Superweapon: Hunter-Seeker** (recharge 12 min); unlocks Cyborg Commando & Mutant Hijacker |
| Missile Silo (NAMISL) | 1300 | −50 | 1000 | wood | Tech Center (TL10) | **Superweapon: Multi-Missile** (recharge 10 min; 10 cluster warheads, 130 HE dmg each, airburst) + **SuperWeapon2: Chemical Missile** (enabled by Waste Facility as AuxBuilding; recharge 0.3 min, unpowered, 8 clusters, 100 Gas dmg, spawns Tiberium/visceroids) — chem shots limited by stored weed |
| Tiberium Waste Facility (NAWAST) | 1600 | −40 | 400 | wood | Missile Silo (TL10) | **BuildLimit 1**; free Weed Eater; collects veins to fuel chem missile |
| Laser Fence Post (NAPOST) | 200 | −25 | 300 | concrete | Adv. Power Plant (TL8) | Auto-links laser fence sections (800 HP, impassable while powered) between posts |
| EMP Cannon (NAPULS) | 1000 | −150 | 500 | heavy | Radar (TL6) | Shared with GDI (see above) |
| Wall (NAWALL) | 50 | 0 | 150 (FS: 225) | concrete | Hand of Nod (TL6) | |
| Gate (NAGATE_A/B) | 250 | 0 | 350 | heavy | Hand of Nod (TL6) | |

*Nod has **no Service Depot** — field repair via Mobile Repair Vehicle. GDI has no standalone SAM/laser — defense via Component Tower plugs.*

---

## 2. INFANTRY

Crushable by default; `Crushable=no` noted. `TiberiumProof` = takes no Tiberium field damage; `TiberiumHeal` = actively heals in Tiberium.

### GDI

| Unit (ID) | Cost | TL | HP | Armor | Weapon (dmg/ROF/rng) | Prereq | Notes |
|---|---|---|---|---|---|---|---|
| Light Infantry (E1) | 120 | 1 | 125 | none | Minigun 8/21/4 | Barracks | Shared basic rifleman; crushed by vehicles, hurt by Tiberium |
| Disc Thrower (E2) | 200 | 2 | 150 | none | Grenade disc 40/60/4.5 (HE, lobbed, bounces) | GDI Barracks | **FS changes: ROF nerfed 60→80, CollateralDamageCoefficient .33** (fewer friendly-fire bounce kills) |
| Medic (MEDIC) | 600 | 4 | 125 | none | Heal −50/80/2.83 (Organic only) | GDI Barracks | Heals infantry only; self-healing |
| Engineer (ENGINEER) | 500 | 2 | 100 | none | none | Barracks (both) | Instantly captures buildings (EngineerCaptureLevel=1); repairs bridges via huts |
| Jumpjet Infantry (JUMPJET) | 600 | 6 | 120 | light | JumpCannon 15×2/40/5 (AA-capable SA) | Barracks + Radar | Flies (jumpjet locomotor); **not crushable**; speed 5 (**FS: 8**); elite: radar-invisible |
| Ghost Stalker (GHOST) | 1750 | 10 | 200 | light | Railgun (RailShot2 warhead, dmg via warhead=100–150% vs most armor)/60/6 + **C4** | Barracks + Tech Center | **Mutant hero, BuildLimit 1**; TiberiumProof + heals in Tiberium; C4 demolishes buildings instantly |
| Chem Spray Infantry (WEEDGUY) | 300 | −1 | 130 | none | MultiCluster 65×2/80/6 | campaign only | Not buildable in MP release; TiberiumProof |

### Nod

| Unit (ID) | Cost | TL | HP | Armor | Weapon | Prereq | Notes |
|---|---|---|---|---|---|---|---|
| Light Infantry (E1) | 120 | 1 | 125 | none | Minigun 8/21/4 | Hand of Nod | Shared |
| Rocket Infantry (E3) | 250 | 2 | 100 | none | Bazooka 25/60/6 (AP, AA-capable) | Hand of Nod | Nod's AT/AA infantry |
| Engineer | 500 | 2 | 100 | none | none | Hand of Nod | Shared |
| Cyborg Infantry (CYBORG) | 650 | 4 | **300** | light | Vulcan3 10×3/30/4 | Hand of Nod | **Not crushable, TiberiumProof, heals in Tiberium, fearless, EMP-vulnerable**; keeps crawling at half body when badly damaged; elite: +HP |
| Cyborg Commando (CYC2) | 2000 | 10 | **500** | heavy | CyCannon plasma **120**/50/7 (PlasmaWH: 350/260/205/150/80%) | Hand of Nod + Temple of Nod | **Hero, BuildLimit 1**; not crushable; heals in Tiberium; FS adds `IsWebImmune=true` |
| Mutant Hijacker (MHIJACK) | 1850 | 10 | 300 | none | none — **steals vehicles** (`VehicleThief`) | Hand of Nod + Temple | **BuildLimit 1**; not crushable; TiberiumProof/heals |
| Chameleon Spy (CHAMSPY) | 700 | −1* | 120 | none | none (`Agent=yes`, `Cloakable`) | Hand of Nod + Tech Center | *TL−1 in final release → campaign only [was intended buildable]; cloaked; reveals enemy radar intel |

### Neutral / campaign infantry
Umagon (400, sniper 150 dmg HollowPoint), Mutant/Mutant Sergeant (100, Vulcan 20), Tiberian Fiend (250 HP, FiendShard 35×3), Tratos, Oxanna, Slavik, Technician, Civilians. All TL−1. Mutants/fiends heal in Tiberium.

---

## 3. VEHICLES

Locomotors: drive (wheel/track), walk (mechs — immune to vein damage terrain effects vary), hover, subterranean, jumpjet. `ImmuneToVeins` noted; harvesters/MCVs self-heal.

### GDI

| Unit (ID) | Cost | TL | HP | Armor | Weapon (dmg/ROF/rng) | Speed/loco | Prereq | Special |
|---|---|---|---|---|---|---|---|---|
| Wolverine (SMECH) | 500 | 2 | 175 | light | AssaultCannon 40/50/5 (SA) | 7 / walker | War Factory | Fast anti-infantry mech; vein-immune; elite: vein-proof |
| Titan (MMCH) | 800 | 3 | 400 | heavy | 120mm 70/80/6.75 (AP) | 4 / walker | War Factory | Main battle mech; elite: sensors |
| Amphibious APC (APC) | 800 | 6 | 200 | heavy | none | 8 / amphibious drive | War Factory + Barracks | Carries 5 infantry; crosses water |
| Hover MLRS (HVR) | 900 | 7 | 230 | wood | HoverMissile 30×2/68/8 (AP, AA-capable) | 7 / hover | War Factory + Radar | Hovers over water; loses control on ice; elite: self-heal |
| Disruptor (SONIC) | 1300 | 9 | 500 | heavy | SonicZap beam (SonicWarhead, hits every unit in beam line; listed dmg 1 + special sonic damage ~50/wave [uncertain — engine applies `SonicBeamDamage`]) rng 6, ROF 120 | 4 (**FS: 5**) / drive | War Factory + Tech Center | Beam damages friendlies in path; FS elite: faster |
| Mammoth Mk. II (HMEC) | 3000 | 10 | 800 | heavy | Dual Railgun (RailShot 200/175/160/100/25%)/60/8 + MammothTusk AA missiles 40×2/80/6 | 3 / walker | War Factory + Tech Center | **BuildLimit 1 per player**; self-heals; crushes walls |
| Mobile Sensor Array (LPST) | 950 | 6 | 600 | wood | none | 6 / drive | Factory + Radar (Owner=GDI,Nod per INI — GDI-associated in practice) | Deploys → detects cloak/subterranean, radius 25 |
| Harvester (HARV) | 1400 | 1 | 1000 | heavy | none | 5 / drive | Factory + Refinery | 28 bails; self-heals; explodes; vein-immune |
| MCV (MCV) | 2500 | 10 | 1000 | heavy | none | 3 / drive | Factory + any Tech Center | Deploys → Construction Yard |
| Mammoth Tank (4TNK) | 1700 | −1 | 600 | heavy | dual 120mm 50×2 + MammothTusk | 4 | campaign/crate only | Classic TD unit, not buildable |

### Nod

| Unit (ID) | Cost | TL | HP | Armor | Weapon | Speed/loco | Prereq | Special |
|---|---|---|---|---|---|---|---|---|
| Attack Buggy (BGGY) | 500 | 2 | 220 | light | RaiderCannon 40/55/4 (SA) | **10** / drive | War Factory | Scout; vein-immune |
| Attack Cycle (BIKE) | 600 | 5 | 150 | wood | BikeMissile 40/60/5 (AP) | **12** / drive | War Factory | Fastest ground unit |
| Tick Tank (TTNK) | 800 | 3 | 350 | light | 90mm 36/50/6.75 (AP) | 6 / drive | War Factory | **Deploys** → dug-in turret (GATICK: armor becomes **concrete**, HP 350) |
| Artillery (ART2) | 975 | 6 | 300 | light | 155mm **150**/110/**18** (ARTYHE, min rng 5) | 5 / drive | War Factory + Radar | Must **deploy** to fire (GAARTY) |
| Devil's Tongue (SUBTANK) | 750 | 7 | 300 | light | FireballLauncher flame ×2, rng 4.25 (Fire warhead 600% vs infantry) | 5 / **subterranean** | War Factory + Tech Center | Burrows and surfaces anywhere (not on concrete/pavement) |
| Subterranean APC (SAPC) | 800 | 6 | 175 | heavy | none | 5 / **subterranean** | War Factory + Tech Center | 5 passengers, tunnels under walls/defenses |
| Stealth Tank (STNK) | 1100 | 8 | 180 (**FS: 200**) | light | Dragon missiles 30×2/50/6 (AP, AA-capable) | 6 / drive | War Factory + Tech Center | **Cloaked** (decloaks to fire, revealed by sensors/infantry proximity); FS sight 5→7 |
| Mobile Repair Vehicle (REPAIR) | 1000 | 7 | 200 | light | RepairBullet −50 HP/80/1.8 (Mechanical) | 6 / drive | War Factory | Field-repairs vehicles (Nod's depot substitute) |
| Weed Eater (WEED) | 1400 | 10 | 600 | heavy | none | 5 / drive | War Factory + Waste Facility | Harvests Tiberium veins (7 storage) → fuels chem missile |
| Harvester / MCV | as GDI | | | | | | shared | |

---

## 4. AIRCRAFT

All rearm at Helipad; **ReloadRate = 0.5 min per ammo point**.

| Unit (ID) | Faction | Cost | TL | HP | Armor | Weapon | **Ammo** | Speed | Prereq | Notes |
|---|---|---|---|---|---|---|---|---|---|---|
| Orca Fighter (ORCA) | GDI | 1000 | 5 | 200 | light | Hellfire missiles 30×2/50/6 (ORCAAP: 150% vs light) | **5** | 20 | Helipad | VTOL gunship |
| Orca Bomber (ORCAB) | GDI | 1600 | 8 | 260 | light | Bombs 160 (ORCAHE, huge spread) | **2** | 12 | Helipad + Tech Center | Carpet-bombs; elite: radar-invisible; FS drop range 5→3 |
| Orca Transport (ORCATRAN) | GDI | 1200 | −1 | 200 | light | none | 5 pax | 9 | campaign only | |
| Orca Carryall (TRNSPORT) | GDI | 750 | 9 | 175 | light | none | 1 vehicle | 16 | Helipad + Service Depot | Airlifts any vehicle (incl. harvesters) |
| Dropship (DSHP) | GDI | — | −1 | 200 | heavy | none | 5 pax | 18 | campaign (Dropship Bay reinforcements) | |
| Drop Pod (DPOD) | GDI | — | −1 | 60 | light | Vulcan2 50/50/6 | — | 16 | superweapon delivery | See FS Drop Pod Node |
| Harpy (APACHE) | Nod | 1000 (**FS: 800**) | 5 | 225 | light | HarpyClaw chaingun 60/36/5 (SA) | **12** | 14 | Helipad | Anti-infantry/light helicopter |
| Banshee (SCRIN) | Nod | 1500 (**FS: 1250**) | 9 | 280 | light | Proton bolts 20×?/3/5 (AP, PlasmaWH-family) | **3** | 18 | Helipad + Tech Center | Alien-tech fighter; elite: radar-invisible |

---

## 5. FIRESTORM EXPANSION (1.13+FS additions & changes)

### New GDI

| Unit/Structure | Cost | TL | HP | Armor | Weapon | Prereq | Notes |
|---|---|---|---|---|---|---|---|
| **Juggernaut** (JUGG) | 950 | 6 | 350 | light | Jugg90mm 75×3/150/**18** (ARTYHE, min 5) | War Factory + Radar | Walker artillery; must **deploy** (DJUGG) to fire; GDI's answer to Nod Artillery |
| **Mobile War Factory** (MOBWARG) | 1800 | 10 | 800 | heavy | none | War Factory + Upgrade Center | **BuildLimit 1**; deploys → full War Factory (DGWEAP, 800 HP); counts as factory prereq |
| **Drop Pod Node** (GAPLUG4) | 1000 | −20 pwr | — | wood | Upgrade Center (TL10) | 3rd plug type; **Superweapon: Drop Pods**, recharge 7 min, drops **5–8 veteran infantry** anywhere |
| **Mobile EMP** (MOBILEMP) | 1000 | 6 | 800 | heavy | MobileEMPulseWeapon 1200/rng 40 (fires once when deployed, then recharges → CMOBILEMP charged state) | GDI War Factory + **EMP Cannon** | Driving EMP burst platform |
| **Limpet Drone** (LIMPET) | 550 | 3 | 100 | none | attaches (LIMP/LIMPY warhead, LimpetFactor 35) | Factory + Radar (**both factions** per INI; GDI-flavored) | Hover mine that sticks to enemy vehicles: slows them ~35% and spies their sight radius; cloaked when deployed (DLIMPET) |

### New Nod

| Unit/Structure | Cost | TL | HP | Armor | Weapon | Prereq | Notes |
|---|---|---|---|---|---|---|---|
| **Cyborg Reaper** (REAPER) | 1100 | 6 | 400 | light | QuadLauncher rockets (cluster ×2, min rng 3) + **WebLauncher** (webs infantry: immobilizes ~600 frames, 600% vs none-armor) | War Factory + Tech Center | Quadruped cyborg; not crushable; heals in Tiberium; TiberiumProof |
| **Mobile Stealth Generator** (SGEN) | 1600 | 9 | 200 | light | none | War Factory + Stealth Generator | Deploys → MSTL, cloaks **6-cell radius** |
| **Fist of Nod** (MOBWARN) | 1800 | 10 | 800 | heavy | none | War Factory + **Temple of Nod** | **BuildLimit 1**; mobile war factory, deploys → DNWEAP |

### Balance changes in FIRESTRM.INI (vs base TS)
- Disc Thrower: Grenade ROF 60→80 (slower), collateral (bounce) damage ×0.33.
- Jumpjet Infantry speed 5→8. Stealth Tank HP 180→200, sight 5→7. Disruptor speed 4→5, elite EXPLODES→FASTER, no acceleration.
- Banshee 1500→1250; Harpy 1000→800. Orca Bomber drop range 5→3.
- Walls (both) 150→225 HP. Tiberian Fiend gains vein immunity. Cyborg Commando web-immune.
- Veterancy: ratio 10→5 (twice as fast), combat/armor bonus .25→.50, ROF .20→.30.
- Prereq aliases updated so deployed mobile factories satisfy FACTORY/GDIFACTORY/NODFACTORY.

### CABAL (FS campaign antagonist — not a playable MP side)
Uses the Nod tech tree + cyborg units (Cyborgs, Reapers, Cyborg Commandos) with these unique campaign-only (`TechLevel=-1`, Owner=Civilian) assets:

| Asset | HP | Armor | Weapon | Notes |
|---|---|---|---|---|
| CABAL Core (CORE) | 3000 | concrete | — | Final-mission objective core |
| **Core Defender** (DEFENDER/DDEFD) | **10000** | heavy | DEFOB laser 350×2/20/10.5 (Super2 warhead) | Giant walker boss; self-heals, heals in Tiberium; deployed form 9999 HP |
| CABAL Obelisk (CROB) | 1000 | concrete | CABLaser 100/70/10.5 (rapid-fire obelisk) | Fires far faster than Nod obelisk |
| Obelisk of Darkness (AAOB) | 1000 | concrete | AALaserFire 250/20/12 (**anti-air laser**) | AA obelisk |
| Flame Tank (FLMTNK) | 300 | light | FireballLauncher | Classic TD flame tank, campaign only |
| Tiberium Floater (JFISH) | 500 | light | Tentacle 16/80/1.5 discharge | New neutral Tiberium lifeform (hover) |

---

## 6. TECH TREE GRAPHS

### GDI

```
MCV ──deploy──> CONSTRUCTION YARD
  └─> Power Plant (300) [+2x Turbine @TL7]
        ├─> Barracks (300) ──> E1, Disc Thrower, Engineer, Medic(TL4)
        │     ├─> Component Tower (200) ──+Vulcan(TL2) / +RPG(TL9) / +SAM(needs Radar,TL5)
        │     ├─> Concrete Wall, Gate (TL6)
        │     └─(+Radar)─> Jumpjet Infantry (TL6)
        └─> Refinery (2000) ──> Silo
              ├─> Radar (1000, TL3) ──> Helipad (500) ──> ORCA FIGHTER
              │       │                        └─(+Tech Ctr)─> ORCA BOMBER
              │       │                        └─(+Service Depot)─> CARRYALL
              │       └─> EMP Cannon (1000, TL6)
              └─(+Barracks)─> WAR FACTORY (2000, TL2)
                       ├─> Wolverine(TL2), Titan(TL3), APC(+Barracks,TL6),
                       │   Hover MLRS(+Radar,TL7), [FS: Juggernaut(+Radar,TL6), Limpet(+Radar,TL3)]
                       ├─> Service Depot (1200, TL7)
                       ├─> Harvester (+Refinery), Mobile Sensor Array (+Radar,TL6)
                       └─(+Radar)─> TECH CENTER (1500, TL6)
                                ├─> Disruptor, Mammoth Mk II (limit 1), Ghost Stalker (limit 1)
                                ├─> Firestorm Generator ──> Firestorm Wall sections
                                ├─> MCV (Factory+Tech)
                                └─(+Refinery)─> UPGRADE CENTER (1000, TL10)
                                         ├─ plug: ION CANNON UPLINK  (superweapon)
                                         ├─ plug: SEEKER CONTROL (+War Factory) (Hunter-Seeker)
                                         └─ [FS] plug: DROP POD NODE / Mobile War Factory (Factory+UpgCtr)
```

### Nod

```
MCV ──deploy──> CONSTRUCTION YARD
  └─> Power Plant (300)
        ├─> Hand of Nod (300) ──> E1, Rocket Infantry, Engineer, Cyborg(TL4)
        │     ├─> Laser Turret (300, TL2)
        │     └─> Wall, Gate (TL6)
        └─> Refinery (2000) ──> Silo
              ├─> Radar (1000, TL3) ──> SAM Site, Helipad ──> HARPY
              │       │                      └─(+Tech Ctr)─> BANSHEE
              │       └─> EMP Cannon (TL6)
              └─(+Hand)─> WAR FACTORY (2000, TL2)
                       ├─> Attack Buggy(TL2), Tick Tank(TL3), Attack Cycle(TL5),
                       │   Mobile Repair Vehicle(TL7), Artillery(+Radar,TL6), Harvester(+Refinery)
                       ├─> Advanced Power Plant (500, TL7) ──> Laser Fence Post (TL8)
                       └─(+Radar)─> TECH CENTER (1500, TL6)
                                ├─> Stealth Tank, Sub. APC, Devil's Tongue, Obelisk of Light
                                ├─> [FS] Cyborg Reaper (Factory+Tech)
                                ├─> Stealth Generator (+Refinery) ──[FS]──> Mobile Stealth Gen (Factory+StealthGen)
                                ├─> MCV (Factory+Tech)
                                ├─> MISSILE SILO (TL10: Multi-Missile SW)
                                │        └─> TIBERIUM WASTE FACILITY (limit 1)
                                │                 ├─> enables CHEMICAL MISSILE (on the Silo)
                                │                 └─> Weed Eater (Factory+Waste)
                                └─> TEMPLE OF NOD (TL10: Hunter-Seeker SW)
                                         ├─> Cyborg Commando (limit 1, +Hand)
                                         ├─> Mutant Hijacker (limit 1, +Hand)
                                         └─> [FS] Fist of Nod (Factory+Temple)
```

---

## 7. SUPERWEAPON SUMMARY

| Superweapon | Building | Recharge (min) | Powered? | Effect |
|---|---|---|---|---|
| Ion Cannon | GDI Ion Cannon Uplink plug | 8.5 | yes | 751 dmg orbital strike (IonCannonWH) |
| Drop Pods [FS] | GDI Drop Pod Node plug | 7 | yes | 5–8 veteran infantry anywhere |
| Firestorm Defense | GDI Firestorm Generator | 3 (charge-drain) | yes | Invulnerable wall/shield while charged |
| Hunter-Seeker | GDI Seeker Control / Nod Temple | 12 | yes | Autonomous drone, 11000-dmg suicide hit on random enemy asset |
| Multi-Missile | Nod Missile Silo | 10 | yes | 10× 130-dmg HE cluster airburst; ignores Firestorm |
| Chemical Missile | Nod Missile Silo + Waste Facility | 0.3 (weed-limited) | no | 8× 100-dmg Gas clusters; spawns Tiberium & visceroids |
| EM Pulse | EMP Cannon (both) | 4.5 | yes | Range-40 EMP, disables mechanical units/cyborgs/defenses (Spread 11) |

## Key cross-cutting mechanics for the spec
- **Crush immunity:** Jumpjets, all cyborgs (Cyborg, Commando, Reaper), Mutant Hijacker. All other infantry crushable by heavy vehicles.
- **Tiberium interaction:** normal infantry take field damage; `TiberiumProof` (all mutants, cyborgs, chem infantry, Ghost Stalker) don't; `TiberiumHeal` (cyborgs, mutant heroes, fiends, visceroids, Reaper) actively regenerate in fields.
- **EMP** disables vehicles, cyborg infantry, and powered defenses (not organic infantry or structures' HP).
- **Deploy mechanics:** Tick Tank (light→concrete armor), Artillery & Juggernaut (must deploy to fire), MCV, Sensor Array, Mobile War Factories, Mobile Stealth Gen, Mobile EMP, Limpet (deploy = arm/cloak).
- **Subterranean** units surface anywhere except pavement/concrete and are blocked by Firestorm walls; detected by Sensor Arrays (radius 25) and Waste Facility (`Sensors=yes`).
- **Aircraft** use `SeparateAircraft` (built at helipad, one pad per pad-aircraft), ammo per table above, 0.5 min per ammo point rearm.

Sources: [Vinifera-Developers/Tiberian-Sun-INIs (original RULES.INI/FIRESTRM.INI)](https://github.com/Vinifera-Developers/Tiberian-Sun-INIs), [CNCNZ Tiberian Sun guides](https://cncnz.com/games/tiberian-sun/gdi-structures/) (blocked from fetch; values cross-checked from INI instead), [C&C Wiki search results](https://cnc.fandom.com/wiki/GDI_power_plant_(Tiberian_Sun)).

The downloaded INIs remain in the session scratchpad (`/tmp/claude-0/-home-user-RedAlert/84746fb6-337c-560d-9167-231af8584ff6/scratchpad/RULES.INI` and `FIRESTRM.INI`) if you want deeper values (art, AI, voxel debris, particle systems) for the re-implementation.
