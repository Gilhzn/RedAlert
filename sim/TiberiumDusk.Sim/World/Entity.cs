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

    public enum HarvestPhase : byte
    {
        Idle = 0,
        ToField,
        Harvesting,
        ToRefinery,
        Unloading,
    }

    /// <summary>Autonomous harvester state machine data.</summary>
    public sealed class HarvesterState
    {
        public HarvestPhase Phase;
        public int CarriedBails;
        public int CarriedValue;
        public CellPos TargetCell;
        public int Timer;
        /// <summary>Ticks with no progress; forces a rescan.</summary>
        public int StallTicks;

        public void AddToHash(ref StateHash hash)
        {
            hash.Add((int)Phase);
            hash.Add(CarriedBails);
            hash.Add(CarriedValue);
            hash.Add(TargetCell.X);
            hash.Add(TargetCell.Y);
            hash.Add(Timer);
        }
    }

    /// <summary>
    /// A simulation entity — unit or structure. Deliberately a plain mutable
    /// class — systems process entities in id order, which is the canonical
    /// deterministic iteration order.
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

        /// <summary>The cell this entity occupies; for structures, the footprint origin (top-left).</summary>
        public CellPos HomeCell;

        public readonly MoveState Move = new MoveState();
        /// <summary>Non-null only for entities with a Harvester spec.</summary>
        public HarvesterState Harvest;

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
            Harvest?.AddToHash(ref hash);
        }
    }
}
