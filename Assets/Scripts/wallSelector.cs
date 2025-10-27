using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;

public class wallSelector : MonoBehaviour
{
    public TMP_Dropdown wallselector;
    public string folderPath = "Assets/wallMaterial";

    public EditButton editButton;
    
    private bool loaded;
    private GameObject loadedObject;
    private int defaultIndex;
    //public int selectedIndex = -1;
    private string defaultWallSkin = "wall-default";
    // Start is called before the first frame update

    public bool Wallchanged = false;

    void Awake()
    {
        wallselector = GetComponent<TMP_Dropdown>();
        wallselector.ClearOptions();
        var WMaterial = Resources.LoadAll<Material>("wallMaterial");
        var WMaterialNames = new List<string>(WMaterial.Length);
        foreach (var WMN in WMaterial)
        {
            WMaterialNames.Add(WMN.name);
        }
        wallselector.AddOptions(WMaterialNames);
        wallselector.RefreshShownValue();
        wallselector.itemText.fontSize = 20;
        wallselector.onValueChanged.AddListener(changeWallMaterial);
        wallselector.onValueChanged.AddListener(changed);
        Wallchanged = false;
        defaultIndex = wallselector.options.FindIndex(option => option.text == defaultWallSkin);
        wallselector.value = defaultIndex;

        if (Wallchanged) Debug.Log("[Awake] Wallchanged is true");
        else Debug.Log("[Awake] Wallchange is false");





    }

    void Start()
    {
        Wallchanged = false;
        if (Wallchanged) Debug.Log("[Start] Wallchanged is true");
        else Debug.Log("[Start] Wallchange is false");

    }

    // Update is called once per frame
    void Update()
    {
        wallselector.interactable = editButton.editMode;
    }

    public void WMSetLoaded(bool isLoaded, GameObject Go)
    {
        loaded = isLoaded;
        if (!loaded)
        {
            wallselector.interactable = false ;
            
        }
        else
        {
            loadedObject = Go;
        }
    }

    public void changeWallMaterial(int index)
    {
        //selectedIndex = index;
        Debug.Log("go in changeWallMaterial");
        if (wallselector == null || index < 0 || index >= wallselector.options.Count) return;
        string selectedText = wallselector.options[index].text;
        Material Material_wall = Resources.Load<Material>("wallMaterial/" + selectedText);
        if (loaded && Material_wall != null)
        {

            var standard = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (standard == null)
            {
                Debug.LogError("Shader 'Standard' not found. Make sure you are using the Built-in Render Pipeline.");
                return;
            }

            if (Material_wall.shader != standard)
            {
                Material_wall.shader = standard;
            }

            Vector2 tiling = new Vector2(1f, 1f);
            Vector2 offset = Vector2.zero;

            Debug.Log("loaded and found Material_wall");
            GameObject Spawned_instance = loadedObject;
            if (Spawned_instance == null)
            {
                Debug.LogError("Cannot find Spawned_instance");

            }
            else
            {
                Debug.Log("Found Spawned_instance: " + loadedObject.name);
                foreach (Transform child in Spawned_instance.transform)
                {

                    if (child.name.Contains("Wall"))
                    {
                        if (child.GetComponent<MeshRenderer>() == null)
                        {
                            Debug.LogError("the child" + child.name + " doesnt have MeshRenderer");
                            continue;
                        }
                        else
                        {
                            
                            var rend = child.GetComponent<MeshRenderer>();
                            if (rend != null)
                            {
                                rend.material = Material_wall;
                                
                            }
                            Debug.Log("changed "+child.name +" Material!!");
                        }
                    }
                    else
                    {
                        Debug.Log("Found " + child.name);
                    }

                }


                editButton.Data.wallIndex = index;
                Debug.Log("Saved wallIndex: " + editButton.Data.wallIndex);
               



            }

            

        }
        else
        {
            return;
        }
    }

    public void changed(int index)
    {
        Wallchanged = true;
    }

    

    
}
