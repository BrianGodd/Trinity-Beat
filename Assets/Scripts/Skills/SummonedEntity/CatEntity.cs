using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatEntity : SummonedEntity
{
    void OnTriggerEnter(Collider other)
    {
        WeaponEffect weaponEffect = other.gameObject.GetComponent<WeaponEffect>();
        if(weaponEffect != null && weaponEffect.GetCurrentCatDistane() > Vector3.Distance(gameObject.transform.position, weaponEffect.transform.position))
        {
            weaponEffect.catEntity = this;
        }
    }
}
