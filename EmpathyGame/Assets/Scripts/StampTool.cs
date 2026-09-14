using UnityEngine;

// Identifies the reusable, ink-free stamper for StampHead.
public class StampTool : MonoBehaviour
{
    [SerializeField] private Renderer stampBaseRenderer;
    [SerializeField] private Color stampColor = Color.white;

    private void Awake()
    {
        if (stampBaseRenderer == null || stampBaseRenderer.sharedMaterial == null) return;
        var block = new MaterialPropertyBlock();
        stampBaseRenderer.GetPropertyBlock(block);
        string property = stampBaseRenderer.sharedMaterial.HasProperty("_BaseColor")
            ? "_BaseColor" : "_Color";
        block.SetColor(property, stampColor);
        stampBaseRenderer.SetPropertyBlock(block);
    }
}
