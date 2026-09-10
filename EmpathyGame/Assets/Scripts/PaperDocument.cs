using System;
using UnityEngine;

public class PaperDocument : MonoBehaviour
{
    [Header("Paper Renderer")]
    [SerializeField] private Renderer paperRenderer;

    [Header("Paper Textures")]
    [SerializeField] private Texture unstampedTexture;
    [SerializeField] private Texture redTexture;
    [SerializeField] private Texture yellowTexture;
    [SerializeField] private Texture greenTexture;

    [Header("Settings")]
    [SerializeField] private bool lockAfterFirstStamp = true;

    public bool IsStamped { get; private set; }
    public StampColor CurrentStamp { get; private set; } = StampColor.None;

    public event Action<PaperDocument, StampColor> Stamped;

    private MaterialPropertyBlock propertyBlock;

    private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexID = Shader.PropertyToID("_MainTex");

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        ResetPaper();
    }

    public bool ApplyStamp(StampColor stampColor)
    {
        if (stampColor == StampColor.None)
            return false;

        if (IsStamped && lockAfterFirstStamp)
            return false;

        CurrentStamp = stampColor;
        IsStamped = true;

        switch (stampColor)
        {
            case StampColor.Red:
                SetTexture(redTexture);
                break;

            case StampColor.Yellow:
                SetTexture(yellowTexture);
                break;

            case StampColor.Green:
                SetTexture(greenTexture);
                break;
        }

        Stamped?.Invoke(this, stampColor);

        return true;
    }

    public void ResetPaper()
    {
        IsStamped = false;
        CurrentStamp = StampColor.None;

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        SetTexture(unstampedTexture);
    }

    private void SetTexture(Texture texture)
    {
        if (paperRenderer == null || texture == null)
            return;

        paperRenderer.GetPropertyBlock(propertyBlock);

        if (paperRenderer.sharedMaterial.HasProperty(BaseMapID))
        {
            propertyBlock.SetTexture(BaseMapID, texture);
        }
        else if (paperRenderer.sharedMaterial.HasProperty(MainTexID))
        {
            propertyBlock.SetTexture(MainTexID, texture);
        }

        paperRenderer.SetPropertyBlock(propertyBlock);
    }
}