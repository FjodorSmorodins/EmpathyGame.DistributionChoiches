using Oculus.Interaction;
using UnityEngine;

[RequireComponent(typeof(Grabbable))]
public class StartGameOnInstructionsGrab : MonoBehaviour
{
    private FamilyDeskManager deskManager;
    private Grabbable grabbable;
    private bool started;

    public static bool TryInstall(FamilyDeskManager manager)
    {
        GameObject instructions = GameObject.Find("Instructions");
        Grabbable target = instructions != null
            ? instructions.GetComponent<Grabbable>()
            : null;

        if (target == null)
        {
            Debug.LogWarning(
                "The game could not wait for instructions because an active " +
                "Grabbable named 'Instructions' was not found.",
                manager
            );
            return false;
        }

        StartGameOnInstructionsGrab trigger =
            instructions.GetComponent<StartGameOnInstructionsGrab>();
        if (trigger == null)
            trigger = instructions.AddComponent<StartGameOnInstructionsGrab>();

        trigger.deskManager = manager;
        return true;
    }

    private void Awake()
    {
        grabbable = GetComponent<Grabbable>();
    }

    private void OnEnable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
    }

    private void OnDisable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
    }

    private void HandlePointerEvent(PointerEvent pointerEvent)
    {
        if (started || pointerEvent.Type != PointerEventType.Select)
            return;

        started = true;
        deskManager.StartNextFamily();
        enabled = false;
    }
}
