using TiberiumDusk.Sim;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Map;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.WorldModel;

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
            // Player 0 (amber): an MCV ready to found a base (press D), an escort,
            // and crystal fields within reach.
            game.Spawn("dm_mcv", 0, new CellPos(12, 14));
            game.Spawn("dm_mbt_walker", 0, new CellPos(10, 18));
            game.Spawn("dm_mbt_walker", 0, new CellPos(14, 18));
            game.Spawn("dm_rifle_infantry", 0, new CellPos(12, 19));
            game.Spawn("so_scout_buggy", 0, new CellPos(16, 18));

            // Green crystal field near the player start.
            SeedField(game, centerX: 22, centerY: 8, radius: 3, CrystalType.Green, density: 7);
            // Richer blue field across the river — worth fighting for.
            SeedField(game, centerX: 46, centerY: 40, radius: 2, CrystalType.Blue, density: 9);

            // Player 1 (crimson) force guarding the blue field.
            game.Spawn("so_scout_buggy", 1, new CellPos(40, 45));
            game.Spawn("dm_mbt_walker", 1, new CellPos(42, 42));
            game.Spawn("dm_rifle_infantry", 1, new CellPos(44, 43));
        }

        private static void SeedField(Game game, int centerX, int centerY, int radius,
            CrystalType type, int density)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > radius * radius + 1) continue;
                    var cell = new CellPos(centerX + dx, centerY + dy);
                    if (!game.World.Map.InBounds(cell)) continue;
                    if (game.World.OccupantOf(cell) != -1) continue;
                    var land = game.World.Rules.Lands[game.World.Map.Land(cell)];
                    if (!land.Buildable) continue;
                    game.World.Crystal.Set(cell, type, density);
                }
            }
        }
    }
}
