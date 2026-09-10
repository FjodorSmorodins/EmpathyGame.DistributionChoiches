using UnityEngine;

public class StampHead : MonoBehaviour
{
    [SerializeField] private StampTool stampTool;

    private void Reset()
    {
        stampTool = GetComponentInParent<StampTool>();
    }

    private void Awake()
    {
        if (stampTool == null)
            stampTool = GetComponentInParent<StampTool>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (stampTool == null)
            return;

        if (stampTool.CurrentInk == StampColor.None)
            return;

        PaperDocument paper = other.GetComponentInParent<PaperDocument>();

        if (paper == null)
            return;

        paper.ApplyStamp(stampTool.CurrentInk);
    }
}