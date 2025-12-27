using UnityEngine;

public class RhythmSessionController : MonoBehaviour
{
    [Header("References")]
    public BeatClock beatClock;
    public ComboRecorder comboRecorder;
    public RhythmHitsAudio debugAudio;

    [Header("Music (BGM)")]
    public AudioSource musicSource;
    public AudioClip bgmClip;

    [Header("Music scheduling")]
    [Tooltip("Small lead time so PlayScheduled is reliable (0.02~0.2).")]
    [Min(0f)] public float musicScheduleLeadSec = 0.05f;

    [Header("Manual intro offset (THIS is what you type)")]
    [Tooltip("Seconds from MUSIC START to RHYTHM BEAT-0 (e.g., chorus start). You can edit this in Inspector while playing BEFORE rhythm starts.")]
    [Min(0f)] public float rhythmOffsetSec = 0f;

    [Header("Tutorial phase")]
    [Min(0)] public int tutorialCycles = 4;
    public bool allowPlayerInputDuringTutorial = true;

    [Header("UI")]
    public bool showOnGuiStartButton = true;
    public string startMusicButtonText = "Start Music";

    bool _musicStarted = false;
    bool _rhythmStarted = false;

    double _musicStartDsp = 0;
    double _rhythmStartDsp = 0;

    int _tutorialEndCycleIndexExclusive;
    TimingPattern _pattern;

    void Awake()
    {
        if (beatClock != null)
        {
            beatClock.autoStartOnAwake = false;
            beatClock.Stop();
        }

        if (comboRecorder != null)
        {
            comboRecorder.recordingEnabled = false; // no listening before rhythm start
            comboRecorder.castingEnabled = false;
            _pattern = comboRecorder.timingPattern;
        }
    }

    void Update()
    {
        if (!_musicStarted || _rhythmStarted) return;

        if (_musicStarted && _rhythmStarted)
        {
            // Guard: don't trigger before the scheduled music actually had a chance to play
            if (AudioSettings.dspTime > _musicStartDsp + 0.1 && !musicSource.isPlaying)
            {
                beatClock.Stop();
                comboRecorder.recordingEnabled = false;
                comboRecorder.castingEnabled = false;

                Debug.Log("[RhythmSession] Music ended -> rhythm stopped.");
            }
        }

        // Rhythm beat-0 time = music start + your typed offset
        double targetRhythmStart = _musicStartDsp + rhythmOffsetSec;
        double now = AudioSettings.dspTime;

        // Start rhythm when we reach (or are very close to) target time
        if (now >= targetRhythmStart - 0.01)
        {
            // If user changed offset too late and target is in the past, we can't "start beat-0 in the past"
            // without rewinding music. So we start immediately (now) and warn.
            if (targetRhythmStart < now - 0.02)
            {
                Debug.LogWarning($"[RhythmSession] rhythmOffsetSec was set too late (target already passed by {(now - targetRhythmStart):0.000}s). " +
                                 $"Starting rhythm NOW. For perfect alignment, set rhythmOffsetSec before reaching the target, or restart.");
                targetRhythmStart = now + 0.02;
            }

            StartRhythmAt(targetRhythmStart);
        }
    }

    void OnEnable()
    {
        if (beatClock != null) beatClock.OnBeat += HandleBeat;
    }

    void OnDisable()
    {
        if (beatClock != null) beatClock.OnBeat -= HandleBeat;
    }

    void OnGUI()
    {
        if (!showOnGuiStartButton) return;
        if (_musicStarted) return;

        const int w = 200, h = 40;
        if (GUI.Button(new Rect(20, 20, w, h), startMusicButtonText))
        {
            StartMusic();
        }
    }

    public void StartMusic()
    {
        if (_musicStarted) return;

        if (musicSource == null || bgmClip == null)
        {
            Debug.LogError("[RhythmSession] Missing musicSource or bgmClip.");
            return;
        }
        if (beatClock == null || comboRecorder == null || debugAudio == null || _pattern == null)
        {
            Debug.LogError("[RhythmSession] Missing references (beatClock/comborecorder/debugAudio/timingPattern).");
            return;
        }

        musicSource.clip = bgmClip;
        musicSource.playOnAwake = false;
        musicSource.loop = true;

        _musicStartDsp = AudioSettings.dspTime + musicScheduleLeadSec;
        musicSource.PlayScheduled(_musicStartDsp);

        _musicStarted = true;

        Debug.Log($"[RhythmSession] Music scheduled at dsp={_musicStartDsp:F6}. " +
                  $"Rhythm will start at dsp={(_musicStartDsp + rhythmOffsetSec):F6} (offset={rhythmOffsetSec:0.###}s).");
    }

    void StartRhythmAt(double rhythmStartDsp)
    {
        if (_rhythmStarted) return;

        _rhythmStartDsp = rhythmStartDsp;

        // Start clock synced to your chosen beat-0 time
        beatClock.StartDspAt(_rhythmStartDsp);

        // Tutorial starts immediately with rhythm start (no casting yet)
        comboRecorder.recordingEnabled = allowPlayerInputDuringTutorial;
        comboRecorder.castingEnabled = false;

        _tutorialEndCycleIndexExclusive = tutorialCycles;

        ScheduleTutorialHits();

        _rhythmStarted = true;

        Debug.Log($"[RhythmSession] Rhythm started at dsp={_rhythmStartDsp:F6}. Tutorial cycles={tutorialCycles}.");
    }

    void ScheduleTutorialHits()
    {
        if (tutorialCycles <= 0) return;

        double beatDur = beatClock.BeatDurationSec;

        for (int cycle = 0; cycle < tutorialCycles; cycle++)
        {
            for (int slot = 0; slot < 3; slot++)
            {
                float off = _pattern.GetExpectedOffset012(slot);

                int absBeatIndex = cycle * 4 + slot; // beats 0,1,2 per cycle
                double beatStart = _rhythmStartDsp + absBeatIndex * beatDur;
                double hitTime = beatStart + off * beatDur;

                debugAudio.ScheduleTutorialHit(hitTime);
            }
        }
    }

    void HandleBeat(int beatInCycle, int cycleIndex, double beatStartTime)
    {
        if (!_rhythmStarted) return;

        // Tutorial ends at first beat of cycle == tutorialCycles
        if (cycleIndex >= _tutorialEndCycleIndexExclusive && beatInCycle == 0)
        {
            comboRecorder.recordingEnabled = true;
            comboRecorder.castingEnabled = true;

            Debug.Log("[RhythmSession] Tutorial ended. Battle casting enabled.");
            _tutorialEndCycleIndexExclusive = int.MaxValue;
        }
    }
}
