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
    public class FamilyTurn
    {
        public string familyName;
        public PaperDocument paper;
        public int laneIndex;
        public FamilyVisitDefinition visit;
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
        if (startAutomatically) StartNextFamily();
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
        SetInstruction("Return and release the stamped paper in its player slot.");
        yield return dialogue.Play(visit != null ? visit.afterStamp : null, speaker);
        yield return new WaitForSeconds(Mathf.Max(0, afterStampDelay));
        while (!lane.returnZone.IsReady)
        {
            SetInstruction(lane.returnZone.Status);
            yield return null;
        }
        yield return slider.SlideTo(lane.representativeSlot);
        SetInstruction(speaker + " has received the paper.");
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
