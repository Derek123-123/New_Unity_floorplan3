using UnityEngine;
using System;

public class WebBridge1 : MonoBehaviour
{
    [Header("Default server URL (ends with slash). Optional, can be overridden per request")]
    //public string serverUrl = "http://localhost:8000/static/assetbundles/";
    public string serverUrl = "/static/assetbundles/";

    [Header("Link to PrefabLoader (auto-find if null)")]
    public PrefabLoader1 loader;

    public EditButton editButton;

    [Serializable]
    private class LoadReq
    {
        public string instanceId;   // 唯一 ID，前端自己決定（必填）
        public string bundleName;   // 伺服器上的 AssetBundle 檔名（必要）
        public string prefabName;   // 包內 prefab 名稱（可省略，預設 = bundleName）
        public string modelId;
        public float px, py, pz;    // 生成位置（可選）
        public float rx, ry, rz;    // 生成旋轉（可選）

        public string serverUrl;    // 覆寫用（可選），若提供則用這個
    }

    void Awake()
    {
        if (loader == null)
            loader = FindObjectOfType<PrefabLoader1>();
        if (loader == null)
            Debug.LogError("WebBridge: PrefabLoader instance not found in scene.");
    }

    // 由 JS 呼叫：SendMessage('WebBridge', 'LoadFromWeb', json)
    public void LoadFromWeb(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("WebBridge.LoadFromWeb: empty json");
            return;
        }

        LoadReq req = null;
        try
        {
            req = JsonUtility.FromJson<LoadReq>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"WebBridge.LoadFromWeb: JSON parse error: {e}");
            return;
        }

        if (req == null || string.IsNullOrEmpty(req.instanceId))
        {
            Debug.LogError("WebBridge.LoadFromWeb: missing instanceId");
            return;
        }
        if (string.IsNullOrEmpty(req.bundleName))
        {
            Debug.LogError("WebBridge.LoadFromWeb: missing bundleName");
            return;
        }

        if (loader == null)
        {
            loader = FindObjectOfType<PrefabLoader1>();
            if (loader == null)
            {
                Debug.LogError("WebBridge.LoadFromWeb: loader is null.");
                return;
            }
        }

        var prefabName = string.IsNullOrEmpty(req.prefabName) ? req.bundleName : req.prefabName;

        // 位置與旋轉（若沒帶值，就用 0）
        var pos = new Vector3(req.px, req.py, req.pz);
        var rot = Quaternion.Euler(req.rx, req.ry, req.rz);

        // 可選：每請求覆寫 serverUrl
        var effectiveServerUrl = string.IsNullOrEmpty(req.serverUrl) ? serverUrl : req.serverUrl;

        Debug.Log($"WebBridge: Load instanceId='{req.instanceId}', bundle='{req.bundleName}', prefab='{prefabName}' @ pos={pos}, rot={rot.eulerAngles}, server='{effectiveServerUrl}'");

        loader.LoadAndSpawnMulti(req.instanceId, req.bundleName, prefabName, req.modelId, pos, rot, effectiveServerUrl);

        //editButton.TriggerLoadWhenReady();
    }

    // 可選：供 JS 呼叫以刪除單一實例
    public void DespawnFromWeb(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return;
        if (loader == null) loader = FindObjectOfType<PrefabLoader1>();
        if (loader == null) return;
        loader.Despawn(instanceId);
    }

    // 可選：供 JS 呼叫以清空全部
    public void ClearAllFromWeb()
    {
        if (loader == null) loader = FindObjectOfType<PrefabLoader1>();
        if (loader == null) return;
        loader.ClearAll();
    }
}