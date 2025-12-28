using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WeaponStaff : WeaponEffect
{
    [SerializeField]
    private Transform startPoint;
    [SerializeField]
    private GameObject attackObject;
    // Start is called before the first frame update


    public override void Attack(Vector3 dir, Vector3 delta, float length)
    {
        delta += catEntity? (catEntity.transform.position - transform.position).normalized :Vector3.zero;
        delta.Normalize();
        delta *= (AimLifeCycle > 0?0.5f:1f) * (RNGLifeCycle > 0?2f:1f);
        dir += delta;
        dir.y = 0;
        dir.Normalize();
        GameObject newAttackObject = Instantiate(attackObject, startPoint.position, startPoint.rotation);
        if(AOELifeCycle > 0)
        {
            newAttackObject.transform.localScale *= 2f;
        }
        newAttackObject.GetComponent<EnergyBall>()?.Init(dir, length, enhancementEffect);
    }
}
