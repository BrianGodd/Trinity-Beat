using System;
using UnityEngine;

public class ComboRecorder : MonoBehaviour
{
    [Header("References")]
    public BeatClock beatClock;
    public TimingPattern timingPattern;
    public InputBindingMap inputBindingMap;

    [Header("Rules")]
    public bool allowCastWithMissingInputs = true;

    [Header("Debug")]
    public bool logBeats = true;
    public bool logInputs = true;
    public bool logIgnoredExtraInputs = false;

    [Serializable]
    public struct Hit
    {
        public bool hasInput;

        public int beatSlot; // 0..2
        public KeyCode key;
        public char glyph;
        public BeatActionType type;
        public BeatDirection9 dir;

        public double pressTime;       // Time.timeAsDouble
        public double expectedTime;    // beatStart + offset*beatDur
        public double signedErrorSec;  // press - expected
        public double signedErrorMs;   // signedErrorSec * 1000
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

    public ComboData LastCombo { get; private set; } = new ComboData();
    public int CurrentBeatInCycle { get; private set; } = 0;

    private bool[] _filled = new bool[3];

    private void Awake()
    {
        // Safe defaults so it works immediately on Play
        BeginCycle(0);
    }

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
        CurrentBeatInCycle = beatInCycle;

        if (logBeats)
            Debug.Log($"[Beat] cycle={cycleIndex} beat={beatInCycle} beatStart={beatStartTime:F6}");

        if (beatInCycle == 0)
        {
            BeginCycle(cycleIndex);
        }

        if (beatInCycle == 3)
        {
            CastNow();
        }
    }

    private void BeginCycle(int cycleIndex)
    {
        Array.Fill(_filled, false);

        LastCombo = new ComboData
        {
            cycleIndex = cycleIndex,
            patternName = timingPattern != null ? timingPattern.patternName : "(no pattern)",
            hits = new Hit[3]
        };

        for (int i = 0; i < 3; i++)
        {
            LastCombo.hits[i] = new Hit
            {
                hasInput = false,
                beatSlot = i,
                key = KeyCode.None,
                glyph = '_',
                type = BeatActionType.Move,
                dir = BeatDirection9.Center,
                pressTime = 0,
                expectedTime = 0,
                signedErrorSec = 0,
                signedErrorMs = 0
            };
        }
    }

    private void Update()
    {
        if (beatClock == null || timingPattern == null || inputBindingMap == null) return;

        // Only accept inputs on beats 0..2
        if (CurrentBeatInCycle < 0 || CurrentBeatInCycle > 2) return;

        int slot = CurrentBeatInCycle;

        // If already filled, optionally log extra presses (but still ignore)
        if (_filled[slot])
        {
            if (logIgnoredExtraInputs)
            {
                foreach (var e in inputBindingMap.Entries)
                {
                    if (Input.GetKeyDown(e.key))
                        Debug.Log($"[Input IGNORED] slot={slot} key={e.key} (already filled this beat)");
                }
            }
            return;
        }

        foreach (var e in inputBindingMap.Entries)
        {
            if (!Input.GetKeyDown(e.key)) continue;

            RecordHit(slot, e);
            _filled[slot] = true;
            return; // one input per beat
        }
    }

    private void RecordHit(int slot, InputBindingMap.Entry e)
    {
        double pressTime = beatClock.Now;
        double beatDur = beatClock.BeatDurationSec;

        // Ground-truth expected time for THIS slot
        double beatStart = beatClock.CurrentBeatStartTime;
        double expectedOffset = timingPattern.GetExpectedOffset012(slot);
        double expectedTime = beatStart + expectedOffset * beatDur;

        double errSec = pressTime - expectedTime;

        char glyphChar = '_';
        if (!string.IsNullOrEmpty(e.glyph))
            glyphChar = e.glyph[0];

        var hit = new Hit
        {
            hasInput = true,
            beatSlot = slot,
            key = e.key,
            glyph = glyphChar,
            type = e.type,
            dir = e.dir,
            pressTime = pressTime,
            expectedTime = expectedTime,
            signedErrorSec = errSec,
            signedErrorMs = errSec * 1000.0
        };

        LastCombo.hits[slot] = hit;

        if (logInputs)
        {
            Debug.Log($"[Input] slot={slot} key={e.key} glyph='{hit.glyph}' type={hit.type} dir={hit.dir} " +
                      $"expectedOffset={expectedOffset:0.###} err={hit.signedErrorSec:+0.000;-0.000;+0.000}s ({hit.signedErrorMs:+0.0;-0.0;+0.0}ms)");
        }
    }

    private void CastNow()
    {
        if (!allowCastWithMissingInputs)
        {
            for (int i = 0; i < 3; i++)
            {
                if (!LastCombo.hits[i].hasInput)
                    return;
            }
        }

        OnComboReady?.Invoke(LastCombo);
    }
}
