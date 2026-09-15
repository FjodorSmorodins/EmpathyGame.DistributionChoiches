using System;

// Pure state machine shared by the scene component and the automated regression checks.
public sealed class ReturnSubmissionGate
{
    private int releaseAtArm;
    private float settled;
    public void Arm(bool inside, int releaseVersion)
    {
        releaseAtArm = releaseVersion;
        settled = 0;
    }
    public bool Tick(bool inside, bool held, bool deliberateRelease, bool releasedInside,
        int releaseVersion, float elapsed, float settleSeconds)
    {
        // A fresh hand release is the deliberate action. Requiring a full exit
        // after stamping rejects normal small lifts and placement into the slot.
        bool valid = inside && !held && deliberateRelease && releasedInside && releaseVersion > releaseAtArm;
        settled = valid ? settled + Math.Max(0, elapsed) : 0;
        return valid && settled >= Math.Max(0, settleSeconds);
    }
}
