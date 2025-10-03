#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MultiUserPipeline
{
    // Source uploads layout:
    // server/uploads/models/{userId}/{modelId}/*.fbx
    // const string ServerUploadRoot_Default = @"C:\Users\derek\Documents\GitHub\floorPlanTo3D\FloorPlanTo3D_original3\mobile_app\uploads\models";
    // const string ServerUploadRoot_Default = @"C:\Users\huxin\Documents\GitHub\floorPlanTo3D\FloorPlanTo3D_original3\mobile_app\uploads\models";
    static readonly string ServerUploadRoot_Default =
        Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                     "floorPlanTo3D", "mobile_app", "uploads", "models");

    // AssetBundle output root:
    // {BUNDLE_OUT_ROOT}/{Target}/... (used by Unity's BuildPipeline.BuildAssetBundles)
    // const string BundleOutRoot_Default    = @"C:\Users\derek\Documents\GitHub\floorPlanTo3D\FloorPlanTo3D_original3\mobile_app\static\assetbundles";
    // const string BundleOutRoot_Default = @"C:\Users\huxin\Documents\GitHub\floorPlanTo3D\FloorPlanTo3D_original3\mobile_app\static\assetbundles";
    static readonly string BundleOutRoot_Default =
        Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                     "floorPlanTo3D", "mobile_app", "static", "assetbundles");

    // Project-local working folders
    const string IncomingRoot = "Assets/IncomingFbx";
    const string PrefabRoot = "Assets/GeneratedPrefabs";

    // Read env overrides (routes.py may set ENV["UPLOADS_ROOT"] / ENV["BUNDLE_OUT_ROOT"])
    static string GetEnv(string name, string fallback)
    {
        var v = System.Environment.GetEnvironmentVariable(name);
        return string.IsNullOrEmpty(v) ? fallback : v;
    }
    static string ServerUploadRoot => GetEnv("UPLOADS_ROOT", ServerUploadRoot_Default);
    static string BundleOutRoot => GetEnv("BUNDLE_OUT_ROOT", BundleOutRoot_Default);

    [MenuItem("Tools/Multi-User Pipeline/Process All")]
    public static void ProcessAll_Menu() => ProcessAll();

    // Batch entry: process everything
    // Unity -batchmode -nographics -quit -projectPath "<path>" -executeMethod MultiUserPipeline.ProcessAll
    public static void ProcessAll()
    {
        Debug.Log($"[MultiUserPipeline] ProcessAll UPLOADS_ROOT={ServerUploadRoot} BUNDLE_OUT_ROOT={BundleOutRoot}");
        EnsureDirs();

        int imported = MirrorUploadsIntoProject();
        AssetDatabase.Refresh();

        int prefabs = CreateOrUpdatePrefabsForAllIncoming();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // If you want to assign bundle names separately instead of per prefab, enable this.
        // AssignBundleNames(); // Optional: not needed when set in CreateOrUpdatePrefab

        BuildBundles();

        Debug.Log($"[MultiUserPipeline] Done. Imported FBX: {imported}, Prefabs updated: {prefabs}");
    }

    // Batch entry: process only missing bundles ("Build Missing")
    // Unity -batchmode -nographics -quit -projectPath "<path>" -executeMethod MultiUserPipeline.ProcessMissingOnly
    public static void ProcessMissingOnly()
    {
        Debug.Log($"[MultiUserPipeline] ProcessMissingOnly UPLOADS_ROOT={ServerUploadRoot} BUNDLE_OUT_ROOT={BundleOutRoot}");
        EnsureDirs();

        var target = EditorUserBuildSettings.activeBuildTarget;
        int imported = 0, updated = 0, skipped = 0;

        foreach (var userDir in EnumDirs(ServerUploadRoot))
        {
            string userId = Path.GetFileName(userDir);
            foreach (var modelDir in EnumDirs(userDir))
            {
                string modelId = Path.GetFileName(modelDir);

                if (BundleExists(userId, modelId, target))
                {
                    skipped++;
                    Debug.Log($"[Skip] Already built: {userId}/{modelId}");
                    continue;
                }

                // Mirror source into project and create prefab
                imported += MirrorOneIntoProject(userId, modelId);

                AssetDatabase.Refresh();

                // Create/update prefab for each FBX we just mirrored; also sets bundleName
                string fbxRoot = Path.Combine(IncomingRoot, userId, modelId).Replace('\\', '/');
                string[] guids = AssetDatabase.FindAssets("t:Model", new[] { fbxRoot });
                foreach (var guid in guids)
                {
                    string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (CreateOrUpdatePrefab(userId, modelId, fbxPath)) updated++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // If you assign bundle names outside CreateOrUpdatePrefab, call this.
        // AssignBundleNames(); // Not required with current flow

        // Build bundles for the active target
        BuildBundles();

        Debug.Log($"[MultiUserPipeline] ProcessMissingOnly Done. Imported: {imported}, Prefabs updated: {updated}, Skipped(existing): {skipped}");
    }

    // Batch entry: process a specific userId/modelId
    // Unity -batchmode -nographics -quit -projectPath "<path>" -executeMethod MultiUserPipeline.ProcessOneCLI -userId 123 -modelId chairA
    public static void ProcessOneCLI()
    {
        var args = System.Environment.GetCommandLineArgs();
        string userId = GetArg(args, "-userId");
        string modelId = GetArg(args, "-modelId");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(modelId))
        {
            Debug.LogWarning("[MultiUserPipeline] ProcessOneCLI missing -userId or -modelId. Fallback to ProcessAll.");
            ProcessAll();
            return;
        }

        Debug.Log($"[MultiUserPipeline] ProcessOneCLI userId={userId} modelId={modelId} UPLOADS_ROOT={ServerUploadRoot} BUNDLE_OUT_ROOT={BundleOutRoot}");
        EnsureDirs();

        int imported = MirrorOneIntoProject(userId, modelId);
        AssetDatabase.Refresh();

        int prefabs = 0;
        if (imported > 0)
        {
            string fbxRoot = Path.Combine(IncomingRoot, userId, modelId).Replace('\\', '/');
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { fbxRoot });
            foreach (var guid in guids)
            {
                string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
                if (CreateOrUpdatePrefab(userId, modelId, fbxPath)) prefabs++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // AssignBundleNames(); // Not needed; set during prefab creation
        BuildBundles();

        Debug.Log($"[MultiUserPipeline] ProcessOneCLI done. Imported: {imported}, Prefabs: {prefabs}");
    }

    // ========== Helpers ==========

    static string GetArg(string[] args, string key)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == key) return args[i + 1];
        return null;
    }

    static void EnsureDirs()
    {
        Directory.CreateDirectory(IncomingRoot);
        Directory.CreateDirectory(PrefabRoot);
        Directory.CreateDirectory(ServerUploadRoot);
        Directory.CreateDirectory(BundleOutRoot);
    }

    static IEnumerable<string> EnumDirs(string root)
    {
        if (!Directory.Exists(root)) yield break;
        foreach (var d in Directory.GetDirectories(root)) yield return d;
    }

    // Check if any bundle exists under bundleName = "{userId}/{modelId}/{fbxBase}"
    static bool BundleExists(string userId, string modelId, BuildTarget target)
    {
        string outRoot = Path.Combine(BundleOutRoot, target.ToString()).Replace('\\', '/');
        // We don't know exact fbxBase beforehand; if any file exists under {userId}/{modelId},
        // we treat it as already built.
        string modelFolder = Path.Combine(outRoot, $"{userId}/{modelId}").Replace('\\', '/');
        if (!Directory.Exists(modelFolder)) return false;
        try
        {
            var files = Directory.GetFiles(modelFolder);
            return files != null && files.Length > 0;
        }
        catch { return false; }
    }

    // Mirror all users/models
    static int MirrorUploadsIntoProject()
    {
        int count = 0;
        foreach (var userDir in EnumDirs(ServerUploadRoot))
        {
            string userId = Path.GetFileName(userDir);
            foreach (var modelDir in EnumDirs(userDir))
            {
                string modelId = Path.GetFileName(modelDir);
                if (MirrorOneIntoProject(userId, modelId) > 0) count++;
            }
        }
        return count;
    }

    // Enumerate .fbx candidates (case-insensitive)
    static IEnumerable<string> GetFbxCandidates(string dir)
    {
        foreach (var pat in new[] { "*.fbx", "*.FBX" })
            foreach (var f in Directory.GetFiles(dir, pat, SearchOption.TopDirectoryOnly))
                yield return f;
    }

    // Mirror a single userId/modelId into Assets/IncomingFbx
    static int MirrorOneIntoProject(string userId, string modelId)
    {
        string modelDir = Path.Combine(ServerUploadRoot, userId, modelId);
        if (!Directory.Exists(modelDir))
        {
            Debug.LogWarning($"[Mirror] Source not found: {modelDir}");
            return 0;
        }

        var fbx = GetFbxCandidates(modelDir).FirstOrDefault();
        if (fbx == null)
        {
            Debug.LogWarning($"[Mirror] No FBX in {modelDir}");
            return 0;
        }

        string dstDir = Path.Combine(IncomingRoot, userId, modelId).Replace('\\', '/');
        Directory.CreateDirectory(dstDir);
        string dstFbx = Path.Combine(dstDir, Path.GetFileName(fbx)).Replace('\\', '/');

        // Copy FBX
        File.Copy(fbx, dstFbx, true);

        // Optionally copy textures folder if exists
        string texSrc = Path.Combine(modelDir, "textures");
        string texDst = Path.Combine(dstDir, "textures");
        if (Directory.Exists(texSrc))
        {
            Directory.CreateDirectory(texDst);
            foreach (var file in Directory.GetFiles(texSrc))
            {
                File.Copy(file, Path.Combine(texDst, Path.GetFileName(file)), true);
            }
        }

        AssetDatabase.ImportAsset(dstFbx, ImportAssetOptions.ForceUpdate); // Force reimport
        Debug.Log($"[Mirror] Imported {userId}/{modelId}");
        return 1;
    }

    // Create or update prefabs for all incoming FBX
    static int CreateOrUpdatePrefabsForAllIncoming()
    {
        int updated = 0;
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { IncomingRoot });
        foreach (var guid in guids)
        {
            string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
            var parts = fbxPath.Replace('\\', '/').Split('/');
            int idx = System.Array.IndexOf(parts, "IncomingFbx");
            if (idx < 0 || parts.Length < idx + 3) continue;

            string userId = parts[idx + 1];
            string modelId = parts[idx + 2];

            if (CreateOrUpdatePrefab(userId, modelId, fbxPath)) updated++;
        }
        return updated;
    }

    // Create or update a prefab
    // We also set assetBundleName = "{userId}/{modelId}/{fbxBase}"
    // Final output will be .../static/assetbundles/<Target>/{userId}/{modelId}/{fbxBase}
    static bool CreateOrUpdatePrefab(string userId, string modelId, string fbxPath)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (source == null)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            source = all.OfType<GameObject>().FirstOrDefault();
            if (source == null)
            {
                Debug.LogWarning($"[Prefab] No GameObject found in {fbxPath}");
                return false;
            }
        }

        // Instantiate temp to allow modifications if needed
        var temp = Object.Instantiate(source);
        temp.name = modelId;

        string prefabDir = Path.Combine(PrefabRoot, userId, modelId).Replace('\\', '/');
        Directory.CreateDirectory(prefabDir);
        string prefabPath = Path.Combine(prefabDir, $"{modelId}.prefab").Replace('\\', '/');

        PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
        Object.DestroyImmediate(temp);

        // Set bundle name on the prefab importer
        var baseName = Path.GetFileNameWithoutExtension(fbxPath); // e.g. "floorplan_2_blender"

        var importer = AssetImporter.GetAtPath(prefabPath);
        if (importer != null)
        {
            importer.assetBundleName = $"{userId}/{modelId}/{baseName}";
            importer.assetBundleVariant = "";
        }

        Debug.Log($"[Prefab] Saved: {prefabPath} | bundleName={userId}/{modelId}/{baseName}");
        return true;
    }

    // Optional: assign bundle names if not set during prefab creation
    // bundleName = "{userId}/{modelId}"
    static void AssignBundleNames()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });
        foreach (var guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var parts = path.Replace('\\', '/').Split('/');
            // Expected: Assets/GeneratedPrefabs/{userId}/{modelId}/{modelId}.prefab
            int idx = System.Array.IndexOf(parts, "GeneratedPrefabs");
            if (idx < 0 || parts.Length < idx + 3) continue;

            string userId = parts[idx + 1];
            string modelId = parts[idx + 2];

            var importer = AssetImporter.GetAtPath(path);
            if (importer == null) continue;

            // Skip if already set in CreateOrUpdatePrefab
            if (!string.IsNullOrEmpty(importer.assetBundleName))
                continue;

            importer.assetBundleName = $"{userId}/{modelId}";
            importer.assetBundleVariant = "";
        }
        AssetDatabase.RemoveUnusedAssetBundleNames();
    }

    // Build all bundles that have a bundleName assigned
    static void BuildBundles()
    {
        var target = EditorUserBuildSettings.activeBuildTarget;
        string outDir = Path.Combine(BundleOutRoot, target.ToString()).Replace('\\', '/');
        Directory.CreateDirectory(outDir);

        // Compression option: ChunkBasedCompression (you can switch to None/LZ4)
        var options = BuildAssetBundleOptions.ChunkBasedCompression;
        var manifest = BuildPipeline.BuildAssetBundles(outDir, options, target);

        if (manifest == null)
        {
            Debug.LogError($"[BuildBundles] Build failed for target={target} outDir={outDir}");
        }
        else
        {
            Debug.Log($"[BuildBundles] Output: {outDir} | Bundles: {manifest.GetAllAssetBundles().Length}");
        }
    }
}
#endif