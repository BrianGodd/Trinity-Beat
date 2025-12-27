using UnityEngine;

public class ComboCaster : MonoBehaviour
{
    public ComboRecorder comboRecorder;

    public bool logCast = true;

    public ComboRecorder.ComboData LastCast { get; private set; }

    private void OnEnable()
    {
        if (comboRecorder != null)
            comboRecorder.OnComboReady += HandleCombo;
    }

    private void OnDisable()
    {
        if (comboRecorder != null)
            comboRecorder.OnComboReady -= HandleCombo;
    }

    private void HandleCombo(ComboRecorder.ComboData combo)
    {
        LastCast = combo;

        if (!logCast) return;

        Debug.Log($"[CAST] cycle={combo.cycleIndex} pattern={combo.patternName} word='{combo.WordString}'");

        for (int i = 0; i < 3; i++)
        {
            var h = combo.hits[i];
            if (!h.hasInput)
            {
                Debug.Log($"  Hit{i}: MISS");
                continue;
            }

            Debug.Log($"  Hit{i}: glyph='{h.glyph}' type={h.type} dir={h.dir} " +
                      $"err={h.signedErrorSec:+0.000;-0.000;+0.000}s ({h.signedErrorMs:+0.0;-0.0;+0.0}ms)");
        }
    }
}
