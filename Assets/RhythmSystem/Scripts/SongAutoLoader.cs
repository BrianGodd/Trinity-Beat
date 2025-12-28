using UnityEngine;

public class SongAutoLoader : MonoBehaviour
{
    public RhythmSongPlayer songPlayer;
    public SongData defaultSong;

    void Start()
    {
        if (songPlayer == null) songPlayer = FindObjectOfType<RhythmSongPlayer>();
        if (songPlayer == null || defaultSong == null) return;

        //songPlayer.LoadSong(defaultSong);
        songPlayer.LoadSong(defaultSong, true);
    }
}
