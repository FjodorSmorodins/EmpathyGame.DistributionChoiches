using UnityEngine;

public class PaperGrabState : MonoBehaviour
{
    public bool IsGrabbed { get; private set; }

    public void MarkGrabbed()
    {
        IsGrabbed = true;
    }

    public void MarkReleased()
    {
        IsGrabbed = false;
    }
}