using TiberiumDusk.Sim;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Map;
using TiberiumDusk.Sim.Math;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Hand-authored Phase 2 demo map: open ground, a river with one bridge,
    /// a raised plateau with ramps, roads — enough to exercise every movement
    /// rule visually. Real map files replace this in a later phase.
    /// </summary>
    public static class DemoMap
    {
        public static MapData Build(RulesData rules)
        {
            byte clear = rules.LandIndex("clear");
            byte water = rules.LandIndex("water");
            byte road = rules.LandIndex("road");
            byte rough = rules.LandIndex("rough");
            byte cliff = rules.LandIndex("cliff");

            var map = new MapData(64, 64, clear);

            // River across the middle with a road bridge.
            map.FillLand(0, 30, 63, 33, water);
            map.FillLand(30, 30, 33, 33, road);
            // Road leading to/from the bridge.
            map.FillLand(30, 10, 33, 29, road);
            map.FillLand(30, 34, 33, 54, road);

            // Rough patch in the north-west.
            map.FillLand(8, 8, 18, 16, rough);

            // Plateau (height 2) in the south-east with ramp cells (height 1) on its west edge.
            for (int y = 44; y <= 58; y++)
            {
                for (int x = 44; x <= 58; x++)
                {
                    map.SetHeightLevel(new CellPos(x, y), 2);
                }
            }
            for (int y = 48; y <= 54; y++)
            {
                map.SetHeightLevel(new CellPos(43, y), 1);
            }

            // Impassable crag.
            map.FillLand(50, 8, 55, 13, cliff);

            return map;
        }

        public static void SpawnUnits(Game game)
        {
            // Player 0 (amber) force, north side.
            game.Spawn("dm_mbt_walker", 0, new CellPos(10, 20));
            game.Spawn("dm_mbt_walker", 0, new CellPos(12, 20));
            game.Spawn("dm_mbt_walker", 0, new CellPos(14, 20));
            game.Spawn("dm_rifle_infantry", 0, new CellPos(10, 22));
            game.Spawn("dm_rifle_infantry", 0, new CellPos(12, 22));
            game.Spawn("so_scout_buggy", 0, new CellPos(16, 20));

            // Player 1 (crimson) force, south side (crossing the bridge to reach them).
            game.Spawn("so_scout_buggy", 1, new CellPos(40, 45));
            game.Spawn("dm_mbt_walker", 1, new CellPos(42, 40));
            game.Spawn("dm_rifle_infantry", 1, new CellPos(44, 41));
        }
    }
}
