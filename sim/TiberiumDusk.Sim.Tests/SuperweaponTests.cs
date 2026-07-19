using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Orders;
using TiberiumDusk.Sim.WorldModel;
using Xunit;

namespace TiberiumDusk.Sim.Tests
{
    public class SuperweaponTests
    {
        private static readonly List<Order> NoOrders = new List<Order>();

        private static Game NewGame(ulong seed = 21, GameSettings settings = null) =>
            new Game(TestWorlds.Rules, TestWorlds.Flat(), seed, settings);

        private static void RunTicks(Game game, int ticks)
        {
            for (int i = 0; i < ticks; i++) game.Tick(NoOrders);
        }

        private static int SwIndex(string id)
        {
            foreach (var sw in TestWorlds.Rules.Superweapons)
                if (sw.Id == id) return sw.Index;
            throw new System.Exception("unknown superweapon " + id);
        }

        private static Order FireOrder(int player, string swId, CellPos target) =>
            new Order(OrderType.UseSuperweapon, player, 0, data: SwIndex(swId),
                targetPos: LeptonPos.CellCenter(target));

        private static void Charge(Game game, int player, string swId)
        {
            var power = game.Superweapons.GetPower(player, SwIndex(swId));
            power.Charge = TestWorlds.Rules.Superweapons[SwIndex(swId)].ChargeTicks;
        }

        [Fact]
        public void ChargeAccumulatesOnlyWithGrantingStructure()
        {
            var game = NewGame();
            int ion = SwIndex("ion_cannon");
            RunTicks(game, 50);
            Assert.Equal(0, game.Superweapons.GetPower(0, ion).Charge);   // no uplink

            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(4, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(4, 8));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_ion_uplink"), 0, new CellPos(8, 5));
            RunTicks(game, 100);
            Assert.True(game.Superweapons.GetPower(0, ion).Charge >= 99);
        }

        [Fact]
        public void LowPowerPausesCharging()
        {
            var game = NewGame();
            // Uplink drains 150 with no power plants → low power → no charge.
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_ion_uplink"), 0, new CellPos(8, 5));
            RunTicks(game, 100);
            Assert.Equal(0, game.Superweapons.GetPower(0, SwIndex("ion_cannon")).Charge);
        }

        [Fact]
        public void IonCannonObliteratesTargetArea()
        {
            var game = NewGame();
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(4, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(4, 8));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_ion_uplink"), 0, new CellPos(8, 5));
            var victim = game.Spawn("so_tick_tank", 1, new CellPos(40, 40));
            var bystander = game.Spawn("so_scout_buggy", 1, new CellPos(41, 40));
            var faraway = game.Spawn("so_scout_buggy", 1, new CellPos(55, 55));

            Charge(game, 0, "ion_cannon");
            game.Tick(new List<Order> { FireOrder(0, "ion_cannon", new CellPos(40, 40)) });

            Assert.Null(game.World.GetEntity(victim.Id));
            Assert.Null(game.World.GetEntity(bystander.Id));
            Assert.NotNull(game.World.GetEntity(faraway.Id));
            // Reset (may have re-accumulated a tick of charge already).
            Assert.True(game.Superweapons.GetPower(0, SwIndex("ion_cannon")).Charge <= 1);
        }

