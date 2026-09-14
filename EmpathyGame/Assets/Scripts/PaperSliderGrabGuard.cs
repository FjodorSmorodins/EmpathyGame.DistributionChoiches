using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.EventSystems;

// UI hover happens before the press/drag: block paper grabs before that press.
public class PaperSliderGrabGuard : MonoBehaviour, IPointerEnterHandler,
    IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IEndDragHandler
{
    private readonly HashSet<int> hovering = new HashSet<int>();
    private readonly HashSet<int> pressed = new HashSet<int>();
    private readonly HashSet<int> dragging = new HashSet<int>();
    private readonly List<Behaviour> suppressed = new List<Behaviour>();
    private Behaviour[] grabTargets;
    private Grabbable grabbable;

    public void Initialize(PaperDocument paper)
    {
        Restore();
        grabbable = paper.GetComponent<Grabbable>();
        var targets = new List<Behaviour>();
        foreach (var component in paper.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component.GetComponentInParent<PaperDocument>() != paper) continue;
            if (component is GrabInteractable || component is HandGrabInteractable ||
                component is DistanceGrabInteractable || component is DistanceHandGrabInteractable)
                targets.Add(component);
        }
        grabTargets = targets.ToArray();
    }

    public void OnPointerEnter(PointerEventData e) { hovering.Add(e.pointerId); Refresh(); }
    public void OnPointerExit(PointerEventData e) { hovering.Remove(e.pointerId); Refresh(); }
    public void OnPointerDown(PointerEventData e) { pressed.Add(e.pointerId); Refresh(); }
    public void OnPointerUp(PointerEventData e) { pressed.Remove(e.pointerId); Refresh(); }
    public void OnBeginDrag(PointerEventData e) { dragging.Add(e.pointerId); Refresh(); }
    public void OnEndDrag(PointerEventData e) { dragging.Remove(e.pointerId); Refresh(); }

    private void Update() => Refresh();

    private void Refresh()
    {
        bool usingSlider = hovering.Count > 0 || pressed.Count > 0 || dragging.Count > 0;
        if (!usingSlider) { Restore(); return; }
        // Do not forcibly release a paper held in the other hand.
        if (grabTargets == null || (grabbable != null && grabbable.SelectingPointsCount > 0)) return;
        foreach (var target in grabTargets)
        {
            if (target == null || !target.enabled) continue;
            if (!suppressed.Contains(target)) suppressed.Add(target);
            target.enabled = false;
        }
    }

    private void Restore()
    {
        foreach (var target in suppressed)
            if (target != null) target.enabled = true;
        suppressed.Clear();
    }

    private void OnDisable()
    {
        hovering.Clear();
        pressed.Clear();
        dragging.Clear();
        Restore();
    }
}
