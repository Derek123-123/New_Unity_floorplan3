using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[ExecuteAlways] // ���\�b�s�边�Y�ɧ�s
public class AutoFitBoxToDoor : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("���ҫ����ڸ`�I�]�t MeshRenderer �����h�^�C�����w�h�۰ʦb������U�j�M�Ҧ� MeshRenderer�C")]
    public Transform sourceRoot;

    [Tooltip("�Y�� true�A�X�֩Ҧ��l���h MeshRenderer �� bounds�F�_�h�u���Ĥ@�� MeshRenderer�C")]
    public bool includeChildren = true;

    [Header("Collider Target")]
    [Tooltip("�n�Q�վ㪺 BoxCollider�C�����w�h���զb������W�۰ʨ��o/�s�W�C")]
    public BoxCollider targetCollider;

    [Header("Options")]
    [Tooltip("�b�s�边�ΰ���ɡA�C�� OnEnable/Update �ɦ۰ʶK�X�C")]
    public bool autoFitOnUpdate = false;

    [Tooltip("��U�b�K�[��Z�]������ BoxCollider �ܤj�A�t���ܤp�^�A��쬰�@�ɪŶ��ؤo�C")]
    public Vector3 margin = new Vector3(0.0f, 0.0f, 0.0f);

    [Tooltip("�л\�p�ס]�@�ɪŶ��^�C�� 0 ��ܤ��л\�A�u�̤p�p�׶b�H Bounds �ؤo���ǡC")]
    public float overrideThickness = 0f;

    [Tooltip("�p�G�A���ҫ��e���p�b�V�P�@�� XYZ ���@�P�A�i�b����M�G�Ҧp Width=X, Height=Y, Depth=Z�C")]
    public AxisMapping axisMapping = AxisMapping.XYZ;

    [Tooltip("�i�����I�A�Y���w�A�|�� BoxCollider �����ߧl���즹�I�]�b�@�ɪŶ��^�A�`�Ω����C")]
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

        // 1) ������V���
        if (!TryGetSourceBounds(out Bounds worldBounds))
            return;

        // 2) �M����Z�P�p���л\�]�@�ɪŶ��^
        Vector3 sizeWS = worldBounds.size + margin * 2f;

        // �p���л\�G��X�̤p���p�׶b�]�q�`�O�����p�ס^
        if (overrideThickness > 0f)
        {
            // ��X���p�׶b�]�H�j�p�̤p�̵����p�ס^
            int minAxis = 0;
            float minVal = sizeWS.x;
            if (sizeWS.y < minVal) { minVal = sizeWS.y; minAxis = 1; }
            if (sizeWS.z < minVal) { minVal = sizeWS.z; minAxis = 2; }
            if (minAxis == 0) sizeWS.x = overrideThickness;
            else if (minAxis == 1) sizeWS.y = overrideThickness;
            else sizeWS.z = overrideThickness;
        }

        Vector3 centerWS = worldBounds.center;

        // �Y���w���I�A������I������
        if (anchor != null)
            centerWS = anchor.position;

        // 3) �ഫ�쥻�a�y�СA�g�J BoxCollider.center/size
        // BoxCollider �� Center/Size �O�b����� Transform �����a�Ŷ�
        Transform t = targetCollider.transform;

        // ���a����
        Vector3 centerLS = t.InverseTransformPoint(centerWS);

        // ��@�ɪ��e���ର���a�ؤo�G�N�T�ӥ@�ɶb�V�V�q��v�쥻�a�b������
        Vector3 rightWS = t.right * sizeWS.x;
        Vector3 upWS = t.up * sizeWS.y;
        Vector3 fwdWS = t.forward * sizeWS.z;

        // ���a�ؤo�ݭn�Ҽ{�b�V��M�]�e���p��������Ӷb�^
        Vector3 sizeMappedWS = ApplyAxisMapping(sizeWS, axisMapping);
        // ���s�� mapped �ؤo�����쥻�a�b
        Vector3 sizeLS = WorldSizeToLocalSize(t, sizeMappedWS);

        targetCollider.center = centerLS;
        targetCollider.size = sizeLS;

        // ���b�G�קK�t�ȩη��p��
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
        // size �H�@�� XYZ ��ܡGX=�e, Y=��, Z=�p�]���]�^
        // �̬M�g���s�w��
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
        // �N�@�ɤؤo�ഫ�����a�ؤo�G��T�ӥ@�ɶb�V�V�q��v�쥻�a�b����
        // �o�̰��] BoxCollider ��� t �������b
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