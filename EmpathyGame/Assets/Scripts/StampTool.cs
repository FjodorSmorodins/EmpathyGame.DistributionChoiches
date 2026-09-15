using UnityEngine;

// Identifies the reusable, ink-free stamper for StampHead.
public class StampTool : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Renderer stampBaseRenderer;
    [SerializeField] private Color stampColor = Color.white;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string stampTriggerName = "Stamp";
    [SerializeField] private bool requireHeld = true;
    private PaperGrabState grabState;
    public bool CanStamp => !requireHeld || (grabState != null && grabState.IsGrabbed);

    private int stampTriggerHash;

    private void Awake()
    {
        grabState = GetComponent<PaperGrabState>();
        if (grabState == null) grabState = gameObject.AddComponent<PaperGrabState>();
        if (stampBaseRenderer != null &&
            stampBaseRenderer.sharedMaterial != null)
        {
            var block = new MaterialPropertyBlock();

            stampBaseRenderer.GetPropertyBlock(block);

            string property =
                stampBaseRenderer.sharedMaterial.HasProperty("_BaseColor")
                    ? "_BaseColor"
                    : "_Color";

            block.SetColor(property, stampColor);

            stampBaseRenderer.SetPropertyBlock(block);
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        stampTriggerHash =
            Animator.StringToHash(stampTriggerName);
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }

    public void PlayStampAnimation()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning(
                "StampTool has no Animator assigned.",
                this
            );

            return;
        }

        animator.ResetTrigger(stampTriggerHash);
        animator.SetTrigger(stampTriggerHash);

    }
}
