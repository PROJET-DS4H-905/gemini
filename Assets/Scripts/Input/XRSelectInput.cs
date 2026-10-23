using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Hands;

/// <summary>
/// XR select: right controller trigger ray, with a hand-tracking pinch fallback.
/// Resolves the active device each frame (controller first, then hands).
/// </summary>
public class XRSelectInput : ISelectInput
{
    private const float PinchDistanceMeters = 0.025f; // thumb tip <-> index tip

    private readonly InputAction triggerAction;
    private bool wasPinching;

    public XRSelectInput()
    {
        triggerAction = new InputAction(type: InputActionType.Button, binding: "<XRController>{RightHand}/triggerPressed");
        triggerAction.Enable();
    }

    public bool WasPressedThisFrame
    {
        get
        {
            if (GetController() != null)
                return triggerAction.WasPressedThisFrame();

            // No controller: use hand pinch with edge detection.
            bool pinching = IsPinching();
            bool pressed = pinching && !wasPinching;
            wasPinching = pinching;
            return pressed;
        }
    }

    public Ray GetPointerRay()
    {
        XRController ctrl = GetController();
        if (ctrl != null)
            return new Ray(ctrl.devicePosition.ReadValue(), ctrl.deviceRotation.ReadValue() * Vector3.forward);

        if (TryGetHandAim(out Ray handRay))
            return handRay;

        // Fallback: aim from the camera center when no controller/hand is available.
        Camera cam = Camera.main;
        return cam != null ? new Ray(cam.transform.position, cam.transform.forward) : new Ray();
    }

    private static XRController GetController()
        => InputSystem.GetDevice<XRController>("RightHand");

    private static bool IsPinching()
    {
        foreach (XRHandSubsystem subsystem in GetRunningHandSubsystems())
        {
            if (!subsystem.rightHand.isTracked)
                continue;

            if (subsystem.rightHand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexTip) &&
                subsystem.rightHand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbTip))
                return Vector3.Distance(indexTip.position, thumbTip.position) < PinchDistanceMeters;
        }
        return false;
    }

    private static bool TryGetHandAim(out Ray ray)
    {
        foreach (XRHandSubsystem subsystem in GetRunningHandSubsystems())
        {
            if (!subsystem.rightHand.isTracked)
                continue;

            if (subsystem.rightHand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexTip) &&
                subsystem.rightHand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbTip))
            {
                Vector3 origin = (indexTip.position + thumbTip.position) * 0.5f;
                ray = new Ray(origin, indexTip.forward);
                return true;
            }
        }

        ray = default;
        return false;
    }

    private static List<XRHandSubsystem> GetRunningHandSubsystems()
    {
        var subs = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subs);
        subs.RemoveAll(s => s == null || !s.running);
        return subs;
    }
}
