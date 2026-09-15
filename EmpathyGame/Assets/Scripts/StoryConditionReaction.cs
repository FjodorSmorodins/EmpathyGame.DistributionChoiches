using UnityEngine;
using UnityEngine.Events;

// For reactions which should happen whenever a variable changes, outside a dialogue sequence too.
public class StoryConditionReaction : MonoBehaviour
{
    public DialogueRunner story;
    public StoryVariable variable;
    public int expectedValue = 1;
    public bool checkOnEnable;
    public bool onlyOnce = true;
    public StoryInteraction interaction;
    public UnityEvent onMatched = new UnityEvent();
    private bool fired;
    private void OnEnable()
    {
        if (story == null) return;
        story.VariableChanged += OnChanged;
        if (checkOnEnable && variable != null) OnChanged(variable, story.GetValue(variable));
    }
    private void OnDisable()
    {
        if (story != null) story.VariableChanged -= OnChanged;
    }
    private void OnChanged(StoryVariable changed, int value)
    {
        if (changed != variable || value != expectedValue || onlyOnce && fired) return;
        fired = true;
        if (interaction != null) story.TriggerInteraction(interaction);
        onMatched.Invoke();
    }
}
