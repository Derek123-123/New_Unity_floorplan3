using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PrefabLoader1 : MonoBehaviour
{
    [Header("Server base URL (must end with slash). Can be overridden per request via WebBridge JSON.")]
    //public string serverUrl = "http://localhost:8000/static/assetbundles/";
    public string serverUrl = "/static/assetbundles/";

    [Header("Parent for all spawned instances (optional)")]
    public Transform parentForInstances;

    [Header("Optional Canvas UI Manager (receives load/remove callbacks)")]
    public CanvasUiManager uiManager; 

    // ��������������key = instanceId
    private readonly Dictionary<string, GameObject> instances = new Dictionary<string, GameObject>();

    // ÿ�� instanceId ���d����ƣ�ȡ��/�汾���ƣ�
    private readonly Dictionary<string, int> loadVersions = new Dictionary<string, int>();
    private readonly Dictionary<string, Coroutine> runningLoads = new Dictionary<string, Coroutine>();

    // ���� API���� WebBridge ����
    // ����ͬ instanceId �Ѵ��ڌ��������� Despawn ���d���µģ�Replace��
    public void LoadAndSpawnMulti(string instanceId, string bundleName, string prefabName,string modelId, Vector3 position, Quaternion rotation, string overrideServerUrl = null)
    {
        if (string.IsNullOrEmpty(instanceId))
        {
            Debug.LogError("PrefabLoader.LoadAndSpawnMulti: instanceId is required and must be unique.");
            return;
        }
        if (string.IsNullOrEmpty(bundleName))
        {
            Debug.LogError("PrefabLoader.LoadAndSpawnMulti: bundleName is null/empty");
            return;
        }
        if (string.IsNullOrEmpty(prefabName))
        {
            prefabName = bundleName; // �A�O prefab �c bundle ͬ��
        }
        uiManager?.OnLoadStarted(bundleName, prefabName, instanceId);
        // ��ԓ instanceId �����d�룬��ȡ����
        if (runningLoads.TryGetValue(instanceId, out var co) && co != null)
        {
            StopCoroutine(co);
            runningLoads[instanceId] = null;
        }

        // bump �汾
        int v = 0;
        if (loadVersions.TryGetValue(instanceId, out var cur))
            v = cur + 1;
        loadVersions[instanceId] = v;

        // �� Replace ���ԣ�ͬһ instanceId ���Ѵ����f�������Ȅh��
        Despawn(instanceId);

        // �����d��
        var loadCo = StartCoroutine(CoLoadAndSpawn(instanceId, v, bundleName, prefabName, modelId, position, rotation, overrideServerUrl));
        runningLoads[instanceId] = loadCo;
    }

    // ж�d��һ����
  

    public void Despawn(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return;
        if (instances.TryGetValue(instanceId, out var go) && go != null)
        {
            // ֪ͨ UI���_ʼ�Ƴ�
            //if (notify)
                //uiManager?.OnRemoveStarted(GetBundleNameByInstance(instanceId), instanceId);

            Destroy(go);
        }
        instances.Remove(instanceId);

        
    }

    // 
    public void ClearAll()
    {
        foreach (var kv in instances)
        {
            if (kv.Value != null) Destroy(kv.Value);
        }
        instances.Clear();

        foreach (var kv in runningLoads)
        {
            if (kv.Value != null) StopCoroutine(kv.Value);
        }
        runningLoads.Clear();
        loadVersions.Clear();
        //uiManager?.OnClearAll();
    }

    private IEnumerator CoLoadAndSpawn(string instanceId, int version, string bundleName, string prefabName,string modelId, Vector3 position, Quaternion rotation, string overrideServerUrl)
    {
        // 1) 
        var baseUrl = string.IsNullOrEmpty(overrideServerUrl) ? (serverUrl ?? "") : overrideServerUrl;
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        var url = baseUrl + bundleName;

        // 2) 
        using (var req = UnityWebRequestAssetBundle.GetAssetBundle(url))
        {
            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                // �汾�z�飺��ͬһ instanceId �M�������d�룬�ŗ��f�d��
                if (!IsLoadVersionCurrent(instanceId, version))
                {
                    yield break;
                }
                yield return null;
            }

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError($"PrefabLoader[{instanceId}]: download failed. url={url}, err={req.error}");
                NotifyFailed(instanceId, bundleName, req.error);
                uiManager?.OnLoadFailed(bundleName, instanceId, req.error);
                yield break;
            }

            var bundle = DownloadHandlerAssetBundle.GetContent(req);
            if (bundle == null)
            {
                Debug.LogError($"PrefabLoader[{instanceId}]: bundle is null. url={url}");
                NotifyFailed(instanceId, bundleName, "bundle null");
                uiManager?.OnLoadFailed(bundleName, instanceId, "bundle null");
                yield break;
            }

            // 3) �� bundle �xȡ prefab
            GameObject prefab = null;
            try
            {
                prefab = bundle.LoadAsset<GameObject>(prefabName);
            }
            catch (Exception e)
            {
                Debug.LogError($"PrefabLoader[{instanceId}]: LoadAsset exception. bundle={bundleName}, prefab={prefabName}, ex={e}");
            }

            if (prefab == null)
            {
                Debug.LogError($"PrefabLoader[{instanceId}]: prefab not found. bundle={bundleName}, prefab={prefabName}");
                bundle.Unload(false);
                NotifyFailed(instanceId, bundleName, "prefab not found");
                uiManager?.OnLoadFailed(bundleName, instanceId, "prefab not found");
                yield break;
            }

            //
            if (!IsLoadVersionCurrent(instanceId, version))
            {
                bundle.Unload(false);
                yield break;
            }

            // 4) 
            var go = Instantiate(prefab, position, rotation, parentForInstances);
            go.name = string.IsNullOrEmpty(instanceId) ? prefabName : instanceId;
            instances[instanceId] = go;

            FloorplanRuntimeSetup.TryAssignRuntimeScripts(go);
            // 5) 
            bundle.Unload(false);

            // 6) ֪ͨ
            NotifyLoaded(instanceId, bundleName);
            uiManager?.OnLoadSucceeded(bundleName, prefabName, instanceId, modelId, go);
        }

        // clear running handle
        runningLoads[instanceId] = null;
    }

    private bool IsLoadVersionCurrent(string instanceId, int version)
    {
        return loadVersions.TryGetValue(instanceId, out var cur) && cur == version;
    }

    // 
    private void NotifyLoaded(string instanceId, string bundleName)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log($"[PrefabLoader] Loaded instance '{instanceId}' from bundle '{bundleName}'");
#else
        Debug.Log($"[PrefabLoader] Loaded instance '{instanceId}' from bundle '{bundleName}'");
#endif
    }

    private void NotifyFailed(string instanceId, string bundleName, string error)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log($"[PrefabLoader] Failed instance '{instanceId}' from bundle '{bundleName}': {error}");
#else
        Debug.LogWarning($"[PrefabLoader] Failed instance '{instanceId}' from bundle '{bundleName}': {error}");
#endif
    }
    private readonly Dictionary<string, string> instanceBundle = new Dictionary<string, string>();
    private string GetBundleNameByInstance(string instanceId)
    {
        if (instanceBundle.TryGetValue(instanceId, out var b)) return b;
        return null;
    }
}