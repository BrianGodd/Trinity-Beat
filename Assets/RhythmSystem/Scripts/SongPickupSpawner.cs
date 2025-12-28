using UnityEngine;

public class SongPickupSpawner : MonoBehaviour
{
    public SongPickup pickupPrefab;
    public Transform[] spawnPoints;
    public SongData[] songs;
    public RhythmSongPlayer songPlayer;

    [Min(0f)] public float pickupLoadDelayAfterCollectSec = 0.3f;

    void Start()
    {
        if (pickupPrefab == null || spawnPoints == null || songs == null) return;

        if (songPlayer == null)
            songPlayer = FindObjectOfType<RhythmSongPlayer>();

        int n = Mathf.Min(spawnPoints.Length, songs.Length);
        for (int i = 0; i < n; i++)
        {
            var p = Instantiate(pickupPrefab, spawnPoints[i].position, spawnPoints[i].rotation);
            p.song = songs[i];
            p.songPlayer = songPlayer;
            p.loadDelayAfterCollectSec = pickupLoadDelayAfterCollectSec;
        }
    }
}
