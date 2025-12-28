using System.Collections;
using UnityEngine;

public class RhythmSongPlayer : MonoBehaviour
{
    [Header("Core refs")]
    public AudioSource musicSource;
    public BeatClock beatClock;
    public ComboRecorder comboRecorder;

    [Header("Optional debug refs")]
    public RhythmHitsAudio debugAudio; // if you have it
    public RhythmDebugUI debugUI;       // if you have it

    [Tooltip("When switching songs during gameplay, music starts after this delay.")]
    [Min(0f)] public float midgameMusicAutostartDelaySec = 0.5f;


    [Header("Auto start behavior")]
    [Tooltip("When LoadSong is called, music starts after this delay (intro start delay).")]
    [Min(0f)] public float musicAutostartDelaySec = 3f;

    [Tooltip("Scheduling lead to make PlayScheduled stable.")]
    [Min(0f)] public float scheduleLeadSec = 0.05f;

    int _loadVersion = 0;
    Coroutine _loadRoutine;

    // For other systems to query
    public SongData CurrentSong { get; private set; }
    public bool IsLoadingOrPlaying { get; private set; }
    Coroutine _armRoutine;

    bool _musicScheduled;
    bool _musicActuallyStarted;

    double _musicStartDsp;     // scheduled start for current play segment
    double _rhythmStartDsp;    // scheduled beat-0 start for current play segment

    float _prevMusicTime;

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

        // Before scheduled start, isPlaying will be false — don't treat that as "ended".
        if (!_musicActuallyStarted)
        {
            // consider it started once we're past the scheduled start and audio is playing
            if (dspNow > _musicStartDsp + 0.01 && musicSource.isPlaying)
            {
                _musicActuallyStarted = true;
                _prevMusicTime = musicSource.time;
            }
            return;
        }

        // Non-loop song: when it ends, stop/reset rhythm.
        if (!CurrentSong.loop)
        {
            if (!musicSource.isPlaying)
            {
                StopCurrent_Internal(); // this already stops beatclock + resets recorder
                _musicScheduled = false;
            }
            return;
        }

        // Loop song: detect wrap-around by time jumping backwards.
        if (musicSource.isPlaying)
        {
            float t = musicSource.time;

            // If time decreased, clip looped back to start.
            if (t + 0.001f < _prevMusicTime)
            {
                HandleClipLooped(dspNow, t);
                // after handling, refresh prev time
                _prevMusicTime = musicSource.time;
                return;
            }

            _prevMusicTime = t;
        }
    }    
    void HandleClipLooped(double dspNow, float currentClipTimeSec)
    {
        // cancel any pending arm coroutine
        if (_armRoutine != null) StopCoroutine(_armRoutine);

        // Stop beat + disable input immediately at loop boundary
        beatClock.Stop();
        comboRecorder.recordingEnabled = false;
        comboRecorder.castingEnabled = false;
        comboRecorder.ResetAllState();
        debugUI?.ResetVisual();
        debugAudio?.StopAllScheduled();

        // Estimate the exact DSP time when this loop started:
        // dspNow = loopStartDsp + currentClipTimeSec  => loopStartDsp = dspNow - currentClipTimeSec
        double loopStartDsp = dspNow - currentClipTimeSec;

        // Beat-0 should start after the song's beat0 offset (intro)
        double newRhythmStartDsp = loopStartDsp + CurrentSong.beat0OffsetSec;

        // If offset is 0 and we're already slightly past it, start "now"
        if (newRhythmStartDsp < dspNow + 0.001)
            newRhythmStartDsp = dspNow + 0.001;

        _musicStartDsp = loopStartDsp;
        _rhythmStartDsp = newRhythmStartDsp;

        beatClock.StartDspAt(newRhythmStartDsp);

        // Re-enable input when rhythm actually starts (no tutorial replay on loops)
        int version = _loadVersion;
        _armRoutine = StartCoroutine(ArmAfterRhythmStart(newRhythmStartDsp, version));
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

        // Schedule rhythm start at beat0OffsetSec AFTER music start
        double rhythmStartDsp = musicStartDsp + song.beat0OffsetSec;

        // Disable input until rhythm actually starts
        comboRecorder.recordingEnabled = false;
        comboRecorder.castingEnabled = false;

        // Start beat clock (it will "arm" until dsp reaches start)
        beatClock.StartDspAt(rhythmStartDsp);
        _musicScheduled = true;
        _musicActuallyStarted = false;
        _musicStartDsp = musicStartDsp;
        _rhythmStartDsp = rhythmStartDsp;
        _prevMusicTime = 0f;

        if (_armRoutine != null) StopCoroutine(_armRoutine);

        // Wait until rhythm start moment to enable recording + tutorial scheduling
        while (AudioSettings.dspTime < rhythmStartDsp - 0.001)
        {
            if (version != _loadVersion) yield break;
            yield return null;
        }

        // Rhythm live now
        comboRecorder.ResetAllState(); // clear old cycle buffers cleanly
        comboRecorder.recordingEnabled = true;

        // tutorial then battle enable
        if (song.tutorialCycles > 0 && debugAudio != null)
        {
            // During tutorial: allow input, but no cast (same as your old flow)
            comboRecorder.castingEnabled = false;

            ScheduleTutorialHits(song, rhythmStartDsp);

            // enable casting after tutorial ends: easiest way is re-use beat events in ComboRecorder,
            // so we just wait enough cycles then enable at next cycle boundary.
            // (4 beats per cycle)
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

        Debug.Log($"[RhythmSongPlayer] Loaded song '{song.name}'. " +
                  $"musicDelay={introDelay:0.###}s, beat0Offset={song.beat0OffsetSec:0.###}s, bpm={song.bpm:0.##}");

        IsLoadingOrPlaying = true;
    }

    void ApplySongMetadata(SongData song)
    {
        // tempo
        beatClock.bpm = song.bpm;

        // pattern
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

        // reset visuals/audio schedule
        debugAudio?.StopAllScheduled();
        debugUI?.ResetVisual();
    }

    void StopCurrent_Internal()
    {
        // stop scheduled tutorial ticks / hits
        debugAudio?.StopAllScheduled();

        // stop music immediately
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }

        // stop beatclock
        beatClock?.Stop();

        // disable + clear recorder
        if (comboRecorder != null)
        {
            comboRecorder.recordingEnabled = false;
            comboRecorder.castingEnabled = false;
            comboRecorder.ResetAllState();
        }

        debugUI?.ResetVisual();
        IsLoadingOrPlaying = false;
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
}
