using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class SummonedEntity : MonoBehaviour
{
    [SerializeField]
    protected int lifeCycle = 0;

    protected PhotonView pv;


    public virtual void Start()
    {
        SkillManager.Instance.AddIntoEntitiesList(this);
        if(lifeCycle == 0)
        {
            SetLifeCycle(5);
        }
        pv = GetComponent<PhotonView>();
    }
    public void SetLifeCycle(int cycle)
    {
        lifeCycle = cycle;
        if (lifeCycle == 0)
        {
            DestroyEffect();
        }
    }
    public void AddLifeCycle(int offset)
    {
        lifeCycle += offset;
        if (lifeCycle == 0)
        {
            DestroyEffect();
        }
    }

    protected virtual void DestroyEffect()
    {
        SkillManager.Instance.RemoveFromEntitiesList(this);
        Debug.Log("Runner.Despawn(Object);");
        PhotonNetwork.Destroy(gameObject);
    }
}
