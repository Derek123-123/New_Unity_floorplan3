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
                float floorTopY = child.position.y;
                var light = child.GetComponent<Light>();
                if (light == null) {
                    light = child.gameObject.AddComponent<Light>();
                }
                light.type = LightType.Directional;
                child.position = targetPos + Vector3.up * (floorTopY + 5f);
                child.rotation = Quaternion.Euler(12f, 35f, 0f);

                light.color = new Color(1.0f,0.67f,0.35f);
                light.intensity = 1.0f;
                light.shadows = LightShadows.Soft;
                light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;
                light.shadowStrength = 0.55f;
                light.shadowBias = 0.04f;
                light.shadowNormalBias = 0.35f;

                var bo = light.bakingOutput;
                bo.isBaked = false; // Unity manages this; set by bake. Keep false at runtime.
                bo.lightmapBakeType = LightmapBakeType.Baked; // or Mixed, Realtime
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
                RenderSettings.ambientLight = new Color(0.62f, 0.54f, 0.48f, 1f);
                RenderSettings.ambientIntensity = 1f;

                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogColor = new Color(0.85f, 0.63f, 0.53f, 1f); // warm haze
                RenderSettings.fogDensity = 0.006f; // 0.004–0.01 depending on scale

                QualitySettings.shadowDistance = 30f;

            }


            if (n.Equals("Floor", StringComparison.OrdinalIgnoreCase) || n.IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0) continue;

            bool isWall = n.StartsWith("Wall", StringComparison.OrdinalIgnoreCase);
            bool isWindow = n.StartsWith("Window", StringComparison.OrdinalIgnoreCase);
            if (isWall || isWindow)
            {
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
        col.convex = false; // static, non-convex for accurate geometry
    }
}
        