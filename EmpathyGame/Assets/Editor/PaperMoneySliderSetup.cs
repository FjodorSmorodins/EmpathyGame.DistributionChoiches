using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PaperMoneySliderSetup
{
    [MenuItem("Tools/Empathy Game/Add Money Sliders to Selected Papers")]
    private static void AddSliders()
    {
        if (Application.isPlaying) return;
        int count = 0;
        foreach (GameObject selected in Selection.gameObjects)
        {
            var paper = selected.GetComponent<PaperDocument>();
            if (paper == null || EditorUtility.IsPersistent(paper) ||
                paper.GetComponentInChildren<PaperMoneySlider>(true) != null ||
                Object.FindObjectsByType<PaperSliderVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Any(visual => visual.paper == paper)) continue;

            var root = new GameObject("Money Slider", typeof(RectTransform), typeof(Canvas),
                typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(root, "Add paper money slider");
            root.transform.SetParent(paper.transform, false);
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(300, 90);
            // World size stays usable even when the paper model has a small scale.
            Vector3 scale = paper.transform.lossyScale;
            rect.localScale = new Vector3(0.001f / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
                0.001f / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
                0.001f / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));
            rect.position = paper.transform.position + Vector3.up * 0.12f;
            rect.rotation = Quaternion.Euler(60, paper.transform.eulerAngles.y, 0);
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            var label = Child("Amount", rect, new Vector2(280, 35), new Vector2(0, 25))
                .gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = 24;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.text = "€0";

            var sliderRect = Child("Allocation", rect, new Vector2(260, 36), new Vector2(0, -18));
            var slider = sliderRect.gameObject.AddComponent<Slider>();
            var background = sliderRect.gameObject.AddComponent<Image>();
            background.color = new Color(0.15f, 0.18f, 0.22f);
            var track = Child("Track", sliderRect, new Vector2(230, 8), Vector2.zero);
            track.gameObject.AddComponent<Image>().color = new Color(0.35f, 0.45f, 0.55f);
            var handleArea = Child("Handle Area", sliderRect, new Vector2(230, 36), Vector2.zero);
            var handle = Child("Handle", handleArea, new Vector2(28, 36), Vector2.zero);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(0.25f, 0.8f, 0.95f);
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.wholeNumbers = true;
            slider.maxValue = paper.StepCount;
            var touchBox = handle.gameObject.AddComponent<BoxCollider>();
            touchBox.isTrigger = true;
            touchBox.size = new Vector3(40, 48, 24);
            handle.gameObject.AddComponent<QuillSliderHandle>();

            var binding = root.AddComponent<PaperMoneySlider>();
            var serialized = new SerializedObject(binding);
            serialized.FindProperty("paper").objectReferenceValue = paper;
            serialized.FindProperty("slider").objectReferenceValue = slider;
            serialized.FindProperty("amountLabel").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var visual = root.AddComponent<PaperSliderVisual>();
            visual.Configure(paper, Camera.main != null ? Camera.main.transform.eulerAngles.y : 0);
            EditorSceneManager.MarkSceneDirty(paper.gameObject.scene);
            count++;
        }
        Debug.Log($"Added {count} money sliders. Adjust each Money Slider transform above its paper, then save the scene.");
    }

    private static RectTransform Child(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        var rect = (RectTransform)child.transform;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }
}
