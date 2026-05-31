using UnityEngine;

[CreateAssetMenu(fileName = "MonsterDefinition", menuName = "Scriptable Objects/MonsterDefinition")]
public class MonsterDefinition : ScriptableObject
{
    public GameObject MonsterPrefab;
    public string[] SpawnTags;
}
