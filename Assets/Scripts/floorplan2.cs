using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class floorplan2 : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
        //GameObject item = this.gameObject;
        //if ((item.name.Contains("Wall_") || item.name.Contains("Window_")) && item.GetComponent<BoxCollider>() == null) {
        //    BoxCollider Collider = item.AddComponent<BoxCollider>();
            
        //}
       
        transform.localPosition = Vector3.zero;
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            renderer = GetComponentInChildren<MeshRenderer>();
            if (renderer == null)
            {
                Debug.LogError("MeshRenderer not found on " + gameObject.name);
                return;
            }
        }
        Debug.Log("Model loaded: " + renderer.gameObject.name);


        if (gameObject.name == "Floor")
        {
            Debug.Log($"[Floorscript] {name} layer={gameObject.layer} hasCol={(GetComponent<Collider>() != null)}");
            if (GetComponent<Collider>() == null && GetComponent<BoxCollider>() == null)
            {
                //var mc = gameObject.AddComponent<BoxCollider>();
                //mc.convex = false;
            }



            // 設 Layer：優先 "Floor"，退而求其次 "FloorLayer"
            int layerFloor = LayerMask.NameToLayer("FloorLayer");
            if (layerFloor != -1) gameObject.layer = layerFloor;
            else
            {
                Debug.LogWarning("[Floorscript] Layer 'FloorLayer' 未建立，請在 Project Settings > Tags and Layers 建立該層。");
            }
        }
        else if (gameObject.name.Contains("Wall") || gameObject.name.Contains("Window")) {
            if (GetComponent<Collider>() == null && GetComponent<BoxCollider>() == null)
            {
                var mc = gameObject.AddComponent<BoxCollider>();
               
            }

        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
