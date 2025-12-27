using UnityEngine;

[CreateAssetMenu(menuName = "BeatCombo/Timing Pattern", fileName = "TimingPattern")]
public class TimingPattern : ScriptableObject
{
    public string patternName = "Default";

    [Range(0f, 0.999f)] public float beat0ExpectedOffset = 0f;
    [Range(0f, 0.999f)] public float beat1ExpectedOffset = 0f;
    [Range(0f, 0.999f)] public float beat2ExpectedOffset = 0f;

    public float GetExpectedOffset012(int slot012)
    {
        return slot012 switch
        {
            0 => beat0ExpectedOffset,
            1 => beat1ExpectedOffset,
            2 => beat2ExpectedOffset,
            _ => 0f
        };
    }
}
