using TiberiumDusk.Balance;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Map;

namespace TiberiumDusk.Sim.Tests
{
    /// <summary>Shared fixtures: real repo rules + standard test maps.</summary>
    public static class TestWorlds
    {
        private static RulesData _rules;

        public static RulesData Rules => _rules ??= RulesCompiler.CompileFromDirectory(GameDataTests.FindDataDir());

        public static byte Land(string id) => Rules.LandIndex(id);

        /// <summary>Flat clear 64×64 map.</summary>
        public static MapData Flat(int size = 64) => new MapData(size, size, Land("clear"));

        /// <summary>
        /// 64×64 map with a vertical water channel at x=30..33 spanning all rows
        /// except a road bridge row at y=50.
        /// </summary>
        public static MapData WaterChannel()
        {
            var map = Flat();
            map.FillLand(30, 0, 33, 63, Land("water"));
            map.FillLand(30, 50, 33, 50, Land("road"));
            return map;
        }
    }
}
