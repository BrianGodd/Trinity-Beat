using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;


public class FogEntity : SummonedEntity
{
    public Renderer renderer;
    public Material originalMaterial;
    public Material visibleMaterial;
    public int counter = 0;

    public void Start()
    {
        SkillManager.Instance.AddIntoEntitiesList(this);
        if(lifeCycle == 0)
        {
            SetLifeCycle(10);
        }
        renderer.sharedMaterial = originalMaterial;
    }


    public void EnableVisibiity(bool visible)
    {
        renderer.sharedMaterial = visible?visibleMaterial : originalMaterial;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("like GetComponent<InvisibleArea>().triggerCount += 1;");
        PlayerController playerController = other.gameObject.GetComponent<PlayerController>();
        if(playerController != null && other.gameObject.GetComponent<PhotonView>() != null && other.gameObject.GetComponent<PhotonView>().IsMine)
        {
            counter += 1;
            EnableVisibiity(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log("like GetComponent<InvisibleArea>().triggerCount -= 1;");
        PlayerController playerController = other.gameObject.GetComponent<PlayerController>();
        if(playerController != null && other.gameObject.GetComponent<PhotonView>() != null && other.gameObject.GetComponent<PhotonView>().IsMine)
        {
            counter -= 1;
            if(counter == 0)
            {
                EnableVisibiity(false);  
            }
        }
    }
}
