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
    [Header("Monitor radio cue")]
    public bool playRadioCue = true;
    [Tooltip("Optional replacement sound. Empty uses a short generated radio crackle.")]
    public AudioClip radioCueClip;
    [Range(0, 1)] public float radioCueVolume = 0.25f;
    [Tooltip("Optional speaker position. Empty uses the dialogue text's position on the monitor.")]
    public Transform radioSpeaker;
    [Range(0, 1)] public float radioSpatialBlend = 1f;
    [Header("Outcome dialogue colors")]
    public Color enoughOutcomeColor = new Color(0.15f, 0.85f, 0.2f, 1f);
    public Color notEnoughOutcomeColor = new Color(0.9f, 0.15f, 0.12f, 1f);
    private AudioSource radioSource;
    private AudioClip generatedRadioClip;
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
    public IEnumerator Play(
        DialogueSequence sequence,
        string defaultSpeaker,
        Color? dialogueColor = null)
    {
        if (sequence == null) yield break;
        if (IsRunning) { Debug.LogError("A dialogue sequence is already running.", this); yield break; }
        Color previousColor = dialogueText != null
            ? dialogueText.color
            : Color.white;
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
                        if (dialogueText != null)
                        {
                            if (dialogueColor.HasValue)
                                dialogueText.color = dialogueColor.Value;
                            string nextText = $"{(string.IsNullOrEmpty(step.speaker) ? defaultSpeaker : step.speaker)}\n{step.text}";
                            bool changed = dialogueText.text != nextText;
                            dialogueText.text = nextText;
                            if (changed) PlayRadioCue();
                        }
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
            if (dialogueText != null)
            {
                dialogueText.text = "";
                dialogueText.color = previousColor;
            }
            if (voiceSource != null) voiceSource.Stop();
            waitingFor = null;
            IsRunning = false;
        }
    }

    [ContextMenu("Preview Radio Cue (Play Mode)")]
    public void PlayRadioCue()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || !playRadioCue || radioCueVolume <= 0) return;
        if (radioSource == null)
        {
            var speaker = new GameObject("Dialogue Radio Speaker");
            speaker.transform.SetParent(transform, false);
            radioSource = speaker.AddComponent<AudioSource>();
            radioSource.playOnAwake = false;
            radioSource.loop = false;
            radioSource.dopplerLevel = 0;
            radioSource.rolloffMode = AudioRolloffMode.Linear;
            radioSource.minDistance = 1;
            radioSource.maxDistance = 8;
        }
        UpdateRadioPosition();
        radioSource.spatialBlend = radioSpatialBlend;
        if (radioCueClip == null && generatedRadioClip == null)
            generatedRadioClip = CreateRadioCrackle();
        // Retrigger rather than stacking bursts during rapid dialogue changes.
        radioSource.Stop();
        radioSource.clip = radioCueClip != null ? radioCueClip : generatedRadioClip;
        radioSource.volume = radioCueVolume;
        radioSource.Play();
    }

    private void LateUpdate() => UpdateRadioPosition();
    private void UpdateRadioPosition()
    {
        if (radioSource == null) return;
        Transform origin = radioSpeaker != null ? radioSpeaker : dialogueText != null ? dialogueText.transform : transform;
        radioSource.transform.position = origin.position;
    }

    private static AudioClip CreateRadioCrackle()
    {
        const int rate = 24000;
        const float duration = 0.22f;
        var samples = new float[(int)(rate * duration)];
        var random = new System.Random(7193); // Does not affect gameplay's random state.
        float lowPass = 0, slowPass = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / rate;
            float noise = (float)(random.NextDouble() * 2 - 1);
            lowPass += 0.52f * (noise - lowPass);
            slowPass += 0.055f * (noise - slowPass);
            float envelope = Mathf.Clamp01(t / 0.008f) * Mathf.Clamp01((duration - t) / 0.04f);
            float flutter = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 173) * Mathf.Sin(t * 61));
            samples[i] = (lowPass - slowPass) * flutter * envelope * 0.8f;
        }
        var clip = AudioClip.Create("Generated Monitor Radio Crackle", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDisable()
    {
        if (radioSource != null) radioSource.Stop();
    }
    private void OnDestroy()
    {
        if (generatedRadioClip != null) Destroy(generatedRadioClip);
        if (radioSource != null) Destroy(radioSource.gameObject);
    }
}
