using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System;
using TMPro;
using UnityEngine.Networking;

public class EditButton : MonoBehaviour
{
    public bool editMode = false;
    public bool DeleteMode = false;
    public Button button;
    public GameObject newPrefab; // Assign this in the Inspector with a prefab asset
    public LayerMask spawnLayerMask;
    public Camera camera2;
    private GameObject Wall;
    public float wallHeight;

    private float clickTimeThreshold = 0.2f;

    private float timeSinceMouseDown;
    public Vector3 current_pos;
    private GameObject Camera1_G;
    private GameObject Camera2_G;

    private GameObject newCube;
    private int touch_count = 0;

    public float rotationSensit = 10;
    private float SpinDegree = 0f;
    private float targetYaw, currentYaw, yawVelocity;
    [Range(0.05f, 4f)] public float yawSmoothTime = 0.12f;
    public float yawDragSensitivity = 300f;
    public bool enableInertia = true;
    public float inertiaDamp = 3f;
    private float angularVelDegPerSec;
    [Range(0f, 1f)] public float inputSmoothing = 0.2f;
    private float smoothedMouseX;

    public LayerMask collideCheckMask = ~0; // 可在 Inspector 調整
    [Range(0.9f, 1f)] public float overlapShrink = 0.98f; // 稍微縮小邊界，避免邊界抖動卡住
    public bool blockRotationOnCollision = true;



    private Button Save_button;
    private Button Cancel_button;
    private Button Delete_button;
    //private GameObject Move_button;
    //private Button Mbutton;
    //private bool Movemode;

    private TMP_Dropdown item_drop;
    private DropdownPopulator item_drop_script;

    private float itemScale = 1;

    private string saveName;

    public bool interactableDD;
    private Action<GameObject> registrar;
    public void SetRegistrar(Action<GameObject> r) { registrar = r; }

    public bool isEdit = false;

    // ----- 序列化用資料結構 -----
    [System.Serializable]
    private class CubeData
    {
        public string prefabName;
        public Vector3 position;
        public Quaternion rotation;
        public float scale;
    }

    [System.Serializable]
    private class SaveData
    {
        public List<CubeData> cubes = new List<CubeData>();
    }

    // 後端 /api/users/me/models 回傳
    [Serializable]
    private class UserModelsEnvelope
    {
        public bool ok;
        public List<string> models;
    }

    // 後端 /api/users/me/models/<model_id>/meta 回傳
    [Serializable]
    private class MetaEnvelope
    {
        public bool ok;
        public string model_id;
        public SaveData meta; // 對應 { "meta": { "cubes": [...] } }
    }

    private SaveData saveData = new SaveData(); //initiallize saveData
    private string savePath; // legacy（未使用），保留以避免編譯錯誤
    private List<GameObject> spawnedCubes = new List<GameObject>(); // Track spawned cubes

    //private string flaskApiUrl = "http://localhost:8000";
    private string flaskApiUrl = "";

    // 目前選定的模型 ID（由外部 JS 或內部查詢設置；不要手動在 Inspector 設）
    [SerializeField] private string currentModelId = null;

