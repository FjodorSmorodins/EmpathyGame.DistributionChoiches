using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FamilyDeskManager : MonoBehaviour
{
    [Serializable]
    public class PaperLane
    {
        public Transform representativeSlot;
        public Transform playerSlot;
    }

    [Serializable]
    public class FamilyTurn
    {
        public string familyName;
        public PaperDocument paper;

        [Tooltip("0 = Family lane 1, 1 = Family lane 2, 2 = Family lane 3")]
        public int laneIndex;
    }

    [Header("Six Paper Slots")]
    [SerializeField] private PaperLane[] lanes = new PaperLane[3];

    [Header("Family Order")]
    [SerializeField] private FamilyTurn[] familyTurns;

    [Header("Shared Budget")]
    [Min(0)]
    [SerializeField] private int startingBudget = 5500;

    [Tooltip("Invoked whenever an allocation changes. The value is the money still available.")]
    public UnityEvent<int> onRemainingBudgetChanged;

    [Header("Timing")]
    [SerializeField] private float beforePaperSlideDelay = 0.5f;
    [SerializeField] private float afterStampDelay = 0.75f;
    [SerializeField] private float betweenFamiliesDelay = 1f;

    [Header("Sequence")]
    [SerializeField] private bool startAutomatically = true;
    [SerializeField] private bool automaticallyStartNextFamily = true;

    [Header("Optional Events")]
    public UnityEvent<int> onFamilyStarted;
    public UnityEvent<int> onFamilyFinished;
    public UnityEvent onAllFamiliesFinished;

    private int currentFamilyIndex = -1;
    private Coroutine currentRoutine;
    private readonly Dictionary<PaperDocument, Action> paperChangeHandlers =
        new Dictionary<PaperDocument, Action>();
    private bool correctingAllocation;
    private int lastReportedRemainingBudget = int.MinValue;

    public int StartingBudget => Mathf.Max(0, startingBudget);
    public int RemainingBudget { get; private set; }
    public int TotalAllocated => StartingBudget - RemainingBudget;

    private void OnEnable()
    {
        SubscribeToPaperChanges();
    }

    private void OnDisable()
    {
        UnsubscribeFromPaperChanges();
    }

    private void Start()
    {
        // OnEnable normally subscribes first, but calling this again is safe and
        // also supports papers assigned by another script before Start.
        SubscribeToPaperChanges();
        EnforceBudgetAcrossAllPapers();

        if (startAutomatically)
            StartNextFamily();
    }

    private void SubscribeToPaperChanges()
    {
        if (familyTurns == null)
            return;

        foreach (FamilyTurn familyTurn in familyTurns)
        {
            PaperDocument paper = familyTurn != null ? familyTurn.paper : null;

            if (paper == null || paperChangeHandlers.ContainsKey(paper))
                continue;

            Action handler = () => HandlePaperAmountChanged(paper);
            paperChangeHandlers.Add(paper, handler);
            paper.Changed += handler;
        }
    }

    private void UnsubscribeFromPaperChanges()
    {
        foreach (KeyValuePair<PaperDocument, Action> entry in paperChangeHandlers)
        {
            if (entry.Key != null)
                entry.Key.Changed -= entry.Value;
        }

        paperChangeHandlers.Clear();
    }

    private void EnforceBudgetAcrossAllPapers()
    {
        if (familyTurns != null)
        {
            foreach (FamilyTurn familyTurn in familyTurns)
            {
                if (familyTurn != null && familyTurn.paper != null)
                    HandlePaperAmountChanged(familyTurn.paper);
            }
        }

        RefreshRemainingBudget();
    }

    private void HandlePaperAmountChanged(PaperDocument changedPaper)
    {
        if (correctingAllocation || changedPaper == null)
            return;

        int allocatedToOtherPapers = GetTotalAllocatedExcept(changedPaper);
        int maximumAvailableForThisPaper =
            Mathf.Max(0, StartingBudget - allocatedToOtherPapers);

        if (changedPaper.AssignedAmount > maximumAvailableForThisPaper)
        {
            if (changedPaper.IsStamped)
            {
                Debug.LogError(
                    $"Stamped paper {changedPaper.name} exceeds the shared budget.",
                    changedPaper
                );
            }
            else
            {
                int maximumAllowedStep =
                    maximumAvailableForThisPaper / changedPaper.AmountStep;

                correctingAllocation = true;
                changedPaper.SetAmountStep(maximumAllowedStep);
                correctingAllocation = false;
            }
        }

        RefreshRemainingBudget();
    }

    private int GetTotalAllocatedExcept(PaperDocument excludedPaper)
    {
        int total = 0;
        var countedPapers = new HashSet<PaperDocument>();

        if (familyTurns == null)
            return total;

        foreach (FamilyTurn familyTurn in familyTurns)
        {
            PaperDocument paper = familyTurn != null ? familyTurn.paper : null;

            if (paper == null || paper == excludedPaper || !countedPapers.Add(paper))
                continue;

            total += paper.AssignedAmount;
        }

        return total;
    }

    private void RefreshRemainingBudget()
    {
        int totalAllocated = GetTotalAllocatedExcept(null);
        RemainingBudget = Mathf.Max(0, StartingBudget - totalAllocated);

        if (RemainingBudget == lastReportedRemainingBudget)
            return;

        lastReportedRemainingBudget = RemainingBudget;
        onRemainingBudgetChanged?.Invoke(RemainingBudget);
    }

    public void StartNextFamily()
    {
        if (currentRoutine != null)
            return;

        currentFamilyIndex++;

        if (currentFamilyIndex >= familyTurns.Length)
        {
            onAllFamiliesFinished?.Invoke();
            return;
        }

        currentRoutine = StartCoroutine(
            RunFamilyTurn(familyTurns[currentFamilyIndex])
        );
    }

    private IEnumerator RunFamilyTurn(FamilyTurn family)
    {
        if (family.paper == null)
        {
            Debug.LogError(
                $"Family {currentFamilyIndex} has no paper assigned."
            );

            currentRoutine = null;
            yield break;
        }

        if (family.laneIndex < 0 || family.laneIndex >= lanes.Length)
        {
            Debug.LogError(
                $"{family.familyName} has an invalid lane index."
            );

            currentRoutine = null;
            yield break;
        }

        PaperLane lane = lanes[family.laneIndex];

        PaperSlider slider =
            family.paper.GetComponent<PaperSlider>();

        PaperGrabState grabState =
            family.paper.GetComponent<PaperGrabState>();

        if (slider == null)
        {
            Debug.LogError(
                $"{family.paper.name} needs a PaperSlider."
            );

            currentRoutine = null;
            yield break;
        }

        // Reset this family's document.
        family.paper.ResetPaper();

        // Paper begins in the representative's assigned hole.
        slider.PlaceAt(lane.representativeSlot);

        onFamilyStarted?.Invoke(currentFamilyIndex);

        yield return new WaitForSeconds(beforePaperSlideDelay);

        // Representative slides paper toward player.
        yield return slider.SlideTo(lane.playerSlot);

        // Player can now grab/read/stamp it.
        yield return new WaitUntil(
            () => family.paper.IsStamped
        );

        yield return new WaitForSeconds(afterStampDelay);

        // If player is still holding it, wait.
        if (grabState != null)
        {
            yield return new WaitUntil(
                () => !grabState.IsGrabbed
            );
        }

        // Representative takes paper back through SAME lane.
        yield return slider.SlideTo(lane.representativeSlot);

        onFamilyFinished?.Invoke(currentFamilyIndex);

        yield return new WaitForSeconds(betweenFamiliesDelay);

        currentRoutine = null;

        if (automaticallyStartNextFamily)
        {
            StartNextFamily();
        }
    }
}
