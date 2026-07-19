using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.WorldModel;

namespace TiberiumDusk.Sim.Systems
{
    /// <summary>
    /// Classic sidebar production: one active item per queue class per player
    /// (structure/infantry/vehicle/aircraft), up to MaxQueuedPerClass waiting.
    /// Credits drain progressively with build progress; low power slows the
    /// structure queue. Finished structures wait for a PlaceStructure order;
    /// finished units spawn at their factory's exit cell.
    /// </summary>
    public sealed class ProductionSystem
    {
        public sealed class QueueState
        {
            public int ActiveSpecIndex = -1;
            /// <summary>Progress in cost-millipoints: cost*1000 means done.</summary>
            public long ProgressMilli;
            public long PaidMilli;
            /// <summary>Structure finished and awaiting placement.</summary>
            public bool ReadyForPlacement;
            public readonly List<int> Waiting = new List<int>();

            public void AddToHash(ref StateHash hash)
            {
                hash.Add(ActiveSpecIndex);
                hash.Add(ProgressMilli);
                hash.Add(PaidMilli);
                hash.Add(ReadyForPlacement ? 1 : 0);
                hash.Add(Waiting.Count);
                for (int i = 0; i < Waiting.Count; i++) hash.Add(Waiting[i]);
            }
        }

        public const int QueueClassCount = 4;

        private readonly World _world;
        private readonly MovementSystem _movement;
        /// <summary>[player][queueClass]</summary>
        private readonly QueueState[][] _queues;

        public ProductionSystem(World world, MovementSystem movement)
        {
            _world = world;
            _movement = movement;
            _queues = new QueueState[World.MaxPlayers][];
            for (int p = 0; p < World.MaxPlayers; p++)
            {
                _queues[p] = new QueueState[QueueClassCount];
                for (int q = 0; q < QueueClassCount; q++) _queues[p][q] = new QueueState();
            }
        }

        public QueueState GetQueue(int playerId, ProductionQueue queue) => _queues[playerId][(int)queue];

        public bool CanBuild(int playerId, UnitSpec spec)
        {
            if (spec.Buildable == null || spec.Buildable.TechLevel < 0) return false;
            // Faction gate: "shared" blueprints belong to everyone.
            var player = _world.Players[playerId];
            if (spec.Faction != "shared" && spec.Faction != player.Faction) return false;
            // Owner must have a factory hosting the queue.
            if (_world.FindPrimaryFactory(playerId, spec.Buildable.Queue) == null) return false;
            foreach (var prereq in spec.Buildable.Prerequisites)
            {
                if (!SatisfiesPrerequisite(playerId, prereq)) return false;
            }
            if (spec.BuildLimit > 0 && CountOwnedAndQueued(playerId, spec) >= spec.BuildLimit) return false;
            return true;
        }

        /// <summary>"any:a|b|c" = own at least one of; plain id = own it.</summary>
        private bool SatisfiesPrerequisite(int playerId, string prereq)
        {
            if (prereq.StartsWith("any:"))
            {
                foreach (var option in prereq.Substring(4).Split('|'))
                {
                    if (_world.OwnsBlueprint(playerId, option)) return true;
                }
                return false;
            }
            return _world.OwnsBlueprint(playerId, prereq);
        }

        private int CountOwnedAndQueued(int playerId, UnitSpec spec)
        {
            int count = 0;
            var entities = _world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var e = entities[i];
                if (e.Alive && e.Owner == playerId && e.Spec.Index == spec.Index) count++;
            }
            var queue = GetQueue(playerId, spec.Buildable.Queue);
            if (queue.ActiveSpecIndex == spec.Index) count++;
            for (int i = 0; i < queue.Waiting.Count; i++)
            {
                if (queue.Waiting[i] == spec.Index) count++;
            }
            return count;
        }

        public void StartBuild(int playerId, int specIndex)
        {
            var spec = _world.Rules.Units[specIndex];
            if (!CanBuild(playerId, spec)) return;

            var queue = GetQueue(playerId, spec.Buildable.Queue);
            if (queue.ActiveSpecIndex == -1)
            {
                queue.ActiveSpecIndex = specIndex;
                queue.ProgressMilli = 0;
                queue.PaidMilli = 0;
                queue.ReadyForPlacement = false;
            }
            else if (queue.Waiting.Count < _world.Rules.Economy.MaxQueuedPerClass)
            {
                queue.Waiting.Add(specIndex);
            }
        }

        public void CancelBuild(int playerId, int specIndex)
        {
            var spec = _world.Rules.Units[specIndex];
            if (spec.Buildable == null) return;
            var queue = GetQueue(playerId, spec.Buildable.Queue);

            // Remove from waiting first (last matching entry).
            for (int i = queue.Waiting.Count - 1; i >= 0; i--)
            {
                if (queue.Waiting[i] == specIndex)
                {
                    queue.Waiting.RemoveAt(i);
                    return;
                }
            }
            if (queue.ActiveSpecIndex == specIndex)
            {
                // Refund what was actually paid.
                _world.Players[playerId].Credits += (int)(queue.PaidMilli / 1000);
                ClearActive(queue);
            }
        }

        public void Tick()
        {
            for (int p = 0; p < World.MaxPlayers; p++)
            {
                for (int q = 0; q < QueueClassCount; q++)
                {
                    TickQueue(p, (ProductionQueue)q, _queues[p][q]);
                }
            }
        }

