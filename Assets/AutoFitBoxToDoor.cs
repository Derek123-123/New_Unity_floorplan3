using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[ExecuteAlways] // 允許在編輯器即時更新
public class AutoFitBoxToDoor : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("門模型的根節點（含 MeshRenderer 的那層）。不指定則自動在本物件下搜尋所有 MeshRenderer。")]
    public Transform sourceRoot;

    [Tooltip("若為 true，合併所有子階層 MeshRenderer 的 bounds；否則只取第一個 MeshRenderer。")]
    public bool includeChildren = true;

    [Header("Collider Target")]
    [Tooltip("要被調整的 BoxCollider。不指定則嘗試在本物件上自動取得/新增。")]
    public BoxCollider targetCollider;

    [Header("Options")]
    [Tooltip("在編輯器或執行時，每次 OnEnable/Update 時自動貼合。")]
    public bool autoFitOnUpdate = false;

    [Tooltip("對各軸添加邊距（正值讓 BoxCollider 變大，負值變小），單位為世界空間尺寸。")]
    public Vector3 margin = new Vector3(0.0f, 0.0f, 0.0f);

    [Tooltip("覆蓋厚度（世界空間）。為 0 表示不覆蓋，沿最小厚度軸以 Bounds 尺寸為準。")]
    public float overrideThickness = 0f;

    [Tooltip("如果你的模型寬高厚軸向與世界 XYZ 不一致，可在此對映：例如 Width=X, Height=Y, Depth=Z。")]
    public AxisMapping axisMapping = AxisMapping.XYZ;

    [Tooltip("可選錨點，若指定，會把 BoxCollider 的中心吸附到此點（在世界空間），常用於鉸鏈。")]
    public Transform anchor;

    [Header("Debug")]
    public Color boundsColor = new Color(0, 1, 0, 0.2f);
    public bool drawGizmos = true;

    public enum AxisMapping
    {
        XYZ, // Width->X, Height->Y, Depth->Z
        XZY, // Width->X, Height->Z, Depth->Y
        YXZ,
        YZX,
        ZXY,
        ZYX
    }

    private void Reset()
    {
        targetCollider = GetComponent<BoxCollider>();
        if (targetCollider == null) targetCollider = gameObject.AddComponent<BoxCollider>();
        if (sourceRoot == null) sourceRoot = transform;
        includeChildren = true;
        autoFitOnUpdate = true;
    }

    private void OnEnable()
    {
        EnsureCollider();
        FitNow();
    }

    private void Update()
    {
        if (!autoFitOnUpdate) return;
        FitNow();
    }

    public void FitNow()
    {
        EnsureCollider();

        if (sourceRoot == null) sourceRoot = transform;

        // 1) 收集渲染邊界
        if (!TryGetSourceBounds(out Bounds worldBounds))
            return;

        // 2) 套用邊距與厚度覆蓋（世界空間）
        Vector3 sizeWS = worldBounds.size + margin * 2f;

        // 厚度覆蓋：找出最小的厚度軸（通常是門的厚度）
        if (overrideThickness > 0f)
        {
            // 找出門厚度軸（以大小最小者視為厚度）
            int minAxis = 0;
            float minVal = sizeWS.x;
            if (sizeWS.y < minVal) { minVal = sizeWS.y; minAxis = 1; }
            if (sizeWS.z < minVal) { minVal = sizeWS.z; minAxis = 2; }
            if (minAxis == 0) sizeWS.x = overrideThickness;
            else if (minAxis == 1) sizeWS.y = overrideThickness;
            else sizeWS.z = overrideThickness;
        }

        Vector3 centerWS = worldBounds.center;

        // 若指定錨點，改用錨點做中心
        if (anchor != null)
            centerWS = anchor.position;

        // 3) 轉換到本地座標，寫入 BoxCollider.center/size
        // BoxCollider 的 Center/Size 是在其所屬 Transform 的本地空間
        Transform t = targetCollider.transform;

        // 本地中心
        Vector3 centerLS = t.InverseTransformPoint(centerWS);

        // 把世界長寬高轉為本地尺寸：將三個世界軸向向量投影到本地軸的長度
        Vector3 rightWS = t.right * sizeWS.x;
        Vector3 upWS = t.up * sizeWS.y;
        Vector3 fwdWS = t.forward * sizeWS.z;

        // 本地尺寸需要考慮軸向對映（寬高厚對應到哪個軸）
        Vector3 sizeMappedWS = ApplyAxisMapping(sizeWS, axisMapping);
        // 重新用 mapped 尺寸對應到本地軸
        Vector3 sizeLS = WorldSizeToLocalSize(t, sizeMappedWS);

        targetCollider.center = centerLS;
        targetCollider.size = sizeLS;

        // 防呆：避免負值或極小值
        targetCollider.size = new Vector3(
            Mathf.Max(1e-4f, targetCollider.size.x),
            Mathf.Max(1e-4f, targetCollider.size.y),
            Mathf.Max(1e-4f, targetCollider.size.z)
        );
    }

    private void EnsureCollider()
    {
        if (targetCollider == null)
        {
            targetCollider = GetComponent<BoxCollider>();
            if (targetCollider == null)
                targetCollider = gameObject.AddComponent<BoxCollider>();
        }
    }

    private bool TryGetSourceBounds(out Bounds worldBounds)
    {
        worldBounds = default;

        if (includeChildren)
        {
            var renderers = (sourceRoot != null ? sourceRoot : transform)
                .GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0) return false;

            bool started = false;
            foreach (var r in renderers)
            {
                if (r.enabled == false) continue;
                if (!started)
                {
                    worldBounds = r.bounds;
                    started = true;
                }
                else
                {
                    worldBounds.Encapsulate(r.bounds);
                }
            }
            return started;
        }
        else
        {
            var r = (sourceRoot != null ? sourceRoot : transform).GetComponent<MeshRenderer>();
            if (r == null) return false;
            worldBounds = r.bounds;
            return true;
        }
    }

    private static Vector3 ApplyAxisMapping(Vector3 size, AxisMapping map)
    {
        // size 以世界 XYZ 表示：X=寬, Y=高, Z=厚（假設）
        // 依映射重新安排
        switch (map)
        {
            case AxisMapping.XYZ: return new Vector3(size.x, size.y, size.z);
            case AxisMapping.XZY: return new Vector3(size.x, size.z, size.y);
            case AxisMapping.YXZ: return new Vector3(size.y, size.x, size.z);
            case AxisMapping.YZX: return new Vector3(size.y, size.z, size.x);
            case AxisMapping.ZXY: return new Vector3(size.z, size.x, size.y);
            case AxisMapping.ZYX: return new Vector3(size.z, size.y, size.x);
            default: return size;
        }
    }

    private static Vector3 WorldSizeToLocalSize(Transform t, Vector3 sizeWS)
    {
        // 將世界尺寸轉換為本地尺寸：把三個世界軸向向量投影到本地軸長度
        // 這裡假設 BoxCollider 對齊 t 的局部軸
        Vector3 sx = t.InverseTransformVector(new Vector3(sizeWS.x, 0, 0));
        Vector3 sy = t.InverseTransformVector(new Vector3(0, sizeWS.y, 0));
        Vector3 sz = t.InverseTransformVector(new Vector3(0, 0, sizeWS.z));

        return new Vector3(
            Mathf.Abs(sx.x) + Mathf.Abs(sy.x) + Mathf.Abs(sz.x),
            Mathf.Abs(sx.y) + Mathf.Abs(sy.y) + Mathf.Abs(sz.y),
            Mathf.Abs(sx.z) + Mathf.Abs(sy.z) + Mathf.Abs(sz.z)
        );
    }

#if UNITY_EDITOR
    [ContextMenu("Fit Now")]
    private void FitNowContext()
    {
        FitNow();
        EditorUtility.SetDirty(this);
        if (targetCollider) EditorUtility.SetDirty(targetCollider);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        if (!TryGetSourceBounds(out var b)) return;

        Gizmos.color = boundsColor;
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(b.center, b.size);
    }
#endif
}