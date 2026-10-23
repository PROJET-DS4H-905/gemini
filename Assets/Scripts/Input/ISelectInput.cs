using UnityEngine;

/// <summary>
/// Abstracts how the user points at / clicks things (mouse/touch/controller/hand).
/// </summary>
public interface ISelectInput
{
    bool WasPressedThisFrame { get; }
    Ray GetPointerRay(); // world-space aim ray
}
