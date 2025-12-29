using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BattleMaster : MonoBehaviour
{
    public RectTransform UIRect;
    public GameObject deathEffectPrefab, winEffectPrefab;
    public GameObject exitButtonPrefab;

    public int deadCount = 0;
    public int playerCount = 0;

    public bool isDead = false;
    
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
        isDead = true;
        StartCoroutine(ShowExitUI());
    }

    public void SomeOneDead()
    {
        deadCount += 1;
        playerCount = Photon.Pun.PhotonNetwork.CurrentRoom.PlayerCount - deadCount;
        Debug.Log($"SomeOneDead called. deadCount: {deadCount}, playerCount: {playerCount}");
        if(playerCount <= 1)
        {
            ShowWinUI();
        }
    }

    public void ShowWinUI()
    {
        if(isDead) return;
        GameObject cd = Instantiate(winEffectPrefab);
        cd.transform.parent = UIRect;
        cd.transform.localPosition = Vector3.zero;
        StartCoroutine(ShowExitUI());   
    }

    IEnumerator ShowExitUI()
    {
        yield return new WaitForSeconds(3f);
        exitButtonPrefab.SetActive(true);
    }

    public void ExitToLobby()
    {
        Photon.Pun.PhotonNetwork.LeaveRoom();
        Photon.Pun.PhotonNetwork.LoadLevel(0);
    }
}
