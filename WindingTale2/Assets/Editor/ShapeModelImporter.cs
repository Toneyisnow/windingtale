using UnityEditor;

/// <summary>
/// Keeps the remastered tile models (Resources/Shapes/Shapes_NN/*.obj) readable
/// from script. ShapesLayer measures each tile's surface height off its mesh so
/// the cursor and range indicators can sit on top of tall tiles (a bridge deck);
/// a model imported without Read/Write enabled returns no vertex data in a
/// player build, and the tile would silently be treated as flat. Runs on every
/// (re)import, so newly exported chapters pick the setting up automatically.
/// </summary>
public class ShapeModelImporter : AssetPostprocessor
{
    private const string ShapesFolder = "/Resources/Shapes/";

    void OnPreprocessModel()
    {
        if (!assetPath.Contains(ShapesFolder))
        {
            return;
        }

        ModelImporter importer = assetImporter as ModelImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
        }
    }
}
