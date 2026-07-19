namespace TiberiumDusk.Sim.Data
{
    public enum SuperweaponKind : byte
    {
        IonCannon = 0,
        ClusterMissile = 1,
        EmpBlast = 2,
        HunterSeeker = 3,
    }

    /// <summary>Compiled from data/superweapons.json.</summary>
    public sealed class SuperweaponSpec
    {
        public string Id;
        public int Index;
        /// <summary>Structure prerequisite granting the power ("any:a|b" supported).</summary>
        public string GrantedBy;
        public int ChargeTicks;
        public SuperweaponKind Kind;
        public int Damage;
        public int RadiusLeptons;
        /// <summary>EMP paralysis duration.</summary>
        public int DurationTicks;
        /// <summary>Max distance from the granting structure (EMP cannon), 0 = unlimited.</summary>
        public int RangeLeptons;
        public int ClusterCount;
        /// <summary>Drone blueprint id (hunter-seeker).</summary>
        public string DroneUnit;
    }

    /// <summary>Ion storm + crates tuning from data/special.json.</summary>
    public sealed class SpecialRules
    {
        public int StormMinIntervalTicks = 13500;
        public int StormMaxIntervalTicks = 27000;
        public int StormWarningTicks = 465;
        public int StormDurationTicks = 1800;
        public int StormBoltEveryTicks = 12;
        public int StormBoltChancePercent = 55;
        public int StormBoltDamage = 500;
        public int StormBoltRadiusLeptons = 384;

        public int CrateMax = 6;
        public int CrateRegenTicks = 2700;
        public int CrateMoneyAmount = 2000;
        public int CrateSharesMoney = 55;
        public int CrateSharesVeterancy = 20;
        public int CrateSharesTrap = 15;
        public int CrateSharesHeal = 10;
        public int CrateTrapDamage = 300;
    }

    /// <summary>Per-match options (lobby settings later; defaults keep older scenarios stable).</summary>
    public sealed class GameSettings
    {
        public bool IonStormsEnabled;
        public bool CratesEnabled;
    }
}
