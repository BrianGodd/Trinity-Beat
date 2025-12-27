using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TntEntity : MonoBehaviour
{
    [SerializeField]
    private float effectRadius = 10f;
    // Start is called before the first frame update
    void Start()
    {
        // in Spawned
        
        Transform player = transform; //TODO: Get player transform
        if(Vector3.Distance(player.position, transform.position) < effectRadius)
        {
            Vector3 dir = player.position - transform.position;
            Vector3 force = (effectRadius - dir.magnitude) * dir.normalized;
            // player.GetComponent<Rigidbody>().AddForce(force);
        }
    }
}