    // ========= Unity lifecycle =========
    void Start()
    {
        Debug.Log($"[EditButton::Start] begin GO={name} activeSelf={gameObject.activeSelf} enabled={enabled}");

        if (button == null) button = GetComponent<Button>();
        if (button == null) { Debug.LogError("[EditButton] Button component missing on this GameObject"); return; }

        //if (Mbutton == null) Mbutton = Move_button.GetComponent<Button>();

        button.onClick.RemoveListener(ToggleEditMode);
        button.onClick.AddListener(ToggleEditMode);
        Debug.Log("Added listener ToggleEditMode");

        //Mbutton.onClick.RemoveListener(ToggleMoveMode);
        //Mbutton.onClick.AddListener(ToggleMoveMode);
        //Mbutton.interactable = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        interactableDD = true;
        savePath = $"{flaskApiUrl}/item_json/cubes.json"; // legacy
#else
        savePath = Path.Combine(Application.persistentDataPath, "cubes.json"); // legacy
#endif

        var saveGO = GameObject.Find("Save");
        var cancelGO = GameObject.Find("Cancel");
        var deleteGO = GameObject.Find("Delete");
        Save_button = saveGO ? saveGO.GetComponent<Button>() : null;
        Cancel_button = cancelGO ? cancelGO.GetComponent<Button>() : null;
        Delete_button = deleteGO ? deleteGO.GetComponent<Button>() : null;
        Debug.Log($"[EditButton] Save_button={(Save_button ? Save_button.name : "null")} Cancel_button={(Cancel_button ? Cancel_button.name : "null")}");
        if (Save_button != null) Save_button.onClick.AddListener(() => SaveItem(newCube, itemScale));
        if (Cancel_button != null) Cancel_button.onClick.AddListener(() => CancelItem(newCube));
        if (Delete_button != null) Delete_button.onClick.AddListener(() => ToggleDeleteMode());

        var go = GameObject.Find("select_ITEM");
        item_drop = go ? go.GetComponent<TMP_Dropdown>() : null;
        if (item_drop == null) { Debug.LogError("[EditButton] TMP_Dropdown 'select_ITEM' not found"); return; }
        item_drop_script = item_drop.GetComponent<DropdownPopulator>();
        if (item_drop_script == null) { Debug.LogError("[EditButton] DropdownPopulator not found on select_ITEM"); return; }

        button.GetComponent<Image>().color = Color.red;
        Delete_button.GetComponent<Image>().color = Color.red;
        Debug.Log($"[EditButton] Button listeners bound. interactable={(button ? button.interactable : false)}");
        if (spawnLayerMask.value == 0) spawnLayerMask = ~0;

        Debug.Log("[EditButton::Start] end");

        // 開機後等環境 ready 再載入目前模型的 meta
        StartCoroutine(WaitUntilReadyThenLoad());
        SyncUiByModes();
    }

