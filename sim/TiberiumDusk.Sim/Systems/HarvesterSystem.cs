using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.WorldModel;

namespace TiberiumDusk.Sim.Systems
{
    /// <summary>
    /// Autonomous harvester loop: find crystal → drive there → harvest bail by
    /// bail → drive to own refinery dock → unload into credits (capped by
    /// storage) → repeat. A player Move order overrides the loop until idle.
    /// </summary>
    public sealed class HarvesterSystem
    {
        private const int StallRescanTicks = 45;

        private readonly World _world;
        private readonly MovementSystem _movement;

        public HarvesterSystem(World world, MovementSystem movement)
        {
            _world = world;
            _movement = movement;
        }

        public void Tick()
        {
            var entities = _world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity.Alive && entity.Harvest != null) Step(entity);
            }
        }

        private void Step(Entity entity)
        {
            var h = entity.Harvest;
            switch (h.Phase)
            {
                case HarvestPhase.Idle:
                {
                    if (entity.Move.Mode != MoveMode.None) return;   // player is driving it
                    if (h.CarriedBails >= entity.Spec.Harvester.CapacityBails)
                    {
                        BeginReturn(entity);
                        return;
                    }
                    var field = FindNearestCrystal(entity.HomeCell);
                    if (field != null)
                    {
                        h.Phase = HarvestPhase.ToField;
                        h.TargetCell = field.Value;
                        _movement.OrderMovePath(entity, field.Value);
                    }
                    break;
                }

                case HarvestPhase.ToField:
                {
                    if (entity.Move.Mode != MoveMode.None) return;   // still driving
                    if (_world.Crystal.HasCrystal(entity.HomeCell))
                    {
                        h.Phase = HarvestPhase.Harvesting;
                        h.Timer = 0;
                    }
                    else if (entity.HomeCell.Equals(h.TargetCell) || ++h.StallTicks > StallRescanTicks)
                    {
                        h.StallTicks = 0;
                        h.Phase = HarvestPhase.Idle;   // field gone — rescan
                    }
                    break;
                }

                case HarvestPhase.Harvesting:
                {
                    var economy = _world.Rules.Economy;
                    if (h.CarriedBails >= entity.Spec.Harvester.CapacityBails)
                    {
                        BeginReturn(entity);
                        return;
                    }
                    if (!_world.Crystal.HasCrystal(entity.HomeCell))
                    {
                        // Cell exhausted — move to the next crystal cell nearby, or rescan.
                        var next = FindNearestCrystal(entity.HomeCell);
                        if (next == null)
                        {
                            if (h.CarriedBails > 0) BeginReturn(entity);
                            else h.Phase = HarvestPhase.Idle;
                            return;
                        }
                        h.Phase = HarvestPhase.ToField;
                        h.TargetCell = next.Value;
                        _movement.OrderMovePath(entity, next.Value);
                        return;
                    }
                    if (++h.Timer >= economy.HarvestTicksPerBail)
                    {
                        h.Timer = 0;
                        int value = _world.Crystal.HarvestBail(entity.HomeCell, economy);
                        if (value > 0)
                        {
                            h.CarriedBails++;
                            h.CarriedValue += value;
                        }
                    }
                    break;
                }

                case HarvestPhase.ToRefinery:
                {
                    if (entity.Move.Mode != MoveMode.None) return;
                    var refinery = FindNearestRefinery(entity);
                    if (refinery == null)
                    {
                        h.Phase = HarvestPhase.Idle;
                        return;
                    }
                    var dock = DockCell(refinery);
                    if (entity.HomeCell.ChebyshevDistance(dock) <= 1)
                    {
                        h.Phase = HarvestPhase.Unloading;
                        h.Timer = 0;
                    }
                    else if (++h.StallTicks > StallRescanTicks)
                    {
                        h.StallTicks = 0;
                        _movement.OrderMovePath(entity, dock);
                    }
                    break;
                }

                case HarvestPhase.Unloading:
                {
                    var economy = _world.Rules.Economy;
                    if (h.CarriedBails == 0)
                    {
                        h.CarriedValue = 0;
                        h.Phase = HarvestPhase.Idle;
                        return;
                    }
                    if (++h.Timer >= economy.UnloadTicksPerBail)
                    {
                        h.Timer = 0;
                        int bailValue = h.CarriedValue / h.CarriedBails;
                        h.CarriedBails--;
                        h.CarriedValue -= bailValue;

                        var player = _world.Players[entity.Owner];
                        // Storage-capped: refined credits beyond capacity are lost ("silos needed").
                        if (player.Credits < player.StorageCapacityCredits)
                        {
                            player.Credits = System.Math.Min(player.Credits + bailValue, player.StorageCapacityCredits);
                        }
                    }
                    break;
                }
            }
        }

        private void BeginReturn(Entity entity)
        {
            var h = entity.Harvest;
            var refinery = FindNearestRefinery(entity);
            if (refinery == null)
            {
                h.Phase = HarvestPhase.Idle;
                return;
            }
            h.Phase = HarvestPhase.ToRefinery;
            h.StallTicks = 0;
            _movement.OrderMovePath(entity, DockCell(refinery));
        }

        private static CellPos DockCell(Entity refinery) => new CellPos(
            refinery.HomeCell.X + refinery.Spec.Refinery.DockX,
            refinery.HomeCell.Y + refinery.Spec.Refinery.DockY + 1);

        private Entity FindNearestRefinery(Entity harvester)
        {
            Entity best = null;
            int bestDist = int.MaxValue;
            var entities = _world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var e = entities[i];
                if (!e.Alive || e.Owner != harvester.Owner || e.Spec.Refinery == null) continue;
                int dist = e.HomeCell.ChebyshevDistance(harvester.HomeCell);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = e;
                }
            }
            return best;
        }

        /// <summary>Nearest crystal cell within scan radius (deterministic ring scan, closest first).</summary>
        private CellPos? FindNearestCrystal(CellPos from)
        {
            int maxRadius = _world.Rules.Economy.FarScanRadiusCells;
            for (int radius = 0; radius <= maxRadius; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != radius) continue;
                        var cell = new CellPos(from.X + dx, from.Y + dy);
                        if (!_world.Map.InBounds(cell)) continue;
                        if (_world.Crystal.HasCrystal(cell) && _world.OccupantOf(cell) == -1)
                        {
                            return cell;
                        }
                    }
                }
            }
            return null;
        }
    }
}
