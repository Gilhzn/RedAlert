using System.Collections.Generic;

namespace TiberiumDusk.Balance
{
    /// <summary>Immutable, validated view of data/*.json. The sim consumes only these types.</summary>
    public sealed class GameData
    {
        public IReadOnlyDictionary<string, UnitBlueprint> Units { get; }
        public IReadOnlyDictionary<string, WeaponBlueprint> Weapons { get; }
        public IReadOnlyDictionary<string, WarheadBlueprint> Warheads { get; }
        public IReadOnlyDictionary<string, LandType> LandTypes { get; }

        public GameData(
            IReadOnlyDictionary<string, UnitBlueprint> units,
            IReadOnlyDictionary<string, WeaponBlueprint> weapons,
            IReadOnlyDictionary<string, WarheadBlueprint> warheads,
            IReadOnlyDictionary<string, LandType> landTypes)
        {
            Units = units;
            Weapons = weapons;
            Warheads = warheads;
            LandTypes = landTypes;
        }
    }

    public sealed class UnitBlueprint
    {
        public string Id { get; set; }
        public string Faction { get; set; }
        /// <summary>Component name → raw config. Systems interpret their own components (trait pattern).</summary>
        public Dictionary<string, Dictionary<string, object>> Components { get; set; }
    }

    public sealed class WeaponBlueprint
    {
        public string Id { get; set; }
        public int Damage { get; set; }
        /// <summary>Frames between shots at 15 ticks/second.</summary>
        public int Rof { get; set; }
        /// <summary>Range in cells (fractional allowed, converted to leptons by the sim).</summary>
        public double Range { get; set; }
        public string Warhead { get; set; }
        /// <summary>instant | direct | arcing.</summary>
        public string ProjectileKind { get; set; }
        public int ProjectileSpeed { get; set; }
        /// <summary>"g", "a", or "ga".</summary>
        public string Targets { get; set; }
        public double MinRange { get; set; }
    }

    public sealed class WarheadBlueprint
    {
        public string Id { get; set; }
        public int Spread { get; set; }
        /// <summary>Damage % vs armor classes: [none, wood, light, heavy, concrete].</summary>
        public int[] Verses { get; set; }
        public bool EmpEffect { get; set; }
    }

    public sealed class LandType
    {
        public string Id { get; set; }
        public bool Buildable { get; set; }
        /// <summary>Locomotor id → speed % (0 = impassable).</summary>
        public Dictionary<string, int> Speed { get; set; }
    }
}
