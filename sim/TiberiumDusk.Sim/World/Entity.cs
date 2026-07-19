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

        // ---- Combat state ----
        /// <summary>Explicit attack target (player order), or -1.</summary>
        public int AttackTargetId = -1;
        /// <summary>Auto-engage while moving (attack-move) or standing guard.</summary>
        public bool AutoEngage = true;
        /// <summary>Attack-move destination to resume after a kill; valid when AttackMovePending.</summary>
        public CellPos AttackMoveDest;
        public bool AttackMovePending;
        /// <summary>Ticks until the weapon may fire again.</summary>
        public int WeaponCooldown;
        public byte TurretFacing;
        /// <summary>EMP paralysis: no move/fire while > 0.</summary>
        public int DisabledTicks;
        /// <summary>Value of enemies destroyed (veterancy XP).</summary>
        public int CombatXp;
        /// <summary>Veterancy rank 0..2.</summary>
        public int Rank;
        /// <summary>Capture mission target (engineer), or -1.</summary>
        public int CaptureTargetId = -1;

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
            hash.Add(AttackTargetId);
            hash.Add(WeaponCooldown);
            hash.Add(TurretFacing);
            hash.Add(DisabledTicks);
            hash.Add(CombatXp);
            hash.Add(Rank);
            hash.Add(CaptureTargetId);
            Harvest?.AddToHash(ref hash);
        }
    }
}
