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

    private bool loaded;
    private GameObject loadedObject;
    // Start is called before the first frame update

    void Start()
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

        wallselector.onValueChanged.AddListener(changeWallMaterial);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void WMSetLoaded(bool isLoaded, GameObject Go)
    {
        loaded = isLoaded;
        if (wallselector != null)
        {
            wallselector.interactable = isLoaded;
        }
        if (!loaded)
        {
            wallselector.interactable = isLoaded;
        }
        else
        {
            loadedObject = Go;
        }
    }

    public void changeWallMaterial(int index)
    {
        Debug.Log("go in changeWallMaterial");
        if (wallselector == null || index < 0 || index >= wallselector.options.Count) return;
        string selectedText = wallselector.options[index].text;
        Material Material_wall = Resources.Load<Material>("wallMaterial/" + selectedText);
        if (loaded && Material_wall != null)
        {

            Shader standard = Shader.Find("Standard");
            var debugMat = new Material(standard);
            debugMat.color = Color.magenta; // flat color
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
                                /*var mats = rend.sharedMaterials; // use shared to avoid instancing explosion
                                for (int i = 0; i < mats.Length; i++)
                                {
                                    mats[i] = Material_wall;
                                }
                                   
                                rend.sharedMaterials = mats;
                                SetTilingPerRenderer(rend, tiling, offset);*/
                            }
                            Debug.Log("changed "+child.name +" Material!!");
                        }
                    }
                    else
                    {
                        Debug.Log("Found " + child.name);
                    }

                }
            }

            GameObject CubeTest = GameObject.Find("Cube");
            if (CubeTest != null)
            {
                var rendC = CubeTest.GetComponent<MeshRenderer>();
                if (rendC != null)
                {
                    
                    rendC.material = debugMat;

                    // 这里调用，保证 Cube 的贴图缩放与墙体一致
                    //SetTilingPerRenderer(rendC, tiling, offset);
                }

            }

        }
        else
        {
            return;
        }
    }

    void SetTilingPerRenderer(MeshRenderer rend, Vector2 tiling, Vector2 offset)
    {
        if (rend == null) return;
        var mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);

        var mat = rend.sharedMaterial;
        if (mat != null)
        {
            // Built-in Standard
            if (mat.HasProperty("_MainTex"))
            {
                mpb.SetVector("_MainTex_ST", new Vector4(tiling.x, tiling.y, offset.x, offset.y));
            }
            // URP Lit
            if (mat.HasProperty("_BaseMap"))
            {
                mpb.SetVector("_BaseMap_ST", new Vector4(tiling.x, tiling.y, offset.x, offset.y));
            }
        }

        rend.SetPropertyBlock(mpb);
    }
}
