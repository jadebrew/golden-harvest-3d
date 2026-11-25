using System.Collections;
using System.Collections.Generic;
using Data;
using Player;
using UnityEngine;
using UnityEngine.AI;

namespace World
{
    public enum AiType
    {
        None,
        Critter,
        Follow,
        Humanoid,
    }

    public enum Race
    {
        NonHuman,
        Human,
        Goblin,
        Elf,
    }

    public enum AuraType
    {
        None,
        Cooking,
        Crafting,
        Fun,
        Portal,
        Housing,
    }

    /// <summary>
    /// Something in the world the player can target and damage: a resource node, a critter or an NPC.
    /// When it dies it gives the player its drop item, then respawns after a delay.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NavMeshObstacle))]
    public class Entity : MonoBehaviour
    {
        private static readonly int WalkingParam = Animator.StringToHash("walking");
        private static readonly int MutantParam = Animator.StringToHash("mutant");
        private static readonly int GroundedParam = Animator.StringToHash("IsGrounded");
        private static readonly int SweepParam = Animator.StringToHash("sweep");
        private static readonly int DeathParam = Animator.StringToHash("death");
        private static readonly int ReviveParam = Animator.StringToHash("revive");
        private static readonly int PullParam = Animator.StringToHash("pull");
        private static readonly int ConsumeParam = Animator.StringToHash("consume");
        private static readonly int DancingParam = Animator.StringToHash("dance");

        private const float WanderRadius = 5f;
        private const float MaxWalkDuration = 10f;
        // Slack on top of the agent's stopping distance. Agents often settle a little short of their
        // destination (avoidance, obstacles), and with a stopping distance of 0 they'd never "arrive".
        private const float ArrivalTolerance = 1;
        // Seconds between path updates while approaching a (possibly moving) target.
        private const float ApproachRepathInterval = 0.25f;

        private static readonly List<Entity> ActiveEntities = new();

        /// <summary>Every enabled entity in the loaded scenes, e.g. for workers looking for resource nodes.</summary>
        public static IReadOnlyList<Entity> All => ActiveEntities;

        // ---------------------------------------------------------------- Inspector

        [Header("Identity")]
        public Race race;
        public AiType aiType;
        
        /// <summary>
        /// The type of perk you get when near Entity.
        /// f.e. Campfire has Cooking Aura => gives Player the ability to cook when near Campfire
        /// </summary>
        public AuraType aura;

        [Header("Health")]
        [Tooltip("Ignores all damage, e.g. buildings. The entity can still be targeted for its aura.")]
        [SerializeField] private bool indestructible;
        [SerializeField] private float maxHealth = 100;
        public DamageType vulnerability;
        [Tooltip("Seconds after death before the entity comes back.")]
        [SerializeField] private float respawnDelay = 30;

        [Header("Drops")]
        [Tooltip("Given to the player on death. Its model is shown on the entity while it is alive.")]
        [SerializeField] private Item dropItem;
        [SerializeField] private int dropAmount = 5;

        [Header("Interaction")]
        [Tooltip("How close the player must be to target this entity.")]
        [SerializeField] private float interactRange = 2;

        [Header("Movement")]
        [Tooltip("Seconds before Humanoid/Follow AI starts moving, and the average pause between wander walks.")]
        [SerializeField] private float idleDelay = 5;
        [Tooltip("Seconds between path updates while following the player.")]
        [SerializeField] private float followRepathInterval = 0.1f;

        // ---------------------------------------------------------------- Runtime state

        public float Health { get; private set; }
        public bool IsAlive { get; private set; }

        /// <summary>True while the entity is removed from the world, e.g. a villager inside its house.</summary>
        public bool IsHidden { get; private set; }

        /// <summary>The item this entity drops when it dies, or null.</summary>
        public Item DropItem => dropItem;

        /// <summary>Raised every time the entity (re)spawns, after its default behaviour has started.</summary>
        public event System.Action Respawned;

        private Coroutine _behaviour;
        private readonly List<Renderer> _hiddenRenderers = new();

        private int currentIdle;

        private NavMeshAgent _agent;
        private NavMeshObstacle _obstacle;
        private Animator _animator;
        private PlayerController _player;
        private GameObject _dropModelInstance;

        public bool Invisible;

        // ---------------------------------------------------------------- Unity lifecycle

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _obstacle = GetComponent<NavMeshObstacle>();

            // Entities without AI block the navmesh instead of moving on it.
            var isStatic = aiType == AiType.None;
            _agent.enabled = !isStatic;
            _obstacle.enabled = isStatic;

            if (aiType == AiType.Humanoid)
            {
                _animator = GetComponent<Animator>();
                _animator.SetBool(MutantParam, race == Race.Goblin);
                // The controller is shared with the player, whose idle drops into "fall" when not grounded.
                // NPCs walk on the navmesh and never leave the ground, so pin this to true.
                _animator.SetBool(GroundedParam, true);
            }

