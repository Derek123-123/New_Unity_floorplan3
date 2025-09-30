#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class EnableReadableForIncoming : AssetPostprocessor
{
    static bool IsIncoming(string p)
    {
        var s = p.Replace('\\','/').ToLowerInvariant();
        return s.StartsWith("assets/incomingfbx/");
    }

    void OnPreprocessModel()
    {
        if (!IsIncoming(assetPath)) return;

        var importer = (ModelImporter)assetImporter;
        importer.isReadable = true; // Allow future mesh access if needed

        // Optional import defaults (safe/common):
        // importer.meshCompression = ModelImporterMeshCompression.Off;
        // importer.importNormals = ModelImporterNormals.Import; // or Calculate
        // importer.importTangents = ModelImporterTangents.CalculateMikk;
        // importer.globalScale = 1f;
        // importer.weldVertices = true;
        // importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
    }

    void OnPostprocessModel(GameObject go)
    {
        if (!IsIncoming(assetPath)) return;
        Debug.Log($"[EnableReadableForIncoming] Read/Write enabled: {assetPath}");
    }
}
#endif