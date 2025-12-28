using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;

public class PunSetting : MonoBehaviourPunCallbacks
{
    //public SoundManager SM;
    //public Animator FadeAnim;
    public TextMeshProUGUI PlayerNum, ButtonName;
    public TMP_InputField RoomName, PlayerName;
    public bool isDebugRoom = false;
    // Start is called before the first frame update
    void Start()
    {
        //JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("❌ Photon 斷線！原因：" + cause.ToString());
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Try to Join");
        ButtonName.text = "Try to Join";
        //SM.PlayMusic(0);
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Game.gameState = Game.GameState.Lobby;
        ButtonName.text = "Success!";
        Debug.Log("Join to Lobby");
        PhotonNetwork.AutomaticallySyncScene = true;
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 12,
            IsVisible = true,
            IsOpen = true
        };
        PhotonNetwork.JoinOrCreateRoom(RoomName.text, roomOptions, TypedLobby.Default);
    }

    public override void OnLeftRoom()
    {
        PhotonNetwork.Disconnect();
    }

    // Update is called once per frame
    void Update()
    {
        PhotonNetwork.NickName = PlayerName.text;
        if(PhotonNetwork.InRoom) 
        {
            PlayerNum.text = "Players: " + PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;
            //change network player name
        }
    }

    public void JoinLobby()
    {
        var settings = PhotonNetwork.PhotonServerSettings.AppSettings;

        Debug.Log("🌐 Photon AppSettings Debug --------------------");
        Debug.Log($"AppIdRealtime: {settings.AppIdRealtime}");
        Debug.Log($"AppVersion: {settings.AppVersion}");
        Debug.Log($"UseNameServer: {settings.UseNameServer}");
        Debug.Log($"FixedRegion: {settings.FixedRegion}");
        Debug.Log($"Protocol: {settings.Protocol}");
        Debug.Log($"Server: {settings.Server}");
        Debug.Log($"Port: {settings.Port}");
        Debug.Log("------------------------------------------------");

        
        bool result = PhotonNetwork.ConnectUsingSettings();
        ButtonName.text = result.ToString() + ", " + (Application.internetReachability).ToString();
    }

    public void LeaveRoom()
    {
        // photon disconnect from room
        PhotonNetwork.LeaveRoom();
        RoomName.text = "";
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(1);
    }

    public void QuickStart()
    {
        StartCoroutine(WaitForSeconds(2f));
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Join to Room, Room name: " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        //Game.RoomInfo.Add(roomList[roomList.Count - 1]);
    }

    IEnumerator WaitForSeconds(float time = 0.5f)
    {
        yield return new WaitForSeconds(1f);
        //FadeAnim.SetBool("fade", true);
        //SM.PlayMusic(1);
        yield return new WaitForSeconds(time);

        PhotonNetwork.LoadLevel(1);
    }
}
