using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    public enum GameState
    { Normal, Lobby, Room, Game};
    public static GameState gameState = GameState.Normal;
    public static List<int> RoomInfo;

    public Animator CameraAnim;
    bool isLoaded = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch(gameState)
        {
            case GameState.Normal:
                break;
            case GameState.Lobby:
                if(!isLoaded)
                {
                    isLoaded = true;
                    CameraAnim.SetBool("GoToBedRoom", true);
                }
                break;
        }
    }
}
