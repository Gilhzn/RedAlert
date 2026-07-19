using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TiberiumDusk.Sim.Data;

namespace TiberiumDusk.Balance
{
    /// <summary>
    /// Compiles loaded JSON content (GameData) into the sim's immutable RulesData.
    /// Land and unit indices are assigned in sorted-id order so every client
    /// derives identical indices from identical content.
    /// </summary>
    public static class RulesCompiler
    {
        public static RulesData Compile(GameData data, JObject terrainJson, JObject economyJson)
        {
            var warheads = CompileWarheads(data);
            var weapons = CompileWeapons(data, warheads);
            var rules = new RulesData
            {
                Lands = CompileLands(data),
                Slopes = CompileSlopes(terrainJson),
                Warheads = warheads.specs,
                Weapons = weapons.specs,
                Units = CompileUnits(data, weapons.indexById),
                Economy = CompileEconomy(economyJson),
            };
            rules.BuildIndices();
            return rules;
        }

        private static (WarheadSpec[] specs, Dictionary<string, int> indexById) CompileWarheads(GameData data)
        {
            var specs = data.Warheads.Values
                .OrderBy(w => w.Id, StringComparer.Ordinal)
                .Select((w, i) => new WarheadSpec
                {
                    Id = w.Id,
                    Index = i,
                    Verses = w.Verses,
                    Spread = w.Spread,
                    EmpEffect = w.EmpEffect,
                })
                .ToArray();
            return (specs, specs.ToDictionary(w => w.Id, w => w.Index));
        }

        private static (WeaponSpec[] specs, Dictionary<string, int> indexById) CompileWeapons(
            GameData data, (WarheadSpec[] specs, Dictionary<string, int> indexById) warheads)
        {
            var specs = data.Weapons.Values
                .OrderBy(w => w.Id, StringComparer.Ordinal)
                .Select((w, i) => new WeaponSpec
                {
                    Id = w.Id,
                    Index = i,
                    Damage = w.Damage,
                    Rof = w.Rof,
                    RangeLeptons = (int)(w.Range * 256),
                    WarheadIndex = warheads.indexById[w.Warhead],
                    Projectile = ParseEnum<ProjectileKind>(w.ProjectileKind ?? "instant", w.Id),
                    ProjectileSpeed = w.ProjectileSpeed,
                    MinRangeLeptons = (int)(w.MinRange * 256),
                    TargetsGround = (w.Targets ?? "g").Contains("g"),
                    TargetsAir = (w.Targets ?? "g").Contains("a"),
                })
                .ToArray();
            return (specs, specs.ToDictionary(w => w.Id, w => w.Index));
        }

        public static RulesData CompileFromDirectory(string dataDir) =>
            CompileFromContent(GameDataLoader.ReadDirectory(dataDir));

        /// <summary>Content-based compile: the WebGL path (files fetched over HTTP).</summary>
        public static RulesData CompileFromContent(IReadOnlyDictionary<string, string> files)
        {
            var data = GameDataLoader.LoadFromContent(files);
            var rules = Compile(data,
                JObject.Parse(files["terrain.json"]),
                JObject.Parse(files["economy.json"]));
            rules.Superweapons = CompileSuperweapons(JObject.Parse(files["superweapons.json"]));
            rules.Special = CompileSpecial(JObject.Parse(files["special.json"]));
            return rules;
        }

        private static SuperweaponSpec[] CompileSuperweapons(JObject json)
        {
            var list = new List<SuperweaponSpec>();
            int index = 0;
            foreach (var token in (JArray)json["superweapons"])
            {
                var kindText = token["kind"].ToString();
                var spec = new SuperweaponSpec
                {
                    Id = token["id"].ToString(),
                    Index = index++,
                    GrantedBy = token["grantedBy"].ToString(),
                    ChargeTicks = (int)token["chargeTicks"],
                    Kind = ParseEnum<SuperweaponKind>(kindText, "superweapon"),
                    Damage = token["damage"] != null ? (int)token["damage"] : 0,
                    RadiusLeptons = token["radiusCells"] != null ? (int)((double)token["radiusCells"] * 256) : 0,
                    DurationTicks = token["durationTicks"] != null ? (int)token["durationTicks"] : 0,
                    RangeLeptons = token["rangeCells"] != null ? (int)((double)token["rangeCells"] * 256) : 0,
                    ClusterCount = token["clusterCount"] != null ? (int)token["clusterCount"] : 0,
                    DroneUnit = token["drone"]?.ToString(),
                };
                list.Add(spec);
            }
            return list.ToArray();
        }

