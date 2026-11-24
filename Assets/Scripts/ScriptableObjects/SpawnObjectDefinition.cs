using System;
using UnityEngine;

public enum ObjectSize { Small, Medium, Large }

[CreateAssetMenu(fileName = "SpawnObjectDefinition", menuName = "Mansion/Spawn Object Definition")]
public class SpawnObjectDefinition : ScriptableObject
{
    public GameObject Prefab;
    public ObjectSize RequiredSize = ObjectSize.Small;
    public bool IsValid => Prefab != null && Enum.IsDefined(typeof(ObjectSize), RequiredSize);
}

[Serializable]
public class ObjectRequirement
{
    public SpawnObjectDefinition Object;
    [Min(1)] public int Quantity = 1;
}
