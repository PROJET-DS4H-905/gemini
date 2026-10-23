using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

/// <summary>
/// XR look: the camera follows the headset (head tracking). No manual rotation.
/// </summary>
public class HeadTrackedLookInput : ILookInput
{
    private XRHMD hmd;

    public void Initialize(Transform cameraTransform, LookSettings settings)
    {
        hmd = InputSystem.GetDevice<XRHMD>();
    }

    public void UpdateLook(Transform cameraTransform)
    {
        if (hmd == null)
            return;

        cameraTransform.localPosition = hmd.centerEyePosition.ReadValue();
        cameraTransform.localRotation = hmd.centerEyeRotation.ReadValue();
    }
}
