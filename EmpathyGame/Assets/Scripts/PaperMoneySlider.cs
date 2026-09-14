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

    private void Awake()
    {
        if (paper == null) paper = GetComponentInParent<PaperDocument>();
        if (slider == null) slider = GetComponentInChildren<Slider>(true);
        if (paper != null && slider != null)
        {
            var guard = slider.GetComponent<PaperSliderGrabGuard>();
            if (guard == null) guard = slider.gameObject.AddComponent<PaperSliderGrabGuard>();
            guard.Initialize(paper);
        }
        graphics = slider != null ? slider.GetComponentsInChildren<Graphic>(true) : new Graphic[0];
        editableColors = new Color[graphics.Length];
        for (int i = 0; i < graphics.Length; i++) editableColors[i] = graphics[i].color;
        if (amountLabel != null) labelColor = amountLabel.color;
    }

    private void OnEnable()
    {
        if (paper == null || slider == null)
        {
            Debug.LogError("PaperMoneySlider needs a PaperDocument and UI Slider.", this);
            return;
        }
        paper.Changed += Refresh;
        slider.onValueChanged.AddListener(OnSliderChanged);
        Refresh();
    }

    private void OnDisable()
    {
        if (paper != null) paper.Changed -= Refresh;
        if (slider != null) slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        paper.SetAmountStep(Mathf.RoundToInt(value));
        // Restore the display if a late drag event arrives after stamping.
        Refresh();
    }

    private void Refresh()
    {
        // Configure without triggering callbacks while limits are changing.
        slider.onValueChanged.RemoveListener(OnSliderChanged);
        slider.minValue = 0;
        slider.maxValue = paper.StepCount;
        slider.wholeNumbers = true;
        int step = paper.AssignedAmount == paper.MaximumAmount ? paper.StepCount
            : Mathf.RoundToInt((float)paper.AssignedAmount / paper.AmountStep);
        slider.SetValueWithoutNotify(step);
        slider.onValueChanged.AddListener(OnSliderChanged);
        slider.interactable = !paper.IsStamped && paper.StepCount > 0;
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].color = paper.IsStamped ? lockedColor : editableColors[i];
        if (amountLabel != null)
        {
            amountLabel.text = $"{currencySymbol}{paper.AssignedAmount:N0}" +
                (paper.IsStamped ? "  •  Locked" : "");
            amountLabel.color = paper.IsStamped ? lockedColor : labelColor;
        }
    }
}
