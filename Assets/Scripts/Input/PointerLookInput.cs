using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Non-XR look: reads the project-wide "Player/Look" action (mouse/touch/gamepad)
/// and rotates the camera, with desktop cursor lock (Esc unlock / click re-lock).
/// </summary>
public class PointerLookInput : ILookInput
{
    private InputAction lookAction;
    private float sensitivity;
    private float pitchClamp;
    private float yaw;
    private float pitch;

    public void Initialize(Transform cameraTransform, LookSettings settings)
    {
        sensitivity = settings.sensitivity;
        pitchClamp = settings.pitchClamp;

        Vector3 euler = cameraTransform.eulerAngles;
        yaw = euler.y;
        pitch = NormalizePitch(euler.x);

        lookAction = InputSystem.actions != null
            ? InputSystem.actions.FindAction("Player/Look")
            : null;
        if (lookAction != null)
            lookAction.Enable();

        if (Mouse.current != null)
            LockCursor();
    }

    public void UpdateLook(Transform cameraTransform)
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            UnlockCursor();

        if (Mouse.current != null && Cursor.lockState != CursorLockMode.Locked)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                LockCursor();
            return;
        }

        if (lookAction == null)
            return;

        Vector2 delta = lookAction.ReadValue<Vector2>();
        yaw += delta.x * sensitivity;
        pitch -= delta.y * sensitivity;               // up = look up
        pitch = Mathf.Clamp(pitch, -pitchClamp, pitchClamp);
        cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private static float NormalizePitch(float eulerX)
    {
        return eulerX > 180f ? eulerX - 360f : eulerX;
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
