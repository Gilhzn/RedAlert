using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace TiberiumDusk.Balance
{
    /// <summary>
    /// Loads and validates the JSON content under data/. Fails loudly on any
    /// inconsistency (dangling weapon/warhead reference, bad armor class…)
    /// so content errors surface in CI, never mid-game.
    /// </summary>
    public static class GameDataLoader
    {
        public static readonly string[] ArmorClasses = { "none", "wood", "light", "heavy", "concrete" };
        public static readonly string[] Locomotors = { "foot", "tracked", "wheeled", "walker", "hover", "amphibious", "subterranean", "aircraft" };

        /// <summary>Every data file the game needs — the load manifest for all platforms.</summary>
        public static readonly string[] DataFiles =
        {
            "units.json", "structures.json", "weapons.json", "warheads.json",
            "terrain.json", "economy.json", "superweapons.json", "special.json",
            "locale/en.json", "locale/he.json",
        };

        public static GameData LoadFromDirectory(string dataDir) =>
            LoadFromContent(ReadDirectory(dataDir));

        /// <summary>Reads the manifest off disk (editor/desktop/server).</summary>
        public static Dictionary<string, string> ReadDirectory(string dataDir)
        {
            var files = new Dictionary<string, string>();
            foreach (var name in DataFiles)
            {
                var path = Path.Combine(dataDir, name.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path))
                    throw new FileNotFoundException($"Game data file missing: {path}");
                files[name] = File.ReadAllText(path);
            }
            return files;
        }

        /// <summary>Content-based load: works from memory (WebGL fetch) or disk dumps.</summary>
        public static GameData LoadFromContent(IReadOnlyDictionary<string, string> files)
        {
            var warheads = LoadWarheads(files["warheads.json"], "warheads.json");
            var weapons = LoadWeapons(files["weapons.json"], "weapons.json", warheads);
            var landTypes = LoadTerrain(files["terrain.json"], "terrain.json");
            var units = LoadUnits(files["units.json"], "units.json", "units", weapons, null);
            LoadUnits(files["structures.json"], "structures.json", "structures", weapons, units);
            return new GameData(units, weapons, warheads, landTypes);
        }

        private static Dictionary<string, WarheadBlueprint> LoadWarheads(string json, string path)
        {
            var result = new Dictionary<string, WarheadBlueprint>();
            foreach (var token in (JArray)JObject.Parse(json)["warheads"])
            {
                var wh = new WarheadBlueprint
                {
                    Id = Required<string>(token, "id", path),
                    Spread = Required<int>(token, "spread", path),
                    Verses = Required<JArray>(token, "verses", path).Select(v => (int)v).ToArray(),
                    EmpEffect = token["empEffect"] != null && (bool)token["empEffect"],
                };
                if (wh.Verses.Length != ArmorClasses.Length)
                    throw new InvalidDataException(
                        $"Warhead '{wh.Id}': verses must have {ArmorClasses.Length} entries (one per armor class), got {wh.Verses.Length}");
                AddUnique(result, wh.Id, wh, path);
            }
            return result;
        }

        private static Dictionary<string, WeaponBlueprint> LoadWeapons(
            string json, string path, IReadOnlyDictionary<string, WarheadBlueprint> warheads)
        {
            var result = new Dictionary<string, WeaponBlueprint>();
            foreach (var token in (JArray)JObject.Parse(json)["weapons"])
            {
                var weapon = new WeaponBlueprint
                {
                    Id = Required<string>(token, "id", path),
                    Damage = Required<int>(token, "damage", path),
                    Rof = Required<int>(token, "rof", path),
                    Range = Required<double>(token, "range", path),
                    Warhead = Required<string>(token, "warhead", path),
                };
                weapon.Targets = token["targets"]?.ToString() ?? "g";
                weapon.MinRange = token["minRange"] != null ? (double)token["minRange"] : 0;
                var projectile = token["projectile"];
                if (projectile != null)
                {
                    weapon.ProjectileKind = projectile["kind"]?.ToString() ?? "instant";
                    weapon.ProjectileSpeed = projectile["speed"] != null ? (int)projectile["speed"] : 0;
                }
                else
                {
                    weapon.ProjectileKind = "instant";
                }
                if (!warheads.ContainsKey(weapon.Warhead))
                    throw new InvalidDataException($"Weapon '{weapon.Id}' references unknown warhead '{weapon.Warhead}'");
                AddUnique(result, weapon.Id, weapon, path);
            }
            return result;
        }

        private static Dictionary<string, LandType> LoadTerrain(string json, string path)
        {
            var result = new Dictionary<string, LandType>();
            foreach (var prop in ((JObject)JObject.Parse(json)["landTypes"]).Properties())
            {
                var land = new LandType
                {
                    Id = prop.Name,
                    Buildable = Required<bool>(prop.Value, "buildable", path),
                    Speed = ((JObject)prop.Value["speed"]).Properties()
                        .ToDictionary(p => p.Name, p => (int)p.Value),
                };
                foreach (var locomotor in Locomotors)
                {
                    if (!land.Speed.ContainsKey(locomotor))
                        throw new InvalidDataException($"Land type '{land.Id}' missing speed for locomotor '{locomotor}'");
                }
                AddUnique(result, land.Id, land, path);
            }
            return result;
        }

        private static Dictionary<string, UnitBlueprint> LoadUnits(
            string json, string path, string arrayKey, IReadOnlyDictionary<string, WeaponBlueprint> weapons,
            Dictionary<string, UnitBlueprint> existing)
        {
            var result = existing ?? new Dictionary<string, UnitBlueprint>();
            foreach (var token in (JArray)JObject.Parse(json)[arrayKey])
            {
                var unit = new UnitBlueprint
                {
                    Id = Required<string>(token, "id", path),
                    Faction = Required<string>(token, "faction", path),
                    Components = ((JObject)token["components"]).Properties().ToDictionary(
                        p => p.Name,
                        p => ((JObject)p.Value).Properties().ToDictionary(c => c.Name, c => (object)c.Value)),
                };
                if (unit.Components.TryGetValue("Armament", out var armament)
                    && armament.TryGetValue("weapon", out var weaponId)
                    && !weapons.ContainsKey(weaponId.ToString()))
                {
                    throw new InvalidDataException($"Unit '{unit.Id}' references unknown weapon '{weaponId}'");
                }
                AddUnique(result, unit.Id, unit, path);
            }
            return result;
        }

        private static T Required<T>(JToken token, string field, string path)
        {
            var value = token[field];
            if (value == null)
                throw new InvalidDataException($"{Path.GetFileName(path)}: entry missing required field '{field}': {token.ToString().Substring(0, System.Math.Min(80, token.ToString().Length))}");
            return value.ToObject<T>();
        }

        private static void AddUnique<T>(Dictionary<string, T> dict, string id, T value, string path)
        {
            if (dict.ContainsKey(id))
                throw new InvalidDataException($"{Path.GetFileName(path)}: duplicate id '{id}'");
            dict.Add(id, value);
        }
    }
}
