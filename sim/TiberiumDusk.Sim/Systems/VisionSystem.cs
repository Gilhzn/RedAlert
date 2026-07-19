using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.WorldModel;

namespace TiberiumDusk.Sim.Systems
{
    /// <summary>
    /// Per-player shroud: cells a player has ever seen (explored, permanent)
    /// and cells currently in sight of their units (visible, recomputed).
    /// Classic model: black shroud until scouted, dimmed when out of sight.
    /// </summary>
    public sealed class VisionSystem
    {
        private const int RecomputeEveryTicks = 5;

        private readonly World _world;
        /// <summary>[player][cellIndex]</summary>
        private readonly bool[][] _explored;
        private readonly bool[][] _visible;
        private readonly int[] _exploredCount;
        private int _tickCounter;

        public VisionSystem(World world)
        {
            _world = world;
            int size = world.Map.Width * world.Map.Height;
            _explored = new bool[World.MaxPlayers][];
            _visible = new bool[World.MaxPlayers][];
            _exploredCount = new int[World.MaxPlayers];
            for (int p = 0; p < World.MaxPlayers; p++)
            {
                _explored[p] = new bool[size];
                _visible[p] = new bool[size];
            }
        }

        public bool IsExplored(int playerId, CellPos cell) => _explored[playerId][_world.Map.CellIndex(cell)];
        public bool IsVisible(int playerId, CellPos cell) => _visible[playerId][_world.Map.CellIndex(cell)];
        public int ExploredCount(int playerId) => _exploredCount[playerId];

        public void Tick()
        {
            if (++_tickCounter % RecomputeEveryTicks != 0) return;

            var map = _world.Map;
            for (int p = 0; p < World.MaxPlayers; p++)
            {
                System.Array.Clear(_visible[p], 0, _visible[p].Length);
            }

            var entities = _world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var e = entities[i];
                if (!e.Alive) continue;
                RevealAround(e.Owner, e.Pos, e.Spec.SightLeptons);
            }
        }

        private void RevealAround(int playerId, LeptonPos center, int radiusLeptons)
        {
            var map = _world.Map;
            int radiusCells = radiusLeptons / 256 + 1;
            var centerCell = center.ToCell();
            long radiusSq = (long)radiusLeptons * radiusLeptons;

            for (int dy = -radiusCells; dy <= radiusCells; dy++)
            {
                for (int dx = -radiusCells; dx <= radiusCells; dx++)
                {
                    var cell = new CellPos(centerCell.X + dx, centerCell.Y + dy);
                    if (!map.InBounds(cell)) continue;
                    if (LeptonPos.CellCenter(cell).DistanceSquared(center) > radiusSq) continue;

                    int idx = map.CellIndex(cell);
                    _visible[playerId][idx] = true;
                    if (!_explored[playerId][idx])
                    {
                        _explored[playerId][idx] = true;
                        _exploredCount[playerId]++;
                    }
                }
            }
        }

        public void AddToHash(ref StateHash hash)
        {
            for (int p = 0; p < World.MaxPlayers; p++) hash.Add(_exploredCount[p]);
        }
    }
}
