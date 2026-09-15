using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DeskSceneSetupEditor
{
    [MenuItem("Tools/Empathy Game/Set Up Family Desk Scene")]
    public static void SetUp()
    {
        if (Application.isPlaying) { Debug.LogWarning("Stop Play mode before setting up the desk."); return; }
        var desk = Object.FindFirstObjectByType<FamilyDeskManager>();
        if (desk == null) { Debug.LogError("Open Test with assets, or add a FamilyDeskManager first."); return; }
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Set up family desk");
        foreach (var root in desk.gameObject.scene.GetRootGameObjects())
            Undo.RegisterFullObjectHierarchyUndo(root, "Set up family desk");
        var before = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(go => go.GetInstanceID()).ToHashSet();
        desk.PrepareScene();
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (go.scene == desk.gameObject.scene && !before.Contains(go.GetInstanceID()) &&
                (go.transform.parent == null || before.Contains(go.transform.parent.gameObject.GetInstanceID())))
                Undo.RegisterCreatedObjectUndo(go, "Create desk setup");
        foreach (var component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (component.gameObject.scene == desk.gameObject.scene)
            {
                EditorUtility.SetDirty(component);
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(desk.gameObject.scene);
        Selection.activeGameObject = desk.gameObject;
        Debug.Log("Desk setup created. Adjust Desk Setup Points (routes, return zones, and respawn points), then save the scene. Starting Budget is on Family Desk System.", desk);
    }

    [MenuItem("Tools/Empathy Game/Inspect Stamper Animation")]
    public static void InspectAnimation()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Models/Stamper Animation.controller");
        if (controller == null) { Debug.LogError("Stamper controller is missing."); return; }
        var stamp = controller.layers[0].stateMachine.states.FirstOrDefault(s => s.state.name == "Stamp").state;
        var clip = stamp != null ? stamp.motion as AnimationClip : null;
        if (clip == null) { Debug.LogError("Stamp state does not resolve to an animation clip.", controller); return; }
        var bindings = AnimationUtility.GetCurveBindings(clip);
        Debug.Log($"Stamper clip: {clip.name}, {clip.length:F2} seconds, {bindings.Length} animated properties.\n" +
            string.Join("\n", bindings.Select(b => b.path + " : " + b.propertyName)), clip);
        if (bindings.Any(b => b.path == "" && b.type == typeof(Transform)))
            Debug.LogWarning("The clip animates the grabbed root transform. Use Bake Stamper Visual Animation to remove root curves and create a matching rest pose.", clip);
    }

    [MenuItem("Tools/Empathy Game/Bake Stamper Visual Animation")]
    public static void BakeAnimation()
    {
        if (Application.isPlaying) return;
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Models/Stamper Animation.controller");
        if (controller == null) return;
        var states = controller.layers[0].stateMachine.states;
        var stamp = states.FirstOrDefault(s => s.state.name == "Stamp").state;
        var idle = states.FirstOrDefault(s => s.state.name == "Idle").state;
        if (stamp == null || idle == null || !(stamp.motion is AnimationClip source)) return;
        if (!AssetDatabase.IsValidFolder("Assets/Desk Content/Animation"))
            AssetDatabase.CreateFolder("Assets/Desk Content", "Animation");
        var press = Object.Instantiate(source);
        press.name = "Stamp Press";
        var rest = new AnimationClip { name = "Stamp Rest", frameRate = source.frameRate };
        foreach (var binding in AnimationUtility.GetCurveBindings(source))
        {
            if (binding.path == "" && binding.type == typeof(Transform))
            { AnimationUtility.SetEditorCurve(press, binding, null); continue; }
            var curve = AnimationUtility.GetEditorCurve(source, binding);
            float value = curve.Evaluate(0);
            AnimationUtility.SetEditorCurve(rest, binding, AnimationCurve.Constant(0, 0.1f, value));
        }
        var settings = AnimationUtility.GetAnimationClipSettings(press);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(press, settings);
        stamp.motion = SaveClip(press, "Assets/Desk Content/Animation/Stamp Press.anim");
        idle.motion = SaveClip(rest, "Assets/Desk Content/Animation/Stamp Rest.anim");
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(stamp);
        EditorUtility.SetDirty(idle);
        AssetDatabase.SaveAssets();
        Debug.Log("Baked the stamper's internal movement and rest pose. Root movement is left to hand tracking.", controller);
    }
    private static AnimationClip SaveClip(AnimationClip clip, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing == null) { AssetDatabase.CreateAsset(clip, path); return clip; }
        EditorUtility.CopySerialized(clip, existing);
        Object.DestroyImmediate(clip);
        EditorUtility.SetDirty(existing);
        return existing;
    }
}
