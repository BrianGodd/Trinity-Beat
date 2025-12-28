using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

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
        animator = GetComponent<Animator>();

        if (!pv.IsMine) return;
        closestPlayer = FindClosestPlayer();
        BeginMovement();
        
    }

    Transform FindClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var p in players)
        {
            PhotonView pv = p.GetComponent<PhotonView>();
            if (pv == null || pv.IsMine) continue;

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
        if (!pv.IsMine) return;
        if (closestPlayer == null) return;
        Vector3 dist = closestPlayer.position - transform.position;
        dist.y = 0;
        if(dist.magnitude < 2.5f)
        {
            if (isMoving)
            {
                StopMovement();
            }

        }
        else
        {
            if (!isMoving)
            {
                BeginMovement();
                animator.SetBool("isWalking", true);
            }
            agent.SetDestination(closestPlayer.position);
        }
    }
    

    public void BeginMovement()
    {
        if(closestPlayer == null)return;
        isMoving = true;
        agent.isStopped = false;
        agent.SetDestination(closestPlayer.position);
        animator.SetBool("isWalking", true);

    }

    public void StopMovement()
    {
        animator.SetBool("isWalking", false);
        isMoving = false;
        agent.isStopped = true;
    }
}
