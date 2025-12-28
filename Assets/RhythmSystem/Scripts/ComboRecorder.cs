using System;
using System.Collections.Generic;
using UnityEngine;

public class ComboRecorder : MonoBehaviour
{
    [Header("References")]
    public BeatClock beatClock;
    public TimingPattern timingPattern;
    public InputBindingMap inputBindingMap;

    [Header("Runtime Toggles")]
    public bool recordingEnabled = true;
    public bool castingEnabled = true;

    [Header("Rules")]
    public bool allowCastWithMissingInputs = true;

    [Serializable]
    public struct HitWindowBeats
    {
        [Min(0f)] public float earlyBeats; // how many beats BEFORE expected time is valid
        [Min(0f)] public float lateBeats;  // how many beats AFTER  expected time is valid
    }

    [Header("Hit Windows (centered on expected time)")]
    [Tooltip("Default example: early=0.5, late=0.5 => [T-0.5beat, T+0.5beat].")]
    public HitWindowBeats beat0Window = new HitWindowBeats { earlyBeats = 0.5f, lateBeats = 0.5f };
    public HitWindowBeats beat1Window = new HitWindowBeats { earlyBeats = 0.5f, lateBeats = 0.5f };
    public HitWindowBeats beat2Window = new HitWindowBeats { earlyBeats = 0.5f, lateBeats = 0.5f };
    
    [Header("Sync Typing")]
    public TypingSync typingSync;

    [Header("Debug")]
    public bool logBeats = true;
    public bool logInputs = true;
    public bool logMisses = true;
    public bool logIgnoredExtraInputs = false;

    [Header("Cycle Gating")]
    [Min(1)] public int castEveryNCycles = 2;  // 2 => every other cycle
    [Min(0)] public int castCycleOffset = 0;   // 0 => cycles 0,2,4... are input/cast cycles
    public bool logSkippedCycles = true;

    bool IsInputCycle(int cycle)
    {
        int m = castEveryNCycles;
        int x = (cycle - castCycleOffset) % m;
        if (x < 0) x += m;
        return x == 0;
    }
    

    [Serializable]
    public struct Hit
    {
        public bool hasInput;

        public int beatSlot; // 0..2
        public KeyCode key;
        public char glyph;
        public BeatActionType type;
        public BeatDirection9 dir;

        public double pressTime;
        public double expectedTime;
        public double signedErrorSec;
        public double signedErrorMs;
    }

    [Serializable]
    public class ComboData
    {
        public int cycleIndex;
        public string patternName;
        public Hit[] hits = new Hit[3];

        public string WordString
        {
            get
            {
                char a = hits[0].hasInput ? hits[0].glyph : '_';
                char b = hits[1].hasInput ? hits[1].glyph : '_';
                char c = hits[2].hasInput ? hits[2].glyph : '_';
                return new string(new[] { a, b, c });
            }
        }
    }

    public event Action<ComboData> OnComboReady;
    public event Action<Hit> OnHitRecorded;

    // Store combos by cycle so early hits (e.g., for next cycle slot0) still land correctly
    private readonly Dictionary<int, ComboData> _combos = new Dictionary<int, ComboData>();

    public ComboData LastCombo { get; private set; }

    private void OnEnable()
    {
        if (beatClock != null)
            beatClock.OnBeat += HandleBeat;
    }

    private void OnDisable()
    {
        if (beatClock != null)
            beatClock.OnBeat -= HandleBeat;
    }

    private void HandleBeat(int beatInCycle, int cycleIndex, double beatStartTime)
    {
        double relBeatStart = beatStartTime - beatClock.StartTime;
        //if (logBeats)
          //  Debug.Log($"[Beat] cycle={cycleIndex} beat={beatInCycle} beatStart={beatStartTime:F6}");
        if (logBeats)
            Debug.Log($"[Beat] cycle={cycleIndex} beat={beatInCycle} beatStartRel={(beatStartTime - beatClock.StartTime):F3}s");

        if (beatInCycle == 0)
            EnsureCombo(cycleIndex);

        //if (beatInCycle == 3)
          //  CastCycle(cycleIndex);
        if (beatInCycle == 3)
        {
            if (IsInputCycle(cycleIndex))
                CastCycle(cycleIndex);
            else if (logSkippedCycles)
                Debug.Log($"[Skip Cast] cycle={cycleIndex} (action-only cycle)");
        }

    }

    private void Update()
    {
        if (!recordingEnabled) return;
        if (beatClock == null || timingPattern == null || inputBindingMap == null) return;

        foreach (var e in inputBindingMap.Entries)
        {
            if (!Input.GetKeyDown(e.key)) continue;
            HandleKeyPress(e);
            return;
        }
    }

