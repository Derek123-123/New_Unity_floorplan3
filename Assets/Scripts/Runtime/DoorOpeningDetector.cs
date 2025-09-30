using UnityEngine;

public static class DoorOpeningDetector
{
    // �������G��Ƶ��c
    public struct Result
    {
        public bool success;

        // ��P�R��
        public Collider leftWall;
        public Collider rightWall;
        public RaycastHit leftHit;
        public RaycastHit rightHit;

        public bool frontHit;
        public bool backHit;
        public RaycastHit frontWallHit;
        public RaycastHit backWallHit;

        public bool upHit;
        public bool downHit;
        public RaycastHit upHitInfo;
        public RaycastHit downHitInfo;

        // �ؤo�]�@�ɪŶ��^
        public float openingWidth;
        public float openingHeight;
        public float wallThickness;

        // �X���I�]�@�ɪŶ��^
        public Vector3 leftEdge;      // ����t�I�]�b��p�����^
        public Vector3 rightEdge;     // �k��t�I�]�b��p�����^
        public Vector3 openingCenter; // �}���ߡ]��p�����^
        public Vector3 wallNormal;    // ���V�u���� forward�v��V����k�u�]����^
        public Vector3 up;            // �Ѫ������A���o�� up
        public Vector3 right;         // �Ѫ������A���o�� right

        // ��K�����Ψӳ]�w BoxCollider ���ؤo�]�@�ɪŶ��e���p�^
        public Vector3 sizeWS => new Vector3(openingWidth, openingHeight, wallThickness);

        public override string ToString()
        {
            return success
                ? $"DoorOpening success: W={openingWidth:F3}, H={openingHeight:F3}, T={wallThickness:F3}"
                : "DoorOpening failed";
        }
    }

