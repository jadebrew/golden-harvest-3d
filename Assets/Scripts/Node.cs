using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public enum AiType
{
    None,
    Critter,
    Follow
}

public enum AuraType
{
    None,
    Cooking,
    Crafting
}

[RequireComponent(typeof(NavMeshAgent))]
public class Node : MonoBehaviour
{
    public Item _spawnable;
    public GameObject _target;
    private float magnetRange = 1.5f;
    public float health = 100;
    public DamageType vulnerability;
    public int _dropAmount = 5;

    public AiType intelligence;

    private float UpdateSpeed = 0.1f;

    private bool isWalking = false;

    public AuraType _aura;

    private NavMeshAgent _agent;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _target = GameObject.Find("Player");
    }

    private IEnumerator FollowTarget(GameObject target)
    {
        WaitForSeconds wait = new WaitForSeconds(UpdateSpeed);

        while (enabled && target != null)
        {
            _agent.SetDestination(target.transform.position);

            yield return wait;
        }

        isWalking = false;
    }

    public bool InRange()
    {
        float distance = Vector3.Distance(transform.position, _target.transform.position);
        return distance < magnetRange;
    }

    void FixedUpdate()
    {
        float distance = Vector3.Distance(transform.position, _target.transform.position);
        if (distance < magnetRange)
        {
            _target.GetComponent<PlayerMovement>().SetTarget(transform.gameObject);
        }

        if ((intelligence == AiType.Follow) && !isWalking)
        {
            isWalking = true;
            StartCoroutine(FollowTarget(_target));
        }
        else if ((intelligence == AiType.Critter) && !isWalking)
        {
            isWalking = true;
            StartCoroutine(Wander());
        }

        if (health<0)
        {
            _target.GetComponent<PlayerMovement>().Give(_spawnable,_dropAmount);
            transform.gameObject.SetActive(false);
        }
    }

    private IEnumerator Wander()
    {
        float minWaitTime = 0.1f;
        float maxWaitTime = 1;
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

            // Wait before choosing another destination
            yield return new WaitForSeconds(
                Random.Range(minWaitTime, maxWaitTime)
            );
            isWalking = false;
        }
    }

}
