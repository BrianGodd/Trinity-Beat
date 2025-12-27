using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class SummonedEntity : MonoBehaviour
{
    [SerializeField]
    private int lifeCycle = 0;

    public virtual void Start()
    {
        SkillManager.Instance.AddIntoEntitiesList(this);
        if(lifeCycle == 0)
        {
            SetLifeCycle(3);
        }
    }
    public void SetLifeCycle(int cycle)
    {
        lifeCycle = cycle;
        if (lifeCycle == 0)
        {
            Destroy(gameObject);
            SkillManager.Instance.RemoveFromEntitiesList(this);
            Debug.Log("Runner.Despawn(Object);");
        }
    }
    public void AddLifeCycle(int offset)
    {
        lifeCycle += offset;
        if (lifeCycle == 0)
        {
            Destroy(gameObject);
            SkillManager.Instance.RemoveFromEntitiesList(this);
            Debug.Log("Runner.Despawn(Object);");
        }
    }
}
