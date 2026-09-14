using UnityEngine;

public class FamilyRepresentative : MonoBehaviour
{
    public enum RequestedAmountLevel
    {
        Small,
        Average,
        Large
    }

    public enum SituationImportance
    {
        Low,
        Medium,
        High
    }

    [Header("Family information")]
    public string familyName;

    [TextArea(4, 10)]
    public string situationDescription;

    [Header("Need")]
    public RequestedAmountLevel requestedAmountLevel;
    public SituationImportance situationImportance;

    [Tooltip("How strongly this family's need affects the final score.")]
    [Min(0.1f)]
    public float importanceWeight = 1f;

    [Tooltip(
        "Minimum percentage of the requested amount required before the money is useful. " +
        "Use 0 for gradual needs and 1 for all-or-nothing needs."
    )]
    [Range(0f, 1f)]
    public float minimumUsefulFraction = 0f;

    [Header("Reaction")]
    [Tooltip(
        "If enabled, the family can understand why a more important need received priority."
    )]
    public bool hasUnderstanding;

    [Header("Current result")]
    public int allocatedAmount;

    private void OnMouseDown()
    {
        Debug.Log("Clicked representative: " + familyName);

        ResourceDistributionManager.Instance.SelectFamily(this);
    }
}