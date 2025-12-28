using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;


public class ParryEffect : MonoBehaviour
{
    Vector3 originalLocalScale;
    PhotonView pv;


    void Start()
    {
        pv = GetComponent<PhotonView>();
        if(!pv.IsMine)return;
        StartCoroutine(DestroyAfterDelay());
    }

    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        PhotonNetwork.Destroy(gameObject);
    }
}