            if (Invisible)
            {
                SetHidden(true);
            }
        }

        private void OnEnable() => ActiveEntities.Add(this);

        private void OnDisable() => ActiveEntities.Remove(this);

        private void Start()
        {
            // Done in Start rather than Awake: Start runs after every Awake,
            // so PlayerController has registered itself by now.
            _player = PlayerController.Instance;
            if (_player == null)
            {
                Debug.LogError($"{name}: no PlayerController found in the scene, disabling Entity.", this);
                enabled = false;
                return;
            }

            Respawn();
        }

        private void FixedUpdate()
        {
            if (IsInPlayerRange())
            {
                _player.SetTarget(this);
            }
        }

        // ---------------------------------------------------------------- Public API

        public bool IsInPlayerRange()
        {
            if (!IsAlive || IsHidden)
            {
                return false;
            }
            var offset = transform.position - _player.transform.position;
            return offset.sqrMagnitude < interactRange * interactRange;
        }

        /// <summary>
        /// Damages the entity. If this kills it, its drop goes into <paramref name="looter"/>,
        /// or to the player when no looter is given.
        /// </summary>
        public void TakeDamage(float amount, Inventory looter = null)
        {
            if (indestructible || !IsAlive || IsHidden)
            {
                return;
            }

            Health -= amount;
            if (Health <= 0)
            {
                Die(looter);
            }
        }

        // ---------------------------------------------------------------- Life cycle

        private void Die(Inventory looter)
        {
            IsAlive = false;
            StopAllCoroutines();
            _behaviour = null;
            StopMoving();

            if (dropItem)
            {
                if (looter != null)
                {
                    looter.Add(dropItem, dropAmount);
                }
                else
                {
                    _player.Give(dropItem, dropAmount);
                }
            }

            if (_dropModelInstance)
            {
                Destroy(_dropModelInstance);
            }

            if (aiType == AiType.Humanoid)
            {
                _animator.SetTrigger(DeathParam);
            }

            StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            Respawn();
        }

        private void Respawn()
        {
            Health = maxHealth;
            IsAlive = true;

            if (dropItem)
            {
                _dropModelInstance = Instantiate(dropItem.worldModel, transform);
                 float rotationRange = 360f;
                 Vector3 positionRange = new Vector3(1, 0, 1);

                Vector3 offset = new Vector3(
                    Random.Range(-positionRange.x, positionRange.x),
                                             Random.Range(-positionRange.y, positionRange.y),
                                             Random.Range(-positionRange.z, positionRange.z));
                _dropModelInstance.transform.position += offset;

                float randomY = Random.Range(-rotationRange, rotationRange);
                if (aiType == AiType.None)
                {
                    _dropModelInstance.transform.Rotate(0f, randomY, 0f, Space.Self);
                }
            }

            if (aiType == AiType.Humanoid)
            {
                _animator.SetTrigger(ReviveParam);
            }

            StartBehaviour();
            Respawned?.Invoke();
        }

        // ---------------------------------------------------------------- AI

        private void StartBehaviour()
        {
            var routine = DefaultBehaviour();
            if (routine != null)
            {
                SetBehaviour(routine);
            }
        }

        /// <summary>The routine this entity runs when nothing else is driving it, or null for static entities.</summary>
        public IEnumerator DefaultBehaviour()
        {
            switch (aiType)
            {
                case AiType.Critter:
                    return WanderLoop(initialDelay: 0);
                case AiType.Humanoid:
                    return Leisure();
                case AiType.Follow:
                    return FollowPlayer(initialDelay: idleDelay);
                default:
                    return null;
            }
        }

        public IEnumerator Leisure()
        {
            switch (aiType)
            {
                case AiType.Humanoid:
                    return WanderLoop(initialDelay: idleDelay);
                case AiType.Critter:
                    return WanderLoop(initialDelay: idleDelay);
                default:
                   return null;
            }
        }

        public IEnumerator Party()
        {
            switch (aiType)
            {
                case AiType.Humanoid:
                    if (!_animator.GetBool(DancingParam))
                    {
                        _animator.SetBool(DancingParam, true);
                    }
                    return null;
                case AiType.Critter:
                    return WanderLoop(initialDelay: idleDelay);
                default:
                    return null;
            }
        }

