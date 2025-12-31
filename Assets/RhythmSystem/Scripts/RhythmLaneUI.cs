using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RhythmLaneUI : MonoBehaviour
{
    [Header("Refs")]
    public BeatClock beatClock;
    public ComboRecorder comboRecorder;
    public TimingPattern timingPattern;

    [Header("UI")]
    public RectTransform trackRect;
    public Image circlePrefab;

    [Tooltip("Optional. A RectTransform (eg your old playhead line) that will be pinned to the target spot.")]
    public RectTransform targetMarker;

    [Header("Lane Layout")]
    [Min(0f)] public float edgePaddingPx = 24f;
    [Min(0f)] public float targetOffsetFromLeftPx = 24f;
    public float laneY = 0f;

    [Header("Motion (time-based)")]
    [Tooltip("How long (in seconds) a note takes to travel from spawn (right) to target (left). Bigger = easier & shows more future notes.")]
    [Min(0.05f)] public float travelTimeSec = 2.0f;

    [Tooltip("Spawn notes slightly early so they appear before entering motion range.")]
    [Min(0f)] public float spawnLeadSec = 0.10f;

    [Tooltip("Keep notes visible this long after they pass the target, then despawn.")]
    [Min(0f)] public float despawnAfterSec = 0.50f;

    [Header("Colors")]
    public Color defaultColor = new Color(1f, 1f, 1f, 0.8f);
    public Color hitColor     = new Color(0.2f, 1f, 0.2f, 1f);
    public Color missColor    = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("No-input (action-only) cycle style")]
    public Color noInputCycleColor = new Color(0.6f, 0.6f, 0.6f, 0.35f);
    [Min(0.1f)] public float noInputCycleScale = 0.85f;

    [Header("Hit Dot (early/late indicator)")]
    public bool showHitDot = true;
    [Min(0f)] public float hitDotMaxOffsetPx = 14f;
    [Min(1f)] public float hitDotSizePx = 6f;
    public Color hitDotColor = new Color(1f, 1f, 1f, 0.95f);

    // ---------------- internal ----------------

    struct SlotState
    {
        public bool hit;
        public bool miss;
        public double signedErrorSec;
        public double expectedTime;
    }

    class NoteVisual
    {
        public int cycle;
        public int slot; // 0..2
        public double expectedTime;

        public RectTransform rt;
        public Image img;

        public RectTransform dotRT;
        public Image dotImg;
    }

    readonly Dictionary<int, SlotState[]> _cycleStates = new Dictionary<int, SlotState[]>();
    readonly Dictionary<long, NoteVisual> _notes = new Dictionary<long, NoteVisual>();

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

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

        if (!beatClock.IsRunning)
        {
            // hide / reset when clock stops
            ResetVisual();
            return;
        }

        double now = beatClock.Now;
        double start = beatClock.StartTime;
        double beatDur = beatClock.BeatDurationSec;

        // pin target marker
        if (targetMarker != null)
        {
            targetMarker.gameObject.SetActive(true);
            targetMarker.anchoredPosition = new Vector2(TargetX(), laneY);
        }

        // Spawn upcoming notes within horizon
        double horizon = now + travelTimeSec + spawnLeadSec;

        // current cycle guess
        double beatsSinceStart = (now - start) / beatDur;
        int curCycle = Mathf.FloorToInt((float)(beatsSinceStart / 4.0));

        // We scan a small range of cycles forward; stop when the earliest slot of a cycle is beyond horizon.
        // This keeps it stable even if bpm changes.
        for (int cycle = curCycle - 1; cycle <= curCycle + 16; cycle++)
        {
            if (cycle < 0) continue;

            // earliest expected time among slots is slot0 (usually), good enough to early-break
            double earliest = ExpectedTime(cycle, 0, start, beatDur);
            if (earliest > horizon) break;

            for (int slot = 0; slot < 3; slot++)
            {
                double tExp = ExpectedTime(cycle, slot, start, beatDur);

                // spawn when it’s close enough to start traveling
                if (tExp - now > travelTimeSec + spawnLeadSec) continue;

                // don’t spawn if it’s already too far past
                if (now > tExp + despawnAfterSec) continue;

                EnsureNote(cycle, slot, tExp);
            }
        }

        // Update positions & visuals; despawn old
        _toRemove.Clear();
        foreach (var kv in _notes)
        {
            var n = kv.Value;

            // position
            float x = XForExpectedTime(n.expectedTime, now);
            n.rt.anchoredPosition = new Vector2(x, laneY);

            // state visuals
            ApplyStateToNote(n);

            // despawn
            if (now > n.expectedTime + despawnAfterSec)
                _toRemove.Add(kv.Key);
        }

        for (int i = 0; i < _toRemove.Count; i++)
        {
            long key = _toRemove[i];
            if (_notes.TryGetValue(key, out var n))
            {
                if (n != null && n.rt != null)
                    Destroy(n.rt.gameObject);
            }
            _notes.Remove(key);
        }
    }

    readonly List<long> _toRemove = new List<long>(128);

    bool ValidateRefs()
    {
        return beatClock != null
            && comboRecorder != null
            && timingPattern != null
            && trackRect != null
            && circlePrefab != null;
    }

    public void ResetVisual()
    {
        // clear notes
        foreach (var kv in _notes)
        {
            if (kv.Value != null && kv.Value.rt != null)
                Destroy(kv.Value.rt.gameObject);
        }
        _notes.Clear();
        _cycleStates.Clear();

        if (targetMarker != null)
            targetMarker.gameObject.SetActive(false);
    }

    // ---------- expected time math ----------

    double ExpectedTime(int cycle, int slot012, double start, double beatDur)
    {
        float off = timingPattern.GetExpectedOffset012(slot012);
        return start + (cycle * 4 + slot012) * beatDur + off * beatDur;
    }

    float TargetX()
    {
        float w = trackRect.rect.width;
        float left = -w * 0.5f + edgePaddingPx;
        return left + targetOffsetFromLeftPx;
    }

    float SpawnX()
    {
        float w = trackRect.rect.width;
        float right = w * 0.5f - edgePaddingPx;
        return right;
    }

    float XForExpectedTime(double expectedTime, double now)
    {
        float targetX = TargetX();
        float spawnX = SpawnX();

        double dt = expectedTime - now;                 // seconds until target moment
        double u = dt / Math.Max(1e-6, travelTimeSec);  // u=1 at spawn, u=0 at target

        // linear motion past the target too (u < 0)
        return targetX + (float)u * (spawnX - targetX);
    }

    // ---------- note spawn / state ----------

    static long Key(int cycle, int slot) => ((long)cycle << 3) | (uint)(slot & 0x7);

    void EnsureNote(int cycle, int slot, double expectedTime)
    {
        long key = Key(cycle, slot);
        if (_notes.ContainsKey(key)) return;

        var img = Instantiate(circlePrefab, trackRect);
        img.name = $"Note_c{cycle}_s{slot}";
        img.color = defaultColor;

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(SpawnX(), laneY);

        RectTransform dotRT = null;
        Image dotImg = null;

        if (showHitDot)
        {
            var dotGO = new GameObject($"HitDot");
            dotGO.transform.SetParent(rt, false);
            dotImg = dotGO.AddComponent<Image>();
            dotImg.color = hitDotColor;

            dotRT = dotImg.rectTransform;
            dotRT.anchorMin = dotRT.anchorMax = new Vector2(0.5f, 0.5f);
            dotRT.pivot = new Vector2(0.5f, 0.5f);
            dotRT.sizeDelta = new Vector2(hitDotSizePx, hitDotSizePx);
            dotRT.anchoredPosition = Vector2.zero;
            dotGO.SetActive(false);
        }

        var note = new NoteVisual
        {
            cycle = cycle,
            slot = slot,
            expectedTime = expectedTime,
            rt = rt,
            img = img,
            dotRT = dotRT,
            dotImg = dotImg
        };

        _notes[key] = note;

        // apply current known state immediately
        ApplyStateToNote(note);
    }

    SlotState[] EnsureCycleState(int cycle)
    {
        if (_cycleStates.TryGetValue(cycle, out var arr)) return arr;
        arr = new SlotState[3];
        _cycleStates[cycle] = arr;
        return arr;
    }

    void ApplyStateToNote(NoteVisual n)
    {
        var arr = EnsureCycleState(n.cycle);

        bool inputCycle = comboRecorder != null ? comboRecorder.IsCycleInputEnabled(n.cycle) : true;

        if (!inputCycle)
        {
            n.img.color = noInputCycleColor;
            n.rt.localScale = Vector3.one * noInputCycleScale;

            if (showHitDot && n.dotRT != null)
                n.dotRT.gameObject.SetActive(false);

            return;
        }

        // input cycle visuals
        if (arr[n.slot].hit) n.img.color = hitColor;
        else if (arr[n.slot].miss) n.img.color = missColor;
        else n.img.color = defaultColor;

        n.rt.localScale = Vector3.one;

        if (showHitDot && n.dotRT != null)
        {
            if (arr[n.slot].hit)
            {
                n.dotRT.gameObject.SetActive(true);
                n.dotRT.anchoredPosition = new Vector2(ErrorToDotOffsetPx(n.slot, arr[n.slot].signedErrorSec), 0f);
            }
            else
            {
                n.dotRT.gameObject.SetActive(false);
            }
        }
    }

    float ErrorToDotOffsetPx(int slot, double signedErrorSec)
    {
        // same mapping idea as your debug UI: normalize inside that slot's early/late window.
        double beatDur = beatClock.BeatDurationSec;

        var w = slot switch
        {
            0 => comboRecorder.beat0Window,
            1 => comboRecorder.beat1Window,
            2 => comboRecorder.beat2Window,
            _ => comboRecorder.beat0Window
        };

        double earlySec = w.earlyBeats * beatDur;
        double lateSec  = w.lateBeats  * beatDur;

        double denom = signedErrorSec < 0 ? Math.Max(1e-6, earlySec) : Math.Max(1e-6, lateSec);
        double norm = signedErrorSec / denom;
        norm = Math.Max(-1.0, Math.Min(1.0, norm));

        return (float)(norm * hitDotMaxOffsetPx);
    }

    // ---------- events from ComboRecorder ----------

    void OnHitRecorded(ComboRecorder.Hit hit)
    {
        if (!beatClock.IsRunning) return;

        //double start = beatClock.StartTime;
        //double beatDur = beatClock.BeatDurationSec;

        //int cycle = (int)Math.Floor((hit.expectedTime - start) / (beatDur * 4.0));
        //if (cycle < 0) return;
        int cycle = hit.cycleIndex;
        if (cycle < 0) return;

        var arr = EnsureCycleState(cycle);
        int slot = hit.beatSlot;

        arr[slot].hit = true;
        arr[slot].miss = false;
        arr[slot].signedErrorSec = hit.signedErrorSec;
        arr[slot].expectedTime = hit.expectedTime;

        // update note if exists
        long key = Key(cycle, slot);
        if (_notes.TryGetValue(key, out var n))
            ApplyStateToNote(n);
    }

    void OnComboReady(ComboRecorder.ComboData combo)
    {
        int cycle = combo.cycleIndex;
        var arr = EnsureCycleState(cycle);

        for (int i = 0; i < 3; i++)
        {
            if (!combo.hits[i].hasInput)
                arr[i].miss = true;
        }

        // update any existing notes for that cycle
        for (int slot = 0; slot < 3; slot++)
        {
            long key = Key(cycle, slot);
            if (_notes.TryGetValue(key, out var n))
                ApplyStateToNote(n);
        }
    }
}
