using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class SummonedEntity : MonoBehaviour
{
    [SerializeField]
    private int lifeCycle;

    public void SetLifeCycle(int cycle)
    {
        lifeCycle = cycle;
        if (lifeCycle == 0)
        {
            Debug.Log("Runner.Despawn(Object);");
        }
    }
    public void AddLifeCycle(int offset)
    {
        lifeCycle -= offset;
        if (lifeCycle == 0)
        {
            Debug.Log("Runner.Despawn(Object);");
        }
    }
}
