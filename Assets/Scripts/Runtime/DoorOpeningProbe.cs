using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class DoorOpeningProbe : MonoBehaviour
{
    [Header("Layers")]
    public LayerMask wallLayer = 0;

    [Header("Probe Distances")]
    public float maxSideProbe = 3f;
    public float maxFrontBackProbe = 1.5f;
    public float maxUpProbe = 4f;
    public float maxDownProbe = 4f;

    [Header("Margins")]
    public float widthMargin = 0.01f;
    public float heightMargin = 0.01f;
    public float thicknessMargin = 0.0f;
    public float minThickness = 0.05f;
    public float surfacePush = 0.001f;
    public float liftForSidecast = 0.2f;

    [Header("Optional Existing Opening")]
    public BoxCollider existingOpening;

    [Header("Debug")]
    public bool autoProbeOnUpdate = false;
    public bool drawGizmos = true;
    public Color gizmoColor = new Color(1, 0.6f, 0, 0.2f);

    public DoorOpeningDetector.Result last;

#if UNITY_EDITOR
    [ContextMenu("Fit Now")]
    public void FitNow()
    {
        DoorOpeningDetector.Detect(
            transform,
            wallLayer,
            maxSideProbe,
            maxFrontBackProbe,
            maxUpProbe,
            maxDownProbe,
            widthMargin,
            heightMargin,
            thicknessMargin,
            minThickness,
            surfacePush,
            liftForSidecast,
            existingOpening,
            out last
        );

        if (last.success)
            Debug.Log($"[DoorOpeningProbe] {last}");
        else
            Debug.LogWarning("[DoorOpeningProbe] Detect failed. Check Layer/axes and distances.");
        EditorUtility.SetDirty(this);
    }
#endif

    private void Update()
    {
        if (autoProbeOnUpdate)
        {
            DoorOpeningDetector.Detect(
                transform, wallLayer,
                maxSideProbe, maxFrontBackProbe,
                maxUpProbe, maxDownProbe,
                widthMargin, heightMargin, thicknessMargin,
                minThickness, surfacePush, liftForSidecast,
                existingOpening,
                out last
            );
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || !last.success) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(last.openingCenter, last.sizeWS);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(last.openingCenter, last.sizeWS);

        // ¥ª¥kÃä½t
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(last.leftEdge, 0.02f);
        Gizmos.DrawSphere(last.rightEdge, 0.02f);

        // ªk½u
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(last.openingCenter, last.openingCenter + last.wallNormal * 0.5f);
    }
}