using UnityEngine;

public class RhythmHitsAudio : MonoBehaviour
{
    [Header("References")]
    public BeatClock beatClock;
    public ComboRecorder comboRecorder;

    [Header("Volumes")]
    [Range(0f, 1f)] public float beatVolume = 0.25f;
    [Range(0f, 1f)] public float barVolume  = 0.35f; // beat 0
    [Range(0f, 1f)] public float hitVolume  = 0.25f;
    [Range(0f, 1f)] public float castVolume = 0.30f;
    [Range(0f, 1f)] public float tutorialHitVolume = 0.25f;

    [Header("Clip Lists (optional). If empty -> fallback beep")]
    public AudioClip[] beatClips;
    public AudioClip[] barClips;
    public AudioClip[] hitClips;
    public AudioClip[] castClips;
    public AudioClip[] tutorialHitClips;

    [Header("Scheduling (for tutorial hits)")]
    [Min(1)] public int scheduledSourcePoolSize = 8;

    AudioSource _oneShotSource;
    AudioSource[] _scheduledSources;
    int _schedIdx = 0;

    AudioClip _fallbackBeat, _fallbackBar, _fallbackHit, _fallbackCast;

    void Awake()
    {
        _oneShotSource = GetComponent<AudioSource>();
        if (_oneShotSource == null) _oneShotSource = gameObject.AddComponent<AudioSource>();
        _oneShotSource.playOnAwake = false;
        _oneShotSource.spatialBlend = 0f;

        _fallbackBeat = MakeBeep("FallbackBeat",  880f, 0.04f);
        _fallbackBar  = MakeBeep("FallbackBar",   660f, 0.07f);
        _fallbackHit  = MakeBeep("FallbackHit",  1320f, 0.03f);
        _fallbackCast = MakeBeep("FallbackCast",  520f, 0.06f);

        // pool for scheduled clips (tutorial hits)
        _scheduledSources = new AudioSource[scheduledSourcePoolSize];
        for (int i = 0; i < scheduledSourcePoolSize; i++)
        {
            var go = new GameObject($"RhythmScheduledSource_{i}");
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            _scheduledSources[i] = s;
        }
    }

    void OnEnable()
    {
        if (beatClock != null) beatClock.OnBeat += OnBeat;
        if (comboRecorder != null)
        {
            comboRecorder.OnHitRecorded += OnHit;
            comboRecorder.OnComboReady += OnCast;
        }
    }

    void OnDisable()
    {
        if (beatClock != null) beatClock.OnBeat -= OnBeat;
        if (comboRecorder != null)
        {
            comboRecorder.OnHitRecorded -= OnHit;
            comboRecorder.OnComboReady -= OnCast;
        }
    }

    void OnBeat(int beatInCycle, int cycleIndex, double beatStartTime)
    {
        if (beatInCycle == 0) PlayOneShotRandom(barClips, _fallbackBar, barVolume);
        else PlayOneShotRandom(beatClips, _fallbackBeat, beatVolume);
    }

    void OnHit(ComboRecorder.Hit hit)
    {
        PlayOneShotRandom(hitClips, _fallbackHit, hitVolume);
    }

    void OnCast(ComboRecorder.ComboData combo)
    {
        PlayOneShotRandom(castClips, _fallbackCast, castVolume);
    }

    public void ScheduleTutorialHit(double dspTime)
    {
        // if user is not using DSP clock, still ok, but scheduling uses dspTime
        var clip = PickRandom(tutorialHitClips);
        if (clip == null) clip = _fallbackHit;

        double now = AudioSettings.dspTime;
        if (dspTime <= now + 0.001) // too late -> play immediately
        {
            _oneShotSource.PlayOneShot(clip, tutorialHitVolume);
            return;
        }

        var src = _scheduledSources[_schedIdx];
        _schedIdx = (_schedIdx + 1) % _scheduledSources.Length;

        src.Stop();
        src.clip = clip;
        src.volume = tutorialHitVolume;
        src.PlayScheduled(dspTime);
    }

    public void StopAllScheduled()
    {
        if (_scheduledSources != null)
        {
            for (int i = 0; i < _scheduledSources.Length; i++)
                if (_scheduledSources[i] != null) _scheduledSources[i].Stop();
        }
    }


    void PlayOneShotRandom(AudioClip[] list, AudioClip fallback, float vol)
    {
        var c = PickRandom(list);
        if (c == null) c = fallback;
        _oneShotSource.PlayOneShot(c, vol);
    }

    static AudioClip PickRandom(AudioClip[] list)
    {
        if (list == null || list.Length == 0) return null;
        int i = Random.Range(0, list.Length);
        return list[i];
    }

    static AudioClip MakeBeep(string name, float freqHz, float durationSec, int sampleRate = 44100)
    {
        int samples = Mathf.Max(1, Mathf.RoundToInt(durationSec * sampleRate));
        float[] data = new float[samples];

        int fade = Mathf.Clamp(samples / 10, 1, samples);
        float twoPiF = 2f * Mathf.PI * freqHz;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float s = Mathf.Sin(twoPiF * t);

            float env = 1f;
            if (i < fade) env *= i / (float)fade;
            if (i > samples - fade) env *= (samples - i) / (float)fade;

            data[i] = s * env * 0.6f;
        }

        var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
