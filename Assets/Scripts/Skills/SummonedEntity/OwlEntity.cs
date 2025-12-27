using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OwlEntity : SummonedEntity
{
    private Transform player;
    private Vector3 offset;
    // Start is called before the first frame update
    void Start()
    {
        player = transform; // get user transform?
        offset = transform.position - player.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = player.position + offset;
        transform.rotation = player.rotation;
    }

    void OnTriggerEnter(Collider other)
    {
        // 處理視野
        Debug.Log("like GetComponent<InvisibleArea>().triggerCount += 1;");
    }

    void OnTriggerExit(Collider other)
    {
        // 處理視野
        Debug.Log("like GetComponent<InvisibleArea>().triggerCount -= 1;");

    }
}
