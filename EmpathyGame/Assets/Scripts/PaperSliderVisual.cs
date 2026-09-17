using UnityEngine;
using UnityEngine.UI;

// A separate, uniformly scaled root avoids shear inherited from the thin paper mesh.
[ExecuteAlways]
public class PaperSliderVisual : MonoBehaviour
{
    public PaperDocument paper;
    public bool followPageRotation = true;
    [Tooltip("Offset along the paper's axes, in metres, independent of its mesh scale.")]
    public Vector3 pageOffset = new Vector3(0, 0.012f, 0);
    [Tooltip("90 degrees around X lays the canvas flat on the paper. Adjust Y to match the printed text.")]
    public Vector3 pageEulerAngles = new Vector3(90, 180, 0);
    public Vector3 worldOffset = new Vector3(0, 0.14f, 0);
    public Vector3 worldEulerAngles = new Vector3(65, 180, 0);
    [Min(0.0001f)] public float metersPerPixel = 0.001f;
    private Canvas canvas;
    private Collider[] colliders;
    public void Configure(PaperDocument owner, float yaw)
    {
        paper = owner;
        worldEulerAngles.y = yaw;
        Prepare();
    }
    private void Start()
    {
        if (Application.isPlaying) Prepare();
    }
    public void Prepare()
    {
        ResolvePaper();
        transform.SetParent(null, true);
        canvas = GetComponent<Canvas>();
        colliders = GetComponentsInChildren<Collider>(true);
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 0;
        }
        foreach (var overlay in GetComponentsInChildren<OVROverlayCanvas>(true))
            overlay.enabled = false;
        foreach (var raycaster in GetComponentsInChildren<GraphicRaycaster>(true)) raycaster.enabled = false;
        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = 5; // Main camera renders normal UI; layer 3 is excluded in this scene.
            if (child != transform && child.name.StartsWith("ISDK_")) child.gameObject.SetActive(false);
        }
        UpdatePose();
    }
    private void LateUpdate()
    {
        ResolvePaper();
        if (paper == null) return;
        // Preview detached displays in the editor without changing the scene hierarchy.
        if (!Application.isPlaying && transform.parent != null) return;
        UpdatePose();
        if (!Application.isPlaying) return;
        bool visible = paper.gameObject.activeInHierarchy;
        if (canvas != null) canvas.enabled = visible;
        if (colliders != null)
            foreach (var collider in colliders)
                if (collider != null) collider.enabled = visible && paper.CanEdit;
    }
    private void ResolvePaper()
    {
        var binding = GetComponent<PaperMoneySlider>();
        if (binding != null && binding.Paper != null) paper = binding.Paper;
    }
    private void UpdatePose()
    {
        if (paper == null) return;
        transform.localScale = Vector3.one * metersPerPixel;
        Quaternion rotation = paper.transform.rotation;
        transform.SetPositionAndRotation(
            paper.transform.position + (followPageRotation ? rotation * pageOffset : worldOffset),
            followPageRotation ? rotation * Quaternion.Euler(pageEulerAngles) : Quaternion.Euler(worldEulerAngles));
    }
}
