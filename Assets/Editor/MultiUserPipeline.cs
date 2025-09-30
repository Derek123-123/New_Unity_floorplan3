#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MultiUserPipeline
{
    // 預設路徑（可被環境變數覆寫）
    // 來源：server/uploads/models/{userId}/{modelId}/*.fbx
    const string ServerUploadRoot_Default = @"C:\Users\derek\Documents\GitHub\floorPlanTo3D\FloorPlanTo3D_original3\mobile_app\uploads\models";
    // 輸出：{BUNDLE_OUT_ROOT}/{Target}/...（Unity 的 BuildAssetBundles 會輸出在這層）
    const string BundleOutRoot_Default    = @"C:\Users\derek\Documents\GitHub\floorPlanTo3D\FloorPlanTo3D_original3\mobile_app\static\assetbundles";

    // 專案內中繼與產出
    const string IncomingRoot = "Assets/IncomingFbx";
    const string PrefabRoot   = "Assets/GeneratedPrefabs";

    // 允許以環境變數覆寫（routes.py 已設置 ENV["UPLOADS_ROOT"]、ENV["BUNDLE_OUT_ROOT"]）
    static string GetEnv(string name, string fallback)
    {
        var v = System.Environment.GetEnvironmentVariable(name);
        return string.IsNullOrEmpty(v) ? fallback : v;
    }
    static string ServerUploadRoot => GetEnv("UPLOADS_ROOT", ServerUploadRoot_Default);
    static string BundleOutRoot    => GetEnv("BUNDLE_OUT_ROOT", BundleOutRoot_Default);

    [MenuItem("Tools/Multi-User Pipeline/Process All")]
    public static void ProcessAll_Menu() => ProcessAll();

    // 命令列：全量處理
    // Unity -batchmode -nographics -quit -projectPath "<path>" -executeMethod MultiUserPipeline.ProcessAll
    public static void ProcessAll()
    {
        Debug.Log($"[MultiUserPipeline] ProcessAll UPLOADS_ROOT={ServerUploadRoot} BUNDLE_OUT_ROOT={BundleOutRoot}");
        EnsureDirs();

        int imported = MirrorUploadsIntoProject();
        AssetDatabase.Refresh();

        int prefabs  = CreateOrUpdatePrefabsForAllIncoming();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 注意：指派 bundle 名稱已在 CreateOrUpdatePrefab 完成，不再覆蓋
        // AssignBundleNames(); // 保留函式，但不再在這裡呼叫以避免覆蓋

        BuildBundles();

        Debug.Log($"[MultiUserPipeline] Done. Imported FBX: {imported}, Prefabs updated: {prefabs}");
    }

    // 命令列：真正的 “Build Missing” 流程
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

                // 只為缺少輸出的模型鏡像與建立 Prefab
                imported += MirrorOneIntoProject(userId, modelId);

                AssetDatabase.Refresh();

                // 只更新該模型的 prefab（並於建立時就設定好 bundleName）
                string fbxRoot = Path.Combine(IncomingRoot, userId, modelId).Replace('\\','/');
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

        // 指派 bundle 名稱：已在 CreateOrUpdatePrefab 內直接指定正確的 3 層路徑（userId/modelId/{fbxBase}）
        // AssignBundleNames(); // 不再全量覆蓋

        // 建置：Unity 會利用快取對未變動資產快速跳過
        BuildBundles();

        Debug.Log($"[MultiUserPipeline] ProcessMissingOnly Done. Imported: {imported}, Prefabs updated: {updated}, Skipped(existing): {skipped}");
    }

    // 命令列：只處理單一 userId/modelId
    // Unity -batchmode -nographics -quit -projectPath "<path>" -executeMethod MultiUserPipeline.ProcessOneCLI -userId 123 -modelId chairA
    public static void ProcessOneCLI()
    {
        var args = System.Environment.GetCommandLineArgs();
        string userId  = GetArg(args, "-userId");
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
            string fbxRoot = Path.Combine(IncomingRoot, userId, modelId).Replace('\\','/');
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { fbxRoot });
            foreach (var guid in guids)
            {
                string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
                if (CreateOrUpdatePrefab(userId, modelId, fbxPath)) prefabs++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // AssignBundleNames(); // 不覆蓋
        BuildBundles();

        Debug.Log($"[MultiUserPipeline] ProcessOneCLI done. Imported: {imported}, Prefabs: {prefabs}");
    }

    // ========== 工具方法 ==========

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

    // 檢查 bundle 是否已存在（依據 bundleName = "{userId}/{modelId}/{fbxBase}"）
    static bool BundleExists(string userId, string modelId, BuildTarget target)
    {
        string outRoot = Path.Combine(BundleOutRoot, target.ToString()).Replace('\\','/');
        // 因為我們希望最終檔名是「fbxBase」，此處無法在不知 fbxBase 的情況精準檢查。
        // 退而求其次：若資料夾 {userId}/{modelId} 底下有任何檔案視為已存在。
        string modelFolder = Path.Combine(outRoot, $"{userId}/{modelId}").Replace('\\','/');
        if (!Directory.Exists(modelFolder)) return false;
        try
        {
            // 有任一非 manifest 或任一檔案即可視為存在
            var files = Directory.GetFiles(modelFolder);
            return files != null && files.Length > 0;
        }
        catch { return false; }
    }

    // 全量鏡像
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

    // 取得 .fbx 候選（含大小寫）
    static IEnumerable<string> GetFbxCandidates(string dir)
    {
        foreach (var pat in new[]{ "*.fbx", "*.FBX" })
            foreach (var f in Directory.GetFiles(dir, pat, SearchOption.TopDirectoryOnly))
                yield return f;
    }

    // 鏡像單一模型
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

        string dstDir = Path.Combine(IncomingRoot, userId, modelId).Replace('\\','/');
        Directory.CreateDirectory(dstDir);
        string dstFbx = Path.Combine(dstDir, Path.GetFileName(fbx)).Replace('\\','/');

        // 拷貝 FBX
        File.Copy(fbx, dstFbx, true);

        // 可選：拷貝 textures 子資料夾（若使用外部貼圖）
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

        AssetDatabase.ImportAsset(dstFbx, ImportAssetOptions.ForceUpdate); // 觸發導入
        Debug.Log($"[Mirror] Imported {userId}/{modelId}");
        return 1;
    }

    // 全量 prefab 生成/更新（用於 ProcessAll）
    static int CreateOrUpdatePrefabsForAllIncoming()
    {
        int updated = 0;
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { IncomingRoot });
        foreach (var guid in guids)
        {
            string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
            var parts = fbxPath.Replace('\\','/').Split('/');
            int idx = System.Array.IndexOf(parts, "IncomingFbx");
            if (idx < 0 || parts.Length < idx + 3) continue;

            string userId = parts[idx + 1];
            string modelId = parts[idx + 2];

            if (CreateOrUpdatePrefab(userId, modelId, fbxPath)) updated++;
        }
        return updated;
    }

    // 生成或更新單一 prefab
    // 關鍵修改：此處直接設定 assetBundleName = "{userId}/{modelId}/{fbxBase}"
    // 這樣輸出將會是：.../static/assetbundles/WebGL/{userId}/{modelId}/{fbxBase}
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

        // 依需求可在這裡加上自動掛件或命名規範
        var temp = Object.Instantiate(source);
        temp.name = modelId;

        string prefabDir  = Path.Combine(PrefabRoot, userId, modelId).Replace('\\','/');
        Directory.CreateDirectory(prefabDir);
        string prefabPath = Path.Combine(prefabDir, $"{modelId}.prefab").Replace('\\','/');

        PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
        Object.DestroyImmediate(temp);

        // 以 FBX 檔名（不含副檔名）作為最末端 bundle 檔名
        var baseName = Path.GetFileNameWithoutExtension(fbxPath); // 例如 "floorplan_2_blender"

        var importer = AssetImporter.GetAtPath(prefabPath);
        if (importer != null)
        {
            // 設成三層：{userId}/{modelId}/{fbxBase}
            importer.assetBundleName = $"{userId}/{modelId}/{baseName}";
            importer.assetBundleVariant = "";
        }

        Debug.Log($"[Prefab] Saved: {prefabPath} | bundleName={userId}/{modelId}/{baseName}");
        return true;
    }

    // 指派 bundle 名稱：bundleName = "{userId}/{modelId}"
    // 保留函式以相容舊流程；現在改為「若已有 bundleName 則不覆蓋；若沒有才設為兩層（較保守）」。
    // 建議：新流程已於 CreateOrUpdatePrefab 設定最終三層 bundleName，此函式可以不再呼叫。
    static void AssignBundleNames()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });
        foreach (var guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var parts = path.Replace('\\','/').Split('/');
            // Assets/GeneratedPrefabs/{userId}/{modelId}/{modelId}.prefab
            int idx = System.Array.IndexOf(parts, "GeneratedPrefabs");
            if (idx < 0 || parts.Length < idx + 3) continue;

            string userId  = parts[idx + 1];
            string modelId = parts[idx + 2];

            var importer = AssetImporter.GetAtPath(path);
            if (importer == null) continue;

            // 若已在 CreateOrUpdatePrefab 設定為三層，這裡不覆蓋
            if (!string.IsNullOrEmpty(importer.assetBundleName))
                continue;

            importer.assetBundleName = $"{userId}/{modelId}";
            importer.assetBundleVariant = "";
        }
        AssetDatabase.RemoveUnusedAssetBundleNames();
    }

    // 建置所有已指派 bundleName 的資產
    static void BuildBundles()
    {
        var target = EditorUserBuildSettings.activeBuildTarget;
        string outDir = Path.Combine(BundleOutRoot, target.ToString()).Replace('\\','/');
        Directory.CreateDirectory(outDir);

        // 建議使用 ChunkBasedCompression；如需與舊版一致可改 None / LZ4
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