    void Update()
    {
        if (item_drop_script == null || camera2 == null)
        {
            // 
            return;
        }
        string selectedKey = item_drop_script.selectedText;
        if (string.IsNullOrEmpty(selectedKey))
        {
            Debug.Log("nothing is selected in the Dropdown");
            return;
        }
        

        if (newPrefab == null || saveName != selectedKey)
        {
            var loaded = Resources.Load<GameObject>(selectedKey);
            if (loaded == null)
            {
                Debug.LogError($"[EditButton] Resources.Load failed key='{selectedKey}'. Make sure prefab sits in Assets/Resources and name matches case.");
                return;
            }
            newPrefab = loaded;
            saveName = selectedKey; // store the actual Resources key
            Debug.Log($"[EditButton::Update] Loaded key='{selectedKey}' prefab='{newPrefab.name}'");
        }
        if (newPrefab == null) return;

        itemScale = item_drop_script.itemScale;
        EnsureWallHeight();

        button.GetComponent<Image>().color = editMode ? Color.green : Color.red;
        //Delete_button.GetComponent<Image>().color = DeleteMode ? Color.green : Color.red;
        if (isEdit)
        {
            if (editMode)
            {
                //fix camera position
                camera2.transform.position = current_pos;

                //spawn item
                if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                {
                    Debug.Log("LMB down and not over UI");
                    touch_count++;
                    interactableDD = false;
                    timeSinceMouseDown = 0;
                    Debug.Log("click");
                    Debug.Log("touch_count: " + touch_count);

                    if (touch_count == 1 && timeSinceMouseDown <= clickTimeThreshold)
                    {
                        Ray ray = camera2.ScreenPointToRay(Input.mousePosition);
                        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);
                        RaycastHit hit;
                        Debug.Log($"ray from {ray.origin} dir={ray.direction} cam2={camera2.name}");
                        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~0))
                        {
                            Debug.Log("shoot");
                            var hitGo = hit.collider != null ? hit.collider.gameObject : null;
                            if (hitGo != null)
                            {
                                Debug.Log($"Raycast hit {hitGo.name} layer={hitGo.layer}");
                                if (IsFloorGO(hitGo))
                                {
                                    Debug.Log("hit floor");
                                    Vector3 spawnPosition = hit.point;
                                    spawnPosition.y = 0.01f;
                                    newCube = Instantiate(newPrefab, spawnPosition, newPrefab.transform.rotation);
                                    if (newCube != null)
                                    {
                                        currentYaw = newCube.transform.eulerAngles.y;
                                        targetYaw = currentYaw;
                                        yawVelocity = 0f;
                                        smoothedMouseX = 0f;
                                        angularVelDegPerSec = 0f;
                                    }
                                    registrar?.Invoke(newCube);
                                    EnsureWallHeight();
                                    newCube.transform.localScale = Vector3.one * wallHeight * itemScale;

                                    int cubeLayer = LayerMask.NameToLayer("CubeLayer");
                                    if (cubeLayer != -1) newCube.layer = cubeLayer;
                                }
                                else
                                {
                                    touch_count = 0;
                                }
                            }
                            else
                            {
                                Debug.Log("Raycast hit but collider is null (unexpected).");
                            }
                        }
                        else
                        {
                            Debug.Log("Raycast miss");
                        }
                    }

                }
                else if (Input.GetMouseButton(0) && !IsPointerOverUI())
                {
                    timeSinceMouseDown += Time.deltaTime;
                    if (timeSinceMouseDown >= clickTimeThreshold && touch_count >= 2)
                    {
                        Debug.Log("Detect second touch and draging...");
                        //float m_x = Input.GetAxis("Mouse X") * rotationSensit;
                        //doSpin(m_x);
                        float rawX = Input.GetAxis("Mouse X");
                        smoothedMouseX = Mathf.Lerp(smoothedMouseX, rawX, 1f - Mathf.Pow(1f - inputSmoothing, Time.unscaledDeltaTime * 60f));
                        DoSmoothSpin(smoothedMouseX);
                    }
                    else if (timeSinceMouseDown >= clickTimeThreshold && touch_count == 1)
                    {
                        if (newCube == null)
                        {
                            Debug.Log("[EditButton::Update] newCube is null, skip move/delete");
                            return;
                        }
                        Ray ray = camera2.ScreenPointToRay(Input.mousePosition);
                        Vector3 targetPos = default;
                        bool gotTarget = false;
                        int floorLayer = LayerMask.NameToLayer("FloorLayer");
                        var hits = Physics.RaycastAll(ray, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore);
                        if (hits.Length == 0) { Debug.Log("RaycastAll: 0 hits"); }
                        else
                        {
                            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                            foreach (var h in hits)
                            {
                                Debug.Log($"RaycastAll hit -> {h.collider.gameObject.name} layer={h.collider.gameObject.layer} dist={h.distance}");
                            }
                        }
                        if (hits != null && hits.Length > 0)
                        {
                            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                            foreach (var h in hits)
                            {
                                var goH = h.collider.gameObject;
                                bool isFloor = (floorLayer != -1 && goH.layer == floorLayer) || goH.name == "Floor";
                                if (isFloor)
                                {
                                    targetPos = new Vector3(h.point.x, 0.01f, h.point.z);
                                    gotTarget = true;
                                    break;
                                }

                            }
                        }
                        /*if (!gotTarget)
                        {
                            Debug.Log("cannot get floor, Building a ground...");
                            Plane ground = new Plane(Vector3.up, new Vector3(0f, 0.01f, 0f));
                            if (ground.Raycast(ray, out float enter))
                            {
                                Vector3 p = ray.GetPoint(enter);
                                targetPos = new Vector3(p.x, 0.01f, p.z);
                                gotTarget = true;
                            }
                        }
                        if (!gotTarget)
                        {
                            return;
                        }*/

                        Collider moverCol = newCube.GetComponent<Collider>();
                        if (moverCol == null) return;

                        Bounds bounds = moverCol.bounds;
                        Vector3 centerOffset = bounds.center - newCube.transform.position;
                        Vector3 desiredCenter = new Vector3(targetPos.x + centerOffset.x, bounds.center.y, targetPos.z + centerOffset.z);
                        Vector3 halfExtents = bounds.extents * 0.95f;

                        Collider[] overlaps = Physics.OverlapBox(
                           desiredCenter,
                           halfExtents,
                           newCube.transform.rotation,
                           ~0,
                           QueryTriggerInteraction.Ignore
                        );

                        bool blocked = false;
                        foreach (var col in overlaps)
                        {
                            if (col == null) continue;
                            if (col.transform == newCube.transform) continue;

                            bool colIsFloor =
                                (floorLayer != -1 && col.gameObject.layer == floorLayer) ||
                                col.gameObject.name == "Floor";

                            if (colIsFloor) continue;

                            blocked = true;
                            break;
                        }

                        if (!blocked)
                        {
                            Vector3 newPos = new Vector3(targetPos.x, newCube.transform.position.y, targetPos.z);
                            newCube.transform.position = newPos;
                        }
                        else
                        {
                            Debug.Log("Blocked! Cannot move into other object.");
                        }
                    }
                }
                else if (Input.GetMouseButtonUp(0) && !IsPointerOverUI())
                {
                    Debug.Log("timeSinceMouseDown :" + timeSinceMouseDown);
                    timeSinceMouseDown = 0;

                    // 重要：鎖定目標避免回彈
                    if (newCube != null)
                    {
                        targetYaw = currentYaw = newCube.transform.eulerAngles.y;
                        yawVelocity = 0f;
                        
                        angularVelDegPerSec = 0f;
                    }

                }