    /// <summary>
    /// �������}��T�C
    /// </summary>
    /// <param name="door">���Ϊ��}�� Transform�]�� right/forward/up �Ψөw�q���k/�e��/�W�U�^</param>
    /// <param name="wallLayer">��Ҧb�� LayerMask</param>
    /// <param name="maxSideProbe">���k�����̤j�Z���]��ĳ���j��i�઺�}�f�@�b�e�^</param>
    /// <param name="maxFrontBackProbe">�e�ᱴ���̤j�Z���]��ĳ >= ��p���@�b�^</param>
    /// <param name="maxUpProbe">�V�W�����̤j�Z���]����^</param>
    /// <param name="maxDownProbe">�V�U�����̤j�Z���]�a��/���e�^</param>
    /// <param name="widthMargin">�e����Z�]�C���U���@���A�`�@���⦸�^</param>
    /// <param name="heightMargin">������Z�]�P�W�^</param>
    /// <param name="thicknessMargin">�p����Z�]�⭱�X�p�^</param>
    /// <param name="minThickness">��������ɪ���p�U��</param>
    /// <param name="surfacePush">���ߪu��k�u�L���A�קK z-fighting</param>
    /// <param name="liftForSidecast">���V�g�u�Y�Q�a�}�u���צ�ɪ���ɰ���</param>
    /// <param name="existingOpening">�i��G�Y�w�g���@�ӥN����}�� BoxCollider�A�i�����Υ��� bounds �@�����G</param>
    /// <param name="result">��X�������G</param>
    public static void Detect(
        Transform door,
        LayerMask wallLayer,
        float maxSideProbe,
        float maxFrontBackProbe,
        float maxUpProbe,
        float maxDownProbe,
        float widthMargin,
        float heightMargin,
        float thicknessMargin,
        float minThickness,
        float surfacePush,
        float liftForSidecast,
        BoxCollider existingOpening,
        out Result result)
    {
        result = new Result
        {
            success = false,
            right = door.right,
            up = door.up,
        };

        // �p�G���{���� opening collider�A�����Υ�
        if (existingOpening != null)
        {
            Bounds b = existingOpening.bounds;
            result.openingWidth = Mathf.Max(0.01f, b.size.x - widthMargin * 2f);
            result.openingHeight = Mathf.Max(0.01f, b.size.y - heightMargin * 2f);
            result.wallThickness = Mathf.Max(minThickness, b.size.z - thicknessMargin * 2f);
            result.openingCenter = b.center + door.forward.normalized * surfacePush;
            result.wallNormal = door.forward.normalized;
            result.leftEdge = b.center - result.right * (result.openingWidth * 0.5f);
            result.rightEdge = b.center + result.right * (result.openingWidth * 0.5f);
            result.success = true;
            return;
        }

        Vector3 origin = door.position;
        Vector3 right = door.right.normalized;
        Vector3 left = -right;
        Vector3 forward = door.forward.normalized;
        Vector3 back = -forward;
        Vector3 up = door.up.normalized;
        result.right = right;
        result.up = up;

        // 1) �e����]�p�ס^
        bool hitFront = Physics.Raycast(origin, forward, out RaycastHit hitF, maxFrontBackProbe, wallLayer, QueryTriggerInteraction.Ignore);
        bool hitBack = Physics.Raycast(origin, back, out RaycastHit hitB, maxFrontBackProbe, wallLayer, QueryTriggerInteraction.Ignore);

        float thickness;
        Vector3 wallMid;
        if (hitFront && hitBack)
        {
            thickness = hitF.distance + hitB.distance;
            wallMid = (hitF.point + hitB.point) * 0.5f;
            result.frontHit = true; result.backHit = true;
            result.frontWallHit = hitF; result.backWallHit = hitB;
            // wallNormal ���V "forward" ��V
            result.wallNormal = forward;
        }
        else if (hitFront)
        {
            thickness = Mathf.Max(minThickness, minThickness);
            wallMid = hitF.point - forward * (thickness * 0.5f);
            result.frontHit = true; result.frontWallHit = hitF;
            result.wallNormal = forward;
        }
        else if (hitBack)
        {
            thickness = Mathf.Max(minThickness, minThickness);
            wallMid = hitB.point + forward * (thickness * 0.5f);
            result.backHit = true; result.backWallHit = hitB;
            result.wallNormal = forward; // �O����V�@�P
        }
        else
        {
            return; // �䤣����A����
        }

        // 2) ���k��]�e�ס^
        Vector3 sideOrigin = new Vector3(wallMid.x, origin.y, wallMid.z);
        bool hitL = Physics.Raycast(sideOrigin, left, out RaycastHit hitLeft, maxSideProbe, wallLayer, QueryTriggerInteraction.Ignore);
        bool hitR = Physics.Raycast(sideOrigin, right, out RaycastHit hitRight, maxSideProbe, wallLayer, QueryTriggerInteraction.Ignore);

        if (!hitL || !hitR)
        {
            Vector3 lifted = sideOrigin + up * Mathf.Max(0.0f, liftForSidecast);
            if (!hitL) hitL = Physics.Raycast(lifted, left, out hitLeft, maxSideProbe, wallLayer, QueryTriggerInteraction.Ignore);
            if (!hitR) hitR = Physics.Raycast(lifted, right, out hitRight, maxSideProbe, wallLayer, QueryTriggerInteraction.Ignore);
        }

        if (!hitL || !hitR) return;

        float width = hitLeft.distance + hitRight.distance;
        Vector3 leftEdge = sideOrigin - right * hitLeft.distance;
        Vector3 rightEdge = sideOrigin + right * hitRight.distance;

        result.leftWall = hitLeft.collider;
        result.rightWall = hitRight.collider;
        result.leftHit = hitLeft;
        result.rightHit = hitRight;
        result.leftEdge = leftEdge;
        result.rightEdge = rightEdge;

        // 3) �W�U�]���ס^
        Vector3 heightOrigin = (leftEdge + rightEdge) * 0.5f;
        bool hitDown = Physics.Raycast(heightOrigin + up * 0.1f, -up, out RaycastHit hitD, maxDownProbe, wallLayer, QueryTriggerInteraction.Ignore);
        bool hitUp = Physics.Raycast(heightOrigin - up * 0.1f, up, out RaycastHit hitU, maxUpProbe, wallLayer, QueryTriggerInteraction.Ignore);
        if (!hitDown && !hitUp)
        {
            // �U�ΤW�Y���b�P�h�A�i���\�u�Ψ䤤�@��
            // �h�ӨD�䦸�G�H�U�t�Ϊ���e Y�A���t�ΩR����
            hitDown = true;
            hitD = default; hitD.point = new Vector3(heightOrigin.x, origin.y, heightOrigin.z);
        }
        if (!hitUp && !hitDown) return;

        float height = Mathf.Abs((hitU.point - hitD.point).y);

        // 4) ��Z�P�p�׽վ�
        width = Mathf.Max(0.01f, width - widthMargin * 2f);
        height = Mathf.Max(0.01f, height - heightMargin * 2f);
        thickness = Mathf.Max(minThickness, thickness - thicknessMargin * 2f);

        // 5) ����
        Vector3 center = new Vector3(heightOrigin.x, (hitU.point.y + hitD.point.y) * 0.5f, heightOrigin.z);
        center += forward * surfacePush; // �L��

        // �g�^���G
        result.openingWidth = width;
        result.openingHeight = height;
        result.wallThickness = thickness;
        result.openingCenter = center;
        result.upHit = hitUp; result.downHit = hitDown;
        result.upHitInfo = hitU; result.downHitInfo = hitD;
        result.success = true;
    }
}