using UnityEngine;

public struct LookSettings
{
    public float sensitivity;
    public float pitchClamp;
}

/// <summary>
/// Abstracts how the camera is driven each frame (mouse/touch look vs head tracking).
/// </summary>
public interface ILookInput
{
    void Initialize(Transform cameraTransform, LookSettings settings);
    void UpdateLook(Transform cameraTransform);
}
