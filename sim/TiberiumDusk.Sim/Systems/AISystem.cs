using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.WorldModel;

namespace TiberiumDusk.Sim.Systems
{
    public enum AIDifficulty : byte
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
    }

    /// <summary>
    /// Skirmish AI: deploys its MCV, follows a faction build order, replaces
    /// harvesters, trains a mixed army, and launches attack waves. Runs
    /// deterministically inside the sim (same decisions on every client).
    /// </summary>
    public sealed class AISystem
    {
        private sealed class AIState
        {
            public bool Enabled;
            public AIDifficulty Difficulty;
            public int WaveCountdown;
            public int ThinkOffset;

            public void AddToHash(ref StateHash hash)
            {
                hash.Add(Enabled ? 1 : 0);
                hash.Add((int)Difficulty);
                hash.Add(WaveCountdown);
            }
        }

        private const int ThinkEveryTicks = 15;

        private static readonly string[] DominionBuildOrder =
        {
            "dm_power_plant", "nx_refinery", "dm_barracks", "dm_factory", "dm_power_plant",
            "dm_guard_tower", "dm_radar", "nx_refinery", "dm_guard_tower", "dm_power_plant",
            "dm_tech_center", "dm_aa_tower", "dm_rocket_tower",
        };
        private static readonly string[] SerpentBuildOrder =
        {
            "so_power_plant", "nx_refinery", "so_hand", "so_factory", "so_power_plant",
            "so_laser_turret", "so_radar", "nx_refinery", "so_laser_turret", "so_adv_power",
            "so_tech_center", "so_sam", "so_obelisk",
        };
        private static readonly string[] DominionArmy =
            { "dm_rifle", "dm_mbt_walker", "dm_wolverine", "dm_rifle", "dm_mbt_walker", "dm_grenadier" };
        private static readonly string[] SerpentArmy =
            { "so_rifle", "so_tick_tank", "so_scout_buggy", "so_rocket_trooper", "so_tick_tank", "so_cyborg" };

        private readonly World _world;
        private readonly ProductionSystem _production;
        private readonly CombatSystem _combat;
        private readonly MovementSystem _movement;
        private readonly DeterministicRandom _random;
        private readonly AIState[] _players = new AIState[World.MaxPlayers];
        private int _tick;

        public AISystem(World world, ProductionSystem production, CombatSystem combat,
            MovementSystem movement, DeterministicRandom random)
        {
            _world = world;
            _production = production;
            _combat = combat;
            _movement = movement;
            _random = random;
            for (int i = 0; i < World.MaxPlayers; i++) _players[i] = new AIState();
        }

        public void Enable(int playerId, AIDifficulty difficulty)
        {
            _players[playerId].Enabled = true;
            _players[playerId].Difficulty = difficulty;
            _players[playerId].WaveCountdown = FirstWaveDelay(difficulty);
            _players[playerId].ThinkOffset = playerId;
        }

        private static int FirstWaveDelay(AIDifficulty difficulty) => difficulty switch
        {
            AIDifficulty.Easy => 5400,     // 6 min
            AIDifficulty.Normal => 4050,   // 4.5 min
            _ => 2700,                     // 3 min
        };

        private static int WaveInterval(AIDifficulty difficulty) => difficulty switch
        {
            AIDifficulty.Easy => 4050,
            AIDifficulty.Normal => 2700,
            _ => 1800,
        };

        private static int WaveSize(AIDifficulty difficulty) => difficulty switch
        {
            AIDifficulty.Easy => 4,
            AIDifficulty.Normal => 6,
            _ => 9,
        };

        public void Tick()
        {
            _tick++;
            for (int p = 0; p < World.MaxPlayers; p++)
            {
                var ai = _players[p];
                if (!ai.Enabled) continue;
                if (ai.WaveCountdown > 0) ai.WaveCountdown--;
                if ((_tick + ai.ThinkOffset) % ThinkEveryTicks != 0) continue;
                Think(p, ai);
            }
        }

        private void Think(int playerId, AIState ai)
        {
            // Hard AI gets the classic funding trickle.
            if (ai.Difficulty == AIDifficulty.Hard && _tick % 150 == 0)
            {
                var player = _world.Players[playerId];
                player.Credits = System.Math.Min(player.Credits + 50, player.StorageCapacityCredits);
            }

            TryDeployMcv(playerId);
            PlanConstruction(playerId);
            PlaceReadyStructure(playerId);
            PlanUnits(playerId, ai);
            TryLaunchWave(playerId, ai);
        }

        private void TryDeployMcv(int playerId)
        {
            if (_world.OwnsBlueprint(playerId, "nx_conyard")) return;
            foreach (var e in _world.Entities)
            {
                if (e.Alive && e.Owner == playerId && e.Spec.DeploysInto != null)
                {
                    // Deploy via the same path an order would take.
                    var spec = _world.Rules.Unit(e.Spec.DeploysInto);
                    var origin = new CellPos(
                        e.HomeCell.X - spec.Structure.FootprintW / 2,
                        e.HomeCell.Y - spec.Structure.FootprintH / 2);
                    _world.ReleaseCell(e.HomeCell, e.Id);
                    if (PlacementValidator.CanPlace(_world, playerId, spec, origin, requireAdjacency: false))
                    {
                        _world.Kill(e);
                        _world.SpawnStructure(spec, playerId, origin);
                    }
                    else
                    {
                        _world.ClaimCell(e.HomeCell, e.Id);
                        // Wander to find open ground.
                        if (e.Move.Mode == MoveMode.None)
                        {
                            var wander = new CellPos(
                                _random.Next(2, _world.Map.Width - 2),
                                _random.Next(2, _world.Map.Height - 2));
                            _movement.OrderMovePath(e, wander);
                        }
                    }
                    return;
                }
            }
        }

        private void PlanConstruction(int playerId)
        {
            var queue = _production.GetQueue(playerId, ProductionQueue.Structure);
            if (queue.ActiveSpecIndex != -1) return;

            var player = _world.Players[playerId];
            var buildOrder = player.Faction == "serpent" ? SerpentBuildOrder : DominionBuildOrder;

            // Emergency power first.
            if (player.LowPower)
            {
                string plant = player.Faction == "serpent" ? "so_power_plant" : "dm_power_plant";
                TryStart(playerId, plant);
                return;
            }

            foreach (var id in buildOrder)
            {
                if (CountOwned(playerId, id) >= CountWanted(buildOrder, id)) continue;
                if (TryStart(playerId, id)) return;
            }
        }

        private static int CountWanted(string[] buildOrder, string id)
        {
            int count = 0;
            foreach (var entry in buildOrder)
            {
                if (entry == id) count++;
            }
            return count;
        }

        private int CountOwned(int playerId, string blueprintId)
        {
            int count = 0;
            foreach (var e in _world.Entities)
            {
                if (e.Alive && e.Owner == playerId && e.Spec.Id == blueprintId) count++;
            }
            return count;
        }

        private bool TryStart(int playerId, string blueprintId)
        {
            var spec = _world.Rules.Unit(blueprintId);
            if (!_production.CanBuild(playerId, spec)) return false;
            if (_world.Players[playerId].Credits < spec.Buildable.Cost / 4) return false;
            _production.StartBuild(playerId, spec.Index);
            return true;
        }

        private void PlaceReadyStructure(int playerId)
        {
            var queue = _production.GetQueue(playerId, ProductionQueue.Structure);
            if (!queue.ReadyForPlacement) return;
            var spec = _world.Rules.Units[queue.ActiveSpecIndex];

            // Spiral around the construction yard for a legal spot.
            var anchor = FindAnchor(playerId);
            if (anchor == null) return;

            for (int radius = 2; radius <= 14; radius++)
            {
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    var origin = new CellPos(
                        anchor.Value.X + _random.Next(-radius, radius + 1),
                        anchor.Value.Y + _random.Next(-radius, radius + 1));
                    if (PlacementValidator.CanPlace(_world, playerId, spec, origin))
                    {
                        _production.TryPlace(playerId, spec.Index, origin);
                        return;
                    }
                }
            }
        }

        private CellPos? FindAnchor(int playerId)
        {
            foreach (var e in _world.Entities)
            {
                if (e.Alive && e.Owner == playerId && e.Spec.IsStructure && e.Spec.Structure.BaseNormal)
                    return e.HomeCell;
            }
            return null;
        }

        private void PlanUnits(int playerId, AIState ai)
        {
            var player = _world.Players[playerId];
            var army = player.Faction == "serpent" ? SerpentArmy : DominionArmy;

            // Replace lost harvesters.
            if (CountOwned(playerId, "nx_harvester") < CountOwned(playerId, "nx_refinery")
                && player.Credits > 1600)
            {
                TryStart(playerId, "nx_harvester");
            }

            if (player.Credits < 600) return;
            int armyCap = WaveSize(ai.Difficulty) * 2 + 4;
            if (CountCombatUnits(playerId) >= armyCap) return;

            var infantryQueue = _production.GetQueue(playerId, ProductionQueue.Infantry);
            var vehicleQueue = _production.GetQueue(playerId, ProductionQueue.Vehicle);
            if (infantryQueue.ActiveSpecIndex == -1)
            {
                TryStart(playerId, army[_random.Next(0, army.Length)]);
            }
            if (vehicleQueue.ActiveSpecIndex == -1)
            {
                TryStart(playerId, army[_random.Next(0, army.Length)]);
            }
        }

        private int CountCombatUnits(int playerId)
        {
            int count = 0;
            foreach (var e in _world.Entities)
            {
                if (IsCombatUnit(e, playerId)) count++;
            }
            return count;
        }

        private static bool IsCombatUnit(Entity e, int playerId) =>
            e.Alive && e.Owner == playerId && !e.Spec.IsStructure
            && e.Spec.WeaponIndex >= 0 && e.Spec.Harvester == null;

        private void TryLaunchWave(int playerId, AIState ai)
        {
            if (ai.WaveCountdown > 0) return;

            var soldiers = new List<Entity>();
            foreach (var e in _world.Entities)
            {
                if (IsCombatUnit(e, playerId) && e.Move.Mode == MoveMode.None && e.AttackTargetId == -1)
                    soldiers.Add(e);
            }
            if (soldiers.Count < WaveSize(ai.Difficulty)) return;

            var target = FindEnemyTarget(playerId);
            if (target == null) return;

            foreach (var soldier in soldiers)
            {
                _combat.OrderAttackMove(soldier, target.Value);
            }
            ai.WaveCountdown = WaveInterval(ai.Difficulty);
        }

        private CellPos? FindEnemyTarget(int playerId)
        {
            // Prefer enemy production structures, then anything alive.
            Entity best = null;
            foreach (var e in _world.Entities)
            {
                if (!e.Alive || e.Owner == playerId) continue;
                if (e.Spec.IsStructure)
                {
                    if (best == null || !best.Spec.IsStructure || e.Spec.ProductionQueues != null) best = e;
                }
                else if (best == null)
                {
                    best = e;
                }
            }
            return best?.HomeCell;
        }

        public void AddToHash(ref StateHash hash)
        {
            hash.Add(_tick);
            for (int p = 0; p < World.MaxPlayers; p++) _players[p].AddToHash(ref hash);
        }
    }
}
