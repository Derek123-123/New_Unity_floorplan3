using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System;
using TMPro;

public class EditButton1 : MonoBehaviour
{
    public bool editMode = false;
    public Button button;
    public GameObject newPrefab; // Assign this in the Inspector with a prefab asset
    public LayerMask spawnLayerMask;
    public Camera camera2;
    private GameObject Wall;
    private float wallHeight;

    private float clickTimeThreshold = 0.2f;

    private float timeSinceMouseDown;
    public Vector3 current_pos;
    private GameObject Camera1_G;
    private GameObject Camera2_G;


    private GameObject newCube;
    private int touch_count = 0;
    public float rotationSensit = 10;
    private float SpinDegree = 0f;


    private Button Save_button;
    private Button Cancel_button;

    private TMP_Dropdown item_drop;
    private DropdownPopulator item_drop_script;

    private float itemScale = 1;

    private string saveName;

    public bool interactableDD;
    // a data class called CubeData
    [System.Serializable]
    private class CubeData
    {
        public string prefabName;
        public Vector3 position;
        public Quaternion rotation;
        public float scale;
    }

    // a class to save the CubeData
    [System.Serializable]
    private class SaveData
    {
        public List<CubeData> cubes = new List<CubeData>();
    }

    private SaveData saveData = new SaveData(); //initiallize saveData
    private string savePath; //where the data json save to
    private List<GameObject> spawnedCubes = new List<GameObject>(); // Track spawned cubes

    // Start is called before the first frame update
    void Start()
    {

        // define save path
        interactableDD = true;
        savePath = Path.Combine(Application.persistentDataPath, "cubes.json"); //derfine the save path

        //load the saved object
        // load the item form the json file

        Camera1_G = GameObject.Find("Camera1");
        Camera2_G = GameObject.Find("Camera2");

        Save_button = GameObject.Find("Save").GetComponent<Button>();
        Cancel_button = GameObject.Find("Cancel").GetComponent<Button>();

        Wall = GameObject.Find("Wall_0");
        MeshRenderer wallrenderer = Wall.GetComponent<MeshRenderer>();
        wallHeight = wallrenderer.bounds.size.y;

        var go = GameObject.Find("select_ITEM");
        if (go != null)
        {
            item_drop = go.GetComponent<TMP_Dropdown>();
        }
        else
        {
            Debug.LogError("go is null");
        }
        if (item_drop == null)
        {
            Debug.LogError("Dropdown component not found on GameObject 'Dropdown'!");
            return;
        }
        item_drop_script = item_drop.GetComponent<DropdownPopulator>();

        LoadItem();

        if (button == null)
        {
            button = GetComponent<Button>();

        }
        button.GetComponent<Image>().color = Color.red;
        camera2 = GameObject.Find("Camera2").GetComponent<Camera>();

        button.onClick.AddListener(ToggleEditMode);
        button.onClick.AddListener(GetCurrentPos);
        Save_button.onClick.AddListener(() => SaveItem(newCube, itemScale));
        Cancel_button.onClick.AddListener(() => CancelItem(newCube));

        if (spawnLayerMask.value == 0)
        {
            spawnLayerMask = ~0; // Hits all layers
        }





    }

