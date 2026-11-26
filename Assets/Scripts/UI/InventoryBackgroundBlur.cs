using UnityEngine;

// Attached to the inventory owner's camera. The render feature queries this camera only.
[DisallowMultipleComponent]
public sealed class InventoryBackgroundBlur : MonoBehaviour
{
    public InventoryUI Owner { get; set; }
    public bool IsActive => isActiveAndEnabled && Owner != null && Owner.IsOpen && Owner.BlurRadius > 0f;
    public float Radius => Owner != null ? Owner.BlurRadius : 0f;
}
