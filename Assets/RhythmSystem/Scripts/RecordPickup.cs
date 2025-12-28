using UnityEngine;

public class RecordPickup : MonoBehaviour
{
    [Header("Data")]
    public SongData song;

    [Header("Visual")]
    public MeshRenderer coverRenderer;          // renderer on the quad
    public string coverTextureProperty = "_MainTex"; // works for most Unlit shaders

    [Header("Load behavior")]
    public RhythmSongPlayer songPlayer;
    [Min(0f)] public float loadDelayAfterEnterSec = 0.2f;
    public string requiredTag = "Player";

    bool _used;

    static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    void Awake()
    {
        ApplyCover();
    }

    void ApplyCover()
    {
        if (coverRenderer == null || song == null) return;
        if (song.albumCover == null) return;

        // Use instance material so each pickup can have its own texture
        var mat = coverRenderer.material;

        // Prefer property name, fallback to _MainTex
        if (mat.HasProperty(coverTextureProperty))
            mat.SetTexture(coverTextureProperty, song.albumCover);
        else if (mat.HasProperty(MainTexId))
            mat.SetTexture(MainTexId, song.albumCover);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_used) return;
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;

        if (songPlayer == null) songPlayer = FindObjectOfType<RhythmSongPlayer>();
        if (songPlayer == null)
        {
            Debug.LogError("[RecordPickup] No RhythmSongPlayer found in scene.");
            return;
        }
        if (song == null)
        {
            Debug.LogError("[RecordPickup] No SongData assigned.");
            return;
        }

        _used = true;
        Invoke(nameof(DoLoad), loadDelayAfterEnterSec);

        // Optional: hide pickup immediately (feels like "collected")
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
        if (coverRenderer) coverRenderer.enabled = false;
    }

    void DoLoad()
    {
        songPlayer.LoadSong(song); // immediate switch behavior already inside RhythmSongPlayer
        Destroy(gameObject);
    }
}
