using UnityEngine;
using UnityEngine.UI;

// A separate, uniformly scaled root avoids shear inherited from the thin paper mesh.
public class PaperSliderVisual : MonoBehaviour
{
    public PaperDocument paper;
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
    private void Start() => Prepare();
    public void Prepare()
    {
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
        var binding = GetComponent<PaperMoneySlider>();
        if (binding != null) binding.RepairLayout();
        UpdatePose();
    }
    private void LateUpdate()
    {
        if (paper == null) { Destroy(gameObject); return; }
        UpdatePose();
        bool visible = paper.gameObject.activeInHierarchy;
        if (canvas != null) canvas.enabled = visible;
        if (colliders != null)
            foreach (var collider in colliders)
                if (collider != null) collider.enabled = visible && paper.CanEdit;
    }
    private void UpdatePose()
    {
        if (paper == null) return;
        transform.localScale = Vector3.one * metersPerPixel;
        transform.SetPositionAndRotation(paper.transform.position + worldOffset, Quaternion.Euler(worldEulerAngles));
    }
}
