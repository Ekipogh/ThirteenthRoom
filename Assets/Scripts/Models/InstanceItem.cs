public class InstanceItem
{
    public ItemDefinition Definition;
    public string TargetID;

    public InstanceItem(ItemDefinition definition, string targetID)
    {
        Definition = definition;
        TargetID = targetID;
    }
}