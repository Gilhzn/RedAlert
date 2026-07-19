using System.IO;
using System.Linq;
using TiberiumDusk.Balance;
using Xunit;

namespace TiberiumDusk.Sim.Tests
{
    /// <summary>
    /// Content validation gate: every JSON file in data/ must load and be
    /// internally consistent. Content mistakes fail CI, not gameplay.
    /// </summary>
    public class GameDataTests
    {
        internal static string FindDataDir()
        {
            // Walk up from the test bin dir to the repo root (identified by data/units.json).
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "data");
                if (File.Exists(Path.Combine(candidate, "units.json")))
                    return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("Could not locate repo data/ directory from test working dir");
        }

        [Fact]
        public void AllGameDataLoadsAndValidates()
        {
            var data = GameDataLoader.LoadFromDirectory(FindDataDir());
            Assert.NotEmpty(data.Units);
            Assert.NotEmpty(data.Weapons);
            Assert.NotEmpty(data.Warheads);
            Assert.NotEmpty(data.LandTypes);
        }

        [Fact]
        public void EveryUnitHasHealthAndBuildableOrIsSpecial()
        {
            var data = GameDataLoader.LoadFromDirectory(FindDataDir());
            foreach (var unit in data.Units.Values)
            {
                Assert.True(unit.Components.ContainsKey("Health"), $"unit '{unit.Id}' missing Health component");
            }
        }

        [Fact]
        public void LocalesCoverSameKeys()
        {
            var dataDir = FindDataDir();
            var en = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(dataDir, "locale/en.json")));
            var he = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(dataDir, "locale/he.json")));
            var enKeys = en.Properties().Select(p => p.Name).OrderBy(k => k).ToArray();
            var heKeys = he.Properties().Select(p => p.Name).OrderBy(k => k).ToArray();
            Assert.Equal(enKeys, heKeys);
        }

        [Fact]
        public void EveryUnitIdHasLocaleName()
        {
            var dataDir = FindDataDir();
            var data = GameDataLoader.LoadFromDirectory(dataDir);
            var en = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(dataDir, "locale/en.json")));
            foreach (var unit in data.Units.Values)
            {
                Assert.True(en.ContainsKey($"unit.{unit.Id}"), $"locale/en.json missing display name for unit '{unit.Id}'");
            }
        }
    }
}