        private static SpecialRules CompileSpecial(JObject json)
        {
            var storm = (JObject)json["ionStorm"];
            var crates = (JObject)json["crates"];
            return new SpecialRules
            {
                StormMinIntervalTicks = (int)storm["minIntervalTicks"],
                StormMaxIntervalTicks = (int)storm["maxIntervalTicks"],
                StormWarningTicks = (int)storm["warningTicks"],
                StormDurationTicks = (int)storm["durationTicks"],
                StormBoltEveryTicks = (int)storm["boltEveryTicks"],
                StormBoltChancePercent = (int)storm["boltChancePercent"],
                StormBoltDamage = (int)storm["boltDamage"],
                StormBoltRadiusLeptons = (int)((double)storm["boltRadiusCells"] * 256),
                CrateMax = (int)crates["maxCrates"],
                CrateRegenTicks = (int)crates["regenTicks"],
                CrateMoneyAmount = (int)crates["moneyAmount"],
                CrateSharesMoney = (int)crates["sharesMoney"],
                CrateSharesVeterancy = (int)crates["sharesVeterancy"],
                CrateSharesTrap = (int)crates["sharesTrap"],
                CrateSharesHeal = (int)crates["sharesHeal"],
                CrateTrapDamage = (int)crates["trapDamage"],
            };
        }

        private static EconomyRules CompileEconomy(JObject json)
        {
            var crystal = (JObject)json["crystal"];
            var harvesting = (JObject)json["harvesting"];
            var production = (JObject)json["production"];
            return new EconomyRules
            {
                GreenBailValue = (int)crystal["greenBailValue"],
                BlueBailValue = (int)crystal["blueBailValue"],
                MaxDensity = (int)crystal["maxDensity"],
                GrowDensityThreshold = (int)crystal["growDensityThreshold"],
                SeedDensityThreshold = (int)crystal["seedDensityThreshold"],
                GrowthIntervalTicks = (int)crystal["growthIntervalTicks"],
                GrowthChancePercent = (int)crystal["growthChancePercent"],
                SpreadChancePercent = (int)crystal["spreadChancePercent"],
                CrystalDamageIntervalTicks = (int)crystal["damageIntervalTicks"],
                CrystalDamageHp = (int)crystal["damageHp"],
                HarvestTicksPerBail = (int)harvesting["harvestTicksPerBail"],
                UnloadTicksPerBail = (int)harvesting["unloadTicksPerBail"],
                FieldScanRadiusCells = (int)harvesting["fieldScanRadiusCells"],
                FarScanRadiusCells = (int)harvesting["farScanRadiusCells"],
                BuildTicksPerThousandCost = (int)production["buildTicksPerThousandCost"],
                MaxQueuedPerClass = (int)production["maxQueuedPerClass"],
                LowPowerWorstPercent = (int)production["lowPowerWorstPercent"],
                LowPowerBestPercent = (int)production["lowPowerBestPercent"],
                SellRefundPercent = (int)production["sellRefundPercent"],
                StartingCredits = (int)production["startingCredits"],
            };
        }

        private static LandRule[] CompileLands(GameData data)
        {
            return data.LandTypes.Values
                .OrderBy(l => l.Id, StringComparer.Ordinal)
                .Select(l => new LandRule
                {
                    Id = l.Id,
                    Buildable = l.Buildable,
                    SpeedPercent = GameDataLoader.Locomotors
                        .Select(loco => l.Speed[loco])
                        .ToArray(),
                })
                .ToArray();
        }

        private static SlopeRules CompileSlopes(JObject terrainJson)
        {
            var slopes = (JObject)terrainJson["slopes"];
            return new SlopeRules
            {
                TrackedUphillPercent = (int)slopes["trackedUphill"],
                TrackedDownhillPercent = (int)slopes["trackedDownhill"],
                WheeledUphillPercent = (int)slopes["wheeledUphill"],
                WheeledDownhillPercent = (int)slopes["wheeledDownhill"],
            };
        }

