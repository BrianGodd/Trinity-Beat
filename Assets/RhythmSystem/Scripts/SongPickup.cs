using System.Collections;
using UnityEngine;

public class SongPickup : MonoBehaviour
{
    public SongData song;
    [Min(0f)] public float loadDelayAfterCollectSec = 0.3f;

    [Tooltip("Optional. If empty, will FindObjectOfType<RhythmSongPlayer>().")]
    public RhythmSongPlayer songPlayer;

    [Tooltip("Only collect when collider has this tag. Leave empty to accept anything.")]
    public string requiredTag = "Player";

    bool _collected = false;

    void Reset()
    {
        // helpful default: ensure trigger
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_collected) return;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        if (song == null)
        {
            Debug.LogError("[SongPickup] No SongData assigned.");
            return;
        }

        if (songPlayer == null)
            songPlayer = FindObjectOfType<RhythmSongPlayer>();

        if (songPlayer == null)
        {
            Debug.LogError("[SongPickup] No RhythmSongPlayer found in scene.");
            return;
        }

        _collected = true;

        // hide pickup immediately
        var r = GetComponentInChildren<Renderer>();
        if (r != null) r.enabled = false;
        var c = GetComponent<Collider>();
        if (c != null) c.enabled = false;

        StartCoroutine(LoadAfterDelay());
    }

    IEnumerator LoadAfterDelay()
    {
        if (loadDelayAfterCollectSec > 0f)
            yield return new WaitForSeconds(loadDelayAfterCollectSec);

        songPlayer.LoadSong(song);

        // optionally destroy pickup
        Destroy(gameObject);
    }
}
