using System.Collections.Generic;

namespace TiberiumDusk.Sim.Data
{
    /// <summary>Movement class of a unit. Order matches data/terrain.json speed keys.</summary>
    public enum LocomotorId : byte
    {
        Foot = 0,
        Tracked = 1,
        Wheeled = 2,
        Walker = 3,
        Hover = 4,
        Amphibious = 5,
        Subterranean = 6,
    }

    public enum ArmorClass : byte
    {
        None = 0,
        Wood = 1,
        Light = 2,
        Heavy = 3,
        Concrete = 4,
    }

    /// <summary>Per-land-type movement/buildability rules, indexed by land id byte.</summary>
    public sealed class LandRule
    {
        public string Id;
        public bool Buildable;
        /// <summary>Speed % per LocomotorId (0 = impassable).</summary>
        public int[] SpeedPercent;
    }

    public sealed class SlopeRules
    {
        public int TrackedUphillPercent = 50;
        public int TrackedDownhillPercent = 110;
        public int WheeledUphillPercent = 50;
        public int WheeledDownhillPercent = 120;
    }

    public sealed class MobileSpec
    {
        /// <summary>Raw speed value (original-engine scale, ~3..25). Leptons/tick = value × 6 × terrain%.</summary>
        public int Speed;
        public LocomotorId Locomotor;
        /// <summary>Facing steps (of 256) turned per tick.</summary>
        public int Rot;
    }

    public sealed class HealthSpec
    {
        public int Max;
        public ArmorClass Armor;
    }

    public enum ProductionQueue : byte
    {
        Structure = 0,
        Infantry = 1,
        Vehicle = 2,
        Aircraft = 3,
    }

    public sealed class BuildableSpec
    {
        public int Cost;
        public ProductionQueue Queue;
        public int TechLevel;
        /// <summary>Blueprint ids the owner must have alive before building this.</summary>
        public string[] Prerequisites;
    }

    public sealed class StructureSpec
    {
        public int FootprintW;
        public int FootprintH;
        /// <summary>Positive = produced, negative = drained.</summary>
        public int Power;
        /// <summary>Build-radius extension in cells beyond the footprint.</summary>
        public int Adjacent;
        /// <summary>Extends the owner's placement radius (walls/deployed defenses won't).</summary>
        public bool BaseNormal;
        /// <summary>Cell (relative to origin) where produced units appear; (-1,-1) = none.</summary>
        public int ExitX = -1, ExitY = -1;
    }

    public sealed class RefinerySpec
    {
        /// <summary>Docking cell relative to footprint origin.</summary>
        public int DockX, DockY;
        /// <summary>Blueprint id spawned free when the refinery is placed (the harvester).</summary>
        public string FreeUnit;
    }

    public sealed class HarvesterSpec
    {
        public int CapacityBails;
    }

    /// <summary>Compiled, sim-ready blueprint (units AND structures). Produced by TiberiumDusk.Balance.</summary>
    public sealed class UnitSpec
    {
        public string Id;
        public int Index;
        public string Faction;
        public HealthSpec Health;
        /// <summary>Null for immobile entities (structures).</summary>
        public MobileSpec Mobile;
        public BuildableSpec Buildable;
        /// <summary>Non-null when this blueprint is a structure.</summary>
        public StructureSpec Structure;
        public RefinerySpec Refinery;
        public HarvesterSpec Harvester;
        /// <summary>Storage bails contributed to the owner (refinery/silo).</summary>
        public int StorageBails;
        /// <summary>Structure blueprint id this unit deploys into (MCV), or null.</summary>
        public string DeploysInto;
        /// <summary>Production queues this structure hosts (ConYard: structure; Factory: vehicle...), or null.</summary>
        public ProductionQueue[] ProductionQueues;
        /// <summary>Takes crystal-field damage (unshielded organic infantry).</summary>
        public bool CrystalVulnerable;

        public bool IsStructure => Structure != null;
    }

    /// <summary>Economy/production constants from data/economy.json.</summary>
    public sealed class EconomyRules
    {
        public int GreenBailValue = 25;
        public int BlueBailValue = 40;
        public int MaxDensity = 11;
        public int GrowDensityThreshold = 11;
        public int SeedDensityThreshold = 6;
        public int GrowthIntervalTicks = 2200;
        public int GrowthChancePercent = 50;
        public int SpreadChancePercent = 20;
        public int CrystalDamageIntervalTicks = 9;
        public int CrystalDamageHp = 2;

        public int HarvestTicksPerBail = 12;
        public int UnloadTicksPerBail = 4;
        public int FieldScanRadiusCells = 12;
        public int FarScanRadiusCells = 48;

        public int BuildTicksPerThousandCost = 720;
        public int MaxQueuedPerClass = 4;
        public int LowPowerWorstPercent = 30;
        public int LowPowerBestPercent = 75;
        public int SellRefundPercent = 50;
        public int StartingCredits = 10000;
    }

    /// <summary>
    /// The full compiled rule set the simulation runs on. Immutable after load;
    /// shared by all lockstep clients (content hash checked at game start).
    /// </summary>
    public sealed class RulesData
    {
        public const int LocomotorCount = 7;

        public LandRule[] Lands;
        public SlopeRules Slopes;
        public UnitSpec[] Units;
        public EconomyRules Economy = new EconomyRules();

        private Dictionary<string, byte> _landIndex;
        private Dictionary<string, int> _unitIndex;

        public void BuildIndices()
        {
            _landIndex = new Dictionary<string, byte>();
            for (byte i = 0; i < Lands.Length; i++) _landIndex[Lands[i].Id] = i;
            _unitIndex = new Dictionary<string, int>();
            for (int i = 0; i < Units.Length; i++)
            {
                Units[i].Index = i;
                _unitIndex[Units[i].Id] = i;
            }
        }

        public byte LandIndex(string id) => _landIndex[id];
        public int UnitIndex(string id) => _unitIndex[id];
        public UnitSpec Unit(string id) => Units[_unitIndex[id]];

        public int SpeedPercent(byte land, LocomotorId locomotor) => Lands[land].SpeedPercent[(int)locomotor];
        public bool IsPassable(byte land, LocomotorId locomotor) => SpeedPercent(land, locomotor) > 0;
    }
}
