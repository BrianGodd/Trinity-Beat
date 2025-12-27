using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TntEntity : SummonedEntity
{
    private float effectRadius = 5f;
    // Start is called before the first frame update
    void Start()
    {
        // in Spawned
        Vector3 explosionPos = transform.position;

        int playerLayerMask = 1 << LayerMask.NameToLayer("PlayerRig");

        Collider[] colliders = Physics.OverlapSphere(explosionPos, effectRadius, playerLayerMask);

        foreach (Collider hit in colliders)
        {
            // 爆炸邏輯
            
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
