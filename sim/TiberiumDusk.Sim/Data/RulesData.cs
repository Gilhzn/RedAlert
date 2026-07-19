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

    /// <summary>Compiled, sim-ready unit blueprint. Produced from data/units.json by TiberiumDusk.Balance.</summary>
    public sealed class UnitSpec
    {
        public string Id;
        public int Index;
        public string Faction;
        public HealthSpec Health;
        /// <summary>Null for immobile entities (structures).</summary>
        public MobileSpec Mobile;
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
