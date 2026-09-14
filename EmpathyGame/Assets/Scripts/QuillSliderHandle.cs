using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class QuillSliderHandle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PaperMoneySlider moneySlider;
    [SerializeField] private Slider slider;

    [Tooltip(
        "Usually the Handle Slide Area RectTransform. " +
        "If left empty, it will be found automatically."
    )]
    [SerializeField] private RectTransform slideArea;

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages;

    private Collider activeQuillTip;

    private void Awake()
    {
        if (slider == null)
            slider = GetComponentInParent<Slider>();

        if (moneySlider == null)
            moneySlider =
                GetComponentInParent<PaperMoneySlider>();

        if (slideArea == null &&
            slider != null &&
            slider.handleRect != null)
        {
            slideArea =
                slider.handleRect.parent as RectTransform;
        }

        Collider col = GetComponent<Collider>();

        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning(
                "QuillSliderHandle collider should have Is Trigger enabled.",
                this
            );
        }

        if (slideArea == null)
        {
            Debug.LogError(
                "QuillSliderHandle could not find the Handle Slide Area.",
                this
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Already being controlled by another quill tip.
        if (activeQuillTip != null)
            return;

        // ONLY the specifically marked quill tip can activate us.
        if (other.GetComponent<QuillTip>() == null)
            return;

        if (moneySlider == null ||
            !moneySlider.CanEditWithQuill)
            return;

        activeQuillTip = other;

        if (showDebugMessages)
            Debug.Log("Quill selected slider handle.", this);

        UpdateSliderFromQuill();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other != activeQuillTip)
            return;

        UpdateSliderFromQuill();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != activeQuillTip)
            return;

        if (showDebugMessages)
            Debug.Log("Quill released slider handle.", this);

        activeQuillTip = null;
    }

    private void UpdateSliderFromQuill()
    {
        if (activeQuillTip == null ||
            moneySlider == null ||
            slider == null ||
            slideArea == null)
            return;

        if (!moneySlider.CanEditWithQuill)
        {
            activeQuillTip = null;
            return;
        }

        // Get the physical center of the quill tip collider.
        Vector3 worldTipPosition =
            activeQuillTip.bounds.center;

        // Convert it into slider-local coordinates.
        Vector3 localPosition =
            slideArea.InverseTransformPoint(
                worldTipPosition
            );

        Rect rect = slideArea.rect;

        float normalized;

        switch (slider.direction)
        {
            case Slider.Direction.LeftToRight:

                normalized = Mathf.InverseLerp(
                    rect.xMin,
                    rect.xMax,
                    localPosition.x
                );

                break;

            case Slider.Direction.RightToLeft:

                normalized = 1f - Mathf.InverseLerp(
                    rect.xMin,
                    rect.xMax,
                    localPosition.x
                );

                break;

            case Slider.Direction.BottomToTop:

                normalized = Mathf.InverseLerp(
                    rect.yMin,
                    rect.yMax,
                    localPosition.y
                );

                break;

            case Slider.Direction.TopToBottom:

                normalized = 1f - Mathf.InverseLerp(
                    rect.yMin,
                    rect.yMax,
                    localPosition.y
                );

                break;

            default:

                normalized = 0f;

                break;
        }

        moneySlider.SetNormalizedFromQuill(
            Mathf.Clamp01(normalized)
        );
    }

    private void OnDisable()
    {
        activeQuillTip = null;
    }
}