        private static UnitSpec[] CompileUnits(GameData data, Dictionary<string, int> weaponIndexById)
        {
            return data.Units.Values
                .OrderBy(u => u.Id, StringComparer.Ordinal)
                .Select(u => CompileUnit(u, weaponIndexById))
                .ToArray();
        }

        private static UnitSpec CompileUnit(UnitBlueprint blueprint, Dictionary<string, int> weaponIndexById)
        {
            var spec = new UnitSpec
            {
                Id = blueprint.Id,
                Faction = blueprint.Faction,
            };

            if (!blueprint.Components.TryGetValue("Health", out var health))
                throw new InvalidDataException($"Unit '{blueprint.Id}' has no Health component");
            spec.Health = new HealthSpec
            {
                Max = GetInt(health, "max", blueprint.Id),
                Armor = ParseEnum<ArmorClass>(GetString(health, "armor", blueprint.Id), blueprint.Id),
            };

            if (blueprint.Components.TryGetValue("Mobile", out var mobile))
            {
                spec.Mobile = new MobileSpec
                {
                    Speed = GetInt(mobile, "speed", blueprint.Id),
                    Locomotor = ParseEnum<LocomotorId>(GetString(mobile, "locomotor", blueprint.Id), blueprint.Id),
                    Rot = GetInt(mobile, "rot", blueprint.Id),
                };
            }

            if (blueprint.Components.TryGetValue("Buildable", out var buildable))
            {
                spec.Buildable = new BuildableSpec
                {
                    Cost = GetInt(buildable, "cost", blueprint.Id),
                    Queue = ParseEnum<ProductionQueue>(GetString(buildable, "queue", blueprint.Id), blueprint.Id),
                    TechLevel = GetInt(buildable, "techLevel", blueprint.Id),
                    Prerequisites = GetStringArray(buildable, "prerequisites", blueprint.Id),
                };
                if (buildable.ContainsKey("buildLimit"))
                    spec.BuildLimit = GetInt(buildable, "buildLimit", blueprint.Id);
            }

            if (blueprint.Components.TryGetValue("Structure", out var structure))
            {
                var footprint = GetIntArray(structure, "footprint", blueprint.Id);
                if (footprint.Length != 2)
                    throw new InvalidDataException($"Structure '{blueprint.Id}': footprint must be [w, h]");
                spec.Structure = new StructureSpec
                {
                    FootprintW = footprint[0],
                    FootprintH = footprint[1],
                    Power = GetInt(structure, "power", blueprint.Id),
                    Adjacent = GetInt(structure, "adjacent", blueprint.Id),
                    BaseNormal = GetBool(structure, "baseNormal"),
                };
                if (structure.ContainsKey("exit"))
                {
                    var exit = GetIntArray(structure, "exit", blueprint.Id);
                    spec.Structure.ExitX = exit[0];
                    spec.Structure.ExitY = exit[1];
                }
            }

            if (blueprint.Components.TryGetValue("Production", out var production))
            {
                var queues = GetStringArray(production, "queues", blueprint.Id);
                spec.ProductionQueues = new ProductionQueue[queues.Length];
                for (int i = 0; i < queues.Length; i++)
                    spec.ProductionQueues[i] = ParseEnum<ProductionQueue>(queues[i], blueprint.Id);
            }

            if (blueprint.Components.TryGetValue("Storage", out var storage))
            {
                spec.StorageBails = GetInt(storage, "bails", blueprint.Id);
            }

            if (blueprint.Components.TryGetValue("Refinery", out var refinery))
            {
                var dock = GetIntArray(refinery, "dock", blueprint.Id);
                spec.Refinery = new RefinerySpec
                {
                    DockX = dock[0],
                    DockY = dock[1],
                    FreeUnit = refinery.ContainsKey("freeUnit") ? GetString(refinery, "freeUnit", blueprint.Id) : null,
                };
            }

            if (blueprint.Components.TryGetValue("Harvester", out var harvester))
            {
                spec.Harvester = new HarvesterSpec
                {
                    CapacityBails = GetInt(harvester, "capacityBails", blueprint.Id),
                };
            }

            if (blueprint.Components.TryGetValue("DeploysInto", out var deploys))
            {
                spec.DeploysInto = GetString(deploys, "structure", blueprint.Id);
            }

            spec.CrystalVulnerable = blueprint.Components.ContainsKey("TiberiumVulnerable")
                && !blueprint.Components.ContainsKey("Cyborg");

            if (blueprint.Components.TryGetValue("Armament", out var armament))
            {
                spec.WeaponIndex = weaponIndexById[GetString(armament, "weapon", blueprint.Id)];
                if (armament.ContainsKey("secondary"))
                    spec.SecondaryWeaponIndex = weaponIndexById[GetString(armament, "secondary", blueprint.Id)];
            }

            if (blueprint.Components.TryGetValue("Aircraft", out var aircraft))
            {
                spec.AircraftAmmo = GetInt(aircraft, "ammo", blueprint.Id);
            }

            if (blueprint.Components.TryGetValue("CloakGenerator", out var cloakGen))
            {
                spec.CloakGeneratorLeptons = GetInt(cloakGen, "radius", blueprint.Id) * 256;
            }

            if (blueprint.Components.TryGetValue("UndeploysInto", out var undeploys))
            {
                spec.UndeploysInto = GetString(undeploys, "unit", blueprint.Id);
            }

            if (blueprint.Components.TryGetValue("Sensors", out var sensors))
            {
                spec.SensorRadiusLeptons = GetInt(sensors, "radius", blueprint.Id) * 256;
            }

            spec.Cloakable = blueprint.Components.ContainsKey("Cloakable");
            spec.IsCyborg = blueprint.Components.ContainsKey("Cyborg");
            spec.IsAircraftPad = blueprint.Components.ContainsKey("AircraftPad");

            if (blueprint.Components.TryGetValue("Turreted", out var turreted))
            {
                spec.Turreted = true;
                spec.TurretRot = GetInt(turreted, "rot", blueprint.Id);
            }

            if (blueprint.Components.TryGetValue("Sight", out var sight))
            {
                spec.SightLeptons = GetInt(sight, "range", blueprint.Id) * 256;
            }

            spec.CanCapture = blueprint.Components.ContainsKey("Engineer");
            spec.Crushable = blueprint.Components.ContainsKey("Crushable");
            spec.Crusher = blueprint.Components.ContainsKey("Crusher");

            return spec;
        }

