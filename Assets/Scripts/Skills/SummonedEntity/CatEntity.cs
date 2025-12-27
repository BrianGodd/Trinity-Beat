using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatEntity : SummonedEntity
{
    void OnTriggerEnter(Collider other)
    {
        // 處理視野
        Debug.Log("Compare this cat and the cat that affect user now, if this cat has shoerter distance, replacce it");
    }
}
