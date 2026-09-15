using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Empathy Game/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    public enum StepKind { Line, SetVariable, Interaction, WaitForVariable, Delay }
    [Serializable]
    public class Step
    {
        public StepKind kind;
        public string speaker;
        [TextArea(2, 6)] public string text;
        public AudioClip voice;
        [Min(0)] public float seconds = 4;
        [Tooltip("For Line: wait for AdvanceDialogue() from a hand interaction after the minimum duration.")]
        public bool waitForAdvance;
        public StoryVariable variable;
        public int value;
        public StoryInteraction interaction;
        [Tooltip("Wait until CompleteInteraction(the same asset) is called.")]
        public bool waitForInteraction;
        [Header("Optional condition: skip unless this variable equals this value")]
        public StoryVariable conditionVariable;
        public int conditionValue;
    }
    public Step[] steps = new Step[0];
}
