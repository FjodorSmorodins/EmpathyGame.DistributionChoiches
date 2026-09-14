using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceDistributionManager : MonoBehaviour
{
    public static ResourceDistributionManager Instance { get; private set; }

    [Header("Budget")]
    public int startingBudget = 5500;
    public int allocationStep = 100;

    [Header("Requested amount values")]
    public int smallRequestAmount = 1000;
    public int averageRequestAmount = 2000;
    public int largeRequestAmount = 3000;

    [Header("Families")]
    public List<FamilyRepresentative> families;

    [Header("UI")]
    public TMP_Text remainingBudgetText;
    public TMP_Text familyNameText;
    public TMP_Text descriptionText;
    public TMP_Text requestedAmountText;
    public TMP_Text allocatedAmountText;
    public TMP_Text resultText;

    public Button addMoneyButton;
    public Button removeMoneyButton;
    public Button finishButton;

    private int remainingBudget;
    private FamilyRepresentative selectedFamily;
    private bool distributionFinished;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        remainingBudget = startingBudget;
        distributionFinished = false;

        // Reset values whenever a new game begins.
        foreach (FamilyRepresentative family in families)
        {
            family.allocatedAmount = 0;
        }

        familyNameText.text = "Select a family";
        descriptionText.text =
            "Click one of the representatives to read their situation.";

        requestedAmountText.text = "";
        allocatedAmountText.text = "";
        resultText.text = "";

        UpdateInterface();
    }

    public int GetRequestedAmount(FamilyRepresentative family)
    {
        switch (family.requestedAmountLevel)
        {
            case FamilyRepresentative.RequestedAmountLevel.Small:
                return smallRequestAmount;

            case FamilyRepresentative.RequestedAmountLevel.Average:
                return averageRequestAmount;

            case FamilyRepresentative.RequestedAmountLevel.Large:
                return largeRequestAmount;

            default:
                return 0;
        }
    }

    public void SelectFamily(FamilyRepresentative family)
    {
        if (distributionFinished)
        {
            return;
        }

        selectedFamily = family;

        int requestedAmount = GetRequestedAmount(family);

        familyNameText.text = family.familyName;
        descriptionText.text = family.situationDescription;

        requestedAmountText.text =
            $"Requested: {family.requestedAmountLevel}\n";

        resultText.text = "";

        UpdateInterface();
    }

    public void AddMoney()
    {
        if (distributionFinished)
        {
            return;
        }

        if (selectedFamily == null)
        {
            resultText.text = "Select a family first.";
            return;
        }

        if (remainingBudget <= 0)
        {
            resultText.text = "No money remains.";
            return;
        }

        int requestedAmount = GetRequestedAmount(selectedFamily);

        if (selectedFamily.allocatedAmount >= requestedAmount)
        {
            resultText.text =
                "This family's requested amount is already fulfilled.";
            return;
        }

        int missingAmount =
            requestedAmount - selectedFamily.allocatedAmount;

        int amountToAdd =
            Mathf.Min(allocationStep, remainingBudget, missingAmount);

        selectedFamily.allocatedAmount += amountToAdd;
        remainingBudget -= amountToAdd;

        resultText.text = "";
        UpdateInterface();
    }

    public void RemoveMoney()
    {
        if (distributionFinished)
        {
            return;
        }

        if (selectedFamily == null)
        {
            resultText.text = "Select a family first.";
            return;
        }

        if (selectedFamily.allocatedAmount <= 0)
        {
            resultText.text =
                "This family has no allocation to remove.";
            return;
        }

        int amountToRemove =
            Mathf.Min(allocationStep, selectedFamily.allocatedAmount);

        selectedFamily.allocatedAmount -= amountToRemove;
        remainingBudget += amountToRemove;

        resultText.text = "";
        UpdateInterface();
    }

    public void FinishDistribution()
    {
        if (distributionFinished)
        {
            return;
        }

        distributionFinished = true;

        int distributionScore = CalculateDistributionScore();

        string summary =
            "Distribution complete\n\n" +
            $"Score: {distributionScore}/100\n\n";

        foreach (FamilyRepresentative family in families)
        {
            int requestedAmount = GetRequestedAmount(family);

            float fulfillment =
                requestedAmount > 0
                    ? (float)family.allocatedAmount / requestedAmount
                    : 0f;

            int fulfillmentPercentage =
                Mathf.RoundToInt(
                    Mathf.Clamp01(fulfillment) * 100f
                );

            summary +=
                $"{family.familyName}: " +
                $"${family.allocatedAmount} / ${requestedAmount} " +
                $"({fulfillmentPercentage}%)\n";

            summary += GetFamilyReaction(family, fulfillment) + "\n\n";
        }

        summary += $"Unallocated money: ${remainingBudget}";

        // Hide the allocation interface.
        remainingBudgetText.gameObject.SetActive(false);
        familyNameText.gameObject.SetActive(false);
        descriptionText.gameObject.SetActive(false);
        requestedAmountText.gameObject.SetActive(false);
        allocatedAmountText.gameObject.SetActive(false);

        addMoneyButton.gameObject.SetActive(false);
        removeMoneyButton.gameObject.SetActive(false);
        finishButton.gameObject.SetActive(false);

        resultText.gameObject.SetActive(true);
        resultText.text = summary;

        Debug.Log(summary);
    }

    private int CalculateDistributionScore()
    {
        float earnedScore = 0f;
        float maximumScore = 0f;

        foreach (FamilyRepresentative family in families)
        {
            int requestedAmount = GetRequestedAmount(family);

            if (requestedAmount <= 0)
            {
                continue;
            }

            float fulfillment =
                Mathf.Clamp01(
                    (float)family.allocatedAmount / requestedAmount
                );

            float effectiveFulfillment = fulfillment;

            // For all-or-nothing needs, insufficient money gives no benefit.
            if (fulfillment < family.minimumUsefulFraction)
            {
                effectiveFulfillment = 0f;
            }

            earnedScore +=
                effectiveFulfillment * family.importanceWeight;

            maximumScore += family.importanceWeight;
        }

        if (maximumScore <= 0f)
        {
            return 0;
        }

        return Mathf.RoundToInt(
            earnedScore / maximumScore * 100f
        );
    }

    private string GetFamilyReaction(
        FamilyRepresentative family,
        float fulfillment
    )
    {
        if (fulfillment >= 1f)
        {
            return "Their requested amount was fulfilled.";
        }

        if (family.hasUnderstanding)
        {
            return
                "Their need was not completely fulfilled, " +
                "but they understand that other urgent needs also existed.";
        }

        return
            "Their need was not fulfilled, and they are angry " +
            "about the decision.";
    }

    private void UpdateInterface()
    {
        remainingBudgetText.text =
            $"Remaining budget: ${remainingBudget}";

        if (selectedFamily != null)
        {
            int requestedAmount =
                GetRequestedAmount(selectedFamily);

            allocatedAmountText.text =
                $"Allocated: ${selectedFamily.allocatedAmount}";
        }
    }
}