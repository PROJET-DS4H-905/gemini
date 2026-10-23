using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Non-XR select: unified pointer (mouse click / touch tap / pen).
/// </summary>
public class PointerSelectInput : ISelectInput
{
    public bool WasPressedThisFrame => Pointer.current != null && Pointer.current.press.wasPressedThisFrame;

    public Ray GetPointerRay()
    {
        Camera cam = Camera.main;
        if (cam == null || Pointer.current == null)
            return new Ray();

        return cam.ScreenPointToRay(Pointer.current.position.ReadValue());
    }
}
