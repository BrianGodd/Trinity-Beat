using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DogEntity : SummonedEntity
{
    Transform closestPlayer;
    NavMeshAgent agent;
    bool isMoving = false;
    // Update is called once per frame
    public override void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        closestPlayer = FindClosestPlayer();
        // BeginMovement();
    }

    Transform FindClosestPlayer()
    {// TODO: get user transform?
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (GameObject p in players)
        {
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = p.transform;
            }
        }

        return closest;
    }
    void Update()
    {
        if (!isMoving || closestPlayer == null) return;
        agent.SetDestination(closestPlayer.position);
    }
    

    public void BeginMovement()
    {
        isMoving = true;
        agent.isStopped = false;
        agent.SetDestination(closestPlayer.position);
    }

    public void StopMovement()
    {
        isMoving = false;
        agent.isStopped = true;
    }
}
