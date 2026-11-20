using Data;
using UnityEngine;

namespace World
{
    /// <summary>
    /// A building <see cref="Character"/>s live in. Sits next to an <see cref="Entity"/> with the
    /// <see cref="AuraType.Housing"/> aura and marks where characters walk to enter and leave.
    /// </summary>
    [RequireComponent(typeof(Entity))]
    public class House : MonoBehaviour
    {
        [Tooltip("Where characters walk to enter and leave. Place it on the navmesh just outside the door.")]
        [SerializeField] private Transform entrance;

        /// <summary>World position of the entrance, or the house itself if none is set.</summary>
        public Vector3 EntrancePosition => entrance ? entrance.position : transform.position;

        /// <summary>Goods the residents brought home, e.g. a woodcutter's wood or a baker's bread.</summary>
        public Inventory Storage { get; } = new();

        private void OnValidate()
        {
            if (GetComponent<Entity>().aura != AuraType.Housing)
            {
                Debug.LogWarning($"{name}: House entity should have the Housing aura.", this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(EntrancePosition, 0.5f);
        }
    }
}
