using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class reSetPos : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //string assetPath = "Assets/floorplan_2_blender.blend";
        //var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        //importer.isReadable = true;
        transform.position = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
   
}
