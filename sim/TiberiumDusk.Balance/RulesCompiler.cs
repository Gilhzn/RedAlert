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
            var rules = new RulesData
            {
                Lands = CompileLands(data),
                Slopes = CompileSlopes(terrainJson),
                Units = CompileUnits(data),
                Economy = CompileEconomy(economyJson),
            };
            rules.BuildIndices();
            return rules;
        }

        public static RulesData CompileFromDirectory(string dataDir)
        {
            var data = GameDataLoader.LoadFromDirectory(dataDir);
            var terrainJson = JObject.Parse(File.ReadAllText(Path.Combine(dataDir, "terrain.json")));
            var economyJson = JObject.Parse(File.ReadAllText(Path.Combine(dataDir, "economy.json")));
            return Compile(data, terrainJson, economyJson);
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

        private static UnitSpec[] CompileUnits(GameData data)
        {
            return data.Units.Values
                .OrderBy(u => u.Id, StringComparer.Ordinal)
                .Select(CompileUnit)
                .ToArray();
        }

        private static UnitSpec CompileUnit(UnitBlueprint blueprint)
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

            spec.CrystalVulnerable = blueprint.Components.ContainsKey("TiberiumVulnerable");

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
