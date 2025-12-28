using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class SpawnInitial : MonoBehaviour
{
    public Transform[] spawnPoint;
    public string[] spawnPrefab;

    // Start is called before the first frame update
    void Start()
    {
        if(!PhotonNetwork.IsMasterClient) return;
        StartCoroutine(PSpawn(1f));
    }

    // Update is called once per frame
    void Update()
    {

    }

    IEnumerator PSpawn(float time)
    {
        yield return new WaitForSeconds(time);
        
        int i = 0;
        foreach(string obj in spawnPrefab)
        {
            GameObject prefab = PhotonNetwork.Instantiate(obj, spawnPoint[i].position, spawnPoint[i].rotation);
            
            i++;
        }
    }
}
