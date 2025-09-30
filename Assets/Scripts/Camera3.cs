using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera3 : MonoBehaviour
{
    public Transform Cam3target;
    
    
    void OnEnable() {
        setPositionAfterLoaded();
    }

    public void setPositionAfterLoaded() {
        if (Cam3target == null)
        {
            Debug.LogWarning("[Camera2::TryInit] target is null (waiting for injection).");
            return; // µÈ´ý CanvasUiManager ×¢Èë
        }
        var renderer = Cam3target.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("[Camera3::TryInit] target has no Renderer in children.");
            return;
        }

        var bounds = renderer.bounds;
        Vector3 center = bounds.center;


        var initialPosition = new Vector3(center.x, 1, center.z);
        
       
        transform.position = initialPosition;
       
       
        
        
        
        Debug.Log($"[Camera3::TryInit] initialized.");
    } 
}