    void Update()
    {
        string selectedText = item_drop_script.selectedText;
        newPrefab = Resources.Load<GameObject>(selectedText);
        itemScale = item_drop_script.itemScale;
        //int selectedIndex = item_drop.value;
        //string selectedText = item_drop.options[selectedIndex].text;


        if (editMode == true)
        {
            button.GetComponent<Image>().color = Color.green;
        }
        else
        {
            button.GetComponent<Image>().color = Color.red;
        }
        if (editMode)
        {
            //fix camera position
            camera2.transform.position = current_pos;
            ////////////////////////


            //spawn item
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                touch_count++;
                interactableDD = false;
                timeSinceMouseDown = 0;

                if (touch_count == 1 && timeSinceMouseDown <= clickTimeThreshold)
                {
                    Ray ray = camera2.ScreenPointToRay(Input.mousePosition);
                    Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, spawnLayerMask))
                    {
                        if ((newPrefab.name.Contains("ch") || newPrefab.name.Contains("Desk")) && hit.collider.gameObject.name == "Floor")
                        {
                            Debug.Log("Hit detected on: " + hit.collider.gameObject.name);
                            Vector3 spawnPosition = hit.point;
                            spawnPosition.y = 0.01f;
                            newCube = Instantiate(newPrefab, spawnPosition, newPrefab.transform.rotation);        // spawn the newly click item and save it in the json files
                            saveName = newPrefab.name;
                            newCube.transform.localScale = Vector3.one * wallHeight * itemScale;
                            int layerindex = LayerMask.NameToLayer("CubeLayer");
                            if (layerindex != -1)
                            {
                                newCube.layer = layerindex;
                            }
                            /*
                            Collider existingCol = newCube.GetComponent<Collider>();
                            if (existingCol != null && !(existingCol is MeshCollider))
                            {
                                Destroy(existingCol);
                            }
                            if (newCube.GetComponent<MeshCollider>() == null)
                            {
                                MeshCollider mc = newCube.AddComponent<MeshCollider>();
                                Debug.Log("MeshCollider gave to " + newCube.name);
                                mc.convex = false; // non-convex is more accurate (ok for placement)
                            }*/
                        }
                        /*else if(newPrefab.name.Contains("Desk") && hit.collider.gameObject.name == "Floor")
                        {
                            Debug.Log("Hit detected on: " + hit.collider.gameObject.name);
                            Vector3 spawnPosition = hit.point;
                            spawnPosition.y = 0.01f;
                            newCube = Instantiate(newPrefab, spawnPosition, newPrefab.transform.rotation);        // spawn the newly click item and save it in the json files
                            saveName = newPrefab.name;
                            newCube.transform.localScale = Vector3.one * wallHeight * itemScale;
                            int layerindex = LayerMask.NameToLayer("CubeLayer");
                            if (layerindex != -1)
                            {
                                newCube.layer = layerindex;
                            }
                            
                            Collider existingCol = newCube.GetComponent<Collider>();
                            if (existingCol != null && !(existingCol is MeshCollider))
                            {
                                Destroy(existingCol);
                            }
                            if (newCube.GetComponent<MeshCollider>() == null)
                            {
                                MeshCollider mc = newCube.AddComponent<MeshCollider>();
                                mc.convex = false; // non-convex is more accurate (ok for placement)
                            }
                        }*/
                        else
                        {
                            touch_count = 0;
                        }

                    }
                }

            }
            else if (Input.GetMouseButton(0) && !IsPointerOverUI())
            {
                timeSinceMouseDown += Time.deltaTime;
                if (timeSinceMouseDown >= clickTimeThreshold && touch_count >= 2)
                {

                    Debug.Log("Detect second touch and draging...");
                    float m_x = Input.GetAxis("Mouse X") * rotationSensit;

                    doSpin(m_x);

                    //touch_count = 0;
                }
                else if (timeSinceMouseDown >= clickTimeThreshold && touch_count == 1)
                {
                    Ray ray = camera2.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out var hit, Mathf.Infinity, spawnLayerMask))
                    {

                        MeshCollider mc = newCube.GetComponent<MeshCollider>();
                        if (mc == null) return;

                        Vector3 targetPos = new Vector3(hit.point.x, 0, hit.point.z);

                        Bounds bounds = mc.bounds;
                        Vector3 halfExtents = bounds.extents * 0.9f;

                        Vector3 checkPos = targetPos + (bounds.center - newCube.transform.position);

                        Collider[] overlaps = Physics.OverlapBox(
                            checkPos,
                            halfExtents,
                            newCube.transform.rotation,
                            ~0, // check against ALL layers (safer than spawnLayerMask)
                            QueryTriggerInteraction.Ignore
                        );

                        // Exclude itself
                        bool blocked = false;
                        foreach (var col in overlaps)
                        {
                            if (col.gameObject.name == "Floor") continue;
                            if (col.gameObject == newCube) continue;
                            if (col.gameObject != newCube)
                            {
                                blocked = true;
                                Debug.Log($"Blocked by: {col.gameObject.name}");
                                break;
                            }
                        }

                        if (!blocked)
                        {
                            newCube.transform.position = targetPos;
                            Debug.Log("Moved to " + targetPos);
                        }
                        else
                        {
                            Debug.Log("Blocked! Cannot move into other object.");
                        }

                    }

                }
            }
            else if (Input.GetMouseButtonUp(0) && !IsPointerOverUI())
            {

                //if (touch_count == 1)
                //{
                //SnapToFreeSpace(newCube);
                //}



            }
            //////////////////////////////////////////////////////////////////


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
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerindex) && !hit.collider.gameObject.name.Contains("Floor") && !hit.collider.gameObject.name.Contains("Window") && !hit.collider.gameObject.name.Contains("Door"))//
                    {
                        Debug.Log("Hit detected on: " + hit.collider.gameObject.name);
                        deletItem(hit.collider.gameObject);                                             // delete the item
                    }
                    else
                    {
                        Debug.Log("No surface hit for spawning. Check collider on target, layer, or camera setup.");
                    }
                }
                timeSinceMouseDown = 0;




            }
        }
    }


    void ToggleEditMode()
    {
        if (Camera1_G.activeSelf == false && Camera2_G.activeSelf == true)
        {
            editMode = !editMode;

        }

        Debug.Log("Edit mode: " + (editMode ? "ON" : "OFF"));
    }

    void GetCurrentPos()
    {
        current_pos = camera2.transform.position;
    }

    private bool IsPointerOverUI()
    {
        // Check if touch/mouse is over a UI element
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



    private void LoadItem()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("Save file is empty, initializing new SaveData.");
                saveData = new SaveData(); // Initialize with empty cubes list
                saveData.cubes = new List<CubeData>();
                return;
            }
            try
            {
                saveData = JsonUtility.FromJson<SaveData>(json);
                if (saveData == null || saveData.cubes == null)
                {
                    Debug.Log("Failed to deserialize save data, initializing new SaveData.");
                    saveData = new SaveData();
                    saveData.cubes = new List<CubeData>();
                }
                else
                {
                    foreach (CubeData cubeData in saveData.cubes)
                    {
                        // Use "ch1" as fallback if prefabName is missing (for old save data)
                        string prefabName = string.IsNullOrEmpty(cubeData.prefabName) ? "ch1" : cubeData.prefabName;
                        GameObject prefabToLoad = Resources.Load<GameObject>(prefabName);
                        float scale = cubeData.scale;
                        if (prefabToLoad == null)
                        {
                            Debug.LogError($"Prefab {prefabName} not found in Resources folder!");
                            continue;
                        }
                        GameObject newCube = Instantiate(prefabToLoad, cubeData.position, cubeData.rotation);
                        newCube.transform.localScale = Vector3.one * wallHeight * scale;
                        int layerindex = LayerMask.NameToLayer("CubeLayer");
                        if (layerindex != -1)
                        {
                            newCube.layer = layerindex;
                        }
                        spawnedCubes.Add(newCube);

                        Debug.Log($"Loaded cube at position: {cubeData.position} with prefab: {prefabName}");
                    }
                }

            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error deserializing save file: {e.Message}. Initializing new SaveData.");
                saveData = new SaveData();
                saveData.cubes = new List<CubeData>();
            }
        }
        else
        {
            Debug.Log("No save file found at: " + savePath);
            saveData = new SaveData();
            saveData.cubes = new List<CubeData>();
        }
        for (int i = 0; i < spawnedCubes.Count; i++)
        {
            Debug.Log(spawnedCubes[i].name);
        }
    }

    void SaveItem(GameObject item, float itemscale)
    {
        if (item != null && editMode)
        {
            spawnedCubes.Add(item);

            CubeData cubeData = new CubeData            // sub the information of the newly added item to a variable cubeData (type CubeData define as begining)
            {
                prefabName = saveName,
                position = item.transform.position,
                rotation = item.transform.rotation,
                scale = itemscale

            };
            saveData.cubes.Add(cubeData);               // add the information to the json
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"Saved cube at position: {cubeData.position}");
        }
        newCube = null;
        touch_count = 0;
        interactableDD = true;



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
        for (int i = 0; i < saveData.cubes.Count; i++)          // read the json file
        {
            if (Vector3.Distance(saveData.cubes[i].position, item.transform.position) < 0.01f)      //if a data's have a similar position recoded as the delete-target position 
            {
                saveData.cubes.RemoveAt(i);                     //remove the corresponding item in the savedata 
                break;
            }
        }
        spawnedCubes.Remove(item);

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);                      // rewrite the json file

        // Destroy the cube in the scene
        Destroy(item);
        touch_count = 0;

    }
}