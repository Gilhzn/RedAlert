using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Pathfinding;

namespace TiberiumDusk.Sim.WorldModel
{
    public enum MoveMode : byte
    {
        None = 0,
        Path,       // following an A* waypoint list
        Flow,       // following a shared flow field
    }

    /// <summary>Mutable movement state of a mobile entity.</summary>
    public sealed class MoveState
    {
        public MoveMode Mode;
        public CellPos Target;
        public List<CellPos> Path;
        public int PathIndex;
        public FlowField Flow;
        /// <summary>Ticks spent waiting for a blocked cell before repathing.</summary>
        public int BlockedTicks;
        public bool HasClaim;
        public CellPos ClaimedCell;

        public void Clear()
        {
            Mode = MoveMode.None;
            Path = null;
            PathIndex = 0;
            Flow = null;
            BlockedTicks = 0;
        }
    }

    /// <summary>
    /// A simulation entity (unit; structures join in Phase 3). Deliberately a
    /// plain mutable class — systems process entities in id order, which is the
    /// canonical deterministic iteration order.
    /// </summary>
    public sealed class Entity
    {
        public int Id;
        public UnitSpec Spec;
        public int Owner;
        public bool Alive = true;

        public LeptonPos Pos;
        public byte FacingValue;
        public int Hp;

        /// <summary>The cell this entity occupies in the occupancy grid.</summary>
        public CellPos HomeCell;

        public readonly MoveState Move = new MoveState();

        public void AddToHash(ref StateHash hash)
        {
            hash.Add(Id);
            hash.Add(Spec.Index);
            hash.Add(Owner);
            hash.Add(Alive ? 1 : 0);
            hash.Add(Pos);
            hash.Add(FacingValue);
            hash.Add(Hp);
            hash.Add((int)Move.Mode);
            hash.Add(Move.Target.X);
            hash.Add(Move.Target.Y);
            hash.Add(Move.PathIndex);
            hash.Add(Move.BlockedTicks);
        }
    }
}
