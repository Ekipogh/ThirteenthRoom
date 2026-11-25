using UnityEngine;

public class SafeKey : ItemPickup, ITargetable
{
    public string TargetID { get; set; }
    public int SafeNumber { get; set; }

    protected override string InventoryDetail => SafeNumber > 0 ? $"Safe #{SafeNumber}" : null;

    public override string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        return SafeNumber > 0
            ? $"Press E to pick up key for safe #{SafeNumber}"
            : base.GetInteractionPrompt(playerInteractor);
    }
}
