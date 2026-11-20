#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Third-party asset metadata is ignored by this repository. Keep the runtime
// bake's required readability settings reproducible after a fresh asset import.
public sealed class MansionNavigationMeshImporter : AssetPostprocessor, IPreprocessBuildWithReport
{
    static readonly string[] RuntimeMeshes = {
        "Assets/Thirdparty/PolygonHorrorMansion/Models/SM_Prop_Rug_01.fbx",
        "Assets/Thirdparty/PolygonHorrorMansion/Models/SM_Prop_Rug_05.fbx"
    };

    void OnPreprocessModel()
    {
        if (System.Array.IndexOf(RuntimeMeshes, assetPath) >= 0)
            ((ModelImporter)assetImporter).isReadable = true;
    }

    [InitializeOnLoadMethod]
    static void ScheduleValidation() => EditorApplication.delayCall += EnsureReadable;

    static void EnsureReadable()
    {
        foreach (string path in RuntimeMeshes)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null || importer.isReadable) continue;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }

    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) => EnsureReadable();
}
#endif
