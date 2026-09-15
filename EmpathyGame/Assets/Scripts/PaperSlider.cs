using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

public class PaperSlider : MonoBehaviour
{
    [Min(0)] [SerializeField] private float slideDuration = 0.75f;
    [SerializeField] private Rigidbody paperRigidbody;
    [SerializeField] private Behaviour[] disableWhileSliding;
    public bool IsSliding { get; private set; }
    private readonly List<Behaviour> suppressed = new List<Behaviour>();
    private bool wasKinematic;
    private void Awake()
    {
        if (paperRigidbody == null) paperRigidbody = GetComponent<Rigidbody>();
    }
    public void PlaceAt(Transform target)
    {
        if (target == null) return;
        ClearVelocity();
        transform.SetPositionAndRotation(target.position, target.rotation);
        if (paperRigidbody != null)
        {
            paperRigidbody.position = target.position;
            paperRigidbody.rotation = target.rotation;
        }
    }
    public IEnumerator SlideTo(Transform target)
    {
        if (target == null || IsSliding) yield break;
        IsSliding = true;
        foreach (var behaviour in GetComponentsInChildren<MonoBehaviour>(true))
            if (behaviour is GrabInteractable || behaviour is HandGrabInteractable ||
                behaviour is DistanceGrabInteractable || behaviour is DistanceHandGrabInteractable)
                Suppress(behaviour);
        if (disableWhileSliding != null)
            foreach (var behaviour in disableWhileSliding) Suppress(behaviour);
        if (paperRigidbody != null)
        {
            wasKinematic = paperRigidbody.isKinematic;
            ClearVelocity();
            paperRigidbody.isKinematic = true;
        }
        Vector3 start = transform.position;
        Quaternion rotation = transform.rotation;
        try
        {
            float timer = 0;
            while (timer < slideDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / slideDuration);
                t = t * t * (3 - 2 * t);
                transform.SetPositionAndRotation(Vector3.Lerp(start, target.position, t),
                    Quaternion.Slerp(rotation, target.rotation, t));
                yield return null;
            }
            PlaceAt(target);
        }
        finally { Restore(); }
    }
    private void Suppress(Behaviour behaviour)
    {
        if (behaviour == null || !behaviour.enabled || behaviour == this) return;
        suppressed.Add(behaviour);
        behaviour.enabled = false;
    }
    private void ClearVelocity()
    {
        if (paperRigidbody == null || paperRigidbody.isKinematic) return;
        paperRigidbody.linearVelocity = Vector3.zero;
        paperRigidbody.angularVelocity = Vector3.zero;
    }
    private void Restore()
    {
        if (!IsSliding) return;
        if (paperRigidbody != null) paperRigidbody.isKinematic = wasKinematic;
        foreach (var behaviour in suppressed)
            if (behaviour != null) behaviour.enabled = true;
        suppressed.Clear();
        ClearVelocity();
        IsSliding = false;
    }
    private void OnDisable() => Restore();
}
