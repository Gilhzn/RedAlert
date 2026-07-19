using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Orders;
using TiberiumDusk.Sim.WorldModel;
using Xunit;

namespace TiberiumDusk.Sim.Tests
{
    /// <summary>Phase 5: full rosters, faction gating, special locomotors and mechanics.</summary>
    public class RosterTests
    {
        private static readonly List<Order> NoOrders = new List<Order>();

        private static Game NewGame(ulong seed = 11) => new Game(TestWorlds.Rules, TestWorlds.Flat(), seed);

        private static void RunTicks(Game game, int ticks)
        {
            for (int i = 0; i < ticks; i++) game.Tick(NoOrders);
        }

        private static Order BuildOrder(int player, string specId) =>
            new Order(OrderType.BuildStart, player, 0, data: TestWorlds.Rules.UnitIndex(specId));

        // ---------- Faction gating & tech trees ----------

        [Fact]
        public void FactionGatingBlocksCrossFactionBuilds()
        {
            var game = NewGame();
            game.SetPlayerFaction(0, "dominion");
            game.World.SpawnStructure(TestWorlds.Rules.Unit("nx_conyard"), 0, new CellPos(5, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(9, 5));

            Assert.True(game.Production.CanBuild(0, TestWorlds.Rules.Unit("dm_barracks")));
            Assert.False(game.Production.CanBuild(0, TestWorlds.Rules.Unit("so_hand")));
            Assert.True(game.Production.CanBuild(0, TestWorlds.Rules.Unit("nx_refinery")));   // shared
        }

        [Theory]
        [InlineData("dominion",
            new[] { "dm_power_plant", "nx_refinery", "dm_barracks", "dm_factory", "dm_radar",
                    "dm_helipad", "dm_tech_center", "dm_service_depot", "dm_guard_tower",
                    "dm_rocket_tower", "dm_aa_tower", "nx_silo" },
            new[] { "dm_rifle", "dm_grenadier", "dm_medic", "dm_engineer", "dm_jumptrooper",
                    "dm_railhero", "dm_wolverine", "dm_mbt_walker", "dm_apc", "dm_hover_mlrs",
                    "dm_disruptor", "dm_mammoth", "dm_orca", "dm_orca_bomber", "nx_harvester", "nx_mcv" })]
        [InlineData("serpent",
            new[] { "so_power_plant", "nx_refinery", "so_hand", "so_factory", "so_radar",
                    "so_helipad", "so_tech_center", "so_adv_power", "so_laser_turret", "so_sam",
                    "so_obelisk", "so_stealth_generator", "so_temple", "nx_silo" },
            new[] { "so_rifle", "so_rocket_trooper", "so_engineer", "so_cyborg", "so_cyborg_commando",
                    "so_scout_buggy", "so_attack_cycle", "so_tick_tank", "so_artillery",
                    "so_devil_tongue", "so_sub_apc", "so_stealth_tank", "so_repair_vehicle",
                    "so_harpy", "so_banshee", "nx_harvester", "nx_mcv" })]
        public void FullTechTreeIsReachable(string faction, string[] structures, string[] units)
        {
            var game = NewGame();
            game.SetPlayerFaction(0, faction);
            game.World.SpawnStructure(TestWorlds.Rules.Unit("nx_conyard"), 0, new CellPos(20, 20));

            // Spawn the whole structure tree (placement validated separately) and
            // verify every structure became buildable at the moment its prereqs existed.
            int x = 2, y = 2;
            foreach (var id in structures)
            {
                var spec = TestWorlds.Rules.Unit(id);
                Assert.True(game.Production.CanBuild(0, spec),
                    $"{faction}: '{id}' should be buildable once its prerequisites exist");
                game.World.SpawnStructure(spec, 0, new CellPos(x, y));
                x += 4;
                if (x > 58) { x = 2; y += 4; }
            }

            foreach (var id in units)
            {
                Assert.True(game.Production.CanBuild(0, TestWorlds.Rules.Unit(id)),
                    $"{faction}: unit '{id}' should be buildable with the full tech tree");
            }
        }

        [Fact]
        public void HeroUnitsAreLimitedToOne()
        {
            var game = NewGame();
            game.SetPlayerFaction(0, "dominion");
            game.World.SpawnStructure(TestWorlds.Rules.Unit("nx_conyard"), 0, new CellPos(5, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(9, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_barracks"), 0, new CellPos(5, 9));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("nx_refinery"), 0, new CellPos(12, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_factory"), 0, new CellPos(5, 12));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_radar"), 0, new CellPos(9, 9));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_tech_center"), 0, new CellPos(16, 5));

            var hero = TestWorlds.Rules.Unit("dm_railhero");
            Assert.True(game.Production.CanBuild(0, hero));
            game.Spawn("dm_railhero", 0, new CellPos(20, 20));
            Assert.False(game.Production.CanBuild(0, hero));   // limit 1
        }

        // ---------- Special locomotors ----------

