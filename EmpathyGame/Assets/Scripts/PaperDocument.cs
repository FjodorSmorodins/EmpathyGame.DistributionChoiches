using System;
using UnityEngine;

public class PaperDocument : MonoBehaviour
{
    [Header("Paper Appearance")]
    [SerializeField] private Renderer paperRenderer;
    [SerializeField] private Texture unstampedTexture;
    [Tooltip("Optional neutral stamped version of this paper.")]
    [SerializeField] private Texture stampedTexture;

    [Header("Money Allocation")]
    [Min(0)] [SerializeField] private int maximumAmount = 3000;
    [Min(1)] [SerializeField] private int amountStep = 100;
    [Min(0)] [SerializeField] private int startingAmount;

    public bool IsStamped { get; private set; }
    public int AssignedAmount { get; private set; }
    public int MaximumAmount => Mathf.Max(0, maximumAmount);
    public int AmountStep => Mathf.Max(1, amountStep);
    public int StepCount => Mathf.CeilToInt((float)MaximumAmount / AmountStep);

    public event Action Changed;
    public event Action<PaperDocument, int> Stamped;

    private MaterialPropertyBlock propertyBlock;
    private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexID = Shader.PropertyToID("_MainTex");

    private void Awake() => ResetPaper();

    public int AmountAtStep(int step)
    {
        return (int)Math.Min((long)Mathf.Clamp(step, 0, StepCount) * AmountStep, MaximumAmount);
    }

    public bool SetAmountStep(int step)
    {
        // Enforce the lock in the data as well as the UI.
        if (IsStamped) return false;
        AssignedAmount = AmountAtStep(step);
        Changed?.Invoke();
        return true;
    }

    public bool ApplyStamp()
    {
        if (IsStamped) return false;
        IsStamped = true;
        SetTexture(stampedTexture);
        Changed?.Invoke();
        Stamped?.Invoke(this, AssignedAmount);
        return true;
    }

    public void ResetPaper()
    {
        IsStamped = false;
        int initial = Mathf.Clamp(startingAmount, 0, MaximumAmount);
        AssignedAmount = initial == MaximumAmount ? MaximumAmount
            : AmountAtStep(Mathf.RoundToInt((float)initial / AmountStep));
        SetTexture(unstampedTexture);
        Changed?.Invoke();
    }

    private void SetTexture(Texture texture)
    {
        if (paperRenderer == null || texture == null || paperRenderer.sharedMaterial == null)
            return;
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        paperRenderer.GetPropertyBlock(propertyBlock);
        if (paperRenderer.sharedMaterial.HasProperty(BaseMapID))
            propertyBlock.SetTexture(BaseMapID, texture);
        else if (paperRenderer.sharedMaterial.HasProperty(MainTexID))
            propertyBlock.SetTexture(MainTexID, texture);
        paperRenderer.SetPropertyBlock(propertyBlock);
    }
}
