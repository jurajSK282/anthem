using UnityEditor;
using UnityEngine;

// Každá importovaná textúra: Point filter, Compression None
public class TextureImportPostprocessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        var importer = (TextureImporter)assetImporter;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