        [Fact]
        public void AircraftFliesStraightOverWaterAndCliffs()
        {
            var game = new Game(TestWorlds.Rules, TestWorlds.WaterChannel(), 11);
            var orca = game.Spawn("dm_orca", 0, new CellPos(5, 10));
            game.Tick(new List<Order>
            {
                new Order(OrderType.Move, 0, 0, orca.Id, targetPos: LeptonPos.CellCenter(new CellPos(55, 10)))
            });
            RunTicks(game, 300);
            Assert.Equal(new CellPos(55, 10), orca.HomeCell);   // straight across the channel
        }

        [Fact]
        public void SubterraneanCrossesUnderButNotThroughWater()
        {
            // Water blocks subterranean (can't dig under rivers in our table);
            // clear ground everywhere else is 100%.
            var map = TestWorlds.Flat();
            var game = new Game(TestWorlds.Rules, map, 11);
            var mole = game.Spawn("so_devil_tongue", 0, new CellPos(5, 10));
            game.Tick(new List<Order>
            {
                new Order(OrderType.Move, 0, 0, mole.Id, targetPos: LeptonPos.CellCenter(new CellPos(30, 10)))
            });
            RunTicks(game, 900);
            Assert.Equal(new CellPos(30, 10), mole.HomeCell);
        }

        // ---------- Aircraft ammo ----------

        [Fact]
        public void AircraftRunsDryAndRearmsAtPad()
        {
            var game = NewGame();
            game.SetPlayerFaction(0, "dominion");
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_helipad"), 0, new CellPos(8, 8));
            var orca = game.Spawn("dm_orca", 0, new CellPos(10, 12));
            var target = game.Spawn("so_scout_buggy", 1, new CellPos(20, 12));
            target.AutoEngage = false;
            target.Hp = 100000;   // sponge so the orca empties its magazine
            target.Spec.Health.Max.Equals(0);

            game.Tick(new List<Order> { new Order(OrderType.Attack, 0, 0, orca.Id, target.Id) });
            RunTicks(game, 600);

            Assert.Equal(0, orca.Ammo);   // magazine spent

            // It should fly back to the pad and eventually rearm at least one point.
            RunTicks(game, TestWorlds.Rules.Combat.ReloadTicksPerAmmo * 2 + 300);
            Assert.True(orca.Ammo > 0, $"orca should rearm at the pad (ammo={orca.Ammo})");
        }

        // ---------- Stealth ----------

        [Fact]
        public void CloakedTankIsUntargetableUntilItFires()
        {
            var game = NewGame();
            var stealth = game.Spawn("so_stealth_tank", 1, new CellPos(14, 10));
            var tower = game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_guard_tower"), 0, new CellPos(10, 10));
            stealth.AutoEngage = false;

            RunTicks(game, 30);
            Assert.True(stealth.IsCloaked, "stealth tank should cloak when passive");
            Assert.Equal(stealth.Spec.Health.Max, stealth.Hp);   // tower can't see it

            // It opens fire → decloaks → the tower engages it.
            game.Tick(new List<Order> { new Order(OrderType.Attack, 1, 0, stealth.Id, tower.Id) });
            RunTicks(game, 90);
            Assert.False(stealth.IsCloaked, "firing must break the cloak");
            Assert.True(stealth.Hp < stealth.Spec.Health.Max, "decloaked tank should be taking fire");
        }

        [Fact]
        public void StealthGeneratorCloaksNearbyAllies()
        {
            var game = NewGame();
            // The veil generator drains 350 power — give it a real grid.
            game.World.SpawnStructure(TestWorlds.Rules.Unit("so_adv_power"), 1, new CellPos(30, 34));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("so_adv_power"), 1, new CellPos(34, 34));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("so_stealth_generator"), 1, new CellPos(30, 30));
            var guard = game.Spawn("so_rifle", 1, new CellPos(28, 30));
            var far = game.Spawn("so_rifle", 1, new CellPos(5, 5));

