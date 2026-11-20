using UnityEditor;

// These meshes are collider sources for the generated mansion's runtime NavMesh.
// Third-party model metadata is ignored by Git, so enforce this on fresh imports too.
public sealed class BookshelfModelImportSettings : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        switch (assetPath)
        {
            case "Assets/Thirdparty/PolygonHorrorMansion/Models/SM_Bld_Bookshelf_01.fbx":
            case "Assets/Thirdparty/PolygonHorrorMansion/Models/SM_Bld_Bookshelf_01_Insert_01.fbx":
            case "Assets/Thirdparty/PolygonHorrorMansion/Models/SM_Bld_Bookshelf_02.fbx":
                ((ModelImporter)assetImporter).isReadable = true;
                break;
        }
    }
}
