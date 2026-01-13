using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using TMPro;

public class TimeController : MonoBehaviourPunCallbacks
{
    public RectTransform UIRect;
    public GameObject dustPrefab, CountDownPrefab;
    public List<Transform> spawnPos = new List<Transform>();

    public RhythmSongPlayer songPlayer;
    public SongData defaultSong;

    const double INTERVAL = 120.0;

    double startTime;
    int lastTick = 0;
    bool isRunning = false;

    int nowInd = 0;

    void Start()
    {
        StartCoroutine(StartTimerByWaitTime(3.0f));
        StartCoroutine(CountDown(3));
        
    }

    void Update()
    {
        if (!isRunning) return;
        if (!PhotonNetwork.IsMasterClient) return;

        double elapsed = PhotonNetwork.Time - startTime;
        int currentTick = (int)(elapsed / INTERVAL);

        if (currentTick > lastTick)
        {
            lastTick = currentTick;
            OnTick(currentTick);
        }
    }

    System.Collections.IEnumerator StartTimerByWaitTime(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        StartTimer();
    }

    System.Collections.IEnumerator CountDown(int ctime)
    {
        for(int i = ctime;i >= 0; i--)
        {
            GameObject cd = Instantiate(CountDownPrefab);
            if(i == 0) 
            {
                cd.GetComponent<TextMeshProUGUI>().text = "Start";
                cd.GetComponent<TextMeshProUGUI>().fontSize = 80;
                if (songPlayer == null) songPlayer = FindObjectOfType<RhythmSongPlayer>();
                //if (songPlayer == null || defaultSong == null) return;

                //songPlayer.LoadSong(defaultSong);
                //yield return new WaitForSeconds(5f);
                songPlayer.LoadSong(defaultSong, false);
            }
            else cd.GetComponent<TextMeshProUGUI>().text = i.ToString();
            cd.transform.parent = UIRect;
            cd.transform.localPosition = Vector3.zero;
            Destroy(cd, 1.5f);
            if(i > 0) yield return new WaitForSeconds(1);       
        }
        
    }

    public void StartTimer()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        startTime = PhotonNetwork.Time;
        lastTick = 0;
        isRunning = true;

        // 同步給其他玩家（可選，但推薦）
        Hashtable props = new Hashtable();
        props["TimerStart"] = startTime;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    void OnTick(int tick)
    {
        SpawnDust();
    }

    void SpawnDust()
    {
        if(nowInd + 4 > spawnPos.Count)
        {
            return;
        }
        for(int i = nowInd; i < nowInd+4; i++)
        {
            PhotonNetwork.Instantiate(
                dustPrefab.name,
                spawnPos[i].position,
                spawnPos[i].rotation
            );
        }
        nowInd += 4;
    }

    Vector3 GetRandomPosition()
    {
        return new Vector3(
            Random.Range(-5f, 5f),
            0f,
            Random.Range(-5f, 5f)
        );
    }
}