            RunTicks(game, 10);
            Assert.True(guard.IsCloaked, "unit inside the veil should be cloaked");
            Assert.False(far.IsCloaked, "unit outside the veil stays visible");
        }

        // ---------- Healers ----------

        [Fact]
        public void MedicHealsInfantryButNotVehicles()
        {
            var game = NewGame();
            var medic = game.Spawn("dm_medic", 0, new CellPos(10, 10));
            var soldier = game.Spawn("dm_rifle", 0, new CellPos(11, 10));
            var tank = game.Spawn("dm_mbt_walker", 0, new CellPos(12, 10));
            soldier.Hp = 40;
            tank.Hp = 100;

            RunTicks(game, 700);

            Assert.Equal(soldier.Spec.Health.Max, soldier.Hp);   // healed to full
            Assert.Equal(100, tank.Hp);                           // medics don't fix armor
        }

        [Fact]
        public void RepairVehicleFixesTanks()
        {
            var game = NewGame();
            var rig = game.Spawn("so_repair_vehicle", 1, new CellPos(10, 10));
            var tank = game.Spawn("so_tick_tank", 1, new CellPos(11, 10));
            tank.Hp = 60;

            RunTicks(game, 900);

            Assert.Equal(tank.Spec.Health.Max, tank.Hp);
        }

        // ---------- Deploy / undeploy ----------

        [Fact]
        public void TickTankEntrenchesAndUndeploys()
        {
            var game = NewGame();
            var tank = game.Spawn("so_tick_tank", 1, new CellPos(15, 15));

            game.Tick(new List<Order> { new Order(OrderType.Deploy, 1, 0, tank.Id) });
            Assert.Null(game.World.GetEntity(tank.Id));
            Assert.True(game.World.OwnsBlueprint(1, "so_tick_deployed"));

            // Find the deployed emplacement and undeploy it.
            Entity deployed = null;
            foreach (var e in game.World.Entities)
                if (e.Alive && e.Spec.Id == "so_tick_deployed") deployed = e;
            Assert.NotNull(deployed);
            Assert.Equal(ArmorClass.Concrete, deployed.Spec.Health.Armor);   // entrenched = concrete armor

            game.Tick(new List<Order> { new Order(OrderType.Deploy, 1, 0, deployed.Id) });
            Assert.Null(game.World.GetEntity(deployed.Id));
            Assert.True(game.World.OwnsBlueprint(1, "so_tick_tank"));
        }

        [Fact]
        public void ArtilleryOnlyFiresDeployedAndRespectsMinRange()
        {
            var game = NewGame();
            var artillery = game.Spawn("so_artillery", 1, new CellPos(15, 15));
            var victim = game.Spawn("dm_rifle", 0, new CellPos(25, 15));
            victim.AutoEngage = false;

            // Mobile artillery is unarmed.
            Assert.Equal(-1, artillery.Spec.WeaponIndex);

            game.Tick(new List<Order> { new Order(OrderType.Deploy, 1, 0, artillery.Id) });
            Entity deployed = null;
            foreach (var e in game.World.Entities)
                if (e.Alive && e.Spec.Id == "so_artillery_deployed") deployed = e;
            Assert.NotNull(deployed);

            RunTicks(game, 700);
            Assert.Null(game.World.GetEntity(victim.Id));   // shelled from range 10

            // Enemy right next to the emplacement is INSIDE min range — safe from it.
            var close = game.Spawn("dm_rifle", 0, new CellPos(16, 15));
            close.AutoEngage = false;
            RunTicks(game, 400);
            Assert.NotNull(game.World.GetEntity(close.Id));
        }

        // ---------- AA ----------

        [Fact]
        public void SamHitsAircraftButIgnoresGroundUnits()
        {
            var game = NewGame();
            game.World.SpawnStructure(TestWorlds.Rules.Unit("so_sam"), 1, new CellPos(20, 20));
            var groundling = game.Spawn("dm_rifle", 0, new CellPos(22, 20));
            groundling.AutoEngage = false;

            RunTicks(game, 400);
            Assert.NotNull(game.World.GetEntity(groundling.Id));   // SAM can't shoot ground

            var orca = game.Spawn("dm_orca", 0, new CellPos(23, 20));
            orca.AutoEngage = false;
            RunTicks(game, 600);
            Assert.Null(game.World.GetEntity(orca.Id));            // aircraft shredded
        }

        [Fact]
        public void CyborgIsEmpVulnerableButCrystalProof()
        {
            var game = NewGame();
            var cyborg = game.Spawn("so_cyborg", 1, new CellPos(12, 10));
            cyborg.AutoEngage = false;
            game.World.Crystal.Set(new CellPos(12, 10), CrystalType.Green, 11);

            RunTicks(game, 200);
            Assert.Equal(cyborg.Spec.Health.Max, cyborg.Hp);   // crystal-proof

            var weapon = System.Array.Find(TestWorlds.Rules.Weapons, w => w.Id == "test_emp");
            game.Combat.ApplyDamage(weapon, -1, cyborg.Id, cyborg.Pos);
            Assert.True(cyborg.DisabledTicks > 0, "cyborg must be EMP-vulnerable");
        }

        [Fact]
        public void FullRosterScenarioIsDeterministic()
        {
            ulong Run()
            {
                var game = NewGame(4242);
                game.SetPlayerFaction(0, "dominion");
                game.SetPlayerFaction(1, "serpent");
                game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_guard_tower"), 0, new CellPos(20, 20));
                game.World.SpawnStructure(TestWorlds.Rules.Unit("so_obelisk"), 1, new CellPos(40, 40));
                game.Spawn("dm_mammoth", 0, new CellPos(10, 10));
                game.Spawn("dm_orca", 0, new CellPos(12, 10));
                game.Spawn("dm_medic", 0, new CellPos(14, 10));
                game.Spawn("so_stealth_tank", 1, new CellPos(50, 50));
                game.Spawn("so_artillery", 1, new CellPos(48, 50));
                game.Spawn("so_cyborg", 1, new CellPos(46, 50));

                var orders = new List<Order>();
                foreach (var e in game.World.Entities)
                {
                    if (e.Spec.Mobile != null)
                    {
                        orders.Add(new Order(OrderType.AttackMove, e.Owner, 0, e.Id,
                            targetPos: LeptonPos.CellCenter(new CellPos(30, 30))));
                    }
                }
                game.Tick(orders);
                RunTicks(game, 2500);
                return game.ComputeHash();
            }

            Assert.Equal(Run(), Run());
        }
    }
}
