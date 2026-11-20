using Data;
using UnityEngine;

namespace World
{
    /// <summary>
    /// The village stockpile. During <see cref="Activity.Shipping"/> villagers bring the goods their job produced here,
    /// and crafters pick up ingredients from here when their own house has none.
    /// </summary>
    public class ShippingPoint : MonoBehaviour
    {
        [Tooltip("Where villagers stand to drop off and pick up goods. Place it on the navmesh.")]
        [SerializeField] private Transform dropOff;

        /// <summary>The shipping point in the current scene, or null if there is none.</summary>
        public static ShippingPoint Instance { get; private set; }

        public Inventory Storage { get; } = new();

        public Vector3 DropOffPosition => dropOff ? dropOff.position : transform.position;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(DropOffPosition, 0.5f);
        }
    }
}
