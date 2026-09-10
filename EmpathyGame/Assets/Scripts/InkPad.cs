using UnityEngine;

public class InkPad : MonoBehaviour
{
    [SerializeField] private StampColor inkColor;

    private void OnTriggerEnter(Collider other)
    {
        StampTool stamp = other.GetComponentInParent<StampTool>();

        if (stamp == null)
            return;

        stamp.LoadInk(inkColor);
    }
}