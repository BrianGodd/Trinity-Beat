using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CatEntity : SummonedEntity
{
    void OnTriggerEnter(Collider other)
    {
        PhotonView pv = other.gameObject.GetComponent<PhotonView>();
        if (pv == null || pv.IsMine)
        {
            return;
        }
        WeaponEffect weaponEffect = other.gameObject.GetComponent<WeaponEffect>();
        if(weaponEffect != null && weaponEffect.GetCurrentCatDistane() > Vector3.Distance(gameObject.transform.position, weaponEffect.transform.position))
        {
            weaponEffect.catEntity = this;
        }
    }
}
