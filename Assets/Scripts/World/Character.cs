using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace World
{
    /// <summary>
    /// A living inhabitant with a daily routine. Sits next to an <see cref="Entity"/> (which handles
    /// health, drops and basic movement) and takes over its behaviour to commute between home and work:
    /// at bedtime it walks to its <see cref="home"/>'s entrance and hides inside; at wake time it comes back out,
    /// walks to its spawn spot and resumes the entity's default behaviour.
    /// </summary>
    [RequireComponent(typeof(Entity))]
    public class Character : MonoBehaviour
    {
        // How far from the entrance marker to look for a walkable spot, in case it's slightly off the navmesh.
        private const float EntranceSearchRadius = 2f;

        [Header("Schedule")]
        [Tooltip("House this character sleeps in. Leave empty to stay out all night.")]
        [SerializeField] private House home;
        [Tooltip("Hour of day the character heads home.")]
        [Range(0f, 24f)]
        [SerializeField] private float bedtimeHour = 0f;
        [Tooltip("Hour of day the character leaves home and returns to its spawn spot.")]
        [Range(0f, 24f)]
        [SerializeField] private float wakeHour = 8f;
        [Tooltip("Seconds to keep walking home or back to spawn before giving up.")]
        [SerializeField] private float commuteTimeout = 60f;

        private Entity _entity;
        private Vector3 _spawnPosition;
        private bool _sentHome;

        private void Awake()
        {
            _entity = GetComponent<Entity>();
            _spawnPosition = transform.position;

            // Subscribed in Awake so we catch the entity's first spawn in its Start.
            _entity.Respawned += OnRespawned;
        }

        private void OnDestroy()
        {
            if (_entity)
            {
                _entity.Respawned -= OnRespawned;
            }
        }

        private void Update()
        {
            if (!home || !_entity.IsAlive || _entity.aiType == AiType.None)
            {
                return;
            }

            var cycle = DayNightCycle.Instance;
            if (!cycle)
            {
                return;
            }

            var isBedtime = DayNightCycle.IsWithinHours(cycle.CurrentHour, bedtimeHour, wakeHour);
            if (isBedtime == _sentHome)
            {
                return;
            }

            _sentHome = isBedtime;
            _entity.SetBehaviour(isBedtime ? GoHome() : LeaveHome());
        }

        // A (re)spawned entity starts on its default behaviour; let Update send it home if it's bedtime.
        private void OnRespawned() => _sentHome = false;

        private IEnumerator GoHome()
        {
            var target = home.EntrancePosition;
            var destination = NavMesh.SamplePosition(target, out var hit, EntranceSearchRadius, NavMesh.AllAreas)
                ? hit.position
                : target;

            yield return _entity.WalkTo(destination, commuteTimeout);
            _entity.SetHidden(true);
        }

        private IEnumerator LeaveHome()
        {
            _entity.SetHidden(false);
            yield return _entity.WalkTo(_spawnPosition, commuteTimeout);
            yield return _entity.DefaultBehaviour();
        }
    }
}
