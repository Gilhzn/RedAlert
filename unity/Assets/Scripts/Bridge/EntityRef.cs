using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>Links a picked collider back to its sim entity.</summary>
    public sealed class EntityRef : MonoBehaviour
    {
        public int EntityId;
        public int Owner;
    }
}
