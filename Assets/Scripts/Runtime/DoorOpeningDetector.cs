using UnityEngine;

public static class DoorOpeningDetector
{
    // 偵測結果資料結構
    public struct Result
    {
        public bool success;

        // 牆與命中
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

        // 尺寸（世界空間）
        public float openingWidth;
        public float openingHeight;
        public float wallThickness;

        // 幾何點（世界空間）
        public Vector3 leftEdge;      // 左邊緣點（在牆厚中面）
        public Vector3 rightEdge;     // 右邊緣點（在牆厚中面）
        public Vector3 openingCenter; // 洞中心（牆厚中面）
        public Vector3 wallNormal;    // 指向「門的 forward」方向的牆法線（近似）
        public Vector3 up;            // 由門的姿態推得的 up
        public Vector3 right;         // 由門的姿態推得的 right

        // 方便直接用來設定 BoxCollider 的尺寸（世界空間寬高厚）
        public Vector3 sizeWS => new Vector3(openingWidth, openingHeight, wallThickness);

        public override string ToString()
        {
            return success
                ? $"DoorOpening success: W={openingWidth:F3}, H={openingHeight:F3}, T={wallThickness:F3}"
                : "DoorOpening failed";
        }
    }

    /// <summary>
    /// 偵測門洞資訊。
    /// </summary>
    /// <param name="door">門或門洞的 Transform（其 right/forward/up 用來定義左右/前後/上下）</param>
    /// <param name="wallLayer">牆所在的 LayerMask</param>
    /// <param name="maxSideProbe">左右探測最大距離（建議略大於可能的洞口一半寬）</param>
    /// <param name="maxFrontBackProbe">前後探測最大距離（建議 >= 牆厚的一半）</param>
    /// <param name="maxUpProbe">向上探測最大距離（頂梁）</param>
    /// <param name="maxDownProbe">向下探測最大距離（地面/門檻）</param>
    /// <param name="widthMargin">寬度邊距（每側各扣一次，總共扣兩次）</param>
    /// <param name="heightMargin">高度邊距（同上）</param>
    /// <param name="thicknessMargin">厚度邊距（兩面合計）</param>
    /// <param name="minThickness">偵測不到時的牆厚下限</param>
    /// <param name="surfacePush">中心沿牆法線微推，避免 z-fighting</param>
    /// <param name="liftForSidecast">側向射線若被地腳線等擋住時的抬升高度</param>
    /// <param name="existingOpening">可選：若已經有一個代表門洞的 BoxCollider，可直接用它的 bounds 作為結果</param>
    /// <param name="result">輸出偵測結果</param>
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

        // 如果有現成的 opening collider，直接用它
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

        // 1) 前後牆（厚度）
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
            // wallNormal 指向 "forward" 方向
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
            result.wallNormal = forward; // 保持方向一致
        }
        else
        {
            return; // 找不到牆，結束
        }

        // 2) 左右牆（寬度）
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

        // 3) 上下（高度）
        Vector3 heightOrigin = (leftEdge + rightEdge) * 0.5f;
        bool hitDown = Physics.Raycast(heightOrigin + up * 0.1f, -up, out RaycastHit hitD, maxDownProbe, wallLayer, QueryTriggerInteraction.Ignore);
        bool hitUp = Physics.Raycast(heightOrigin - up * 0.1f, up, out RaycastHit hitU, maxUpProbe, wallLayer, QueryTriggerInteraction.Ignore);
        if (!hitDown && !hitUp)
        {
            // 下或上若不在同層，可允許只用其中一個
            // 退而求其次：以下緣用門當前 Y，高緣用命中者
            hitDown = true;
            hitD = default; hitD.point = new Vector3(heightOrigin.x, origin.y, heightOrigin.z);
        }
        if (!hitUp && !hitDown) return;

        float height = Mathf.Abs((hitU.point - hitD.point).y);

        // 4) 邊距與厚度調整
        width = Mathf.Max(0.01f, width - widthMargin * 2f);
        height = Mathf.Max(0.01f, height - heightMargin * 2f);
        thickness = Mathf.Max(minThickness, thickness - thicknessMargin * 2f);

        // 5) 中心
        Vector3 center = new Vector3(heightOrigin.x, (hitU.point.y + hitD.point.y) * 0.5f, heightOrigin.z);
        center += forward * surfacePush; // 微推

        // 寫回結果
        result.openingWidth = width;
        result.openingHeight = height;
        result.wallThickness = thickness;
        result.openingCenter = center;
        result.upHit = hitUp; result.downHit = hitDown;
        result.upHitInfo = hitU; result.downHitInfo = hitD;
        result.success = true;
    }
}