        private static string[] GetStringArray(Dictionary<string, object> component, string key, string unitId)
        {
            if (!component.TryGetValue(key, out var value)) return new string[0];
            return ((JArray)(JToken)value).Select(t => t.ToString()).ToArray();
        }

        private static int[] GetIntArray(Dictionary<string, object> component, string key, string unitId)
        {
            if (!component.TryGetValue(key, out var value))
                throw new InvalidDataException($"Unit '{unitId}': missing component field '{key}'");
            return ((JArray)(JToken)value).Select(t => (int)t).ToArray();
        }

        private static bool GetBool(Dictionary<string, object> component, string key)
        {
            return component.TryGetValue(key, out var value) && (bool)(JToken)value;
        }

        private static int GetInt(Dictionary<string, object> component, string key, string unitId)
        {
            if (!component.TryGetValue(key, out var value))
                throw new InvalidDataException($"Unit '{unitId}': missing component field '{key}'");
            return Convert.ToInt32(((JToken)value).ToObject<object>());
        }

        private static string GetString(Dictionary<string, object> component, string key, string unitId)
        {
            if (!component.TryGetValue(key, out var value))
                throw new InvalidDataException($"Unit '{unitId}': missing component field '{key}'");
            return ((JToken)value).ToString();
        }

        private static T ParseEnum<T>(string value, string unitId) where T : struct
        {
            if (!Enum.TryParse<T>(value, ignoreCase: true, out var result))
                throw new InvalidDataException($"Unit '{unitId}': invalid {typeof(T).Name} value '{value}'");
            return result;
        }
    }
}
