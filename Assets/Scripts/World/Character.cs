using System.Collections;
using Data;
using UnityEngine;
using UnityEngine.AI;

namespace World
{
    /// <summary>
    /// A living inhabitant with a weekly routine. Sits next to an <see cref="Entity"/> (which handles
    /// health, drops and basic movement) and takes over its behaviour to follow its <see cref="Schedule"/>:
    /// sleeping and eating happen inside its <see cref="home"/>, work runs the <see cref="Job"/> on the same object,
    /// shipping brings the job's goods to the <see cref="ShippingPoint"/>, and leisure resumes the entity's default behaviour.
    /// </summary>
    [RequireComponent(typeof(Entity))]
    public class Character : MonoBehaviour
    {
        // How far from a marker to look for a walkable spot, in case it's slightly off the navmesh.
        private const float MarkerSearchRadius = 2f;

        [Header("Schedule")]
        [Tooltip("House this character sleeps, eats and stores its goods in. Without one the character just wanders.")]
        [SerializeField] private House home;
        [Tooltip("Weekly timetable. Leave empty to use the village default.")]
        [SerializeField] private Schedule schedule;

        private Entity _entity;
        private Job _job;
        private Vector3 _spawnPosition;
        private Activity? _currentActivity;

        public Entity Entity => _entity;
        public House Home => home;

        /// <summary>What the character is holding, e.g. wood on the way home. Dropped off whenever it gets home.</summary>
        public Inventory Carried { get; } = new();

        /// <summary>The activity currently being carried out, or null before the first update.</summary>
        public Activity? CurrentActivity => _currentActivity;

        private void Awake()
        {
            _entity = GetComponent<Entity>();
            _job = GetComponent<Job>();
            _spawnPosition = transform.position;

            if (!schedule)
            {
                schedule = Schedule.CreateDefault();
            }

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

            var activity = Resolve(schedule.GetActivity(cycle.CurrentDay, cycle.CurrentHour));

            // TODO: Supernatural hour (03:00-04:00) has no behaviour yet. For now the character simply carries on
            // with whatever it was doing (normally sleeping).
            if (activity == Activity.Supernatural || activity == _currentActivity)
            {
                return;
            }

            _currentActivity = activity;
            _entity.SetBehaviour(Run(activity));
        }

        // A (re)spawned entity starts on its default behaviour; let Update pick the scheduled activity again.
        private void OnRespawned() => _currentActivity = null;

        /// <summary>Maps activities that don't have their own behaviour yet onto ones that do.</summary>
        private static Activity Resolve(Activity activity) => activity switch
        {
            Activity.Party => Activity.Leisure,
            _ => activity,
        };

        private IEnumerator Run(Activity activity)
        {
            switch (activity)
            {
                case Activity.Work when _job:
                    return Work();
                case Activity.Shipping when _job && ShippingPoint.Instance:
                    return Ship();
                case Activity.Work:
                case Activity.Shipping:
                case Activity.Leisure:
                    return Leisure();
                default:
                    // Sleep and meals.
                    return StayHome();
            }
        }

        private IEnumerator StayHome()
        {
            yield return WalkHome();
            _entity.SetHidden(true);
        }

        private IEnumerator Leisure()
        {
            _entity.SetHidden(false);
            yield return _entity.WalkTo(_spawnPosition, float.PositiveInfinity);
            yield return _entity.DefaultBehaviour();
        }

        private IEnumerator Work()
        {
            _entity.SetHidden(false);
            yield return _job.Work(this);
        }

        private IEnumerator Ship()
        {
            // Load up while still at the door, then haul everything the job produced to the stockpile.
            _entity.SetHidden(false);
            home.Storage.MoveAllTo(Carried, _job.IsProduct);

            if (!Carried.IsEmpty)
            {
                var shippingPoint = ShippingPoint.Instance;
                yield return WalkTo(shippingPoint.DropOffPosition, shippingPoint);
                Carried.MoveAllTo(shippingPoint.Storage);
            }

            yield return _entity.DefaultBehaviour();
        }

        /// <summary>Walks to the house entrance and drops off everything being carried, staying outside.</summary>
        public IEnumerator WalkHome()
        {
            yield return WalkTo(home.EntrancePosition, home);
            Carried.MoveAllTo(home.Storage);
        }

        /// <summary>Walks to a marker that should be on the navmesh, without a timeout.</summary>
        public IEnumerator WalkTo(Vector3 destination, Object context)
        {
            if (NavMesh.SamplePosition(destination, out var hit, MarkerSearchRadius, NavMesh.AllAreas))
            {
                destination = hit.position;
            }
            else
            {
                Debug.LogWarning(
                    $"{name}: {context.name}'s marker is more than {MarkerSearchRadius} units from the navmesh; " +
                    "move it onto walkable ground.", context);
            }

            // No timeout: only continue once actually there.
            yield return _entity.WalkTo(destination, float.PositiveInfinity);
        }
    }
}
