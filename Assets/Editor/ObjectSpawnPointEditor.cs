using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObjectSpawnPoint))]
[CanEditMultipleObjects]
public class ObjectSpawnPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        foreach (ObjectSpawnPoint point in targets)
            if (point.ValidationError != null) EditorGUILayout.HelpBox(point.ValidationError, MessageType.Error);
        EditorGUILayout.HelpBox("One object per point. Larger points accept smaller allowed objects. Gizmos illustrate size tiers, not measured bounds.", MessageType.Info);
    }
}
