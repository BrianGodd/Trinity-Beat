using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DogEntity : SummonedEntity
{
    Transform closestPlayer;
    NavMeshAgent agent;
    Animator animator;
    bool isMoving = false;
    // Update is called once per frame
    public override void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        closestPlayer = FindClosestPlayer();
        BeginMovement();
        animator = GetComponent<Animator>();
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
        Vector3 dist = closestPlayer.position - transform.position;
        dist.y = 0;
        if(dist.magnitude < 1f)
        {
            animator.SetBool("isWalking", false);
        }
        else
        {
            agent.SetDestination(closestPlayer.position);
            animator.SetBool("isWalking", true);

        }
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