        private void TickQueue(int playerId, ProductionQueue queueClass, QueueState queue)
        {
            if (queue.ActiveSpecIndex == -1)
            {
                if (queue.Waiting.Count > 0)
                {
                    queue.ActiveSpecIndex = queue.Waiting[0];
                    queue.Waiting.RemoveAt(0);
                    queue.ProgressMilli = 0;
                    queue.PaidMilli = 0;
                    queue.ReadyForPlacement = false;
                }
                return;
            }
            if (queue.ReadyForPlacement) return;

            var spec = _world.Rules.Units[queue.ActiveSpecIndex];
            var player = _world.Players[playerId];
            var economy = _world.Rules.Economy;

            // The factory may have died mid-build.
            if (_world.FindPrimaryFactory(playerId, queueClass) == null) return;

            long totalMilli = (long)spec.Buildable.Cost * 1000;
            long ticksTotal = (long)spec.Buildable.Cost * economy.BuildTicksPerThousandCost / 1000;
            if (ticksTotal <= 0) ticksTotal = 1;

            // Power scaling (applies to every queue, per original low-power rules).
            int ratePercent = player.BuildRatePercent(economy.LowPowerWorstPercent, economy.LowPowerBestPercent);
            long stepMilli = totalMilli * ratePercent / (ticksTotal * 100);
            if (stepMilli <= 0) stepMilli = 1;
            if (queue.ProgressMilli + stepMilli > totalMilli) stepMilli = totalMilli - queue.ProgressMilli;

            // Progressive payment: pay for this step or stall.
            long owedMilli = queue.ProgressMilli + stepMilli - queue.PaidMilli;
            int owedCredits = (int)((owedMilli + 999) / 1000);
            if (owedCredits > 0)
            {
                if (player.Credits < owedCredits) return;   // insufficient funds — stall
                player.Credits -= owedCredits;
                queue.PaidMilli += owedCredits * 1000L;
            }

            queue.ProgressMilli += stepMilli;
            if (queue.ProgressMilli < totalMilli) return;

            // Complete.
            if (spec.IsStructure)
            {
                queue.ReadyForPlacement = true;
            }
            else
            {
                TrySpawnUnit(playerId, queueClass, spec, queue);
            }
        }

        private void TrySpawnUnit(int playerId, ProductionQueue queueClass, UnitSpec spec, QueueState queue)
        {
            var factory = _world.FindPrimaryFactory(playerId, queueClass);
            if (factory == null) return;

            var exitCell = FindExitCell(factory);
            if (exitCell == null) return;   // exit blocked — retry next tick

            var unit = _world.Spawn(spec, playerId, exitCell.Value);
            // Nudge fresh units away from the door.
            if (unit.Spec.Mobile != null)
            {
                var rally = new CellPos(exitCell.Value.X, exitCell.Value.Y + 2);
                if (_world.Map.InBounds(rally)) _movement.OrderMovePath(unit, rally);
            }
            ClearActive(queue);
        }

        /// <summary>Placement of a finished structure (PlaceStructure order).</summary>
        public void TryPlace(int playerId, int specIndex, CellPos origin)
        {
            var spec = _world.Rules.Units[specIndex];
            if (spec.Structure == null) return;
            var queue = GetQueue(playerId, spec.Buildable.Queue);
            if (!queue.ReadyForPlacement || queue.ActiveSpecIndex != specIndex) return;
            if (!PlacementValidator.CanPlace(_world, playerId, spec, origin)) return;

            var structure = _world.SpawnStructure(spec, playerId, origin);
            OnStructurePlaced(structure);
            ClearActive(queue);
        }

        private void OnStructurePlaced(Entity structure)
        {
            // Refinery arrives with a free harvester next to its dock.
            var refinery = structure.Spec.Refinery;
            if (refinery?.FreeUnit != null)
            {
                var dock = new CellPos(structure.HomeCell.X + refinery.DockX, structure.HomeCell.Y + refinery.DockY + 1);
                var spot = FindFreeCellNear(dock);
                if (spot != null)
                {
                    _world.Spawn(_world.Rules.Unit(refinery.FreeUnit), structure.Owner, spot.Value);
                }
            }
        }

        private CellPos? FindExitCell(Entity factory)
        {
            var s = factory.Spec.Structure;
            if (s.ExitX >= 0)
            {
                var exit = new CellPos(factory.HomeCell.X + s.ExitX, factory.HomeCell.Y + s.ExitY);
                if (_world.Map.InBounds(exit) && _world.OccupantOf(exit) == -1) return exit;
            }
            return FindFreeCellNear(new CellPos(factory.HomeCell.X, factory.HomeCell.Y + s.FootprintH));
        }

        private CellPos? FindFreeCellNear(CellPos center)
        {
            for (int radius = 0; radius <= 3; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        var cell = new CellPos(center.X + dx, center.Y + dy);
                        if (_world.Map.InBounds(cell) && _world.OccupantOf(cell) == -1
                            && _world.Rules.Lands[_world.Map.Land(cell)].SpeedPercent[(int)LocomotorId.Tracked] > 0)
                        {
                            return cell;
                        }
                    }
                }
            }
            return null;
        }

        private static void ClearActive(QueueState queue)
        {
            queue.ActiveSpecIndex = -1;
            queue.ProgressMilli = 0;
            queue.PaidMilli = 0;
            queue.ReadyForPlacement = false;
        }

        public void AddToHash(ref StateHash hash)
        {
            for (int p = 0; p < World.MaxPlayers; p++)
            {
                for (int q = 0; q < QueueClassCount; q++)
                {
                    _queues[p][q].AddToHash(ref hash);
                }
            }
        }
    }
}
