using System.Collections;
using UnityEngine;

public class PaperSlider : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float slideDuration = 0.75f;

    [Header("Physics")]
    [SerializeField] private Rigidbody paperRigidbody;

    [Header("Optional")]
    [Tooltip("Put Meta XR interaction behaviours here if you want grabbing disabled during the automatic slide.")]
    [SerializeField] private Behaviour[] disableWhileSliding;

    public bool IsSliding { get; private set; }

    private void Awake()
    {
        if (paperRigidbody == null)
            paperRigidbody = GetComponent<Rigidbody>();
    }

    public void PlaceAt(Transform target)
    {
        if (target == null)
            return;

        transform.SetPositionAndRotation(
            target.position,
            target.rotation
        );
    }

    public IEnumerator SlideTo(Transform target)
    {
        if (target == null)
            yield break;

        IsSliding = true;

        SetInteractionEnabled(false);

        bool previousKinematic = false;

        if (paperRigidbody != null)
        {
            previousKinematic = paperRigidbody.isKinematic;
            paperRigidbody.isKinematic = true;
        }

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        float timer = 0f;

        while (timer < slideDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / slideDuration);

            // Makes the movement start/end more naturally.
            float smoothT = t * t * (3f - 2f * t);

            Vector3 newPosition = Vector3.Lerp(
                startPosition,
                target.position,
                smoothT
            );

            Quaternion newRotation = Quaternion.Slerp(
                startRotation,
                target.rotation,
                smoothT
            );

            transform.SetPositionAndRotation(
                newPosition,
                newRotation
            );

            yield return null;
        }

        transform.SetPositionAndRotation(
            target.position,
            target.rotation
        );

        if (paperRigidbody != null)
            paperRigidbody.isKinematic = previousKinematic;

        SetInteractionEnabled(true);

        IsSliding = false;
    }

    private void SetInteractionEnabled(bool enabled)
    {
        if (disableWhileSliding == null)
            return;

        foreach (Behaviour behaviour in disableWhileSliding)
        {
            if (behaviour != null)
                behaviour.enabled = enabled;
        }
    }
}