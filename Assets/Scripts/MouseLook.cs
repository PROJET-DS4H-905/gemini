using UnityEngine;

/// <summary>
/// Camera look controller. Delegates to an ILookInput chosen by platform:
/// mouse/touch look on desktop/mobile, head tracking in XR.
/// </summary>
public class MouseLook : MonoBehaviour
{
    [Tooltip("Camera rotation in degrees per unit of look input.")]
    [SerializeField] private float sensitivity = 0.15f;

    [Tooltip("Maximum pitch angle in degrees (89 avoids the gimbal flip at 90).")]
    [SerializeField] private float pitchClamp = 89f;

    private ILookInput look;

    private void Start()
    {
        look = LookInputFactory.Create();
        look.Initialize(transform, new LookSettings { sensitivity = sensitivity, pitchClamp = pitchClamp });
    }

    private void LateUpdate()
    {
        look.UpdateLook(transform);
    }
}
