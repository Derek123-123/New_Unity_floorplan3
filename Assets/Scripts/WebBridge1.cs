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
        public string instanceId;   // Ψһ ID��ǰ���Լ��Q�������
        public string bundleName;   // �ŷ����ϵ� AssetBundle �n������Ҫ��
        public string prefabName;   // ���� prefab ���Q����ʡ�ԣ��A�O = bundleName��
        public string modelId;
        public float px, py, pz;    // ����λ�ã����x��
        public float rx, ry, rz;    // �������D�����x��

        public string serverUrl;    // �����ã����x�������ṩ�t���@��
    }

    void Awake()
    {
        if (loader == null)
            loader = FindObjectOfType<PrefabLoader1>();
        if (loader == null)
            Debug.LogError("WebBridge: PrefabLoader instance not found in scene.");
    }

    // �� JS ���У�SendMessage('WebBridge', 'LoadFromWeb', json)
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

        // λ���c���D�����]��ֵ������ 0��
        var pos = new Vector3(req.px, req.py, req.pz);
        var rot = Quaternion.Euler(req.rx, req.ry, req.rz);

        // ���x��ÿՈ�󸲌� serverUrl
        var effectiveServerUrl = string.IsNullOrEmpty(req.serverUrl) ? serverUrl : req.serverUrl;

        Debug.Log($"WebBridge: Load instanceId='{req.instanceId}', bundle='{req.bundleName}', prefab='{prefabName}' @ pos={pos}, rot={rot.eulerAngles}, server='{effectiveServerUrl}'");

        loader.LoadAndSpawnMulti(req.instanceId, req.bundleName, prefabName, req.modelId, pos, rot, effectiveServerUrl);

        //editButton.TriggerLoadWhenReady();
    }

    // ���x���� JS �����Ԅh����һ����
    public void DespawnFromWeb(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return;
        if (loader == null) loader = FindObjectOfType<PrefabLoader1>();
        if (loader == null) return;
        loader.Despawn(instanceId);
    }

    // ���x���� JS ���������ȫ��
    public void ClearAllFromWeb()
    {
        if (loader == null) loader = FindObjectOfType<PrefabLoader1>();
        if (loader == null) return;
        loader.ClearAll();
    }
}