        public IEnumerator Consume()
        {

            switch (aiType)
            {
                case AiType.Humanoid:
                    if (!_animator.GetBool(ConsumeParam))
                    {
                        _animator.SetBool(ConsumeParam, true);
                    }
                    return null;
                case AiType.Critter:
                    return null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Replaces whatever the entity is currently doing with <paramref name="routine"/>.
        /// The routine runs on the entity, so it stops automatically when the entity dies.
        /// </summary>
        public void SetBehaviour(IEnumerator routine)
        {
            if (_behaviour != null)
            {
                StopCoroutine(_behaviour);
            }

            StopMoving();
            _behaviour = StartCoroutine(routine);
        }

        /// <summary>
        /// Removes the entity from the world (invisible, untargetable, off the navmesh) or brings it back.
        /// </summary>
        public void SetHidden(bool hidden)
        {
            if (hidden == IsHidden)
            {
                return;
            }

            IsHidden = hidden;

            if (hidden)
            {
                // Only remember renderers that were visible, so we don't re-enable ones that started hidden.
                _hiddenRenderers.Clear();
                foreach (var r in GetComponentsInChildren<Renderer>())
                {
                    if (r.enabled)
                    {
                        r.enabled = false;
                        _hiddenRenderers.Add(r);
                    }
                }
            }
            else
            {
                foreach (var r in _hiddenRenderers)
                {
                    if (r)
                    {
                        r.enabled = true;
                    }
                }
                _hiddenRenderers.Clear();
            }

            // Stop other agents from steering around an invisible entity.
            if (aiType != AiType.None)
            {
                _agent.enabled = !hidden;
            }
        }

        private IEnumerator WanderLoop(float initialDelay)
        {
            if (initialDelay > 0)
            {
                yield return new WaitForSeconds(initialDelay);
            }

            while (true)
            {
                if (TryGetWanderPoint(out var destination))
                {
                    yield return WalkTo(destination);
                }

                yield return new WaitForSeconds(Random.Range(idleDelay * 0.5f, idleDelay * 1.5f));
            }
        }

        private IEnumerator FollowPlayer(float initialDelay)
        {
            yield return new WaitForSeconds(initialDelay);

            var repathWait = new WaitForSeconds(followRepathInterval);
            while (_player)
            {
                _agent.SetDestination(_player.transform.position);
                yield return repathWait;
            }

            StopMoving();
        }

        private bool TryGetWanderPoint(out Vector3 point)
        {
            var candidate = transform.position + Random.insideUnitSphere * WanderRadius;
            var found = NavMesh.SamplePosition(candidate, out var hit, WanderRadius, NavMesh.AllAreas);
            point = hit.position;
            return found;
        }

        /// <summary>Walks to <paramref name="destination"/>, giving up after <paramref name="timeout"/> seconds.</summary>
        public IEnumerator WalkTo(Vector3 destination, float timeout = MaxWalkDuration)
        {
            if (_agent.enabled)
            {
                _agent.SetDestination(destination);
                SetWalkingAnimation(true);

                // Give up after a while so an unreachable destination can't stall the AI forever.
                var giveUpTime = Time.time + timeout;
                while (!HasArrived() && Time.time < giveUpTime)
                {
                    yield return null;
                }
            }
            StopMoving();
        }

        /// <summary>
        /// Walks towards <paramref name="target"/> until within <paramref name="reach"/> of it, following it if it moves.
        /// Gives up after <paramref name="timeout"/> seconds or when the target is destroyed.
        /// </summary>
        public IEnumerator Approach(Transform target, float reach, float timeout = MaxWalkDuration)
        {
            SetWalkingAnimation(true);

            var giveUpTime = Time.time + timeout;
            var nextRepathTime = 0f;
            while (target && !IsWithin(target.position, reach) && Time.time < giveUpTime)
            {
                if (Time.time >= nextRepathTime)
                {
                    _agent.SetDestination(target.position);
                    nextRepathTime = Time.time + ApproachRepathInterval;
                }
                yield return null;
            }

            StopMoving();
        }

        /// <summary>True if <paramref name="point"/> is within <paramref name="distance"/>, ignoring height.</summary>
        public bool IsWithin(Vector3 point, float distance)
        {
            var offset = point - transform.position;
            offset.y = 0;
            return offset.sqrMagnitude <= distance * distance;
        }

        /// <summary>Turns to look at <paramref name="point"/> on the horizontal plane.</summary>
        public void FaceTowards(Vector3 point)
        {
            var direction = point - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        /// <summary>Plays the animation for using an item (swing, pull, consume). Does nothing on non-humanoids.</summary>
        public void PlayItemAnimation(AnimationType type)
        {
            if (_animator == null)
            {
                return;
            }

            _animator.SetTrigger(type switch
            {
                AnimationType.Sweep => SweepParam,
                AnimationType.Consume => ConsumeParam,
                _ => PullParam,
            });
        }

        private bool HasArrived()
        {
            if (_agent.pathPending)
            {
                return false;
            }

            // Nothing to follow (no path could be built): treat as arrived rather than walking forever.
            if (!_agent.hasPath || _agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                return true;
            }

            return _agent.remainingDistance <= _agent.stoppingDistance + ArrivalTolerance;
        }

        private void StopMoving()
        {
            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }

            SetWalkingAnimation(false);
        }

        private void SetWalkingAnimation(bool isWalking)
        {
            if (_animator != null)
            {
                _animator.SetBool(DancingParam, false);
                _animator.SetBool(ConsumeParam, false);
                _animator.SetBool(WalkingParam, isWalking);
            }
        }
    }
}
