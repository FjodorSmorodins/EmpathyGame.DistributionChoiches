using Oculus.Interaction;
using UnityEngine;

public class PaperGrabState : MonoBehaviour
{
    [Header("Generated pickup sound (papers and stamper)")]
    [Range(0, 1)] public float pickupVolume = 0.4f;
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
        bool firstHand = !IsGrabbed;
        IsGrabbed = true;
        HasDeliberateRelease = false;
        if (!firstHand || pickupVolume <= 0) return;
        if (GetComponent<PaperDocument>() != null)
            GeneratedDeskAudio.For(gameObject).Play(GeneratedDeskAudio.Sound.PaperPickup, pickupVolume);
        else if (GetComponent<StampTool>() != null)
            GeneratedDeskAudio.For(gameObject).Play(GeneratedDeskAudio.Sound.StamperPickup, pickupVolume);
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
