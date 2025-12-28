using UnityEngine;

[CreateAssetMenu(menuName = "Rhythm/Song Data", fileName = "SongData")]
public class SongData : ScriptableObject
{
    [Header("Title")]
    public string title;

    [Header("Cover (optional)")]
    public Texture2D albumCover;

    [Header("Audio")]
    public AudioClip bgm;
    public bool loop = true;

    [Header("Tempo")]
    [Min(1f)] public float bpm = 120f;

    [Header("Beat-0 offset (intro -> first beat)")]
    [Tooltip("Seconds AFTER music starts that rhythm beat-0 begins (e.g., chorus start).")]
    [Min(0f)] public float beat0OffsetSec = 0f;

    [Header("Pattern")]
    [Tooltip("Your existing TimingPattern asset used by ComboRecorder + UI.")]
    public TimingPattern timingPattern;

    [Header("Tutorial (optional)")]
    [Min(0)] public int tutorialCycles = 0;
}
