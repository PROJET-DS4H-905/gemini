using UnityEngine.XR;

/// <summary>
/// Factory: picks the select implementation for the current platform.
/// </summary>
public static class SelectInputFactory
{
    public static ISelectInput Create()
        => XRSettings.isDeviceActive ? new XRSelectInput() : new PointerSelectInput();
}
