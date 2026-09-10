using System;
using System.Collections;
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

    private void Start()
    {
        if (startAutomatically)
            StartNextFamily();
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