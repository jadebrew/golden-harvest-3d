using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

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
    Crafting
}

//[RequireComponent(typeof(NavMeshAgent))]
public class Node : MonoBehaviour
{
    public Item _spawnable;
    public GameObject _target;
    private float magnetRange = 3;
    public float health = 100;
    public DamageType vulnerability;
    public int _dropAmount = 5;
    private float age = 0;
    private float deathAge;
    private float respawnRate = 30;
    public GameObject model;
    private GameObject modelInstance;

    public AiType intelligence;

    public float UpdateSpeed = 0.1f;

    public float idleDelay = 5;
    private float waitTimer = 0;

    private bool isWalking = false;
    private bool alive = true;

    public AuraType _aura;

    private NavMeshAgent _agent;

    private Animator _animator;

    public Race _race;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _target = GameObject.Find("Player");
        alive = false;
        age = 0;
        if (intelligence == AiType.Humanoid)
        {
            _animator = GetComponent<Animator>();
            if (_race == Race.Goblin)
            {
                _animator.SetBool("mutant",true);
            }
        }
        Respawn();
    }

    private IEnumerator FollowTarget(GameObject target)
    {
        WaitForSeconds wait = new WaitForSeconds(UpdateSpeed);

        while (enabled && target != null)
        {
            _agent.SetDestination(target.transform.position);

            yield return wait;
        }

        StopWalking();
    }

    public bool InRange()
    {

        float distance = Vector3.Distance(transform.position, _target.transform.position);
        if (alive)
        {
            return distance < magnetRange;
        }
        return false;
    }

    void FixedUpdate()
    {
        if (!isWalking)
        {
            waitTimer+= Time.fixedDeltaTime;
        }

        if (alive)
        {
            float distance = Vector3.Distance(transform.position, _target.transform.position);
            if (distance < 5)
            {
                _target.GetComponent<PlayerMovement>().SetTarget(transform.gameObject);
            }

            if ((intelligence == AiType.Follow || intelligence == AiType.Humanoid) && !isWalking && waitTimer > idleDelay)
            {
                isWalking = true;
                if (intelligence == AiType.Humanoid)
                {
                    StartCoroutine(Wander());
                    _animator.SetBool("walking",true);
                } else {
                    StartCoroutine(FollowTarget(_target));
                }

            }
            else if ((intelligence == AiType.Critter) && !isWalking)
            {
                isWalking = true;
                StartCoroutine(Wander());
            }
            if (health<0)
            {
                if (_spawnable != null)
                {
                    _target.GetComponent<PlayerMovement>().Give(_spawnable,_dropAmount);
                    alive = false;
                    Destroy(modelInstance);
                    age = 0;
                }
            }
        } else {
            if (!alive && age > respawnRate)
            {
                Respawn();
            }
        }

    }

    void Update()
    {
        age+= Time.deltaTime;
    }

    void Respawn()
    {
        alive = true;
        health = 100;
        age = 0;
        if (_spawnable != null)
        {
        modelInstance = Instantiate(_spawnable.model,this.transform);
        }
    }

    private IEnumerator Wander()
    {
        float minWaitTime = 1f;
        float maxWaitTime = 10;
        float wanderRadius = 5;

        while (true)
        {
            Vector3 randomPosition = Random.insideUnitSphere * wanderRadius;
            randomPosition += transform.position;

            if (NavMesh.SamplePosition(
                randomPosition,
                out NavMeshHit hit,
                wanderRadius,
                NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);

                // Wait until we reach the destination
                while (_agent.pathPending)
                    yield return null;

                while (_agent.remainingDistance > _agent.stoppingDistance)
                    yield return null;
            }
            StopWalking();
            // Wait before choosing another destination
            yield return new WaitForSeconds(
                Random.Range(minWaitTime, maxWaitTime)
            );
        }
    }

    void StopWalking()
    {
        isWalking = false;
        if (!isWalking && intelligence == AiType.Humanoid)
        {
            _animator.SetBool("walking",false);
        }
        waitTimer = 0;
    }

}
