using System;
using UnityEngine;

public class BeatClock : MonoBehaviour
{
    public double StartTime => _startTime;
    [Header("Tempo")]
    [Min(1f)] public float bpm = 120f;
    [Min(1)] public int beatsPerCycle = 4;

    [Header("Start Mode")]
    public bool autoStartOnAwake = true;

    // beatInCycle: 0..beatsPerCycle-1
    // cycleIndex: 0.. increasing
    // beatStartTime: absolute time in the clock's domain (Unity time or DSP time)
    public event Action<int, int, double> OnBeat;

    public double BeatDurationSec => 60.0 / Math.Max(1e-9, bpm);

    public bool IsRunning { get; private set; }
    public bool UsingDspTime { get; private set; }

    public double Now => UsingDspTime ? AudioSettings.dspTime : Time.timeAsDouble;

    public int CurrentBeatInCycle { get; private set; }
    public int CurrentCycleIndex { get; private set; }
    public double CurrentBeatStartTime { get; private set; } // same domain as Now

    double _startTime;              // absolute start time in chosen domain
    int _lastAbsBeatIndex = int.MinValue;

    void Awake()
    {
        if (autoStartOnAwake)
            StartUnityTimeNow();
    }

    public void StartUnityTimeNow()
    {
        UsingDspTime = false;
        _startTime = Time.timeAsDouble;
        _lastAbsBeatIndex = int.MinValue;
        IsRunning = true;
    }

    // Use this for music-sync: pass the exact DSP time the music starts.
    public void StartDspAt(double dspStartTime)
    {
        UsingDspTime = true;
        _startTime = dspStartTime;
        _lastAbsBeatIndex = int.MinValue;
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    // Replace BeatClock.Update() body with a catch-up loop:
void Update()
{
    if (!IsRunning) return;

    double t = Now - _startTime;
    if (t < 0) return;

    double beatDur = BeatDurationSec;
    int absBeatIndex = (int)System.Math.Floor(t / beatDur);

    if (_lastAbsBeatIndex == int.MinValue)
        _lastAbsBeatIndex = absBeatIndex - 1;

    if (absBeatIndex <= _lastAbsBeatIndex) return;

    for (int b = _lastAbsBeatIndex + 1; b <= absBeatIndex; b++)
    {
        CurrentBeatInCycle = Mod(b, beatsPerCycle);
        CurrentCycleIndex = b / beatsPerCycle;
        CurrentBeatStartTime = _startTime + b * beatDur;
        OnBeat?.Invoke(CurrentBeatInCycle, CurrentCycleIndex, CurrentBeatStartTime);
    }

    _lastAbsBeatIndex = absBeatIndex;
}

    static int Mod(int a, int m)
    {
        int r = a % m;
        return r < 0 ? r + m : r;
    }
}
