using System;
using System.Collections.Generic;
using UnityEngine;

public enum BeatActionType
{
    Move = 0,
    Attack = 1,
    Parry = 2
}

public enum BeatDirection9
{
    Center = 0,
    N, NE, E, SE, S, SW, W, NW
}

[CreateAssetMenu(menuName = "BeatCombo/Input Binding Map", fileName = "InputBindingMap")]
public class InputBindingMap : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public KeyCode key;

        [Tooltip("One character. Example: A, R, N. If longer, only the first char is used.")]
        public string glyph;

        public BeatActionType type;
        public BeatDirection9 dir;
    }

    public List<Entry> entries = new List<Entry>();

    public IEnumerable<Entry> Entries => entries;
}
