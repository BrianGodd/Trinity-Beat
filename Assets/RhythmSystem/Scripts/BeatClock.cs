using System;
using UnityEngine;

public class BeatClock : MonoBehaviour
{
    [Header("Tempo")]
    [Min(1f)] public float bpm = 120f;
    [Min(1)] public int beatsPerCycle = 4;

    // beatInCycle: 0..beatsPerCycle-1
    // cycleIndex: increasing integer
    // beatStartTime: absolute seconds in Time.timeAsDouble domain
    public event Action<int, int, double> OnBeat;

    private double _startTime;
    private int _lastAbsBeatIndex = int.MinValue;

    public double BeatDurationSec => 60.0 / Math.Max(1e-9, bpm);
    public double Now => Time.timeAsDouble;

    public int CurrentBeatInCycle { get; private set; }
    public int CurrentCycleIndex { get; private set; }
    public double CurrentBeatStartTime { get; private set; }

    private void Awake()
    {
        _startTime = Time.timeAsDouble;
    }

    private void Update()
    {
        double t = Time.timeAsDouble - _startTime;
        double beatDur = BeatDurationSec;

        int absBeatIndex = (int)Math.Floor(t / beatDur);
        if (absBeatIndex == _lastAbsBeatIndex) return;

        _lastAbsBeatIndex = absBeatIndex;

        CurrentBeatInCycle = Mod(absBeatIndex, beatsPerCycle);
        CurrentCycleIndex = absBeatIndex / beatsPerCycle;
        CurrentBeatStartTime = _startTime + absBeatIndex * beatDur;

        OnBeat?.Invoke(CurrentBeatInCycle, CurrentCycleIndex, CurrentBeatStartTime);
    }

    private static int Mod(int a, int m)
    {
        int r = a % m;
        return r < 0 ? r + m : r;
    }
}
