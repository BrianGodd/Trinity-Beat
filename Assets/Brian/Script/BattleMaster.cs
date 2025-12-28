using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BattleMaster : MonoBehaviour
{
    public RectTransform UIRect;
    public GameObject deathEffectPrefab, winEffectPrefab;

    public int deadCount = 0;
    public int playerCount = 0;

    private GameObject showUI;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        playerCount = Photon.Pun.PhotonNetwork.CurrentRoom.PlayerCount - deadCount;
    }

    public void ShowDeadUI()
    {
        GameObject cd = Instantiate(deathEffectPrefab);
        cd.transform.parent = UIRect;
        cd.transform.localPosition = Vector3.zero;
        showUI = cd;
    }

    public void SomeOneDead()
    {
        deadCount += 1;
        if(playerCount <= 1)
        {
            ShowWinUI();
        }
    }

    public void ShowWinUI()
    {
        if(showUI == null) return;
        GameObject cd = Instantiate(winEffectPrefab);
        cd.transform.parent = UIRect;
        cd.transform.localPosition = Vector3.zero;   
    }
}
