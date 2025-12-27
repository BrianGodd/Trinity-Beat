using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OwlEntity : SummonedEntity
{
    private Transform player;
    public float cnt = 0;
    private Vector3 offset;
    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        player = transform; // TODO: get user transform?
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
        FogEntity fogEntity = other.gameObject.GetComponent<FogEntity>();
        if(fogEntity != null)
        {
            fogEntity.EnableVisibiity(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        // 處理視野
        Debug.Log("like GetComponent<InvisibleArea>().triggerCount -= 1;");
        FogEntity fogEntity = other.gameObject.GetComponent<FogEntity>();
        if(fogEntity != null)
        {
            fogEntity.EnableVisibiity(false);
        }
    }
    protected override void DestroyEffect()
    {
        SkillManager.Instance.RemoveFromEntitiesList(this);
        Debug.Log("Runner.Despawn(Object);");
        StartCoroutine(DelayDestroy());
    }
    
    IEnumerator DelayDestroy()
    {
        float remain = 1.5f;
        while(remain > 0)
        {
            remain -= Time.deltaTime;
            transform.position += Vector3.up * Time.deltaTime * 5f;
            yield return null;
        }
        Destroy(gameObject);
    }
}
