using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RhythmDebugUI : MonoBehaviour
{
    [Header("References")]
    public BeatClock beatClock;
    public ComboRecorder comboRecorder;
    public TimingPattern timingPattern;

    [Header("UI")]
    public RectTransform trackRect;      // a RectTransform that defines the horizontal area
    public Image circlePrefab;          // UI Image prefab (a circle sprite)
    public RectTransform playheadRect;  // UI Image/RectTransform (vertical line)

    [Header("Layout")]
    [Tooltip("Visual span in beats across the track. Use 4 to show the full 4-beat cycle.")]
    public float beatsShown = 4f;

    [Header("Colors")]
    public Color defaultColor = new Color(1f, 1f, 1f, 0.35f);
    public Color hitColor     = new Color(0.2f, 1f, 0.2f, 0.9f);
    public Color missColor    = new Color(1f, 0.2f, 0.2f, 0.9f);
    public Color cueColor     = new Color(0.2f, 0.6f, 1f, 0.9f);

    [Header("Cue / Pulse")]
    public bool showExpectedCuePulse = true;
    [Min(0.02f)] public float cuePulseDuration = 0.12f;
    [Min(1f)] public float cuePulseScale = 1.25f;

    [Header("Hit Dot (early/late indicator)")]
    public bool showHitDot = true;
    [Tooltip("Max pixel offset of the dot inside the circle.")]
    [Min(0f)] public float hitDotMaxOffsetPx = 14f;
    [Min(1f)] public float hitDotSizePx = 6f;
    
    public Color hitDotColor = new Color(1f, 1f, 1f, 0.95f);

    [Min(0f)] public float edgePaddingPx = 24f;

    RectTransform[] _circleRT = new RectTransform[3];
    Image[] _circleImg = new Image[3];
    RectTransform[] _dotRT = new RectTransform[3];
    Image[] _dotImg = new Image[3];



    struct SlotState
    {
        public bool hit;
        public bool miss;
        public double signedErrorSec;
        public double expectedTime;
    }

    // store per cycle so early hits for next cycle still show correctly
    readonly Dictionary<int, SlotState[]> _cycleStates = new Dictionary<int, SlotState[]>();

    int _shownCycle = -9999;
    double _lastNow = double.NaN;

    void Start()
    {
        if (!ValidateRefs()) return;
        BuildIfNeeded();
    }

    void OnEnable()
    {
        if (comboRecorder != null)
        {
            comboRecorder.OnHitRecorded += OnHitRecorded;
            comboRecorder.OnComboReady += OnComboReady;
        }
    }

    void OnDisable()
    {
        if (comboRecorder != null)
        {
            comboRecorder.OnHitRecorded -= OnHitRecorded;
            comboRecorder.OnComboReady -= OnComboReady;
        }
    }

    void Update()
    {
        if (!ValidateRefs()) return;
        BuildIfNeeded();

        if (!beatClock.IsRunning)
        {
            if (playheadRect != null) playheadRect.gameObject.SetActive(false);
            return;
        }

        if (playheadRect != null) playheadRect.gameObject.SetActive(true);

        double now = beatClock.Now;
        double start = beatClock.StartTime;
        double beatDur = beatClock.BeatDurationSec;

        // which cycle are we currently in (based on clock)
        double beatsSinceStart = (now - start) / beatDur;
        int curCycle = (int)Math.Floor(beatsSinceStart / 4.0);

        if (curCycle != _shownCycle)
        {
            _shownCycle = curCycle;
            RefreshCircleStatesForCycle(_shownCycle);
            PlaceCirclesForPattern(); // spacing may depend on pattern
        }

        // playhead in beats within the cycle (0..4)
        double beatsInCycle = beatsSinceStart - curCycle * 4.0;
        UpdatePlayhead((float)beatsInCycle);

        // cue pulse when expected times pass
        if (showExpectedCuePulse)
        {
            if (double.IsNaN(_lastNow)) _lastNow = now;
            TryCuePulse(_shownCycle, _lastNow, now);
            _lastNow = now;
        }
    }

    bool ValidateRefs()
    {
        if (beatClock == null || comboRecorder == null || timingPattern == null)
        {
            // avoid spamming log
            return false;
        }
        if (trackRect == null || circlePrefab == null || playheadRect == null)
            return false;
        return true;
    }

    void BuildIfNeeded()
    {
        if (_circleRT[0] != null) return;

        for (int i = 0; i < 3; i++)
        {
            var img = Instantiate(circlePrefab, trackRect);
            img.name = $"SlotCircle_{i}";
            img.color = defaultColor;

            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            _circleImg[i] = img;
            _circleRT[i] = rt;

            if (showHitDot)
            {
                var dotGO = new GameObject($"HitDot_{i}");
                dotGO.transform.SetParent(rt, false);
                var dotImg = dotGO.AddComponent<Image>();
                dotImg.color = hitDotColor;

                var dotRT = dotImg.rectTransform;
                dotRT.anchorMin = dotRT.anchorMax = new Vector2(0.5f, 0.5f);
                dotRT.pivot = new Vector2(0.5f, 0.5f);
                dotRT.sizeDelta = new Vector2(hitDotSizePx, hitDotSizePx);
                dotRT.anchoredPosition = Vector2.zero;

                _dotImg[i] = dotImg;
                _dotRT[i] = dotRT;

                dotGO.SetActive(false);
            }
        }

        PlaceCirclesForPattern();
        RefreshCircleStatesForCycle(_shownCycle);
    }

    void PlaceCirclesForPattern()
    {
        float w = trackRect.rect.width;
        float left = -w * 0.5f + edgePaddingPx;
        float right =  w * 0.5f - edgePaddingPx;

        // expected times inside a cycle in "beats"
        // slot beat position = slotIndex + offset(slotIndex)
        for (int slot = 0; slot < 3; slot++)
        {
            float beatPos = slot + timingPattern.GetExpectedOffset012(slot);
            float t = Mathf.Clamp01(beatPos / Mathf.Max(0.001f, beatsShown));
            float x = Mathf.Lerp(left, right, t);

            _circleRT[slot].anchoredPosition = new Vector2(x, 0f);
        }
    }

    void UpdatePlayhead(float beatsInCycle)
    {
        float w = trackRect.rect.width;
        float left = -w * 0.5f + edgePaddingPx;
        float right =  w * 0.5f - edgePaddingPx;

        //float t = Mathf.Clamp01(beatsInCycle / Mathf.Max(0.001f, beatsShown));
        beatsInCycle = Mathf.Min(beatsInCycle, beatsShown); // clamp at 3 beats
        float t = Mathf.Clamp01(beatsInCycle / Mathf.Max(0.001f, beatsShown));
        float x = Mathf.Lerp(left, right, t);

        playheadRect.anchorMin = playheadRect.anchorMax = new Vector2(0.5f, 0.5f);
        playheadRect.pivot = new Vector2(0.5f, 0.5f);
        playheadRect.anchoredPosition = new Vector2(x, 0f);
    }

    SlotState[] EnsureCycleState(int cycle)
    {
        if (_cycleStates.TryGetValue(cycle, out var arr)) return arr;

        arr = new SlotState[3];
        _cycleStates[cycle] = arr;
        return arr;
    }

    void RefreshCircleStatesForCycle(int cycle)
    {
        var arr = EnsureCycleState(cycle);

        for (int i = 0; i < 3; i++)
        {
            // base color by state
            if (arr[i].hit) _circleImg[i].color = hitColor;
            else if (arr[i].miss) _circleImg[i].color = missColor;
            else _circleImg[i].color = defaultColor;

            // dot
            if (showHitDot && _dotRT[i] != null)
            {
                if (arr[i].hit)
                {
                    _dotRT[i].gameObject.SetActive(true);
                    _dotRT[i].anchoredPosition = new Vector2(ErrorToDotOffsetPx(i, arr[i].signedErrorSec), 0f);
                }
                else
                {
                    _dotRT[i].gameObject.SetActive(false);
                }
            }

            // reset scale (pulse will override temporarily)
            _circleRT[i].localScale = Vector3.one;
        }
    }

    float ErrorToDotOffsetPx(int slot, double signedErrorSec)
    {
        // map error to [-hitDotMaxOffsetPx, +hitDotMaxOffsetPx] using your tuned window (early/late)
        double beatDur = beatClock.BeatDurationSec;

        // Access windows from ComboRecorder
        // (Your window-based ComboRecorder has beat0Window/beat1Window/beat2Window public)
        var w = slot switch
        {
            0 => comboRecorder.beat0Window,
            1 => comboRecorder.beat1Window,
            2 => comboRecorder.beat2Window,
            _ => comboRecorder.beat0Window
        };

        double earlySec = w.earlyBeats * beatDur;
        double lateSec = w.lateBeats * beatDur;

        double denom = signedErrorSec < 0 ? Math.Max(1e-6, earlySec) : Math.Max(1e-6, lateSec);
        double norm = signedErrorSec / denom; // -1..1 (in-window)
        norm = Math.Max(-1.0, Math.Min(1.0, norm));

        return (float)(norm * hitDotMaxOffsetPx);
    }

    void OnHitRecorded(ComboRecorder.Hit hit)
    {
        if (!beatClock.IsRunning) return;

        double start = beatClock.StartTime;
        double beatDur = beatClock.BeatDurationSec;

        // cycle index derived from expectedTime (works even for early hits mapped to next cycle)
        int cycle = (int)Math.Floor((hit.expectedTime - start) / (beatDur * 4.0));
        if (cycle < 0) return;

        var arr = EnsureCycleState(cycle);
        int slot = hit.beatSlot;

        arr[slot].hit = true;
        arr[slot].miss = false;
        arr[slot].signedErrorSec = hit.signedErrorSec;
        arr[slot].expectedTime = hit.expectedTime;

        if (cycle == _shownCycle)
            RefreshCircleStatesForCycle(_shownCycle);
    }

    void OnComboReady(ComboRecorder.ComboData combo)
    {
        // mark misses for that cycle
        int cycle = combo.cycleIndex;
        var arr = EnsureCycleState(cycle);

        for (int i = 0; i < 3; i++)
        {
            if (!combo.hits[i].hasInput)
                arr[i].miss = true;
        }

        if (cycle == _shownCycle)
            RefreshCircleStatesForCycle(_shownCycle);
    }

    void TryCuePulse(int cycle, double prevNow, double now)
    {
        // If a slot is already hit/missed, don't cue it
        var arr = EnsureCycleState(cycle);

        double start = beatClock.StartTime;
        double beatDur = beatClock.BeatDurationSec;

        // expected times for cycle slots
        for (int slot = 0; slot < 3; slot++)
        {
            if (arr[slot].hit || arr[slot].miss) continue;

            double expected = start + (cycle * 4 + slot) * beatDur + timingPattern.GetExpectedOffset012(slot) * beatDur;

            // pulse exactly when we cross expected time
            if (prevNow < expected && now >= expected)
            {
                StartCoroutine(Pulse(slot));
            }
        }
    }

    System.Collections.IEnumerator Pulse(int slot)
    {
        var rt = _circleRT[slot];
        var img = _circleImg[slot];

        Color prevColor = img.color;
        Vector3 prevScale = rt.localScale;

        img.color = cueColor;
        rt.localScale = prevScale * cuePulseScale;

        float t = 0f;
        while (t < cuePulseDuration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // restore state color based on current cycle state
        RefreshCircleStatesForCycle(_shownCycle);
    }
}
