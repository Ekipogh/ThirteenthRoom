using System;
using UnityEngine;

public class ObjectSpawnPoint : MonoBehaviour
{
    public ObjectSize MaximumSize = ObjectSize.Small;
    public SpawnObjectDefinition[] AllowedObjects = Array.Empty<SpawnObjectDefinition>();
    public SpawnObjectDefinition DefaultObject;
    public GameObject SpawnedObject { get; private set; }

    public bool Accepts(SpawnObjectDefinition definition) => definition != null && definition.IsValid &&
        Enum.IsDefined(typeof(ObjectSize), MaximumSize) && definition.RequiredSize <= MaximumSize &&
        Array.IndexOf(AllowedObjects ?? Array.Empty<SpawnObjectDefinition>(), definition) >= 0;

    public bool CanSpawn(SpawnObjectDefinition definition) => SpawnedObject == null && Accepts(definition);

    public GameObject Spawn(SpawnObjectDefinition definition)
    {
        if (!CanSpawn(definition)) throw new InvalidOperationException($"Object point '{name}' cannot spawn '{definition?.name}'.");
        SpawnedObject = Instantiate(definition.Prefab, transform, false);
        SpawnedObject.transform.localPosition = Vector3.zero;
        SpawnedObject.transform.localRotation = Quaternion.identity;
        return SpawnedObject;
    }

    public string ValidationError
    {
        get
        {
            if (!Enum.IsDefined(typeof(ObjectSize), MaximumSize)) return "Invalid maximum size.";
            if (AllowedObjects == null || AllowedObjects.Length == 0) return "Choose at least one allowed object.";
            foreach (var definition in AllowedObjects)
                if (definition == null || !definition.IsValid) return "Allowed objects contain a missing definition or invalid prefab/size.";
            if (DefaultObject != null && !Accepts(DefaultObject)) return "Default object does not fit or is not allowed.";
            return null;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = ValidationError == null ? Color.green : Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;
        float size = MaximumSize == ObjectSize.Small ? 1f : MaximumSize == ObjectSize.Medium ? 2f : 4f;
        Gizmos.DrawWireCube(Vector3.up * size * .5f, Vector3.one * size);
    }
}
