using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Attach this to the empty GameObject that sits on top of a pedestal.
/// That GameObject needs a Collider with "Is Trigger" enabled.
/// As soon as a tagged block touches the trigger, it snaps into place.
/// It does NOT move the block while it's being held — that's handled
/// entirely by Meta XR SDK's grab building blocks.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SnapZone : MonoBehaviour
{
    public enum PedestalColor { Red, Green }

    [Header("Setup")]
    [Tooltip("Only objects with this tag will be considered for snapping.")]
    [SerializeField] private string blockTag = "Block";

    [Tooltip("Which pedestal this is. Useful for reading the choice result later.")]
    [SerializeField] private PedestalColor pedestalColor = PedestalColor.Red;

    [Tooltip("Where the block should snap to. Defaults to this transform.")]
    [SerializeField] private Transform snapPoint;

    [Header("On Snap")]
    [Tooltip("If true, the block's Rigidbody is set to kinematic once snapped so it can't be nudged out of place.")]
    [SerializeField] private bool lockInPlaceAfterSnap = true;

    [Tooltip("If true, the block is parented to the snap point after snapping.")]
    [SerializeField] private bool parentToSnapPoint = true;

    public UnityEvent<GameObject> OnBlockSnapped;

    private bool hasSnappedBlock;

    private void Awake()
    {
        if (snapPoint == null)
            snapPoint = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasSnappedBlock) return;
        if (!other.CompareTag(blockTag)) return;

        Rigidbody rb = other.attachedRigidbody;
        Snap(other.transform, rb);
    }

    private void Snap(Transform block, Rigidbody rb)
    {
        hasSnappedBlock = true;

        block.position = snapPoint.position;
        block.rotation = snapPoint.rotation;

        if (parentToSnapPoint)
            block.SetParent(snapPoint, true);

        if (rb != null && lockInPlaceAfterSnap)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        OnBlockSnapped?.Invoke(block.gameObject);
    }

    /// <summary>Which pedestal (red/green) this snap zone represents — read this to resolve the player's choice.</summary>
    public PedestalColor Color => pedestalColor;

    /// <summary>Whether a block has already been snapped here.</summary>
    public bool HasSnappedBlock => hasSnappedBlock;
}
