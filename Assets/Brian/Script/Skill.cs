using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class Skill : MonoBehaviour
{
    // network owner view ID
    public int ownerViewID = -1;

    void Awake()
    {
        var pv = GetComponent<PhotonView>();
        if (pv != null && pv.InstantiationData != null && pv.InstantiationData.Length > 0)
        {
            try
            {
                ownerViewID = System.Convert.ToInt32(pv.InstantiationData[0]);
            }
            catch
            {
                ownerViewID = -1;
            }
        }
    }
}
