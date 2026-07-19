using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Pathfinding;
using TiberiumDusk.Sim.WorldModel;

namespace TiberiumDusk.Sim.Systems
{
    /// <summary>
    /// Moves mobile entities along their paths/flow fields: rotate-then-move,
    /// terrain- and slope-scaled speed, cell claiming (one ground unit per cell),
    /// wait-then-repath on blockage, stop-near-target crowd resolution.
    /// </summary>
    public sealed class MovementSystem
    {
        /// <summary>Leptons per tick at speed value 1 on 100% terrain.</summary>
        public const int LeptonsPerSpeedUnit = 6;
        /// <summary>Ticks to wait on a blocked cell before repathing.</summary>
        public const int BlockedRepathDelay = 15;
        /// <summary>Vehicles may drive once facing is within this many steps of the move direction.</summary>
        public const int MoveFacingTolerance = 4;
        /// <summary>Give up (consider arrived) when blocked this close to the target.</summary>
        public const int CrowdedArrivalRadius = 2;

        private readonly World _world;

        public MovementSystem(World world)
        {
            _world = world;
        }

        public void Tick()
        {
            var entities = _world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity.Alive && entity.Spec.Mobile != null && entity.Move.Mode != MoveMode.None)
                {
                    Step(entity);
                }
            }
        }

        public void OrderMovePath(Entity entity, CellPos target)
        {
            if (entity.Spec.IsAircraft)
            {
                entity.Move.Clear();
                entity.Move.Mode = MoveMode.Path;
                entity.Move.Target = target;
                return;
            }
            ReleaseTransientClaim(entity);
            // Cells held by other units cost extra so paths flow around blockers.
            int self = entity.Id;
            var path = AStar.FindPath(_world.Map, _world.Rules, entity.Spec.Mobile.Locomotor,
                entity.HomeCell, target,
                cell => _world.IsCellFreeFor(cell, self) ? 0 : AStar.OccupiedCellPenalty);
            if (path == null || path.Count == 0)
            {
                entity.Move.Clear();
                return;
            }
            entity.Move.Clear();
            entity.Move.Mode = MoveMode.Path;
            entity.Move.Target = target;
            entity.Move.Path = path;
            entity.Move.PathIndex = 0;
        }

        public void OrderMoveFlow(Entity entity, FlowField flow)
        {
            ReleaseTransientClaim(entity);
            entity.Move.Clear();
            entity.Move.Mode = MoveMode.Flow;
            entity.Move.Target = flow.Destination;
            entity.Move.Flow = flow;
        }

        public void OrderStop(Entity entity)
        {
            ReleaseTransientClaim(entity);
            entity.Move.Clear();
        }

        private void Step(Entity entity)
        {
            var move = entity.Move;

            if (entity.Spec.IsAircraft)
            {
                StepAircraft(entity);
                return;
            }

            if (entity.HomeCell.Equals(move.Target) && !move.HasClaim)
            {
                FinishMove(entity);
                return;
            }

            CellPos? nextCell = CurrentWaypoint(entity);
            if (nextCell == null)
            {
                FinishMove(entity);
                return;
            }

            // Claim the next cell before driving into it.
            if (!nextCell.Value.Equals(entity.HomeCell) && !move.HasClaim)
            {
                if (_world.IsCellFreeFor(nextCell.Value, entity.Id))
                {
                    _world.ClaimCell(nextCell.Value, entity.Id);
                    move.HasClaim = true;
                    move.ClaimedCell = nextCell.Value;
                    move.BlockedTicks = 0;
                }
                else if (CanCrushOccupant(entity, nextCell.Value))
                {
                    // Crusher rolls over enemy infantry blocking the cell.
                    var victim = _world.GetEntity(_world.OccupantOf(nextCell.Value));
                    _world.Kill(victim);
                    _world.ClaimCell(nextCell.Value, entity.Id);
                    move.HasClaim = true;
                    move.ClaimedCell = nextCell.Value;
                    move.BlockedTicks = 0;
                }
                else
                {
                    OnBlocked(entity, nextCell.Value);
                    return;
                }
            }

            MoveToward(entity, LeptonPos.CellCenter(nextCell.Value));
        }

        /// <summary>Straight-line flight: no cells, no claims, terrain ignored.</summary>
        private void StepAircraft(Entity entity)
        {
            var mobile = entity.Spec.Mobile;
            var waypoint = LeptonPos.CellCenter(entity.Move.Target);
            int dx = waypoint.X - entity.Pos.X;
            int dy = waypoint.Y - entity.Pos.Y;

            byte desired = Facing.FromVector(dx, dy, entity.FacingValue);
            entity.FacingValue = Facing.TurnToward(entity.FacingValue, desired, mobile.Rot);

            int speed = mobile.Speed * LeptonsPerSpeedUnit;
            long dist = LeptonPos.IntSqrt((long)dx * dx + (long)dy * dy);
            if (dist <= speed)
            {
                entity.Pos = waypoint;
                entity.HomeCell = entity.Move.Target;
                entity.Move.Clear();
            }
            else
            {
                entity.Pos = new LeptonPos(
                    entity.Pos.X + (int)(dx * speed / dist),
                    entity.Pos.Y + (int)(dy * speed / dist));
                entity.HomeCell = entity.Pos.ToCell();
            }
        }

        private CellPos? CurrentWaypoint(Entity entity)
        {
            var move = entity.Move;
            if (move.HasClaim) return move.ClaimedCell;

            if (move.Mode == MoveMode.Path)
            {
                if (move.Path == null || move.PathIndex >= move.Path.Count) return null;
                return move.Path[move.PathIndex];
            }

            // Flow mode
            return move.Flow.NextCell(_world.Map, entity.HomeCell);
        }

        private bool CanCrushOccupant(Entity mover, CellPos cell)
        {
            if (!mover.Spec.Crusher) return false;
            var occupant = _world.GetEntity(_world.OccupantOf(cell));
            return occupant != null && occupant.Owner != mover.Owner && occupant.Spec.Crushable;
        }

        private void OnBlocked(Entity entity, CellPos blockedCell)
        {
            var move = entity.Move;

            // Blocked right next to a crowded destination: accept this as arrival.
            if (entity.HomeCell.ChebyshevDistance(move.Target) <= CrowdedArrivalRadius)
            {
                FinishMove(entity);
                return;
            }

            move.BlockedTicks++;
            if (move.BlockedTicks < BlockedRepathDelay) return;

            if (move.Mode == MoveMode.Flow)
            {
                // Flow fields ignore units; fall back to an individual path around the blocker.
                OrderMovePath(entity, move.Target);
            }
            else
            {
                var target = move.Target;
                OrderMovePath(entity, target);
            }

            if (entity.Move.Mode == MoveMode.None)
            {
                // No path found anymore — stay put.
                return;
            }
            entity.Move.BlockedTicks = 0;
        }

        private void MoveToward(Entity entity, LeptonPos waypoint)
        {
            var mobile = entity.Spec.Mobile;
            int dx = waypoint.X - entity.Pos.X;
            int dy = waypoint.Y - entity.Pos.Y;

            if (dx == 0 && dy == 0)
            {
                ArriveAtWaypoint(entity);
                return;
            }

            // Rotate toward the movement direction; vehicles only drive once roughly aligned.
            byte desired = Facing.FromVector(dx, dy, entity.FacingValue);
            entity.FacingValue = Facing.TurnToward(entity.FacingValue, desired, mobile.Rot);
            bool aligned = System.Math.Abs(Facing.Difference(entity.FacingValue, desired)) <= MoveFacingTolerance;
            if (!aligned && mobile.Locomotor != LocomotorId.Foot) return;

            int speedLeptons = SpeedLeptonsPerTick(entity);
            if (speedLeptons <= 0) return;

            long dist = LeptonPos.IntSqrt((long)dx * dx + (long)dy * dy);
            if (dist <= speedLeptons)
            {
                entity.Pos = waypoint;
                ArriveAtWaypoint(entity);
            }
            else
            {
                entity.Pos = new LeptonPos(
                    entity.Pos.X + (int)(dx * speedLeptons / dist),
                    entity.Pos.Y + (int)(dy * speedLeptons / dist));
                SyncHomeCell(entity);
            }
        }

        private int SpeedLeptonsPerTick(Entity entity)
        {
            var mobile = entity.Spec.Mobile;
            int terrainPercent = _world.Rules.SpeedPercent(_world.Map.Land(entity.HomeCell), mobile.Locomotor);
            int speed = mobile.Speed * LeptonsPerSpeedUnit * terrainPercent / 100;

            // Slope: climbing into a higher claimed cell slows tracked/wheeled drives.
            if (entity.Move.HasClaim)
            {
                int heightDiff = _world.Map.HeightLevel(entity.Move.ClaimedCell) - _world.Map.HeightLevel(entity.HomeCell);
                if (heightDiff != 0)
                {
                    var slopes = _world.Rules.Slopes;
                    int coef = 100;
                    if (mobile.Locomotor == LocomotorId.Tracked || mobile.Locomotor == LocomotorId.Walker)
                        coef = heightDiff > 0 ? slopes.TrackedUphillPercent : slopes.TrackedDownhillPercent;
                    else if (mobile.Locomotor == LocomotorId.Wheeled)
                        coef = heightDiff > 0 ? slopes.WheeledUphillPercent : slopes.WheeledDownhillPercent;
                    speed = speed * coef / 100;
                }
            }

            return speed > 0 ? speed : 1;
        }

        private void ArriveAtWaypoint(Entity entity)
        {
            var move = entity.Move;
            if (move.HasClaim)
            {
                _world.ReleaseCell(entity.HomeCell, entity.Id);
                entity.HomeCell = move.ClaimedCell;
                move.HasClaim = false;
                if (move.Mode == MoveMode.Path) move.PathIndex++;
            }
            SyncHomeCell(entity);

            bool atFinal = move.Mode == MoveMode.Path
                ? move.PathIndex >= move.Path.Count
                : entity.HomeCell.Equals(move.Target);
            if (atFinal) FinishMove(entity);
        }

        private void SyncHomeCell(Entity entity)
        {
            // Position may drift across the boundary before the formal waypoint
            // arrival; home cell stays authoritative for occupancy either way.
        }

        private void FinishMove(Entity entity)
        {
            ReleaseTransientClaim(entity);
            entity.Move.Clear();
        }

        private void ReleaseTransientClaim(Entity entity)
        {
            if (entity.Move.HasClaim)
            {
                _world.ReleaseCell(entity.Move.ClaimedCell, entity.Id);
                entity.Move.HasClaim = false;
            }
        }
    }
}
