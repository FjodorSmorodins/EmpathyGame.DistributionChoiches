using Oculus.Interaction;
using UnityEngine;

public class PaperGrabState : MonoBehaviour
{
    private Grabbable grabbable;
    public bool IsGrabbed { get; private set; }
    public int ReleaseVersion { get; private set; }
    public Vector3 ReleasePosition { get; private set; }
    public bool HasDeliberateRelease { get; private set; }
    private void OnEnable()
    {
        grabbable = GetComponent<Grabbable>();
        if (grabbable != null) grabbable.WhenPointerEventRaised += OnPointer;
    }
    private void OnDisable()
    {
        if (grabbable != null) grabbable.WhenPointerEventRaised -= OnPointer;
        IsGrabbed = false;
        HasDeliberateRelease = false;
    }
    private void OnPointer(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select) MarkGrabbed();
        else if (evt.Type == PointerEventType.Unselect && grabbable.SelectingPointsCount == 0)
            MarkReleased();
        else if (evt.Type == PointerEventType.Cancel && grabbable.SelectingPointsCount == 0)
        {
            IsGrabbed = false;
            HasDeliberateRelease = false;
        }
    }

    public void MarkGrabbed()
    {
        IsGrabbed = true;
        HasDeliberateRelease = false;
    }

    public void MarkReleased()
    {
        if (!IsGrabbed) return;
        IsGrabbed = false;
        ReleasePosition = transform.position;
        ReleaseVersion++;
        HasDeliberateRelease = true;
    }
}
