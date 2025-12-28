using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class OwlEntity : SummonedEntity
{
    private Transform player;
    private Vector3 offset;
    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        player = FindMyPlayerTransform(); // TODO: get user transform?
        offset = transform.position - player.position;
    }
    Transform FindMyPlayerTransform()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();

        for (int i = 0; i < players.Length; i++)
        {
            PhotonView pv = players[i].GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return players[i].transform;
            }
        }

        return null;
    }

    // Update is called once per frame
    void Update()
    {
        if(!pv.IsMine) return;
        transform.position = player.position + offset;
        transform.rotation = player.rotation;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!pv.IsMine) return;
        // 處理視野
        Debug.Log("like GetComponent<InvisibleArea>().triggerCount += 1;");
        FogEntity fogEntity = other.gameObject.GetComponent<FogEntity>();
        if(fogEntity != null)
        {
            fogEntity.counter += 1;

            fogEntity.EnableVisibiity(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!pv.IsMine) return;
        // 處理視野
        Debug.Log("like GetComponent<InvisibleArea>().triggerCount -= 1;");
        FogEntity fogEntity = other.gameObject.GetComponent<FogEntity>();
        if(fogEntity != null)
        {
            fogEntity.counter -= 1;
            if(fogEntity.counter  == 0)
            {
                fogEntity.EnableVisibiity(false); 
            }
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
        PhotonNetwork.Destroy(gameObject);

    }
}
