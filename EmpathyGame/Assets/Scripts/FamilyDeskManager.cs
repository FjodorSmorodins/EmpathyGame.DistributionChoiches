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
        public PaperReturnSlot returnZone;
        public Transform entrance;
        public Transform boothStandingPoint;
        public Transform exit;
        [Tooltip("Optional waypoints between entrance and booth, in walking order.")]
        public Transform[] approachWaypoints = new Transform[0];
    }

    [Serializable]
    public class MoneyOutcome
    {
        public enum OutcomeTone
        {
            AutoFromName,
            NotEnough,
            Enough
        }

        [Tooltip("A descriptive name shown only in the Inspector, for example 'No help' or 'Fully funded'.")]
        public string outcomeName;

        [Tooltip("Controls the reaction dialogue color. Auto treats an outcome named 'Enough' as green and 'Not Enough' as red.")]
        public OutcomeTone outcomeTone = OutcomeTone.AutoFromName;

        [Min(0)]
        [Tooltip("This outcome matches when the stamped amount is at least this value.")]
        public int minimumAmount;

        [Min(0)]
        [Tooltip("This outcome matches when the stamped amount is no more than this value. Both limits are inclusive.")]
        public int maximumAmount = 1000;

        [Tooltip("Dialogue shown on the monitor when this range matches. It may contain any number of dialogue steps.")]
        public DialogueSequence monitorReaction;

        [Header("Optional story effect")]
        [Tooltip("Optionally set a story variable when this outcome matches.")]
        public StoryVariable setVariable;
        public int variableValue;

        [Header("Optional scene actions")]
        [Tooltip("Use this to trigger animations, audio, object visibility, particles, or other Inspector-configured actions.")]
        public UnityEvent onMatched = new UnityEvent();

        public bool Matches(int amount)
        {
            int lower = Mathf.Min(minimumAmount, maximumAmount);
            int upper = Mathf.Max(minimumAmount, maximumAmount);
            return amount >= lower && amount <= upper;
        }

        public bool IsEnough
        {
            get
            {
                if (outcomeTone == OutcomeTone.Enough) return true;
                if (outcomeTone == OutcomeTone.NotEnough) return false;

                return !string.IsNullOrWhiteSpace(outcomeName) &&
                    outcomeName.IndexOf(
                        "not enough",
                        StringComparison.OrdinalIgnoreCase
                    ) < 0 &&
                    outcomeName.IndexOf(
                        "enough",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0;
            }
        }
    }

    [Serializable]
    public class FamilyTurn
    {
        public string familyName;
        public PaperDocument paper;
        public int laneIndex;
        public FamilyVisitDefinition visit;

        [Tooltip("Checked from top to bottom. The first range containing the stamped amount is selected.")]
        public MoneyOutcome[] moneyOutcomes = new MoneyOutcome[0];
    }

    [Header("Slots and visit order")]
    [SerializeField] private PaperLane[] lanes = new PaperLane[3];
    [SerializeField] private FamilyTurn[] familyTurns;
    [Header("Shared systems")]
    public DeskBudget budget;
    public DialogueRunner dialogue;
    [Min(0)] public int startingBudget = 3000;
    [Tooltip("World floor height used only when generating default capsule routes.")]
    public float floorY;
    [Header("Timing")]
    [SerializeField] private float beforePaperSlideDelay = 0.5f;
    [SerializeField] private float afterStampDelay = 0.75f;
    [SerializeField] private float betweenFamiliesDelay = 1f;
    [Header("Sequence")]
    [SerializeField] private bool startAutomatically = true;
    [SerializeField]
    [Tooltip("Wait for the player to grab the Instructions object before the first family arrives.")]
    private bool requireInstructionsGrabToStart = true;
    [SerializeField] private bool automaticallyStartNextFamily = true;
    [Header("Optional events")]
    public UnityEvent<int> onFamilyStarted = new UnityEvent<int>();
    public UnityEvent<int> onFamilyFinished = new UnityEvent<int>();
    public UnityEvent onAllFamiliesFinished = new UnityEvent();
    public PaperLane[] Lanes => lanes;
    public FamilyTurn[] FamilyTurns => familyTurns;
    public GameObject CurrentRepresentative { get; private set; }
    public PaperDocument CurrentPaper { get; private set; }
    private int currentFamilyIndex = -1;
    private bool running, finished;
    private string instruction = "";

    // Can be selected from a MoneyOutcome UnityEvent. This resolves the
    // representative created at runtime and triggers an animation on it.
    public void TriggerCurrentRepresentativeAnimation(string triggerName)
    {
        if (CurrentRepresentative == null || string.IsNullOrWhiteSpace(triggerName))
            return;

        Animator representativeAnimator =
            CurrentRepresentative.GetComponentInChildren<Animator>();

        if (representativeAnimator == null ||
            representativeAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning(
                $"{CurrentRepresentative.name} has no configured Animator.",
                CurrentRepresentative
            );
            return;
        }

        representativeAnimator.SetTrigger(triggerName);
    }

    private void Start()
    {
        PrepareScene();
        budget.totalMoney = startingBudget;
        budget.ResetForNewSession();
        dialogue.ResetStory();
        budget.Changed += RefreshStatus;
        if (familyTurns == null) familyTurns = new FamilyTurn[0];
        var documents = new HashSet<PaperDocument>();
        foreach (var turn in familyTurns)
        {
            if (turn?.paper == null) continue;
            if (!documents.Add(turn.paper))
            {
                Debug.LogError("Each family turn needs its own PaperDocument. A paper is listed twice.", this);
                return;
            }
            turn.paper.ConfigureBudget(budget);
            turn.paper.ResetPaper();
            turn.paper.SetInteractionAllowed(false);
            // Papers are presented only when their representative reaches the booth.
            turn.paper.gameObject.SetActive(false);
        }
        if (requireInstructionsGrabToStart &&
            StartGameOnInstructionsGrab.TryInstall(this))
        {
            SetInstruction("Pick up the instructions to begin.");
        }
        else if (startAutomatically)
        {
            StartNextFamily();
        }
    }

    public void PrepareScene() => DeskSceneDefaults.Prepare(this);

    public void StartNextFamily()
    {
        if (running || finished) return;
        if (familyTurns == null || ++currentFamilyIndex >= familyTurns.Length)
        {
            finished = true;
            SetInstruction("All families have been seen.");
            onAllFamiliesFinished.Invoke();
            return;
        }
        // Set before StartCoroutine: validation can finish the coroutine synchronously.
        running = true;
        StartCoroutine(RunFamilyTurn(familyTurns[currentFamilyIndex]));
    }

    private IEnumerator RunFamilyTurn(FamilyTurn family)
    {
        if (family?.paper == null || family.laneIndex < 0 || family.laneIndex >= lanes.Length)
        {
            Debug.LogError("Family visit has a missing paper or invalid lane. Check Family Desk System.", this);
            running = false;
            yield break;
        }
        var lane = lanes[family.laneIndex];
        var slider = family.paper.GetComponent<PaperSlider>();
        if (slider == null || lane.playerSlot == null || lane.representativeSlot == null || lane.returnZone == null)
        {
            Debug.LogError("Family visit needs both slot transforms, a return zone, and PaperSlider.", this);
            running = false;
            yield break;
        }
        var visit = family.visit;
        string speaker = visit != null && !string.IsNullOrEmpty(visit.displayName) ? visit.displayName : family.familyName;
        CurrentPaper = family.paper;
        SetInstruction("Waiting for " + speaker + ".");
        if (visit != null)
        {
            if (visit.arrivalVariable != null)
                yield return new WaitUntil(() => dialogue.GetValue(visit.arrivalVariable) == visit.arrivalValue);
            yield return new WaitForSeconds(Mathf.Max(0, visit.arrivalDelay));
        }
        CurrentRepresentative = CreateRepresentative(visit, speaker);
        CurrentRepresentative.transform.SetPositionAndRotation(lane.entrance.position, lane.entrance.rotation);
        float speed = visit != null ? Mathf.Max(0.01f, visit.walkSpeed) : 0.8f;
        if (lane.approachWaypoints != null)
            foreach (var waypoint in lane.approachWaypoints)
                if (waypoint != null) yield return WalkTo(waypoint, speed);
        yield return WalkTo(lane.boothStandingPoint, speed);
        CurrentRepresentative.transform.rotation = lane.boothStandingPoint.rotation;
        onFamilyStarted.Invoke(currentFamilyIndex);

        SetInstruction(speaker + " is speaking.");
        yield return dialogue.Play(visit != null ? visit.onArrival : null, speaker);
        family.paper.gameObject.SetActive(true);
        family.paper.SetInteractionAllowed(false);
        slider.PlaceAt(lane.representativeSlot);
        yield return new WaitForSeconds(Mathf.Max(0, beforePaperSlideDelay));
        yield return slider.SlideTo(lane.playerSlot);
        family.paper.SetInteractionAllowed(true);
        SetInstruction("Use the quill to choose an amount, then stamp the paper.");
        // Arm immediately in the stamp event so a quick release cannot be missed.
        Action<PaperDocument, int> onStamp = (paper, amount) => lane.returnZone.Arm(paper);
        family.paper.Stamped += onStamp;
        try { yield return new WaitUntil(() => family.paper.IsStamped); }
        finally { family.paper.Stamped -= onStamp; }
        SetInstruction("Return and release the stamped paper in its corresponding slot.");
        yield return new WaitForSeconds(Mathf.Max(0, afterStampDelay));
        while (!lane.returnZone.IsReady)
        {
            SetInstruction(lane.returnZone.Status);
            yield return null;
        }
        yield return slider.SlideTo(lane.representativeSlot);
        SetInstruction(speaker + " has received the paper.");
        yield return PlayOutcomeReaction(family, visit, speaker);
        yield return dialogue.Play(visit != null ? visit.afterReturn : null, speaker);
        onFamilyFinished.Invoke(currentFamilyIndex);
        family.paper.gameObject.SetActive(false);
        yield return WalkTo(lane.exit, speed);
        Destroy(CurrentRepresentative);
        CurrentRepresentative = null;
        CurrentPaper = null;
        yield return new WaitForSeconds(Mathf.Max(0, betweenFamiliesDelay));
        running = false;
        if (automaticallyStartNextFamily) StartNextFamily();
        else SetInstruction("Ready for the next family.");
    }

    private IEnumerator PlayOutcomeReaction(
        FamilyTurn family,
        FamilyVisitDefinition visit,
        string speaker)
    {
        MoneyOutcome matchedOutcome = null;
        int assignedAmount = family.paper.AssignedAmount;

        if (family.moneyOutcomes != null)
        {
            foreach (MoneyOutcome outcome in family.moneyOutcomes)
            {
                if (outcome != null && outcome.Matches(assignedAmount))
                {
                    matchedOutcome = outcome;
                    break;
                }
            }
        }

        DialogueSequence reaction = visit != null ? visit.afterStamp : null;

        if (matchedOutcome != null)
        {
            if (matchedOutcome.setVariable != null)
            {
                dialogue.SetValue(
                    matchedOutcome.setVariable,
                    matchedOutcome.variableValue
                );
            }

            matchedOutcome.onMatched?.Invoke();

            if (matchedOutcome.monitorReaction != null)
                reaction = matchedOutcome.monitorReaction;

            Debug.Log(
                $"{family.familyName} received {assignedAmount:N0}. " +
                $"Matched outcome: {matchedOutcome.outcomeName}",
                this
            );
        }
        else
        {
            Debug.LogWarning(
                $"No money outcome range on {family.familyName} contains " +
                $"the stamped amount {assignedAmount:N0}. Using the visit's " +
                $"default After Stamp dialogue.",
                this
            );
        }

        Color? outcomeColor = matchedOutcome != null
            ? matchedOutcome.IsEnough
                ? dialogue.enoughOutcomeColor
                : dialogue.notEnoughOutcomeColor
            : null;

        yield return dialogue.Play(reaction, speaker, outcomeColor);
    }

    private GameObject CreateRepresentative(FamilyVisitDefinition visit, string displayName)
    {
        if (visit != null && visit.representativePrefab != null)
            return Instantiate(visit.representativePrefab);
        var root = new GameObject(displayName + " Representative");
        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Placeholder Body";
        capsule.transform.SetParent(root.transform, false);
        capsule.transform.localPosition = Vector3.up * 0.85f;
        capsule.transform.localScale = new Vector3(0.5f, 0.85f, 0.5f);
        // Placeholder visitors follow authored paths and do not push desk objects.
        capsule.GetComponent<Collider>().enabled = false;
        var renderer = capsule.GetComponent<Renderer>();
        var block = new MaterialPropertyBlock();
        Color color = visit != null ? visit.capsuleColor : Color.gray;
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        renderer.SetPropertyBlock(block);
        return root;
    }

    private IEnumerator WalkTo(Transform target, float speed)
    {
        if (target == null || CurrentRepresentative == null) yield break;
        var actor = CurrentRepresentative.transform;
        while (Vector3.Distance(actor.position, target.position) > 0.01f)
        {
            Vector3 direction = target.position - actor.position;
            Vector3 horizontal = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (horizontal.sqrMagnitude > 0.0001f)
                actor.rotation = Quaternion.RotateTowards(actor.rotation, Quaternion.LookRotation(horizontal), 180 * Time.deltaTime);
            actor.position = Vector3.MoveTowards(actor.position, target.position, speed * Time.deltaTime);
            yield return null;
        }
        actor.position = target.position;
    }
    private void SetInstruction(string text) { instruction = text; RefreshStatus(); }
    private void RefreshStatus()
    {
        if (dialogue != null && budget != null)
            dialogue.SetStatus($"Remaining: {budget.Remaining:N0} / {startingBudget:N0}\n{instruction}");
    }
    private void OnDestroy()
    {
        if (budget != null) budget.Changed -= RefreshStatus;
        if (CurrentRepresentative != null) Destroy(CurrentRepresentative);
    }
}