    public void ResetAllState()
    {
        // clears stored combos and last combo
        _combos.Clear();
        LastCombo = null;
    }
    private void HandleKeyPress(InputBindingMap.Entry e)
    {
        double pressTime = beatClock.Now;

        if (!TryResolveSlotByWindow(pressTime, out int cycleIndex, out int slot012, out double expectedTime))
        {
            if (logMisses)
                Debug.Log($"[MISS] key={e.key} (no slot window matched)");
            return;
        }

        // disallow negative cycles
        if (cycleIndex < 0) 
        {
            if (logMisses)
                Debug.Log($"[MISS] key={e.key} (resolved to negative cycle {cycleIndex})");
            return;
        }

        var combo = EnsureCombo(cycleIndex);

        if (combo.hits[slot012].hasInput)
        {
            if (logIgnoredExtraInputs)
                Debug.Log($"[Input IGNORED] cycle={cycleIndex} slot={slot012} already filled.");
            return;
        }

        char glyphChar = '_';
        if (!string.IsNullOrEmpty(e.glyph))
            glyphChar = e.glyph[0];

        double errSec = pressTime - expectedTime;

        var hit = new Hit
        {
            hasInput = true,
            beatSlot = slot012,
            key = e.key,
            glyph = glyphChar,
            type = e.type,
            dir = e.dir,
            pressTime = pressTime,
            expectedTime = expectedTime,
            signedErrorSec = errSec,
            signedErrorMs = errSec * 1000.0
        };

        combo.hits[slot012] = hit;
        LastCombo = combo;

        if (!IsInputCycle(cycleIndex))
        {
            if (logSkippedCycles)
                Debug.Log($"[Skip Input] cycle={cycleIndex} slot={slot012} (action-only cycle)");
            return;
        }

        OnHitRecorded?.Invoke(hit);

        typingSync.ChangeWord(slot012, glyphChar);

        if (logInputs)
        {
            Debug.Log($"[Input] cycle={cycleIndex} slot={slot012} key={e.key} glyph='{hit.glyph}' type={hit.type} dir={hit.dir} " +
                      $"err={hit.signedErrorSec:+0.000;-0.000;+0.000}s ({hit.signedErrorMs:+0.0;-0.0;+0.0}ms)");
        }
    }

    private ComboData EnsureCombo(int cycleIndex)
    {
        if (_combos.TryGetValue(cycleIndex, out var existing))
            return existing;

        var combo = new ComboData
        {
            cycleIndex = cycleIndex,
            patternName = timingPattern != null ? timingPattern.patternName : "(no pattern)",
            hits = new Hit[3]
        };

        for (int i = 0; i < 3; i++)
        {
            combo.hits[i] = new Hit
            {
                hasInput = false,
                beatSlot = i,
                key = KeyCode.None,
                glyph = '_',
                type = BeatActionType.Move,
                dir = BeatDirection9.Center
            };
        }

        _combos[cycleIndex] = combo;
        return combo;
    }

    private void CastCycle(int cycleIndex)
    {
        if (!castingEnabled) return;

        var combo = EnsureCombo(cycleIndex);

        if (!allowCastWithMissingInputs)
        {
            for (int i = 0; i < 3; i++)
                if (!combo.hits[i].hasInput) return;
        }

        LastCombo = combo;
        OnComboReady?.Invoke(combo);

        // cleanup
        _combos.Remove(cycleIndex - 2);
    }

    // ---------------------------
    // Window-based slot resolution
    // ---------------------------
    private HitWindowBeats GetWindow(int slot012)
    {
        return slot012 switch
        {
            0 => beat0Window,
            1 => beat1Window,
            2 => beat2Window,
            _ => beat0Window
        };
    }

    private bool TryResolveSlotByWindow(double t, out int bestCycle, out int bestSlot, out double bestExpectedTime)
    {
        bestCycle = 0;
        bestSlot = 0;
        bestExpectedTime = 0;

        // Must be 4 beats per cycle for this system
        if (beatClock.beatsPerCycle != 4)
        {
            Debug.LogError("[ComboRecorder] Window resolver assumes beatsPerCycle=4.");
            return false;
        }

        double start = beatClock.StartTime;
        double beatDur = beatClock.BeatDurationSec;

        if (t < start) return false; // before rhythm starts

        int absBeatIndex = (int)Math.Floor((t - start) / beatDur);
        int guessCycle = absBeatIndex / 4;

        // Evaluate nearby cycles (prev/current/next) and slots (0..2)
        bool found = false;
        double bestAbsErr = double.MaxValue;

        for (int cycle = guessCycle - 1; cycle <= guessCycle + 1; cycle++)
        {
            for (int slot = 0; slot <= 2; slot++)
            {
                // Disallow writing slot2 once cast beat has started for that cycle
                if (slot == 2)
                {
                    double castStart = start + (cycle * 4 + 3) * beatDur; // beat3 start
                    if (t >= castStart) continue;
                }

                double expectedTime = ExpectedTime(cycle, slot, start, beatDur);
                double errSec = t - expectedTime;

                var w = GetWindow(slot);
                double earlySec = w.earlyBeats * beatDur;
                double lateSec  = w.lateBeats  * beatDur;

                if (errSec < -earlySec || errSec > lateSec)
                    continue; // outside this slot window => not a match

                double absErr = Math.Abs(errSec);
                if (absErr < bestAbsErr)
                {
                    bestAbsErr = absErr;
                    bestCycle = cycle;
                    bestSlot = slot;
                    bestExpectedTime = expectedTime;
                    found = true;
                }
            }
        }

        return found;
    }

    private double ExpectedTime(int cycle, int slot012, double start, double beatDur)
    {
        float off = timingPattern.GetExpectedOffset012(slot012);
        return start + (cycle * 4 + slot012) * beatDur + off * beatDur;
    }
}
