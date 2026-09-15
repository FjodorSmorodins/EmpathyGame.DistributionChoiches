using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaperMoneySlider : MonoBehaviour
{
    [SerializeField] private PaperDocument paper;
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text amountLabel;
    [SerializeField] private string currencySymbol = "€";
    [SerializeField] private Color lockedColor = Color.gray;

    private Graphic[] graphics;
    private Color[] editableColors;
    private Color labelColor;

    public void RepairLayout()
    {
        if (slider == null) slider = GetComponentInChildren<Slider>(true);
        foreach (var rect in GetComponentsInChildren<RectTransform>(true))
        {
            if (rect == transform) continue;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            Vector3 p = rect.localPosition;
            p.z = 0;
            rect.localPosition = p;
        }
        if (transform is RectTransform root) root.sizeDelta = new Vector2(300, 110);
        if (amountLabel != null)
        {
            amountLabel.rectTransform.anchoredPosition = new Vector2(0, 32);
            amountLabel.rectTransform.sizeDelta = new Vector2(280, 42);
            amountLabel.fontSize = 26;
            amountLabel.raycastTarget = false;
        }
        if (slider != null)
        {
            var rect = (RectTransform)slider.transform;
            rect.anchoredPosition = new Vector2(0, -22);
            rect.sizeDelta = new Vector2(260, 40);
            if (slider.handleRect != null)
            {
                slider.handleRect.sizeDelta = new Vector2(32, 40);
                var box = slider.handleRect.GetComponent<BoxCollider>();
                if (box != null)
                {
                    box.isTrigger = true;
                    box.center = Vector3.zero;
                    box.size = new Vector3(40, 48, 24);
                }
            }
        }
    }

    public bool CanEditWithQuill =>
        paper != null &&
        paper.CanEdit &&
        paper.StepCount > 0;

    private void Awake()
    {
        if (paper == null)
            paper = GetComponentInParent<PaperDocument>();

        if (slider == null)
            slider = GetComponentInChildren<Slider>(true);

        graphics = slider != null
            ? slider.GetComponentsInChildren<Graphic>(true)
            : new Graphic[0];

        editableColors = new Color[graphics.Length];

        for (int i = 0; i < graphics.Length; i++)
            editableColors[i] = graphics[i].color;

        if (amountLabel != null)
            labelColor = amountLabel.color;

        if (slider != null)
        {
            // The player cannot manipulate this through normal UI input.
            // The quill controls it instead.
            slider.interactable = false;

            // Prevent Unity's disabled UI tint from changing its appearance.
            slider.transition = Selectable.Transition.None;
        }
    }

    private void OnEnable()
    {
        if (paper == null || slider == null)
        {
            Debug.LogError(
                "PaperMoneySlider needs a PaperDocument and UI Slider.",
                this
            );

            return;
        }

        paper.Changed += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (paper != null)
            paper.Changed -= Refresh;
    }

    /// <summary>
    /// Called by the physical quill interaction.
    /// normalizedPosition should be between 0 and 1.
    /// </summary>
    public void SetNormalizedFromQuill(float normalizedPosition)
    {
        if (!CanEditWithQuill)
            return;

        normalizedPosition =
            Mathf.Clamp01(normalizedPosition);

        int step = Mathf.RoundToInt(
            normalizedPosition * paper.StepCount
        );

        paper.SetAmountStep(step);
    }

    private void Refresh()
    {
        if (paper == null || slider == null)
            return;

        slider.minValue = 0;
        slider.maxValue = paper.StepCount;
        slider.wholeNumbers = true;

        int step =
            paper.AssignedAmount == paper.MaximumAmount
                ? paper.StepCount
                : Mathf.RoundToInt(
                    (float)paper.AssignedAmount /
                    paper.AmountStep
                );

        // Visual only. Doesn't trigger any UI callbacks.
        slider.SetValueWithoutNotify(step);

        // Always disabled from standard Unity UI interaction.
        slider.interactable = false;

        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].color =
                !paper.CanEdit
                    ? lockedColor
                    : editableColors[i];
        }

        if (amountLabel != null)
        {
            amountLabel.text =
                $"{currencySymbol}{paper.AssignedAmount:N0}" +
                (paper.IsStamped ? "  •  Locked" : !paper.CanEdit ? "  •  Waiting" : "");

            amountLabel.color =
                !paper.CanEdit
                    ? lockedColor
                    : labelColor;
        }
    }
}
