using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class AutoAddCollidersAll : MonoBehaviour
{
    [Header("Scan")]
    [Tooltip("Include inactive GameObjects in the scan.")]
    public bool includeInactive = true;

    [Tooltip("Process only objects that are in a scene (exclude assets in the Project).")]
    public bool onlySceneObjects = true;

    [Header("Removal")]
    [Tooltip("If true, remove colliders only on the object itself; if false, also remove on children.")]
    public bool removeOnSelfOnly = true;

    [Tooltip("If the object (or children) already has multiple colliders as a compound, skip it to avoid destroying manual setups.")]
    public bool skipIfHasCompound = false;

    [Header("Collider")]
    [Tooltip("If the object has a Mesh, prefer using a MeshCollider.")]
    public bool preferMeshColliderIfHasMesh = false;

    [Tooltip("If detected as thin, force MeshCollider when a Mesh is available.")]
    public bool forceMeshColliderForThin = true;

    [Tooltip("Whether MeshCollider should be convex. Recommended OFF for static level geometry to keep precision; ON for dynamic rigidbodies.")]
    public bool meshColliderConvex = false;

    [Tooltip("Thin detection using world-space Renderer.bounds: if the smallest axis is less than this, the object is considered thin.")]
    public float thinThreshold = 0.05f;

    [Tooltip("Target thickness in world space for thin objects when using BoxCollider (the thinnest axis will be clamped to this).")]
    public float thinThickness = 0.02f;

    [Header("Logging")]
    [Tooltip("Print verbose logs for every processed object.")]
    public bool verboseLog = false;

#if UNITY_EDITOR
    [ContextMenu("Run Now (Editor Only)")]
    public void RunNowEditor()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[AutoAddCollidersAll] Please run in Edit Mode (not during Play).");
            return;
        }
        RunCore();
    }

    [CustomEditor(typeof(AutoAddCollidersAll))]
    class AutoAddCollidersAllEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(6);
            if (GUILayout.Button("Run Now (Editor Only)"))
            {
                var t = (AutoAddCollidersAll)target;
                if (Application.isPlaying)
                    Debug.LogWarning("[AutoAddCollidersAll] Please run in Edit Mode (not during Play).");
                else
                    t.RunCore();
            }
        }
    }
#endif

    void RunCore()
    {
        int processed = 0;
        int removed = 0;
        int added = 0;
        int skippedCompound = 0;

        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            // Skip non-scene assets if requested
            if (onlySceneObjects && !go.scene.IsValid()) continue;

            // Skip inactive if requested
            if (!includeInactive && !go.activeInHierarchy) continue;

            // Only process objects that have a Renderer
            var r = go.GetComponent<Renderer>();
            if (r == null) continue;

            processed++;

            // Optionally skip objects that already have compound colliders
            if (skipIfHasCompound)
            {
                int totalCols = removeOnSelfOnly
                    ? go.GetComponents<Collider>().Length
                    : go.GetComponentsInChildren<Collider>(true).Length;

                if (totalCols > 1)
                {
                    skippedCompound++;
                    if (verboseLog) Debug.Log($"[AutoAddCollidersAll] Skip compound: {GetPath(go)}");
                    continue;
                }
            }

            // 1) Remove existing colliders
            removed += RemoveExistingColliders(go, removeOnSelfOnly);

            // 2) Decide and add a collider
            // Use world-space AABB only for thin detection
            var worldBounds = r.bounds;
            Vector3 wsSize = worldBounds.size;

            // Find thinnest axis (world space)
            int minAxis = 0; float minVal = wsSize.x;
            if (wsSize.y < minVal) { minAxis = 1; minVal = wsSize.y; }
            if (wsSize.z < minVal) { minAxis = 2; minVal = wsSize.z; }
            bool isThin = minVal < Mathf.Max(1e-6f, thinThreshold);

            // Mesh availability
            var mf = go.GetComponent<MeshFilter>();
            bool hasMesh = mf != null && mf.sharedMesh != null;

            // Prefer MeshCollider (and force for thin if set)
            if ((preferMeshColliderIfHasMesh && hasMesh) || (forceMeshColliderForThin && hasMesh && isThin))
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.convex = meshColliderConvex;
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(mc, "Add MeshCollider");
#endif
                added++;

                if (verboseLog)
                    Debug.Log($"[AutoAddCollidersAll] +MeshCollider {(isThin ? "(thin)" : "")}: {GetPath(go)}");

                continue;
            }

            // Otherwise add a BoxCollider with correct local size/center
            var bc = go.AddComponent<BoxCollider>();
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(bc, "Add BoxCollider");
#endif

            Vector3 localCenter;
            Vector3 localSize;

            if (hasMesh)
            {
                // Most accurate: use mesh local bounds
                var lb = mf.sharedMesh.bounds;
                localCenter = lb.center;
                localSize = lb.size;
            }
            else
            {
                // Fallback: approximate local size by undoing lossy scale from world AABB
                var t = go.transform;
                Vector3 s = AbsNonZero(t.lossyScale);
                localSize = new Vector3(wsSize.x / s.x, wsSize.y / s.y, wsSize.z / s.z);
                localCenter = t.InverseTransformPoint(worldBounds.center);
            }

            // If thin, clamp the thinnest axis to target world thickness, converted to local
            if (isThin)
            {
                Vector3 s = AbsNonZero(go.transform.lossyScale);
                float localThin = thinThickness / (minAxis == 0 ? s.x : (minAxis == 1 ? s.y : s.z));
                localThin = Mathf.Max(localThin, 1e-5f);

                if (minAxis == 0) localSize.x = localThin;
                else if (minAxis == 1) localSize.y = localThin;
                else localSize.z = localThin;
            }

            bc.center = localCenter;
            bc.size = localSize;

            added++;

            if (verboseLog)
                Debug.Log($"[AutoAddCollidersAll] +BoxCollider {(isThin ? "(thin)" : "")}: {GetPath(go)}");
        }

        Debug.Log($"[AutoAddCollidersAll] Processed: {processed}, Removed: {removed}, Added: {added}, SkippedCompound: {skippedCompound}");
    }

    static Vector3 AbsNonZero(Vector3 v)
    {
        return new Vector3(
            Mathf.Abs(v.x) < 1e-6f ? 1e-6f : Mathf.Abs(v.x),
            Mathf.Abs(v.y) < 1e-6f ? 1e-6f : Mathf.Abs(v.y),
            Mathf.Abs(v.z) < 1e-6f ? 1e-6f : Mathf.Abs(v.z)
        );
    }

    static string GetPath(GameObject go)
    {
        if (go == null) return "(null)";
        string path = go.name;
        var t = go.transform;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    int RemoveExistingColliders(GameObject go, bool selfOnly)
    {
        int count = 0;

        if (selfOnly)
        {
            var cols = go.GetComponents<Collider>();
            for (int i = cols.Length - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(cols[i]);
#else
                DestroyImmediate(cols[i]);
#endif
                count++;
            }
        }
        else
        {
            var cols = go.GetComponentsInChildren<Collider>(true);
            for (int i = cols.Length - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(cols[i]);
#else
                DestroyImmediate(cols[i]);
#endif
                count++;
            }
        }

        return count;
    }
}