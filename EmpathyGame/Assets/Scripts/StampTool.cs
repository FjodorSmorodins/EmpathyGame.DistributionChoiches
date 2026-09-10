using UnityEngine;

public class StampTool : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Renderer stampBaseRenderer;

    [Header("Colors")]
    [SerializeField] private Color noInkColor = Color.white;
    [SerializeField] private Color redColor = Color.red;
    [SerializeField] private Color yellowColor = Color.yellow;
    [SerializeField] private Color greenColor = Color.green;

    public StampColor CurrentInk { get; private set; } = StampColor.None;

    private MaterialPropertyBlock propertyBlock;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        UpdateVisual();
    }

    public void LoadInk(StampColor newColor)
    {
        if (newColor == StampColor.None)
            return;

        CurrentInk = newColor;
        UpdateVisual();
    }

    public void ClearInk()
    {
        CurrentInk = StampColor.None;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (stampBaseRenderer == null)
            return;

        Color targetColor = noInkColor;

        switch (CurrentInk)
        {
            case StampColor.Red:
                targetColor = redColor;
                break;

            case StampColor.Yellow:
                targetColor = yellowColor;
                break;

            case StampColor.Green:
                targetColor = greenColor;
                break;
        }

        stampBaseRenderer.GetPropertyBlock(propertyBlock);

        if (stampBaseRenderer.sharedMaterial.HasProperty(BaseColorID))
        {
            propertyBlock.SetColor(BaseColorID, targetColor);
        }
        else if (stampBaseRenderer.sharedMaterial.HasProperty(ColorID))
        {
            propertyBlock.SetColor(ColorID, targetColor);
        }

        stampBaseRenderer.SetPropertyBlock(propertyBlock);
    }
}