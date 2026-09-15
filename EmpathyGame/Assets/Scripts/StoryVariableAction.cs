using UnityEngine;

// Wire Apply() into a hand interaction's UnityEvent to change a story variable.
public class StoryVariableAction : MonoBehaviour
{
    public DialogueRunner story;
    public StoryVariable variable;
    public int value;
    public void Apply() { if (story != null) story.SetValue(variable, value); }
    public void SetValue(int newValue) { value = newValue; Apply(); }
}
