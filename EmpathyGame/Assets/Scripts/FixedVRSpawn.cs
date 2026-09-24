using System.Collections;
using UnityEngine;

[RequireComponent(typeof(OVRCameraRig))]
public class FixedVRSpawn : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstalled()
    {
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null && rig.GetComponent<FixedVRSpawn>() == null)
            rig.gameObject.AddComponent<FixedVRSpawn>();
    }

    [SerializeField]
    [Tooltip("Also place the player's eyes at the authored spawn height. Recommended for this seated desk scene.")]
    private bool alignHeight = true;

    [SerializeField, Min(0f)]
    [Tooltip("Wait after Quest reports tracked head pose before applying the scene spawn.")]
    private float headsetPoseSettleSeconds = 1.5f;

    [SerializeField, Min(0f)]
    [Tooltip("Avoid waiting indefinitely when testing without a headset.")]
    private float maximumTrackingWaitSeconds = 5f;

    private OVRCameraRig cameraRig;
    private Vector3 spawnPosition;
    private Vector3 spawnForward;

    private void Awake()
    {
        cameraRig = GetComponent<OVRCameraRig>();

        // The rig's edit-time transform is the intended eye spawn pose.
        spawnPosition = transform.position;
        spawnForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (spawnForward.sqrMagnitude < 0.0001f)
            spawnForward = Vector3.forward;
        else
            spawnForward.Normalize();

    }

    private IEnumerator Start()
    {
        // On a cold Quest launch, the first few anchor poses can still use a
        // temporary guardian-space origin. Waiting for tracking to settle is
        // what makes startup behave like a later scene restart.
        float deadline = Time.realtimeSinceStartup +
            maximumTrackingWaitSeconds;
        while (!HasTrackedHeadPose() && Time.realtimeSinceStartup < deadline)
            yield return null;

        yield return new WaitForSecondsRealtime(headsetPoseSettleSeconds);
        yield return new WaitForEndOfFrame();
        ApplyAlignment();
    }

    private static bool HasTrackedHeadPose()
    {
        return OVRManager.isHmdPresent &&
            OVRPlugin.GetNodeOrientationTracked(OVRPlugin.Node.EyeCenter) &&
            OVRPlugin.GetNodePositionTracked(OVRPlugin.Node.EyeCenter);
    }

    [ContextMenu("Align VR Rig To Spawn")]
    public void AlignToSpawn()
    {
        ApplyAlignment();
    }

    private void ApplyAlignment()
    {
        Transform eye = cameraRig != null ? cameraRig.centerEyeAnchor : null;
        if (eye == null)
            return;

        Vector3 eyeForward = Vector3.ProjectOnPlane(eye.forward, Vector3.up);
        if (eyeForward.sqrMagnitude > 0.0001f)
        {
            float yaw = Vector3.SignedAngle(
                eyeForward.normalized,
                spawnForward,
                Vector3.up
            );
            transform.RotateAround(eye.position, Vector3.up, yaw);
        }

        Vector3 offset = spawnPosition - eye.position;
        if (!alignHeight)
            offset.y = 0f;

        transform.position += offset;
    }
}
