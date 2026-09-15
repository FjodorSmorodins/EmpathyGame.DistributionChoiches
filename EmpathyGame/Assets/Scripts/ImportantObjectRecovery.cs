using UnityEngine;
using UnityEngine.Events;

public class ImportantObjectRecovery : MonoBehaviour
{
    public Transform respawnPoint;
    [Tooltip("World Y height below the table. Objects recover when their origin drops below it.")]
    public float fallBelowY = 0.35f;
    public UnityEvent onRecovered = new UnityEvent();
    public int RecoveryVersion { get; private set; }
    private Rigidbody body;
    private PaperGrabState grab;
    private PaperSlider slider;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        grab = GetComponent<PaperGrabState>();
        if (grab == null) grab = gameObject.AddComponent<PaperGrabState>();
        slider = GetComponent<PaperSlider>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }
    private void FixedUpdate()
    {
        if (transform.position.y < fallBelowY) Recover();
    }
    public void Recover()
    {
        if (grab != null && grab.IsGrabbed || slider != null && slider.IsSliding) return;
        Vector3 position = respawnPoint != null ? respawnPoint.position : initialPosition;
        Quaternion rotation = respawnPoint != null ? respawnPoint.rotation : initialRotation;
        if (body != null)
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.position = position;
            body.rotation = rotation;
        }
        transform.SetPositionAndRotation(position, rotation);
        RecoveryVersion++;
        onRecovered.Invoke();
    }
}
