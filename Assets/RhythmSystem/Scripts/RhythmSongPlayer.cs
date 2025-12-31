using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RhythmSongPlayer : MonoBehaviour
{
    [Header("Core refs")]
    public AudioSource musicSource;
    public BeatClock beatClock;
    public ComboRecorder comboRecorder;

    [Header("Optional debug refs")]
    public RhythmHitsAudio debugAudio; // if you have it (tutorial hits)
    public RhythmDebugUI debugUI;      // if you have it

    [Header("Extra UI Reset Targets (any script with ResetVisual())")]
    public MonoBehaviour[] extraUIResetTargets;

    [Tooltip("When switching songs during gameplay, music starts after this delay (intro start delay).")]
    [Min(0f)] public float midgameMusicAutostartDelaySec = 0.5f;

    [Tooltip("Initial load music autostart delay (intro before music begins).")]
    [Min(0f)] public float musicAutostartDelaySec = 3f;

    [Tooltip("Scheduling lead to make PlayScheduled stable.")]
    [Min(0f)] public float scheduleLeadSec = 0.05f;

    [Header("Loop sync (HARD FIX)")]
    [Tooltip("If ON, loop boundaries are derived from DSP scheduled start + clip duration (samples/frequency).")]
    public bool useClipDurationForLoopSync = true;

    // Optional: sample-accurate beat/bar tick for debugging
    [Header("Metronome (optional, sample-accurate)")]
    public bool enableMetronome = false;
    public AudioClip beatTickClip;
    public AudioClip barTickClip;
    [Range(0f, 1f)] public float tickVolume = 1f;
    [Min(0.05f)] public float tickScheduleAheadSec = 0.50f;
    [Min(4)] public int tickPoolSize = 16;

    int _loadVersion = 0;
    Coroutine _loadRoutine;
    Coroutine _armRoutine;

    public SongData CurrentSong { get; private set; }
    public bool IsLoadingOrPlaying { get; private set; }

    bool _musicScheduled;
    bool _musicActuallyStarted;

    double _musicStartDsp;      // scheduled start for current segment
    double _rhythmStartDsp;     // scheduled beat-0 start (musicStart + beat0Offset)
    double _clipDurSec;         // exact clip duration in seconds (samples/frequency)
    double _nextLoopStartDsp;   // next loop boundary in DSP time

    // metronome pool
    readonly List<AudioSource> _tickPool = new();
    int _tickCursor = 0;
    double _ticksScheduledUntil = double.NegativeInfinity;

    public void LoadSong(SongData song, bool isInitialLoad = false)
    {
        if (song == null)
        {
            Debug.LogError("[RhythmSongPlayer] LoadSong called with null SongData.");
            return;
        }

        _loadVersion++;
        if (_loadRoutine != null) StopCoroutine(_loadRoutine);
        _loadRoutine = StartCoroutine(LoadSongRoutine(song, _loadVersion, isInitialLoad));
    }

    void Update()
    {
        if (!_musicScheduled) return;
        if (CurrentSong == null) return;
        if (musicSource == null || musicSource.clip == null) return;

        double dspNow = AudioSettings.dspTime;

        // Before scheduled start, isPlaying will be false — don't treat that as ended.
        if (!_musicActuallyStarted)
        {
            if (dspNow > _musicStartDsp + 0.01 && musicSource.isPlaying)
            {
                _musicActuallyStarted = true;
            }
            return;
        }

        // Non-loop song: when it ends, stop/reset rhythm.
        if (!CurrentSong.loop)
        {
            if (!musicSource.isPlaying)
            {
                StopCurrent_Internal();
                _musicScheduled = false;
            }
            return;
        }

        // Loop song: HARD FIX — do NOT use AudioSource.time. Use DSP + clip duration.
        if (useClipDurationForLoopSync)
        {
            // catch up if we missed multiple loops (pause/hitch)
            const double eps = 0.0005; // 0.5ms guard
            while (dspNow >= _nextLoopStartDsp - eps)
            {
                HandleClipLoopedExact(_nextLoopStartDsp);
                _nextLoopStartDsp += _clipDurSec;
            }
        }

        // Optional sample-accurate metronome ticks (does NOT rely on frame timing)
        if (enableMetronome)
        {
            EnsureTickPool();
            ScheduleMetronomeTicks(dspNow);
        }
    }

    void HandleClipLoopedExact(double loopStartDsp)
    {
        // cancel any pending arm coroutine
        if (_armRoutine != null) StopCoroutine(_armRoutine);

        // stop beat + disable input immediately at loop boundary
        beatClock.Stop();
        comboRecorder.recordingEnabled = false;
        comboRecorder.castingEnabled = false;
        comboRecorder.ResetAllState();

        //debugUI?.ResetVisual();
        ResetAllUI();
        debugAudio?.StopAllScheduled(); // tutorial hits only
        StopAllMetronomeTicks();

        // Beat-0 should start after the song's beat0 offset (intro)
        // IMPORTANT: DO NOT CLAMP FORWARD. Keep it tied to the true loopStartDsp.
        double newRhythmStartDsp = loopStartDsp + CurrentSong.beat0OffsetSec;

        _musicStartDsp = loopStartDsp;
        _rhythmStartDsp = newRhythmStartDsp;

        beatClock.StartDspAt(newRhythmStartDsp);

        // Re-enable input when rhythm actually starts (no tutorial replay on loops)
        int version = _loadVersion;
        _armRoutine = StartCoroutine(ArmAfterRhythmStart(newRhythmStartDsp, version));

        // reset metronome scheduling window
        _ticksScheduledUntil = double.NegativeInfinity;
    }

    IEnumerator ArmAfterRhythmStart(double rhythmStartDsp, int version)
    {
        while (AudioSettings.dspTime < rhythmStartDsp - 0.001)
        {
            if (version != _loadVersion) yield break;
            yield return null;
        }

        comboRecorder.ResetAllState();
        comboRecorder.recordingEnabled = true;
        comboRecorder.castingEnabled = true;
    }

    IEnumerator LoadSongRoutine(SongData song, int version, bool isInitialLoad)
    {
        IsLoadingOrPlaying = true;

        StopCurrent_Internal();
        ApplySongMetadata(song);

        float introDelay = isInitialLoad ? musicAutostartDelaySec : midgameMusicAutostartDelaySec;
        if (introDelay > 0f)
            yield return new WaitForSeconds(introDelay);

        if (version != _loadVersion) yield break;

        // Schedule music start
        double dspNow = AudioSettings.dspTime;
        double musicStartDsp = dspNow + scheduleLeadSec;

        musicSource.clip = song.bgm;
        musicSource.loop = song.loop;
        musicSource.PlayScheduled(musicStartDsp);

        // Exact clip duration for loop boundaries
        if (song.bgm != null)
            _clipDurSec = (double)song.bgm.samples / Math.Max(1, song.bgm.frequency);
        else
            _clipDurSec = 0;

        _musicStartDsp = musicStartDsp;
        _nextLoopStartDsp = musicStartDsp + _clipDurSec;

        // Schedule rhythm start at beat0OffsetSec AFTER music start
        double rhythmStartDsp = musicStartDsp + song.beat0OffsetSec;
        _rhythmStartDsp = rhythmStartDsp;

        // Disable input until rhythm actually starts
        comboRecorder.recordingEnabled = false;
        comboRecorder.castingEnabled = false;

        // Start beat clock (it will "arm" until dsp reaches start)
        beatClock.StartDspAt(rhythmStartDsp);

        _musicScheduled = true;
        _musicActuallyStarted = false;

        if (_armRoutine != null) StopCoroutine(_armRoutine);

        // Wait until rhythm start moment to enable recording + tutorial scheduling
        while (AudioSettings.dspTime < rhythmStartDsp - 0.001)
        {
            if (version != _loadVersion) yield break;
            yield return null;
        }

        // Rhythm live now
        comboRecorder.ResetAllState();
        comboRecorder.recordingEnabled = true;

        // tutorial then battle enable
        if (song.tutorialCycles > 0 && debugAudio != null)
        {
            comboRecorder.castingEnabled = false;

            ScheduleTutorialHits(song, rhythmStartDsp);

            double beatDur = beatClock.BeatDurationSec;
            double tutorialEndDsp = rhythmStartDsp + (song.tutorialCycles * 4) * beatDur;

            while (AudioSettings.dspTime < tutorialEndDsp)
            {
                if (version != _loadVersion) yield break;
                yield return null;
            }

            comboRecorder.castingEnabled = true;
        }
        else
        {
            comboRecorder.castingEnabled = true;
        }

        CurrentSong = song;

        // reset metronome scheduling window
        _ticksScheduledUntil = double.NegativeInfinity;

        Debug.Log($"[RhythmSongPlayer] Loaded song '{song.name}'. " +
                  $"musicDelay={introDelay:0.###}s, beat0Offset={song.beat0OffsetSec:0.###}s, bpm={song.bpm:0.##}, clipDur={_clipDurSec:0.###}s");

        IsLoadingOrPlaying = true;
    }

    void ApplySongMetadata(SongData song)
    {
        beatClock.bpm = song.bpm;

        if (song.timingPattern != null)
        {
            comboRecorder.timingPattern = song.timingPattern;
            if (debugUI != null) debugUI.timingPattern = song.timingPattern;
        }
        else
        {
            Debug.LogWarning($"[RhythmSongPlayer] Song '{song.name}' has no TimingPattern assigned.");
        }

        CurrentSong = song;

        debugAudio?.StopAllScheduled();
        //debugUI?.ResetVisual();
        ResetAllUI();
        StopAllMetronomeTicks();
    }

    void StopCurrent_Internal()
    {
        debugAudio?.StopAllScheduled();
        StopAllMetronomeTicks();

        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }

        beatClock?.Stop();

        if (comboRecorder != null)
        {
            comboRecorder.recordingEnabled = false;
            comboRecorder.castingEnabled = false;
            comboRecorder.ResetAllState();
        }

        //debugUI?.ResetVisual();
        ResetAllUI();

        IsLoadingOrPlaying = false;
        _musicScheduled = false;
        _musicActuallyStarted = false;
        _clipDurSec = 0;
        _nextLoopStartDsp = 0;
        _ticksScheduledUntil = double.NegativeInfinity;
    }

    void ScheduleTutorialHits(SongData song, double rhythmStartDsp)
    {
        if (song.timingPattern == null) return;

        double beatDur = beatClock.BeatDurationSec;

        for (int cycle = 0; cycle < song.tutorialCycles; cycle++)
        {
            for (int slot = 0; slot < 3; slot++)
            {
                float off = song.timingPattern.GetExpectedOffset012(slot);
                double beatStart = rhythmStartDsp + (cycle * 4 + slot) * beatDur;
                double hitTime = beatStart + off * beatDur;

                debugAudio.ScheduleTutorialHit(hitTime);
            }
        }
    }

    // ---------------- Metronome (optional) ----------------

    void EnsureTickPool()
    {
        if (_tickPool.Count > 0) return;

        for (int i = 0; i < tickPoolSize; i++)
        {
            var go = new GameObject($"MetronomeTick_{i}");
            go.transform.SetParent(transform, false);

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f; // 2D debug tick
            src.volume = tickVolume;

            _tickPool.Add(src);
        }
    }

    void StopAllMetronomeTicks()
    {
        for (int i = 0; i < _tickPool.Count; i++)
        {
            if (_tickPool[i] == null) continue;
            _tickPool[i].Stop();
            _tickPool[i].clip = null;
        }
    }

    void ScheduleMetronomeTicks(double dspNow)
    {
        if (beatTickClip == null && barTickClip == null) return;
        if (!beatClock.UsingDspTime) return;
        if (!beatClock.IsRunning) return;

        double beatDur = beatClock.BeatDurationSec;
        double scheduleUntil = dspNow + tickScheduleAheadSec;

        // we only schedule ticks >= rhythmStart
        double from = Math.Max(_ticksScheduledUntil + 0.0001, Math.Max(dspNow + 0.02, _rhythmStartDsp));

        int nextAbsBeat = (int)Math.Ceiling((from - _rhythmStartDsp) / beatDur);
        if (nextAbsBeat < 0) nextAbsBeat = 0;

        for (;; nextAbsBeat++)
        {
            double t = _rhythmStartDsp + nextAbsBeat * beatDur;
            if (t > scheduleUntil) break;

            bool isBar = (beatClock.beatsPerCycle > 0) && (nextAbsBeat % beatClock.beatsPerCycle == 0);
            var clip = isBar ? (barTickClip != null ? barTickClip : beatTickClip) : beatTickClip;
            if (clip == null) continue;

            var src = _tickPool[_tickCursor++ % _tickPool.Count];
            src.volume = tickVolume;
            src.clip = clip;
            src.PlayScheduled(t);
            src.SetScheduledEndTime(t + clip.length);

            _ticksScheduledUntil = t;
        }
    }

    void ResetAllUI()
    {
        // keep old behavior
        debugUI?.ResetVisual();

        // reset any other UI scripts (Taiko lane UI, etc.)
        if (extraUIResetTargets == null) return;

        for (int i = 0; i < extraUIResetTargets.Length; i++)
        {
            var mb = extraUIResetTargets[i];
            if (mb == null) continue;

            // calls ResetVisual() if it exists on that component
            mb.SendMessage("ResetVisual", SendMessageOptions.DontRequireReceiver);
        }
    }

}
