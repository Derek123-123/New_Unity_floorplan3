// Assets/Scripts/DoorwayColliderUtil.cs
using UnityEngine;

public static class DoorwayColliderUtil
{
    // 建立門洞 BoxCollider：forward 對準牆法線，size = [寬, 高, 厚]
    public static GameObject CreateDoorwayCollider(
        string name,
        Transform parent,
        Vector3 centerWorld,
        Vector3 wallNormal,
        float doorWidth,
        float doorHeight,
        float wallThickness,
        bool isTrigger = true,
        int layer = -1 // -1 表示不設定層
    )
    {
        if (doorWidth <= 0f || doorHeight <= 0f || wallThickness <= 0f)
        {
            Debug.LogWarning($"[DoorwayColliderUtil] Invalid size: W={doorWidth}, H={doorHeight}, T={wallThickness}. Abort.");
            return null;
        }

        var go = new GameObject(string.IsNullOrEmpty(name) ? "DoorwayCollider" : name);
        if (parent != null) go.transform.SetParent(parent, worldPositionStays: true);

        Vector3 up = Vector3.up;
        Vector3 n = wallNormal.normalized;
        if (n.sqrMagnitude < 1e-8f) n = Vector3.forward;
        if (Mathf.Abs(Vector3.Dot(up, n)) > 0.99f) up = Vector3.right;

        go.transform.SetPositionAndRotation(centerWorld, Quaternion.LookRotation(n, up));

        var bc = go.AddComponent<BoxCollider>();
        bc.size = new Vector3(doorWidth, doorHeight, wallThickness);
        bc.center = Vector3.zero;
        bc.isTrigger = isTrigger;

        if (layer >= 0 && layer <= 31) go.layer = layer;

#if UNITY_EDITOR
        var giz = go.AddComponent<DoorwayWireGizmo>();
        giz.color = new Color(1f, 0f, 0f, 0.85f);
#endif
        return go;
    }

    // 沿牆法線兩側各打一條射線，回推牆厚；失敗回傳 fallback
    public static float MeasureWallThickness(
        Vector3 center,
        Vector3 wallNormal,
        float searchHalfExtent = 0.6f,
        LayerMask wallMask = default,
        float fallback = 0.12f
    )
    {
        Vector3 n = wallNormal.sqrMagnitude > 1e-8f ? wallNormal.normalized : Vector3.forward;

        bool ok1 = Physics.Raycast(center - n * searchHalfExtent, n, out var hitF, searchHalfExtent * 2f, wallMask, QueryTriggerInteraction.Ignore);
        bool ok2 = Physics.Raycast(center + n * searchHalfExtent, -n, out var hitB, searchHalfExtent * 2f, wallMask, QueryTriggerInteraction.Ignore);

#if UNITY_EDITOR
        Debug.DrawRay(center - n * searchHalfExtent, n * (searchHalfExtent * 2f), ok1 ? Color.green : Color.red, 2f);
        Debug.DrawRay(center + n * searchHalfExtent, -n * (searchHalfExtent * 2f), ok2 ? Color.green : Color.red, 2f);
#endif

        if (ok1 && ok2) return Vector3.Distance(hitF.point, hitB.point);
        return fallback;
    }

    // 供已存在門模型用 Renderer.bounds 推導門洞尺寸
    public static GameObject CreateFromDoorRenderer(
        Renderer doorRenderer,
        Transform parent,
        Vector3 wallNormal,
        float thicknessFallback = 0.12f,
        bool isTrigger = true,
        int layer = -1
    )
    {
        if (doorRenderer == null) return null;
        Bounds b = doorRenderer.bounds;
        float doorHeight = b.size.y;
        float doorWidth = Mathf.Max(b.size.x, b.size.z);
        float wallThickness = Mathf.Min(b.size.x, b.size.y, b.size.z);
        if (wallThickness <= 0f) wallThickness = thicknessFallback;

        return CreateDoorwayCollider($"Doorway_{doorRenderer.gameObject.name}", parent, b.center, wallNormal, doorWidth, doorHeight, wallThickness, isTrigger, layer);
    }
}

#if UNITY_EDITOR
// 在場景視圖畫出門洞紅色線框（僅編輯器）
[ExecuteAlways]
public class DoorwayWireGizmo : MonoBehaviour
{
    public Color color = Color.red;

    void OnDrawGizmos()
    {
        var bc = GetComponent<BoxCollider>();
        if (bc == null) return;

        Gizmos.color = color;

        Matrix4x4 prev = Gizmos.matrix;

        Vector3 worldSize = Vector3.Scale(transform.lossyScale, bc.size);
        Vector3 worldCenter = transform.TransformPoint(bc.center);

        Gizmos.matrix = Matrix4x4.TRS(worldCenter, transform.rotation, worldSize);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        Gizmos.matrix = prev;
    }
}
#endif