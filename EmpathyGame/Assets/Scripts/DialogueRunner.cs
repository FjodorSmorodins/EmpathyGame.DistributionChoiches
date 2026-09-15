using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class DialogueRunner : MonoBehaviour
{
    [Serializable]
    public class InteractionBinding
    {
        public StoryInteraction interaction;
        public UnityEvent onTriggered = new UnityEvent();
    }
    public TMP_Text dialogueText;
    public TMP_Text statusText;
    public AudioSource voiceSource;
    public InteractionBinding[] interactions = new InteractionBinding[0];
    private readonly Dictionary<StoryVariable, int> values = new Dictionary<StoryVariable, int>();
    private StoryInteraction waitingFor;
    private bool advance;
    public bool IsRunning { get; private set; }
    public event Action<StoryVariable, int> VariableChanged;
    public int GetValue(StoryVariable variable) => variable == null ? 0 :
        values.TryGetValue(variable, out int value) ? value : variable.initialValue;
    public void SetValue(StoryVariable variable, int value)
    {
        if (variable == null || GetValue(variable) == value) return;
        values[variable] = value;
        VariableChanged?.Invoke(variable, value);
    }
    public void ResetStory() => values.Clear(); // Never edits the asset.
    public void AdvanceDialogue() => advance = true;
    public void CompleteInteraction(StoryInteraction interaction)
    {
        if (waitingFor == interaction) waitingFor = null;
    }
    public void TriggerInteraction(StoryInteraction interaction) => InvokeInteraction(interaction);
    private bool InvokeInteraction(StoryInteraction interaction)
    {
        bool found = false;
        foreach (var binding in interactions)
            if (binding != null && interaction != null && binding.interaction == interaction)
            { found = true; binding.onTriggered.Invoke(); }
        return found;
    }
    public void SetStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }
    public IEnumerator Play(DialogueSequence sequence, string defaultSpeaker)
    {
        if (sequence == null) yield break;
        if (IsRunning) { Debug.LogError("A dialogue sequence is already running.", this); yield break; }
        IsRunning = true;
        try
        {
            foreach (var step in sequence.steps)
            {
                if (step == null || step.conditionVariable != null &&
                    GetValue(step.conditionVariable) != step.conditionValue) continue;
                switch (step.kind)
                {
                    case DialogueSequence.StepKind.Line:
                        advance = false;
                        if (dialogueText != null) dialogueText.text =
                            $"{(string.IsNullOrEmpty(step.speaker) ? defaultSpeaker : step.speaker)}\n{step.text}";
                        if (voiceSource != null && step.voice != null)
                        {
                            voiceSource.clip = step.voice;
                            voiceSource.Play();
                        }
                        yield return new WaitForSeconds(Mathf.Max(step.seconds, step.voice != null ? step.voice.length : 0));
                        if (step.waitForAdvance) yield return new WaitUntil(() => advance);
                        break;
                    case DialogueSequence.StepKind.SetVariable:
                        SetValue(step.variable, step.value);
                        break;
                    case DialogueSequence.StepKind.WaitForVariable:
                        if (step.variable == null) Debug.LogError("WaitForVariable needs a variable.", sequence);
                        else yield return new WaitUntil(() => GetValue(step.variable) == step.value);
                        break;
                    case DialogueSequence.StepKind.Interaction:
                        waitingFor = step.waitForInteraction ? step.interaction : null;
                        bool found = InvokeInteraction(step.interaction);
                        if (!found)
                        {
                            Debug.LogWarning($"No scene binding for interaction in {sequence.name}; skipping it.", this);
                            waitingFor = null;
                        }
                        yield return new WaitUntil(() => waitingFor == null);
                        break;
                    case DialogueSequence.StepKind.Delay:
                        yield return new WaitForSeconds(Mathf.Max(0, step.seconds));
                        break;
                }
            }
        }
        finally
        {
            if (dialogueText != null) dialogueText.text = "";
            if (voiceSource != null) voiceSource.Stop();
            waitingFor = null;
            IsRunning = false;
        }
    }
}
