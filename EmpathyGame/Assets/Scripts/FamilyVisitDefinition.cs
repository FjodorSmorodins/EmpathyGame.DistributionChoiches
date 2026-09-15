using UnityEngine;

[CreateAssetMenu(menuName = "Empathy Game/Family Visit")]
public class FamilyVisitDefinition : ScriptableObject
{
    public string displayName;
    [Tooltip("Leave empty for a capsule. A custom prefab should have its origin at its feet.")]
    public GameObject representativePrefab;
    public Color capsuleColor = new Color(0.65f, 0.45f, 0.65f);
    [Min(0.01f)] public float walkSpeed = 0.8f;
    [Min(0)] public float arrivalDelay;
    public DialogueSequence onArrival;
    public DialogueSequence afterStamp;
    public DialogueSequence afterReturn;
    [Tooltip("Optional condition that must become true before this visit can start.")]
    public StoryVariable arrivalVariable;
    public int arrivalValue;
}
