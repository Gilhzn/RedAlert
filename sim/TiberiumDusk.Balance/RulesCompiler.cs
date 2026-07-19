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
        public static RulesData Compile(GameData data, JObject terrainJson)
        {
            var rules = new RulesData
            {
                Lands = CompileLands(data),
                Slopes = CompileSlopes(terrainJson),
                Units = CompileUnits(data),
            };
            rules.BuildIndices();
            return rules;
        }

        public static RulesData CompileFromDirectory(string dataDir)
        {
            var data = GameDataLoader.LoadFromDirectory(dataDir);
            var terrainJson = JObject.Parse(File.ReadAllText(Path.Combine(dataDir, "terrain.json")));
            return Compile(data, terrainJson);
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

            return spec;
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
