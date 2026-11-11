using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

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
    Portal
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
    private static readonly int SweepParam = Animator.StringToHash("sweep");
    private static readonly int DeathParam = Animator.StringToHash("death");
    private static readonly int ReviveParam = Animator.StringToHash("revive");

    private const float WanderRadius = 5f;
    private const float MaxWalkDuration = 10f;

    // ---------------------------------------------------------------- Inspector

    [Header("Identity")]
    public Race race;
    public AiType aiType;
    public AuraType aura;

    [Header("Health")]
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

    private NavMeshAgent _agent;
    private NavMeshObstacle _obstacle;
    private Animator _animator;
    private PlayerController _player;
    private GameObject _dropModelInstance;

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
        }
    }

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
        if (!IsAlive)
        {
            return false;
        }
        var offset = transform.position - _player.transform.position;
        return offset.sqrMagnitude < interactRange * interactRange;
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive)
        {
            return;
        }

        Health -= amount;
        if (Health <= 0)
        {
            Die();
        }
    }

    // ---------------------------------------------------------------- Life cycle

    private void Die()
    {
        IsAlive = false;
        StopAllCoroutines();
        StopMoving();

        if (dropItem != null)
        {
            _player.Give(dropItem, dropAmount);
        }

        if (_dropModelInstance != null)
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

        if (dropItem != null)
        {
            _dropModelInstance = Instantiate(dropItem.worldModel, transform);
        }

        if (aiType == AiType.Humanoid)
        {
            _animator.SetTrigger(ReviveParam);
        }

        StartBehaviour();
    }

    // ---------------------------------------------------------------- AI

    private void StartBehaviour()
    {
        switch (aiType)
        {
            case AiType.Critter:
                StartCoroutine(WanderLoop(initialDelay: 0));
                break;
            case AiType.Humanoid:
                StartCoroutine(WanderLoop(initialDelay: idleDelay));
                break;
            case AiType.Follow:
                StartCoroutine(FollowPlayer(initialDelay: idleDelay));
                break;
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
        while (_player != null)
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

    private IEnumerator WalkTo(Vector3 destination)
    {
        _agent.SetDestination(destination);
        SetWalkingAnimation(true);

        // Give up after a while so an unreachable destination can't stall the AI forever.
        var giveUpTime = Time.time + MaxWalkDuration;
        while (!HasArrived() && Time.time < giveUpTime)
        {
            yield return null;
        }

        StopMoving();
    }

    private bool HasArrived()
    {
        if (_agent.pathPending)
        {
            return false;
        }

        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            return true;
        }

        return _agent.remainingDistance <= _agent.stoppingDistance;
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
            _animator.SetBool(WalkingParam, isWalking);
        }
    }
}
