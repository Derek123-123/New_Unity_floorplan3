using System;
using System.Linq;
using UnityEngine;

public static class FloorplanRuntimeSetup
{
    // �� Instantiate ֮�����̺����@������
    public static void TryAssignRuntimeScripts(GameObject root)
    {
        if (root == null) { Debug.LogWarning("[RuntimeSetup] root is null"); return; }
        // ���x���yһ����
        // root.name = "floorPlan";

        // ����������U�� Collider���Ǳ�Ҫ����������
        if (root.GetComponent<Collider>() == null)
        {
            var mcRoot = root.AddComponent<MeshCollider>();
            mcRoot.convex = false;
        }
        
        // Camera -> Camera1 + camera_drag�����������C����
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.Equals("Camera", StringComparison.OrdinalIgnoreCase))
            {
                t.name = "Camera1";
            }
        }
        var cam1 = root.GetComponentsInChildren<Transform>(true)
                       .FirstOrDefault(t => t.name.Equals("Camera1", StringComparison.OrdinalIgnoreCase));
        if (cam1 && cam1.GetComponent<camera_drag>() == null)
        {
            cam1.gameObject.AddComponent<camera_drag>();
        }

        // �� Floor ����������Q�� floor �� Tag=Floor��
        var floorT = root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t =>
                t.name.Equals("Floor", StringComparison.OrdinalIgnoreCase) ||
                t.name.IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0
//||SafeCompareTag(t.gameObject, "FloorLayer")
            );

        if (floorT)
        {
            var floorGO = floorT.gameObject;
            EnsureMeshCollider(floorGO);
            int floorLayer = LayerMask.NameToLayer("FloorLayer");
            if (floorLayer != -1) floorGO.layer = floorLayer;
            else Debug.LogWarning("[RuntimeSetup] Layer 'FloorLayer' not found. Create it in Project Settings > Tags and Layers.");

            Debug.Log($"[RuntimeSetup] floor='{floorGO.name}' layer={floorGO.layer} hasCol={(floorGO.GetComponent<Collider>() != null)}");
        }
        else
        {
            Debug.LogWarning("[RuntimeSetup] δ�ҵ� Floor �������by name/tag����Raycast ���ܴ򲻵��ذ塣");
        }

        // �����������ҕ�����a������_��
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root.transform) continue;
            var n = child.name;
            if (n.Equals("Camera1", StringComparison.OrdinalIgnoreCase)) continue;

            if (n.Equals("Light", StringComparison.OrdinalIgnoreCase) ||
                n.Equals("light", StringComparison.OrdinalIgnoreCase) ||
                n.IndexOf("directional", StringComparison.OrdinalIgnoreCase) >= 0) {


                var floorR = root.GetComponentsInChildren<Renderer>(true)
                    .FirstOrDefault(r =>
                       r.name.Equals("Floor", StringComparison.OrdinalIgnoreCase) ||
                       r.name.IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0);


                var bounds = floorR.bounds;
                Vector3 targetPos = bounds.center;
                
                var light = child.GetComponent<Light>();
                if (light == null) {
                    light = child.gameObject.AddComponent<Light>();
                }
                light.type = LightType.Directional;
                child.position = targetPos + Vector3.up * 5f;
                child.rotation = Quaternion.Euler(35f, 30f, 0f);

                light.color = Color.white;
                light.intensity = 0.9f;

                light.shadows = LightShadows.Soft;
                light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
                light.shadowStrength = 0.7f;
                light.shadowBias = 0.05f;
                light.shadowNormalBias = 0.4f;

                var bo = light.bakingOutput;
                bo.isBaked = false; // Unity manages this; set by bake. Keep false at runtime.
                bo.lightmapBakeType = LightmapBakeType.Realtime; // or Mixed, Realtime
                light.bakingOutput = bo;

                if (light.type == LightType.Point)
                {
                    light.range = 12f;
                }
                else if (light.type == LightType.Spot)
                {
                    light.range = 15f;
                    light.spotAngle = 60f;
                }

                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.62f, 0.65f, 0.65f, 1f);
                RenderSettings.ambientIntensity = 1f;

                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                

                QualitySettings.shadowDistance = 45f;

            }


            if (n.Equals("Floor", StringComparison.OrdinalIgnoreCase) || n.IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0) { 
                if (child.GetComponent<reSetPos>() == null)
                {
                    //child.gameObject.AddComponent<reSetPos>();
                }
            }

            bool isWall = n.StartsWith("Wall", StringComparison.OrdinalIgnoreCase);
            bool isWindow = n.StartsWith("Window", StringComparison.OrdinalIgnoreCase);
            bool isDoor = n.StartsWith("Door", StringComparison.OrdinalIgnoreCase);
            if (isWall || isWindow || isDoor)
            {
                
                if (child.GetComponent<reSetPos>() == null)
                {
                    //child.gameObject.AddComponent<reSetPos>();
                }
                EnsureMeshCollider(child.gameObject);
                // Optional: assign layers if needed for filtering
                // int wallLayer = LayerMask.NameToLayer("WallLayer");
                // if (isWall && wallLayer != -1) t.gameObject.layer = wallLayer;
                // int windowLayer = LayerMask.NameToLayer("WindowLayer");
                // if (isWindow && windowLayer != -1) t.gameObject.layer = windowLayer;
            }
        }
    }
    private static void EnsureMeshCollider(GameObject go)
    {
        var col = go.GetComponent<MeshCollider>();
        if (col == null)
        {
            col = go.AddComponent<MeshCollider>();
        }
        if (col != null) {
            Debug.Log(go.name + " has a MeshCollider");
        }
        Component[] allComponents = go.GetComponents<Component>();
        Debug.Log("Components on " + go.name + ":");
        foreach (Component component in allComponents)
        {
            Debug.Log("- " + component.GetType().Name);
        }
        col.convex = false; // static, non-convex for accurate geometry
    }
}
        