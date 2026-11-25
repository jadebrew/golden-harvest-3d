using System.Collections;
using System.Collections.Generic;
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
        [Tooltip("Favorite AuraType for Leisure.")]
        [SerializeField] private AuraType favoriteAura;

        [Tooltip("How far from the worker to look for targets.")]
        [SerializeField] private float searchRadius = 50f;

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

        private readonly Dictionary<Entity, float> _ignoredUntil = new();

        private void Awake()
        {
            _entity = GetComponent<Entity>();
            _job = GetComponent<Job>();

            //start at home
            if(!home)
            {

            } else {
                _spawnPosition = home.EntrancePosition;
                transform.position = home.EntrancePosition;
            }


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
                    //return Ship(); -- not done yet
                    return Leisure();
                case Activity.Work:
                    return Leisure();
                case Activity.Shipping:
                    return Leisure();
                case Activity.Leisure:
                    return Leisure();
                case Activity.Sleep:
                    return StayHome();
                default:
                    // Sleep and meals.
                    return Meal();
            }
        }

        private IEnumerator StayHome()
        {
            yield return WalkHome();
            _entity.SetHidden(true);
        }

        public IEnumerator Congregate(AuraType aura, float zone)
        {
            var _congregationTarget = FindTargetAura(_entity,aura);
            var offset = new Vector3(Random.Range(-zone,zone), 0,Random.Range(-zone,zone));
            if (_congregationTarget)
            {
                yield return _entity.WalkTo(_congregationTarget.transform.position+offset,25);
                _entity.FaceTowards(_congregationTarget.transform.position);
            }
            else {
                yield break;
            }
        }

        private IEnumerator Meal()
        {
            if (_entity.aiType == AiType.Humanoid)
            {
                yield return Congregate(AuraType.Cooking,5);
                yield return _entity.Consume();
            } else {
                StayHome();
            }
        }


        private IEnumerator Leisure()
        {
            if (_entity.aiType == AiType.Humanoid)
            {
                yield return Congregate(AuraType.Fun,3);
                yield return _entity.Party();
            }
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

        private Entity FindTargetAura(Entity self, AuraType aura)
        {
            Entity nearest = null;
            var nearestDistance = searchRadius * searchRadius;
            var origin = self.transform.position;

            foreach (var candidate in Entity.All)
            {
                if (candidate == self || !IsTargetableAura(candidate,aura) ||
                    (_ignoredUntil.TryGetValue(candidate, out var until) && Time.time < until))
                {
                    continue;
                }

                var distance = (candidate.transform.position - origin).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private bool IsTargetableAura(Entity entity, AuraType aura) =>
        entity && entity.IsAlive && !entity.IsHidden &&
        entity.aura == aura;

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
