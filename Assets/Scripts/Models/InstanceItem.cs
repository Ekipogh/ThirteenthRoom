public class InstanceItem
{
    public ItemDefinition Definition;
    public string TargetID;
    public string InventoryDetail;

    public InstanceItem(ItemDefinition definition, string targetID, string inventoryDetail = null)
    {
        Definition = definition;
        TargetID = targetID;
        InventoryDetail = inventoryDetail;
    }
}