using System.Collections;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas), typeof(PointableCanvas))]
public class SceneRestartButton : MonoBehaviour
{
    public bool IsRestarting { get; private set; }

    private void Awake()
    {
        ConfigureHandPokeInteraction();
    }

    private void ConfigureHandPokeInteraction()
    {
        Canvas canvas = GetComponent<Canvas>();
        PointableCanvas pointableCanvas = GetComponent<PointableCanvas>();
        pointableCanvas.InjectCanvas(canvas);

        // PointableCanvas forwards events to Unity UI, but it is not itself a
        // target for the fingertip. A PokeInteractable and finite surface are
        // also required. Build them at runtime so this canvas works like the
        // Meta SDK's "Add Poke Interaction to Canvas" building block.
        if (GetComponent<PokeInteractable>() != null)
            return;

        Button restartButton = GetComponentInChildren<Button>(true);
        if (restartButton == null)
        {
            Debug.LogError("The restart canvas has no Unity UI Button.", this);
            return;
        }

        var surfaceObject = new GameObject(
            "Restart Poke Surface",
            typeof(RectTransform)
        );
        surfaceObject.layer = restartButton.gameObject.layer;

        RectTransform surfaceTransform =
            (RectTransform)surfaceObject.transform;
        surfaceTransform.SetParent(restartButton.transform, false);
        surfaceTransform.anchorMin = Vector2.zero;
        surfaceTransform.anchorMax = Vector2.one;
        surfaceTransform.anchoredPosition3D = Vector3.zero;
        surfaceTransform.sizeDelta = Vector2.zero;
        surfaceTransform.localRotation = Quaternion.identity;
        surfaceTransform.localScale = Vector3.one;

        PlaneSurface plane = surfaceObject.AddComponent<PlaneSurface>();
        // Double-sided makes the surface tolerant of the button's authored
        // 180-degree rotation while retaining a stable event normal.
        plane.InjectAllPlaneSurface(
            PlaneSurface.NormalFacing.Backward,
            true
        );

        BoundsClipper clipper = surfaceObject.AddComponent<BoundsClipper>();
        Rect rect = surfaceTransform.rect;
        clipper.Size = new Vector3(rect.width, rect.height, 0.01f);

        ClippedPlaneSurface surface =
            surfaceObject.AddComponent<ClippedPlaneSurface>();
        surface.InjectAllClippedPlaneSurface(
            plane,
            new[] { clipper }
        );

        PokeInteractable pokeInteractable =
            gameObject.AddComponent<PokeInteractable>();
        pokeInteractable.InjectAllPokeInteractable(surface);
        pokeInteractable.InjectOptionalPointableElement(pointableCanvas);
        pokeInteractable.EnterHoverNormal = 0.03f;
        pokeInteractable.ExitHoverNormal = 0.05f;
    }

    // Connect this method to a Unity UI Button onClick event. Meta's
    // PointableCanvas converts a hand poke into the same event.
    public void RestartScene()
    {
        if (IsRestarting)
            return;

        IsRestarting = true;
        Time.timeScale = 1f;

        // Clear the selected UI object before unloading it. This prevents the
        // hand pointer from retaining a reference to the old scene's button.
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        StartCoroutine(ReloadActiveScene());
    }

    private IEnumerator ReloadActiveScene()
    {
        // Let the current poke/click event finish before unloading its targets.
        yield return new WaitForEndOfFrame();

        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.buildIndex < 0)
        {
            Debug.LogError(
                $"Cannot restart '{currentScene.name}' because it is not in Build Profiles.",
                this
            );
            IsRestarting = false;
            yield break;
        }

        AsyncOperation reload = SceneManager.LoadSceneAsync(
            currentScene.buildIndex,
            LoadSceneMode.Single
        );

        if (reload == null)
        {
            Debug.LogError($"Unity could not start reloading '{currentScene.name}'.", this);
            IsRestarting = false;
        }
    }
}
