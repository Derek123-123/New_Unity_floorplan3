using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CanvasUiManager : MonoBehaviour
{
    [Header("Scene references (assign in Inspector)")]
    public GameObject camera2GO;                 // 場景中的第二視角相機
    public GameObject camera3GO;                
    public Button cameraSwitchButton;            // 場景中的切換相機按鈕
    public EditButton editButton;                // 場景中的 EditButton 腳本
   
    [Header("Optional Root to parent all runtime objects (assign or auto-create)")]
    public Transform runtimeRoot;

     
    private Camera2 camera2Comp;                 // 挂在 camera2GO 上
    private Camera3 camera3Comp;                 // 挂在 camera2GO 上
    private Switch_camera_button switchComp;     // 挂在 cameraSwitchButton.gameObject 上
    private GameObject currentInstance;          // 當前載入的 prefab 實例
    private Transform floorTransform;            // 從實例找到的 Floor

    public Button Wmb;
    private readonly List<GameObject> spawnedItems = new List<GameObject>();

    void Awake()
    {
       
        if (runtimeRoot == null)
        {
            var go = new GameObject("RuntimeRoot");
            runtimeRoot = go.transform;
        }
       
    }

    void PrintState(string tag)
    {
        Debug.Log($"[CanvasUiManager::{tag}] camera2GO={(camera2GO ? camera2GO.name : "null")} " +
                  $"cameraSwitchButton={(cameraSwitchButton ? cameraSwitchButton.name : "null")} " +
                  $"editButton={(editButton ? editButton.name : "null")} " +
                  $"currentInstance={(currentInstance ? currentInstance.name : "null")} " +
                  $"floor={(floorTransform ? floorTransform.name : "null")} " +
                  $"camera2Comp={(camera2Comp ? camera2Comp.GetType().Name : "null")} " +
                  $"switchComp={(switchComp ? switchComp.GetType().Name : "null")}");
    }

    public void OnLoadStarted(string bundleName, string prefabName, string instanceId)
    {
        
        ClearAllSpawnsAndInstance();
        Debug.Log($"[OnLoadStarted] bundle={bundleName} prefab={prefabName} id={instanceId}");
        PrintState("OnLoadStarted");
        // 暫時鎖定 UI/解除事件
        if (cameraSwitchButton != null)
            cameraSwitchButton.onClick.RemoveListener(OnSwitchClicked);
        if (switchComp != null) switchComp.enabled = false;
        if (camera2Comp != null) camera2Comp.enabled = false;
        if (camera3Comp != null) camera3Comp.enabled = false;

        try
        {
            var MS = FindObjectOfType<MODESWITCH>(true);
            if (MS != null) MS.SetLoaded(false, null);
                
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Probe] Walkmode reset block threw: " + ex);
        }

        try
        {
            var Wall_M = FindObjectOfType<wallSelector>(true);
            if (Wall_M != null) Wall_M.WMSetLoaded(false, null);

        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Probe] WM reset block threw: " + ex);
        }


    }

    public void OnLoadSucceeded(string bundleName, string prefabName, string instanceId, string modelId, GameObject instance)
    {
        
        if (runtimeRoot != null && instance != null)
            instance.transform.SetParent(runtimeRoot, worldPositionStays: true);
        Debug.Log($"[OnLoadSucceeded] bundle={bundleName} prefab={prefabName} id={instanceId} instance={(instance ? instance.name : "null")}");
        currentInstance = instance;

        //StartCoroutine(editButton.LoadItem());
        
        // 在實例中尋找 Floor（盡量用 tag；這裡以名稱為例）
        floorTransform = FindChildByName(instance.transform, "Floor");
        Debug.Log($"[OnLoadSucceeded] Found Floor={(floorTransform ? floorTransform.name : "null")}");

        if (camera2GO == null)
        {
            
            Debug.LogError("[OnLoadSucceeded] camera2GO not assigned in Inspector!");
        }
        if (camera3GO == null)
        {

            Debug.LogError("[OnLoadSucceeded] camera3GO not assigned in Inspector!");
        }
        if (editButton == null)
        {
            Debug.LogError("[OnLoadSucceeded] editButton not assigned in Inspector!");
        }
        if (cameraSwitchButton == null) Debug.LogError("[OnLoadSucceeded] cameraSwitchButton not assigned in Inspector!");
        // 準備 Camera2 腳本（存在就取用，不存在就加上），並注入 target 與 EditButton
        try
        {
            if (camera2GO != null)
            {
                camera2Comp = EnsureComp<Camera2>(camera2GO);
                camera2Comp.target = floorTransform;      // 由外部注入，避免 Start 內部 Find
                camera2Comp.editButton = editButton;      // 注入 EditButton
                camera2Comp.enabled = true;
                camera2Comp.TryInit();
                Debug.Log($"[OnLoadSucceeded] Camera2 injected target={(floorTransform ? floorTransform.name : "null")}");
            }
        }
        catch(System.Exception ex)
        {
            Debug.LogError("[Probe] Camera2 inject block threw: " + ex);
        }

        try
        {
            if (camera3GO != null)
            {
                camera3Comp = EnsureComp<Camera3>(camera3GO);
                camera3Comp.Cam3target = floorTransform;      // 由外部注入，避免 Start 內部 Final
                camera3Comp.enabled = true;
                camera3Comp.setPositionAfterLoaded();
                Debug.Log($"[OnLoadSucceeded] Camera2 injected target={(floorTransform ? floorTransform.name : "null")}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Probe] Camera2 inject block threw: " + ex);
        }

        // 準備 Switch_camera_button 腳本，注入 Camera1/Camera2/Button/EditButton
        try
        {
            if (cameraSwitchButton != null)
            {
                switchComp = EnsureComp<Switch_camera_button>(cameraSwitchButton.gameObject);
                switchComp.Camera1 = FindChildByName(instance.transform, "Camera1")?.gameObject;
                if (switchComp.Camera1 != null && floorTransform != null)
                {
                    var cam1Drag = switchComp.Camera1.GetComponent<camera_drag>();
                    if (cam1Drag != null) cam1Drag.SetTarget(floorTransform);
                }
                switchComp.Camera2 = camera2GO;
                if (switchComp.MainCamera == null) switchComp.MainCamera = GameObject.Find("MainCamera");
                switchComp.button = cameraSwitchButton;
                switchComp.editButton = editButton;
                switchComp.enabled = true;

                bool camsReady = (switchComp.Camera1 != null && switchComp.Camera2 != null);
                cameraSwitchButton.onClick.RemoveListener(OnSwitchClicked);
                if (camsReady)
                {
                    cameraSwitchButton.onClick.AddListener(OnSwitchClicked);
                }
                cameraSwitchButton.interactable = camsReady;
                Debug.Log($"[OnLoadSucceeded] Camera1={(switchComp.Camera1 ? switchComp.Camera1.name : "null")} Camera2={(switchComp.Camera2 ? switchComp.Camera2.name : "null")} camsReady={camsReady}");
                Debug.Log("[Probe] After SwitchCam injected");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Probe] SwitchCam inject block threw: " + ex);
        }

        try
        {
            if (editButton != null)
            {
                editButton.editMode = false;
                editButton.DeleteMode = false;
                editButton.SetRegistrar(RegisterSpawnedGO);
                editButton.SetCameras(switchComp?.Camera1, camera2GO);
                Debug.Log("[Probe] Before EditButton.SetWall()");
                editButton.SetWall();
                Debug.Log("[Probe] After EditButton.SetWall()");
                if (editButton.button != null) editButton.button.interactable = true;
                editButton.SyncUiByModes();
            }
            Debug.Log("[Probe] After EditButton injected");

        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Probe] EditButton inject block threw: " + ex);
        }

       
        try 
        {
            var MS = FindObjectOfType<MODESWITCH>(true);
            if (MS != null)
            {
                Debug.Log("Now Calling MS.SetLoaded");
                MS.SetLoaded(true, switchComp?.Camera1);
                //if (MS.Camera2 == null) walk.Camera2 = camera2GO;
                //if (MS.MainCamera == null) walk.MainCamera = GameObject.Find("MainCamera");


            
            }
        }
        catch(System.Exception ex)
        {
            Debug.LogError("[Probe] Walk Button inject block threw: " + ex);
        }

        try
        {
            var Wall_M = FindObjectOfType<wallSelector>(true);
            if (Wall_M != null)
            {
                Debug.Log("Now Calling MS.SetLoaded");
                Wall_M.WMSetLoaded(true, instance);
                //if (MS.Camera2 == null) walk.Camera2 = camera2GO;
                //if (MS.MainCamera == null) walk.MainCamera = GameObject.Find("MainCamera");



            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Probe] Walk Button inject block threw: " + ex);
        }

        PrintState("OnLoadSucceeded-END");
        Debug.Log($"[OnLoadSucceeded] editButton cameras injected: C1={(switchComp?.Camera1 ? switchComp.Camera1.name : "null")} C2={(camera2GO ? camera2GO.name : "null")}");
        editButton.SelectModelAndReload(modelId);
    }


    public void OnLoadFailed(string bundleName, string instanceId, string error)
    {
       
        // 還原 UI 狀態
        if (cameraSwitchButton != null)
            cameraSwitchButton.onClick.RemoveListener(OnSwitchClicked);
        if (switchComp != null) switchComp.enabled = false;
        if (camera2Comp != null) camera2Comp.enabled = false;
        if (camera3Comp != null) camera3Comp.enabled = false;

        currentInstance = null;
        floorTransform = null;
       
        try
        {
            var MS = FindObjectOfType<MODESWITCH>(true);
            if (MS != null) MS.SetLoaded(false, null);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Probe] Walkmode reset block threw: " + ex);
        }

        try
        {
            var Wall_M = FindObjectOfType<wallSelector>(true);
            if (Wall_M != null) Wall_M.WMSetLoaded(false, null);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Probe] WM reset block threw: " + ex);
        }


    }

    /*public void OnRemoveStarted(string bundleName, string instanceId)
    {
        Debug.Log($"[OnRemoveStarted] bundle={bundleName} id={instanceId}");
        PrintState("OnRemoveStarted");
        // 先解除事件、停用腳本，避免下一幀觸到已 Destroy 的東西
        if (cameraSwitchButton != null)
            cameraSwitchButton.onClick.RemoveListener(OnSwitchClicked);
        if (switchComp != null) switchComp.enabled = false;
        if (camera2Comp != null) camera2Comp.enabled = false;
        if (editButton != null && editButton.button != null) editButton.button.interactable = false;
        ClearAllSpawnsAndInstance();
    }

    public void OnRemoveCompleted(string instanceId)
    {
        Debug.Log($"[OnRemoveCompleted] id={instanceId}");
        PrintState("OnRemoveCompleted-BEFORE");
        // 清空引用
        currentInstance = null;
        floorTransform = null;

        if (camera2Comp != null)
        {
            camera2Comp.target = null;
            camera2Comp.enabled = false;
        }
        if (switchComp != null)
        {
            switchComp.enabled = false;
            switchComp.Camera1 = null;
            switchComp.Camera2 = null;
        }
        if (editButton != null)
        {
            editButton.SetCameras(null, null); // implement to nullify Camera1_G/Camera2_G/camera2
            editButton.editMode = false;
        }
        PrintState("OnRemoveCompleted-AFTER");
    }*/
    // 讓 EditButton 呼叫，把每個動態生成的物件登記進來
    public void RegisterSpawnedGO(GameObject go)
    {
        if (go == null) return;
        if (runtimeRoot != null)
            go.transform.SetParent(runtimeRoot, worldPositionStays: true);
        if (!spawnedItems.Contains(go))
            spawnedItems.Add(go);
    }

    // 清空：載入的 instance + 編輯生成的物件 + 可選相機
    private void ClearAllSpawnsAndInstance()
    {
        
        // 1) 刪編輯生成的物件
        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            var go = spawnedItems[i];
            if (go != null) Destroy(go);
        }
        spawnedItems.Clear();

        // 2) 刪載入的 instance（floorplan）
        if (currentInstance != null)
        {
            Destroy(currentInstance);
            currentInstance = null;
        }

        // 3) 如果你要連 Camera1 也清掉：從載入的 instance 內找 Camera1 已會一併刪掉
        // Camera2 是場景常駐，如果你也要清它（不太建議），可以另外 Destroy(camera2GO)，
        // 但通常 Camera2 是常駐只重置狀態即可。

        // 4) 若需要，一併清空 runtimeRoot 底下的孩物件（保險做法）
        if (runtimeRoot != null)
        {
            var toDestroy = new List<Transform>();
            foreach (Transform child in runtimeRoot)
                toDestroy.Add(child);
            foreach (var t in toDestroy)
                if (t != null) Destroy(t.gameObject);
        }
    }

    // 切換相機的按鈕回呼（包一層，避免直接依賴 private 方法）
    private void OnSwitchClicked()
    {
        if (switchComp == null || !switchComp.enabled) return;
        if (switchComp.Camera1 == null || switchComp.Camera2 == null) return;
           
        // 直接呼叫公開 API（請把 switchCamera 改成 public）
        switchComp.switchCamera();
    }

    private static T EnsureComp<T>(GameObject go) where T : Behaviour
    {
        var c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
        }
        return null;
    }

    public void RequestSetEM()
    {
        var ms = FindObjectOfType<MODESWITCH>();
        Debug.Log("[CanvasUiManager] request to Switch mode to EM");
        if (ms != null) ms.JsSetEM();
    }
    public void RequestSetWM()
    {
        var ms = FindObjectOfType<MODESWITCH>();
        Debug.Log("[CanvasUiManager] request to Switch mode to WM");
        if (ms != null) ms.JsSetWM();
    }
    public void SwitchIfWM()
    {
        var ms = FindObjectOfType<MODESWITCH>();
        Debug.Log("[CanvasUiManager] request to get mode");
        string Currentmode = (ms != null)? ms.GetMode():"";
        if (Currentmode == "WM") {
            RequestSetEM();
        }
        else
        {
            Debug.Log("it is not in walk mode~~");
            return;
            
        }
        
    }

}