                if (!Input.GetMouseButton(0) && newCube != null && enableInertia)
                {
                    // 把 targetYaw 往前帶一點，製造慣性
                    targetYaw += angularVelDegPerSec * Time.deltaTime;
                    // 自然衰減角速度
                    float sign = Mathf.Sign(angularVelDegPerSec);
                    angularVelDegPerSec = Mathf.MoveTowards(angularVelDegPerSec, 0f, inertiaDamp * Time.deltaTime);

                    // 以相同 SmoothDamp 再套一次
                    currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, yawSmoothTime);
                    var e = newCube.transform.eulerAngles;
                    e.y = currentYaw;
                    newCube.transform.eulerAngles = e;
                }
                //de-spawn item
                if (Input.GetMouseButtonDown(1) && !IsPointerOverUI())
                {
                    timeSinceMouseDown = 0;
                }
                else if (Input.GetMouseButton(1) && !IsPointerOverUI())
                {
                    timeSinceMouseDown += Time.deltaTime;
                }
                else if (Input.GetMouseButtonUp(1) && !IsPointerOverUI())
                {
                    if (timeSinceMouseDown < clickTimeThreshold)
                    {
                        int layerindex = LayerMask.GetMask("CubeLayer");
                        Ray ray = camera2.ScreenPointToRay(Input.mousePosition);
                        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);
                        RaycastHit hit;
                        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerindex) && !hit.collider.gameObject.name.Contains("Floor") && !hit.collider.gameObject.name.Contains("Window") && !hit.collider.gameObject.name.Contains("Door"))
                        {
                            Debug.Log("Hit detected on: " + hit.collider.gameObject.name);
                            deletItem(hit.collider.gameObject);
                        }
                        else
                        {
                            Debug.Log("No surface hit for spawning. Check collider on target, layer, or camera setup.");
                        }
                    }
                    timeSinceMouseDown = 0;
                }
            }
            else
            {
                newCube = null;
                touch_count = 0;
                if (DeleteMode)
                {
                    if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                    {
                        int layerindex = LayerMask.GetMask("CubeLayer");
                        Ray ray = camera2.ScreenPointToRay(Input.mousePosition);
                        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);
                        RaycastHit hit;
                        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerindex) && !hit.collider.gameObject.name.Contains("Floor") && !hit.collider.gameObject.name.Contains("Window") && !hit.collider.gameObject.name.Contains("Door"))
                        {
                            Debug.Log("Hit detected on: " + hit.collider.gameObject.name);
                            deletItem(hit.collider.gameObject);
                        }
                        else
                        {
                            Debug.Log("No surface hit for spawning. Check collider on target, layer, or camera setup.");
                        }
                    }
                }
            }
        }
       
    }

    void ToggleEditMode()
    {
        Debug.Log($"[EditButton::ToggleEditMode] start cam1={(Camera1_G ? Camera1_G.name : "null")} cam2={(Camera2_G ? Camera2_G.name : "null")} c2cam={(camera2 ? camera2.name : "null")} c1Active={(Camera1_G ? Camera1_G.activeSelf : false)} c2Active={(Camera2_G ? Camera2_G.activeSelf : false)}");
        Debug.Log($"[EditButton] ToggleEditMode clicked. before={editMode}");
        try
        {
            if (Camera1_G != null && Camera2_G != null && !Camera1_G.activeSelf && Camera2_G.activeSelf)
            {
                editMode = !editMode;
            }
            else
            {
                editMode = false;
            }

            Debug.Log("Edit mode: " + (editMode ? "ON" : "OFF"));
            if (editMode)
            {
                
                DeleteMode = false;
                
                if (camera2 == null)
                {
                    Debug.LogWarning("[EditButton] enter edit but camera2 null");
                    editMode = false;
                }
                else GetCurrentPos();
            }
            else {
                
                touch_count = 0;
                CancelItem(newCube);
            }
        }
        catch (System.Exception ex) { Debug.LogError("[EditButton::ToggleEditMode] exception: " + ex); }
        Debug.Log("[EditButton::ToggleEditMode] end");
        SyncUiByModes();
    }

    void ToggleDeleteMode()
    {
        if (!editMode && Camera1_G != null && Camera2_G != null && !Camera1_G.activeSelf && Camera2_G.activeSelf)
        {
                DeleteMode = !DeleteMode;
        }

        //Delete_button.GetComponent<Image>().color = DeleteMode ? Color.green : Color.red;
        SyncUiByModes();
    }
    /*public void ToggleMoveMode()
    {
        if (editMode)
        {
            Movemode = !Movemode;
        }
    }*/

    public void SyncUiByModes()
    {
        if (Delete_button != null)
        {
            Delete_button.interactable = !editMode;
            Delete_button.GetComponent<Image>().color = DeleteMode ? Color.green : Color.red;
        }
        if (button != null)
        {
            button.GetComponent<Image>().color = editMode ? Color.green : Color.red;
        }
    }

    void GetCurrentPos()
    {
        Debug.Log($"[EditButton::GetCurrentPos] camera2={(camera2 ? camera2.name : "null")}");
        if (camera2 == null)
        {
            Debug.LogWarning("[EditButton] GetCurrentPos skipped: camera2 is null");
            return;
        }
        current_pos = camera2.transform.position;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current != null)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.touchCount > 0 ? Input.GetTouch(0).position : Input.mousePosition;
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            return results.Count > 0;
        }
        return false;
    }

    //currently not used
    private void doSpin(float x)
    {
        if (newCube != null)
        {
            Debug.Log("Do spinning");
            float rotationAmount = (x / Screen.width) * 360f * rotationSensit;
            SpinDegree -= rotationAmount;
            Debug.Log("Spining Degree: " + SpinDegree);

            newCube.transform.eulerAngles = new Vector3(0, SpinDegree, 0);
        }
    }
    /// //////////////////////////////
    
    private void DoSmoothSpin(float mouseX)
    {
        if (newCube == null) return;
        if (float.IsNaN(currentYaw)|| float.IsNaN(targetYaw))
        {
            currentYaw = newCube.transform.eulerAngles.y;
            targetYaw = currentYaw;
            yawVelocity = 0f;
        }

        float deltaYaw = mouseX * yawDragSensitivity * Time.deltaTime;
        targetYaw -= deltaYaw;

        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, yawSmoothTime);

        var e = newCube.transform.eulerAngles;
        e.y = currentYaw;
        newCube.transform.eulerAngles = e;
        float instvel = deltaYaw / Mathf.Max(Time.deltaTime, 1e-5f);
        angularVelDegPerSec = Mathf.Lerp(angularVelDegPerSec, instvel, 0.5f);
    }
    // the following functions are not used temporarily
    private void DoSpinWithCollision(float x)
    {
        if (newCube == null) return;
        if (float.IsNaN(currentYaw) || float.IsNaN(targetYaw) || (currentYaw == 0f && targetYaw == 0f))
        {
            currentYaw = newCube.transform.eulerAngles.y;
            targetYaw = currentYaw;
            yawVelocity = 0f;
        }

        float deltaYaw = x * yawDragSensitivity * Time.deltaTime;
        float candidateTarget = targetYaw - deltaYaw;
        float testYaw = Mathf.SmoothDampAngle(currentYaw, candidateTarget, ref yawVelocity, yawSmoothTime);

        if (!WouldCollideAtYaw(testYaw))
        {
            targetYaw = candidateTarget;
            currentYaw = testYaw;

            var e = newCube.transform.eulerAngles;
            e.y = currentYaw;
            newCube.transform.eulerAngles = e;

        }
        else
        {
            if (blockRotationOnCollision)
            {
                yawVelocity = 0f;
            }
            else
            {
                if (TryFindMaxAllowedYaw(currentYaw, candidateTarget, 6, out float edgeYaw))
                {
                    targetYaw = currentYaw = edgeYaw;
                    yawVelocity = 0f;
                    var e = newCube.transform.eulerAngles;
                    e.y = currentYaw;
                    newCube.transform.eulerAngles = e;
                }
                else
                {
                    yawVelocity = 0f;
                }
            }
        }
    }
    private bool WouldCollideAtYaw(float yawDeg)
    {
        // 取目標物的任一 Collider（支援 MeshCollider）
        var col = newCube.GetComponentInChildren<Collider>();
        if (col == null) return false; // 沒碰撞器就視為不會撞
                                       // 以當前世界 AABB 估中心與半尺寸（AABB 會較保守，但足以防穿模）
        Bounds b = col.bounds;
        Vector3 center = b.center;
        Vector3 halfExtents = b.extents * overlapShrink;

        // 只繞 Y 旋轉
        Quaternion rot = Quaternion.Euler(0f, yawDeg, 0f);

        // 查詢所有重疊的碰撞器
        Collider[] hits = Physics.OverlapBox(center, halfExtents, rot, collideCheckMask, QueryTriggerInteraction.Ignore);

        foreach (var h in hits)
        {
            if (h == null) continue;

            // 忽略自身（包含子節點）
            if (IsSelfOrChild(h.transform, newCube.transform)) continue;

            // 忽略地板（依你的規則）
            if (IsFloor(h.gameObject)) continue;

            // 只要有一個阻擋物就算會撞
            return true;
        }
        return false;
    }
    private bool TryFindMaxAllowedYaw(float fromYaw, float toYaw, int iters, out float okYaw)
    {
        okYaw = fromYaw;
        float lo = fromYaw, hi = toYaw;
        // 從當前角往目標角做二分搜尋，找到最接近且不碰撞的角度
        for (int i = 0; i < iters; i++)
        {
            float mid = Mathf.Lerp(lo, hi, 0.5f);
            if (!WouldCollideAtYaw(mid))
            {
                okYaw = mid;
                lo = mid;
            }
            else
            {
                hi = mid;
            }
        }
        return !Mathf.Approximately(okYaw, fromYaw);
    }
    private static bool IsSelfOrChild(Transform t, Transform root)
    {
        return t == root || t.IsChildOf(root);
    }
    ///////////////////////////////
    // ========= New: per-user per-model API helpers =========
    private string GetModelsListUrl()
    {
        return $"/api/users/me/models";
    }
    private string GetMetaGetUrl(string modelId)
    {
        return $"/api/users/me/models/{UnityWebRequest.EscapeURL(modelId)}/meta";
    }
    private string GetMetaPostUrl(string modelId)
    {
        return $"/api/users/me/models/{UnityWebRequest.EscapeURL(modelId)}/meta";
    }

    private IEnumerator ResolveCurrentModelId()
    {
        // 若外部已指定，略過
        if (!string.IsNullOrEmpty(currentModelId)) yield break;

        UnityWebRequest uwr = UnityWebRequest.Get(GetModelsListUrl());
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[EditButton] GET models list failed: {uwr.error} body={uwr.downloadHandler.text}");
            yield break;
        }

        string body = uwr.downloadHandler.text;
        UserModelsEnvelope env = null;
        try
        {
            env = JsonUtility.FromJson<UserModelsEnvelope>(body);
        }
        catch (Exception ex)
        {
            Debug.LogError("[EditButton] parse models list error: " + ex);
            yield break;
        }

        if (env == null || !env.ok || env.models == null || env.models.Count == 0)
        {
            Debug.LogWarning("[EditButton] No models available for current user.");
            yield break;
        }

        currentModelId = env.models[0]; // 預設選第一個，可依需求改規則
        Debug.Log($"[EditButton] currentModelId resolved = {currentModelId}");
    }

    // ========= Load / Save per model =========
    public IEnumerator LoadItem()
    {
        // 確保有 modelId
        if (string.IsNullOrEmpty(currentModelId))
        {
            yield return ResolveCurrentModelId();
        }
        if (string.IsNullOrEmpty(currentModelId))
        {
            Debug.LogWarning("[EditButton::LoadItem] No modelId to load. Skipped.");
            yield break;
        }

        string url = GetMetaGetUrl(currentModelId);
        Debug.Log($"[EditButton::LoadItem] GET {url}");

        UnityWebRequest uwr = UnityWebRequest.Get(url);
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[EditButton::LoadItem] GET meta failed: {uwr.error}\nBody: {uwr.downloadHandler.text}");
            yield break;
        }

        string body = uwr.downloadHandler.text;
        if (string.IsNullOrEmpty(body))
        {
            Debug.LogWarning("[EditButton::LoadItem] Empty meta body.");
            yield break;
        }

        MetaEnvelope env = null;
        try
        {
            env = JsonUtility.FromJson<MetaEnvelope>(body);
        }
        catch (Exception ex)
        {
            Debug.LogError("[EditButton::LoadItem] JSON parse error: " + ex);
            yield break;
        }

        if (env == null || !env.ok)
        {
            Debug.LogWarning("[EditButton::LoadItem] meta not ok. Raw=" + body);
            yield break;
        }

        saveData = env.meta ?? new SaveData();

        // 清空現場再生成
        foreach (var go in spawnedCubes)
        {
            if (go != null) Destroy(go);
        }
        spawnedCubes.Clear();

        EnsureWallHeight();

        int layerindex = LayerMask.NameToLayer("CubeLayer");
        foreach (var cubeData in saveData.cubes)
        {
            string prefabName = string.IsNullOrEmpty(cubeData.prefabName) ? "ch1" : cubeData.prefabName;
            GameObject prefabToLoad = Resources.Load<GameObject>(prefabName);
            if (prefabToLoad == null)
            {
                Debug.LogWarning($"[EditButton::LoadItem] prefab '{prefabName}' not found, skip.");
                continue;
            }

            GameObject loaded = Instantiate(prefabToLoad, cubeData.position, cubeData.rotation);
            registrar?.Invoke(loaded);

            loaded.transform.localScale = Vector3.one * wallHeight * cubeData.scale;

            if (layerindex != -1) loaded.layer = layerindex;

            spawnedCubes.Add(loaded);
        }

        Debug.Log($"[EditButton::LoadItem] Spawned {spawnedCubes.Count} items from model '{env.model_id}'.");
        Debug.Log($"[EditButton] GET url={url} modelId='{currentModelId}'");
        yield break;
    }

    void SaveItem(GameObject item, float itemscale)
    {
        if (item != null && editMode)
        {
            spawnedCubes.Add(item);

            CubeData cubeData = new CubeData
            {
                prefabName = saveName,
                position = item.transform.position,
                rotation = item.transform.rotation,
                scale = itemscale
            };
            saveData.cubes.Add(cubeData);

            string json = JsonUtility.ToJson(saveData, true);
#if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(SaveFileToServer(json));
#else
            // Editor/Standalone 同樣走 API，便於驗證
            StartCoroutine(SaveFileToServer(json));
#endif
        }
        newCube = null;
        touch_count = 0;
        interactableDD = true;
    }

    IEnumerator SaveFileToServer(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("SaveFileToServer called with empty json!");
            yield break;
        }

        // 確保有 modelId（由 JS SendMessage 或 ResolveCurrentModelId 設定）
        if (string.IsNullOrEmpty(currentModelId))
        {
            yield return ResolveCurrentModelId();
        }
        if (string.IsNullOrEmpty(currentModelId))
        {
            Debug.LogError("[EditButton] Save failed: no modelId. Upload a model first or call SelectModelAndReload(id).");
            yield break;
        }

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        string url = GetMetaPostUrl(currentModelId);
        UnityWebRequest uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");

        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[EditButton] Meta saved for model '{currentModelId}'");
            Debug.Log($"[EditButton] POST url={url} bodyLen={bodyRaw.Length} modelId='{currentModelId}'");
        }
        else
        {
            Debug.LogError("Save error: " + uwr.error + "\nResponse: " + uwr.downloadHandler.text);
        }
    }

    void CancelItem(GameObject item)
    {
        Destroy(item);
        newCube = null;
        touch_count = 0;
        interactableDD = true;
    }

    void deletItem(GameObject item)
    {
        for (int i = 0; i < saveData.cubes.Count; i++)
        {
            if (Vector3.Distance(saveData.cubes[i].position, item.transform.position) < 0.01f)
            {
                saveData.cubes.RemoveAt(i);
                break;
            }
        }
        spawnedCubes.Remove(item);

        string json = JsonUtility.ToJson(saveData, true);

#if UNITY_WEBGL && !UNITY_EDITOR
        StartCoroutine(SaveFileToServer(json));
#else
        StartCoroutine(SaveFileToServer(json));
#endif

        Destroy(item);
        touch_count = 0;
    }

    public void SetCameras(GameObject c1, GameObject c2)
    {
        Camera1_G = c1;
        Camera2_G = c2;
        camera2 = c2 ? c2.GetComponent<Camera>() : null;
        Debug.Log($"[EditButton::SetCameras] cam1={(c1 ? c1.name : "null")} cam2={(c2 ? c2.name : "null")} camera2={(camera2 ? camera2.name : "null")}");
    }

    public void SetWall()
    {
        Debug.Log("[EditButton::SetWall] begin");
        try
        {
            Wall = GameObject.Find("Wall_0");
            if (Wall == null)
            {
                Debug.LogWarning("[EditButton] Wall_0 not found, wallHeight default=1");
                wallHeight = 1f;
            }
            else
            {
                var wallrenderer = Wall.GetComponent<MeshRenderer>();
                if (wallrenderer != null) wallHeight = wallrenderer.bounds.size.y;
                else { wallHeight = 1f; Debug.LogWarning("[EditButton] Wall_0 has no MeshRenderer, wallHeight default=1"); }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[EditButton::SetWall] exception: " + ex);
        }

        Debug.Log("[EditButton::SetWall] begin");
    }

    void OnEnable()
    {
        if (button != null) button.interactable = true;
        if (button != null) Debug.Log($"[OnLoadSucceeded] editButton interactable=true ref={button.name} id={button.GetInstanceID()}");
    }
    void OnDisable() { Debug.Log($"[EditButton::OnDisable] {name}"); }

    bool IsFloorGO(GameObject go)
    {
        int floorLayer = LayerMask.NameToLayer("FloorLayer");
        return (floorLayer != -1 && go.layer == floorLayer);
    }
    //THIS FUNCTION IS NOT USED
    private bool IsFloor(GameObject go)
    {
        int floorLayer = LayerMask.NameToLayer("FloorLayer");
        if (floorLayer != -1 && go.layer == floorLayer) return true;
        if (go.name == "Floor") return true;
        return false;
    }
    //
    IEnumerator WaitUntilReadyThenLoad()
    {
        while (camera2 == null) yield return null;
        GameObject floor = null;
        int floorLayer = LayerMask.NameToLayer("FloorLayer");
        for (; ; )
        {
            floor = GameObject.Find("Floor");
            bool ok = (floor != null) || (floorLayer != -1 && GameObject.FindObjectOfType<Transform>() != null && AnyObjectOnLayer(floorLayer));
            if (floor != null || ok) break;
            yield return null;
        }
        yield return StartCoroutine(LoadItem());
        Debug.Log("[EditButton] LoadItem done after floor/camera ready");
    }

    static bool AnyObjectOnLayer(int layer)
    {
        var all = GameObject.FindObjectsOfType<Transform>();
        foreach (var t in all) if (t.gameObject.layer == layer) return true;
        return false;
    }

    public void TriggerLoadWhenReady()
    {
        StartCoroutine(WaitUntilReadyThenLoad());
    }

    void EnsureWallHeight()
    {
        if (wallHeight <= 1f)
        {
            SetWall();
            if (wallHeight <= 1f) wallHeight = 2.5f;
        }
    }

    // ===== 新增：供網頁 JS 呼叫，切換模型並重新載入 =====
    // JS 用法（houseModels_backup 的「載入」按鈕）：
    // unityInstance.SendMessage("EditButtonGO", "SelectModelAndReload", modelId);
    public void SelectModelAndReload(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            Debug.LogWarning("[EditButton] SelectModelAndReload: empty modelId ignored.");
            return;
        }

        var trimmed = modelId.Trim();
        if (string.Equals(trimmed, "undefined", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"[EditButton] SelectModelAndReload: invalid modelId '{trimmed}' ignored.");
            return;
        }
        Debug.Log($"[EditButton] SelectModelAndReload -> {trimmed}");
        // 清除進行中的新物件編輯狀態
        if (newCube != null)
        {
            Destroy(newCube);
            newCube = null;
        }
        touch_count = 0;
        interactableDD = true;
        SpinDegree = 0f;

        // 更新目前模型 ID
        currentModelId = trimmed;
        StopAllCoroutines();
        // 重新載入該模型的 meta
        StartCoroutine(WaitUntilReadyThenLoad());
    }
}