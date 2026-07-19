using TiberiumDusk.Sim.Math;

namespace TiberiumDusk.Sim.Orders
{
    public enum OrderType : byte
    {
        None = 0,
        Move,
        AttackMove,
        Attack,
        Stop,
        Deploy,
        BuildStart,
        BuildCancel,
        PlaceStructure,
        Sell,
        Repair,
        SetRallyPoint,
        UseSuperweapon,
    }

    /// <summary>
    /// The ONLY way anything (player input, AI, network, replay) mutates the
    /// simulation. Orders are scheduled for a specific tick and executed
    /// identically on every lockstep client. Plain struct: cheap to serialize
    /// for the network relay and the replay log.
    /// </summary>
    public readonly struct Order
    {
        public readonly OrderType Type;
        public readonly int PlayerId;
        /// <summary>Tick at which the order executes (issue tick + lockstep delay).</summary>
        public readonly int ExecuteTick;
        /// <summary>Subject entity (unit/structure), or -1 for player-scoped orders.</summary>
        public readonly int EntityId;
        /// <summary>Target entity for targeted orders, or -1.</summary>
        public readonly int TargetEntityId;
        public readonly LeptonPos TargetPos;
        /// <summary>Order-specific payload (blueprint index for build orders, etc.).</summary>
        public readonly int Data;

        public Order(OrderType type, int playerId, int executeTick,
            int entityId = -1, int targetEntityId = -1, LeptonPos targetPos = default, int data = 0)
        {
            Type = type;
            PlayerId = playerId;
            ExecuteTick = executeTick;
            EntityId = entityId;
            TargetEntityId = targetEntityId;
            TargetPos = targetPos;
            Data = data;
        }
    }
}
