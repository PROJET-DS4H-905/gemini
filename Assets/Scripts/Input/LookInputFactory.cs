using UnityEngine.XR;

/// <summary>
/// Factory: picks the look implementation for the current platform.
/// </summary>
public static class LookInputFactory
{
    public static ILookInput Create()
        => XRSettings.isDeviceActive ? new HeadTrackedLookInput() : new PointerLookInput();
}
