using Oculus.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared by the editor setup command and Play mode. Existing assigned settings are retained.
public static class DeskSceneDefaults
{
    public static void Prepare(FamilyDeskManager desk)
    {
        if (desk.budget == null) desk.budget = GetOrAdd<DeskBudget>(desk.gameObject);
        if (desk.dialogue == null) desk.dialogue = GetOrAdd<DialogueRunner>(desk.gameObject);
        var root = desk.transform.Find("Desk Setup Points");
        if (root == null) root = Point("Desk Setup Points", desk.transform, Vector3.zero, Quaternion.identity);
        var lanes = desk.Lanes;
        if (lanes == null || lanes.Length == 0 || desk.FamilyTurns == null) return;
        for (int i = 0; i < lanes.Length; i++)
        {
            var lane = lanes[i];
            if (lane == null || lane.playerSlot == null || lane.representativeSlot == null) continue;
            Vector3 away = Vector3.ProjectOnPlane(lane.representativeSlot.position - lane.playerSlot.position, Vector3.up).normalized;
            if (away.sqrMagnitude < 0.1f) away = Vector3.forward;
            Quaternion facing = Quaternion.LookRotation(-away);
            Vector3 stand = lane.representativeSlot.position + away * 0.85f;
            stand.y = desk.floorY;
            if (lane.boothStandingPoint == null) lane.boothStandingPoint = Point($"Lane {i} - Stand", root, stand, facing);
            if (lane.entrance == null) lane.entrance = Point($"Lane {i} - Entrance", root, stand + away * 2.5f, facing);
            if (lane.exit == null) lane.exit = Point($"Lane {i} - Exit", root, stand + away * 2.5f, facing);
            if (lane.returnZone == null)
            {
                var zone = Point($"Lane {i} - Player Return Zone", root, lane.playerSlot.position, Quaternion.identity);
                var box = zone.gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                // Independent of the very thin and nonuniformly scaled slot marker.
                box.size = new Vector3(0.28f, 0.16f, 0.30f);
                lane.returnZone = zone.gameObject.AddComponent<PaperReturnSlot>();
            }
        }
        foreach (var turn in desk.FamilyTurns)
        {
            if (turn?.paper == null || turn.laneIndex < 0 || turn.laneIndex >= lanes.Length) continue;
            var lane = lanes[turn.laneIndex];
            if (lane?.playerSlot == null) continue;
            GetOrAdd<PaperGrabState>(turn.paper.gameObject);
            AddRecovery(turn.paper.gameObject, root, lane.playerSlot.position + Vector3.up * 0.06f,
                lane.playerSlot.rotation, lane.playerSlot.position.y - 0.45f);
            var money = turn.paper.GetComponentInChildren<PaperMoneySlider>(true);
            if (money == null)
                foreach (var existing in Object.FindObjectsByType<PaperSliderVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (existing.paper == turn.paper) { money = existing.GetComponent<PaperMoneySlider>(); break; }
            if (money != null)
            {
                var visual = money.GetComponent<PaperSliderVisual>();
                if (visual == null)
                {
                    visual = money.gameObject.AddComponent<PaperSliderVisual>();
                    Vector3 forward = Vector3.ProjectOnPlane(lane.representativeSlot.position - lane.playerSlot.position, Vector3.up);
                    float yaw = forward.sqrMagnitude > 0.01f ? Quaternion.LookRotation(forward).eulerAngles.y : 0;
                    visual.Configure(turn.paper, yaw);
                }
            }
        }
        // Includes the quill and stamper, and future important desk grabbables.
        foreach (var grabbable in Object.FindObjectsByType<Grabbable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (grabbable.gameObject.scene != desk.gameObject.scene || grabbable.GetComponent<Rigidbody>() == null) continue;
            AddRecovery(grabbable.gameObject, root, grabbable.transform.position + Vector3.up * 0.04f,
                grabbable.transform.rotation, grabbable.transform.position.y - 0.45f);
        }
        if (desk.dialogue.voiceSource == null)
        {
            desk.dialogue.voiceSource = GetOrAdd<AudioSource>(desk.gameObject);
            desk.dialogue.voiceSource.playOnAwake = false;
            desk.dialogue.voiceSource.spatialBlend = 0;
        }
        if (desk.dialogue.dialogueText == null && lanes[0]?.representativeSlot != null)
            BuildDialogueBoard(desk, root);
    }

    private static void AddRecovery(GameObject target, Transform root, Vector3 position, Quaternion rotation, float threshold)
    {
        var recovery = target.GetComponent<ImportantObjectRecovery>();
        if (recovery != null) return;
        GetOrAdd<PaperGrabState>(target);
        recovery = target.AddComponent<ImportantObjectRecovery>();
        recovery.respawnPoint = Point(target.name + " - Respawn", root, position, rotation);
        recovery.fallBelowY = threshold;
    }
    private static void BuildDialogueBoard(FamilyDeskManager desk, Transform root)
    {
        var lane = desk.Lanes[0];
        Vector3 forward = Vector3.ProjectOnPlane(lane.representativeSlot.position - lane.playerSlot.position, Vector3.up);
        Quaternion rotation = forward.sqrMagnitude > 0.01f ? Quaternion.LookRotation(forward) : Quaternion.identity;
        var board = new GameObject("Dialogue and Budget", typeof(RectTransform), typeof(Canvas));
        board.transform.SetParent(root, false);
        board.transform.SetPositionAndRotation(lane.representativeSlot.position + Vector3.up * 0.70f, rotation);
        board.transform.localScale = Vector3.one * 0.0015f;
        var canvas = board.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        ((RectTransform)board.transform).sizeDelta = new Vector2(620, 280);
        var background = board.AddComponent<Image>();
        background.color = new Color(0.025f, 0.035f, 0.045f, 0.94f);
        background.raycastTarget = false;
        desk.dialogue.statusText = Text("Budget and Instructions", board.transform, new Vector2(580, 90), new Vector2(0, 85), 23);
        desk.dialogue.dialogueText = Text("Dialogue Lines", board.transform, new Vector2(580, 160), new Vector2(0, -42), 28);
        desk.dialogue.statusText.text = $"Remaining: {desk.startingBudget:N0}";
        desk.dialogue.dialogueText.text = "Family dialogue appears here.";
    }
    private static TMP_Text Text(string name, Transform parent, Vector2 size, Vector2 position, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }
    private static Transform Point(string name, Transform parent, Vector3 position, Quaternion rotation)
    {
        var point = new GameObject(name).transform;
        point.SetParent(parent, false);
        point.SetPositionAndRotation(position, rotation);
        return point;
    }
    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        // Unity can return a missing-component wrapper that is not CLR null.
        // Use Unity's equality check rather than the null-coalescing operator.
        T component = target.GetComponent<T>();
        if (component == null) component = target.AddComponent<T>();
        return component;
    }
}
