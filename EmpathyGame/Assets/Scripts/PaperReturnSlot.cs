using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PaperReturnSlot : MonoBehaviour
{
    [Min(0)] public float settleSeconds = 0.25f;
    [Tooltip("Extra height above the slot where the hand may release the paper.")]
    [Min(0)] public float releaseHeightAllowance = 0.25f;
    [Min(0)] public float releaseSideAllowance = 0.05f;
    [SerializeField] private string returnStatus = "Waiting for stamp";
    public string Status => returnStatus;
    private BoxCollider volume;
    private PaperDocument paper;
    private PaperGrabState grab;
    private ImportantObjectRecovery recovery;
    private int recoveryAtArm;
    private readonly ReturnSubmissionGate gate = new ReturnSubmissionGate();
    private bool ready;
    private int releaseAtArm;
    public bool IsReady => ready && grab != null && !grab.IsGrabbed && grab.HasDeliberateRelease &&
        paper != null && Contains(paper.transform.position) &&
        (recovery == null || recovery.RecoveryVersion == recoveryAtArm);
    public void Arm(PaperDocument document)
    {
        paper = document;
        grab = paper.GetComponent<PaperGrabState>();
        recovery = paper.GetComponent<ImportantObjectRecovery>();
        volume = GetComponent<BoxCollider>();
        recoveryAtArm = recovery != null ? recovery.RecoveryVersion : 0;
        gate.Arm(Contains(paper.transform.position), grab != null ? grab.ReleaseVersion : 0);
        releaseAtArm = grab != null ? grab.ReleaseVersion : 0;
        ready = false;
        returnStatus = "Place and release the stamped paper in its corresponding slot.";
    }
    private bool ReleasedNearSlot(Vector3 point)
    {
        // World-space allowance stays in metres regardless of the slot's scale.
        Vector3 scale = transform.lossyScale;
        Vector3 p = transform.InverseTransformPoint(point) - volume.center;
        Vector3 half = Vector3.Scale(volume.size * 0.5f,
            new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        p = Vector3.Scale(p, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        return Mathf.Abs(p.x) <= half.x + releaseSideAllowance &&
            Mathf.Abs(p.z) <= half.z + releaseSideAllowance &&
            p.y >= -half.y && p.y <= half.y + releaseHeightAllowance;
    }
    public bool Contains(Vector3 point)
    {
        if (volume == null) volume = GetComponent<BoxCollider>();
        Vector3 p = transform.InverseTransformPoint(point) - volume.center;
        Vector3 h = volume.size * 0.5f;
        return Mathf.Abs(p.x) <= h.x && Mathf.Abs(p.y) <= h.y && Mathf.Abs(p.z) <= h.z;
    }
    private void Update()
    {
        if (paper == null || grab == null || !paper.IsStamped) return;
        if (recovery != null && recovery.RecoveryVersion != recoveryAtArm)
        {
            Arm(paper); // A teleport back to the table must never submit a paper.
            return;
        }
        bool inside = Contains(paper.transform.position);
        bool releasedNear = ReleasedNearSlot(grab.ReleasePosition);
        ready = gate.Tick(inside, grab.IsGrabbed, grab.HasDeliberateRelease, releasedNear,
            grab.ReleaseVersion, Time.deltaTime, settleSeconds);
        returnStatus = grab.IsGrabbed ? "Place the paper in its corresponding slot and release your hand." :
            !grab.HasDeliberateRelease || grab.ReleaseVersion <= releaseAtArm ? "Pick up the stamped paper and place it back in its corresponding slot." :
            !releasedNear ? "Release the paper over its corresponding slot." :
            !inside ? "Move the paper farther into its corresponding slot." :
            !ready ? "Paper received; waiting for it to settle." : "Paper returned.";
    }
    private void Reset() => GetComponent<BoxCollider>().isTrigger = true;
    private void OnDrawGizmosSelected()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