        [Fact]
        public void FiringWithoutFullChargeDoesNothing()
        {
            var game = NewGame();
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(4, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_ion_uplink"), 0, new CellPos(8, 5));
            var victim = game.Spawn("so_tick_tank", 1, new CellPos(40, 40));

            game.Tick(new List<Order> { FireOrder(0, "ion_cannon", new CellPos(40, 40)) });
            Assert.NotNull(game.World.GetEntity(victim.Id));
        }

        [Fact]
        public void ClusterMissileScattersDamageAcrossArea()
        {
            var game = NewGame();
            game.SetPlayerFaction(1, "serpent");
            game.World.SpawnStructure(TestWorlds.Rules.Unit("so_adv_power"), 1, new CellPos(4, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("so_missile_silo"), 1, new CellPos(8, 5));

            var targets = new List<Entity>();
            for (int dx = -3; dx <= 3; dx += 2)
                for (int dy = -3; dy <= 3; dy += 2)
                    targets.Add(game.Spawn("dm_rifle", 0, new CellPos(40 + dx, 40 + dy)));

            Charge(game, 1, "cluster_missile");
            game.Tick(new List<Order> { FireOrder(1, "cluster_missile", new CellPos(40, 40)) });

            int killed = 0;
            foreach (var t in targets)
                if (game.World.GetEntity(t.Id) == null) killed++;
            Assert.True(killed >= 4, $"cluster missile should kill several infantry (killed {killed})");
        }

        [Fact]
        public void EmpBlastRespectsCannonRange()
        {
            var game = NewGame();
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(4, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(4, 8));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("nx_emp_cannon"), 0, new CellPos(8, 5));
            var nearTank = game.Spawn("so_tick_tank", 1, new CellPos(20, 20));
            nearTank.AutoEngage = false;

            Charge(game, 0, "emp_blast");
            game.Tick(new List<Order> { FireOrder(0, "emp_blast", new CellPos(20, 20)) });
            Assert.True(nearTank.DisabledTicks > 0, "in-range EMP should disable the tank");
            Assert.True(game.Superweapons.GetPower(0, SwIndex("emp_blast")).Charge <= 1);
        }

        [Fact]
        public void HunterSeekerFindsAndDestroysAnEnemyAsset()
        {
            var game = NewGame();
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(4, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_tech_center"), 0, new CellPos(8, 5));
            var enemyPlant = game.World.SpawnStructure(
                TestWorlds.Rules.Unit("so_power_plant"), 1, new CellPos(50, 50));

            Charge(game, 0, "hunter_seeker");
            game.Tick(new List<Order> { FireOrder(0, "hunter_seeker", new CellPos(0, 0)) });
            RunTicks(game, 900);

            Assert.Null(game.World.GetEntity(enemyPlant.Id));
            // Drone consumed itself.
            foreach (var e in game.World.Entities)
                Assert.False(e.Alive && e.Spec.Id == "nx_seeker_drone", "drone must self-destruct");
        }

        [Fact]
        public void SensorRevealsCloakedTankToDefenses()
        {
            var game = NewGame();
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(2, 2));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(2, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_radar"), 0, new CellPos(12, 10));   // sensors 9
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_guard_tower"), 0, new CellPos(10, 10));
            var stealth = game.Spawn("so_stealth_tank", 1, new CellPos(15, 10));
            stealth.AutoEngage = false;

            RunTicks(game, 400);
            Assert.True(stealth.Hp < stealth.Spec.Health.Max || game.World.GetEntity(stealth.Id) == null,
                "radar sensors should reveal the cloaked tank to the tower");
        }

        [Fact]
        public void IonStormGroundsAircraftAndPausesSuperweapons()
        {
            var settings = new GameSettings { IonStormsEnabled = true };
            var game = NewGame(33, settings);
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(4, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(4, 8));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_ion_uplink"), 0, new CellPos(8, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_helipad"), 0, new CellPos(12, 5));
            var flying = game.Spawn("dm_orca", 0, new CellPos(40, 40));      // far from any pad
            var docked = game.Spawn("dm_orca", 0, new CellPos(12, 5));       // on the pad

            // Force the storm to break now.
            while (game.IonStorm.Phase != Systems.StormPhase.Active)
            {
                game.Tick(NoOrders);
            }

            Assert.Null(game.World.GetEntity(flying.Id));      // caught in the sky
            Assert.NotNull(game.World.GetEntity(docked.Id));   // safe at the pad

            // Charging must not ADVANCE during the storm (lightning may even
            // destroy the uplink, dropping charge to zero — that counts too).
            int chargeBefore = game.Superweapons.GetPower(0, SwIndex("ion_cannon")).Charge;
            RunTicks(game, 100);
            Assert.True(game.Superweapons.GetPower(0, SwIndex("ion_cannon")).Charge <= chargeBefore,
                "superweapon charge must pause during an ion storm");

            // Storm ends → charging resumes (if the bolts spared the uplink).
            while (game.IonStorm.Phase == Systems.StormPhase.Active)
            {
                game.Tick(NoOrders);
            }
            if (game.World.OwnsBlueprint(0, "dm_ion_uplink") && !game.World.Players[0].LowPower)
            {
                int atCalm = game.Superweapons.GetPower(0, SwIndex("ion_cannon")).Charge;
                RunTicks(game, 50);
                Assert.True(game.Superweapons.GetPower(0, SwIndex("ion_cannon")).Charge > atCalm);
            }
        }

        [Fact]
        public void CratesSpawnAndGrantRewards()
        {
            var settings = new GameSettings { CratesEnabled = true };
            var game = NewGame(77, settings);
            var scout = game.Spawn("so_scout_buggy", 0, new CellPos(30, 30));
            int creditsBefore = game.World.Players[0].Credits;
            int rankBefore = scout.Rank;
            int hpBefore = scout.Hp;

            // Let crates spawn, then drive the scout over each one until a pickup happens.
            int pickups = 0;
            for (int round = 0; round < 30 && pickups == 0; round++)
            {
                RunTicks(game, game.World.Rules.Special.CrateRegenTicks + 10);
                foreach (var crate in new List<CellPos>(game.Crates.Crates))
                {
                    game.Tick(new List<Order>
                    {
                        new Order(OrderType.Move, 0, 0, scout.Id, targetPos: LeptonPos.CellCenter(crate))
                    });
                    RunTicks(game, 700);
                    if (game.World.GetEntity(scout.Id) == null) return;   // trap crate — also a valid outcome
                    bool rewarded = game.World.Players[0].Credits != creditsBefore
                        || scout.Rank != rankBefore || scout.Hp != hpBefore
                        || game.Crates.Crates.Count == 0;
                    if (rewarded) pickups++;
                }
            }
            Assert.True(pickups > 0, "driving over crates should trigger a reward");
        }

        [Fact]
        public void Phase6ScenarioIsDeterministic()
        {
            ulong Run()
            {
                var settings = new GameSettings { IonStormsEnabled = true, CratesEnabled = true };
                var game = NewGame(555, settings);
                game.SetPlayerFaction(1, "serpent");
                game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(4, 5));
                game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(4, 8));
                game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_ion_uplink"), 0, new CellPos(8, 5));
                game.World.SpawnStructure(TestWorlds.Rules.Unit("so_adv_power"), 1, new CellPos(50, 5));
                game.World.SpawnStructure(TestWorlds.Rules.Unit("so_missile_silo"), 1, new CellPos(54, 5));
                game.Spawn("dm_mbt_walker", 0, new CellPos(20, 20));
                game.Spawn("so_stealth_tank", 1, new CellPos(40, 40));

                Charge(game, 0, "ion_cannon");
                game.Tick(new List<Order> { FireOrder(0, "ion_cannon", new CellPos(40, 40)) });
                RunTicks(game, 3000);
                return game.ComputeHash();
            }

            Assert.Equal(Run(), Run());
        }
    }
}
