using UnityEngine;

[CreateAssetMenu(fileName = "SpawnItemDefinition", menuName = "Scriptable Objects/SpawnItemDefinition")]
public class SpawnItemDefinition : ScriptableObject
{
    public ItemDefinition ItemDefinition;
    public GameObject ItemPrefab;
    public string targetID;
}
