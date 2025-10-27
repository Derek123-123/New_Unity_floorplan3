using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class Switch_camera_button : MonoBehaviour
{

    public GameObject Camera1;
    public GameObject Camera2;
    public GameObject Camera3;
    public GameObject MainCamera;
    
    public Button button;

    
    public EditButton editButton;
    
    void OnEnable()
    {
        // 不做 Find，不主動綁定事件
        // 僅保證初始狀態
        Debug.Log($"[SwitchCam::OnEnable] C1={(Camera1 ? Camera1.name : "null")} C2={(Camera2 ? Camera2.name : "null")} C3={(Camera3 ? Camera3.name : "null")} btn={(button ? button.name : "null")} edit={(editButton ? editButton.name : "null")}");
        if (Camera1 != null && Camera2 != null)
        {
            Camera2.SetActive(false);
            Camera1.SetActive(true);
            MainCamera.SetActive(false);
            Camera3.SetActive(false);
        }
    }



    // Update is called once per frame
    void Update()
    {
        
    }

    public void switchCamera() {
        Debug.Log($"[SwitchCam::switchCamera] C1={(Camera1 ? Camera1.name : "null")} C2={(Camera2 ? Camera2.name : "null")}");
        if (Camera1 == null || Camera2 == null)
        {
            Debug.LogWarning("[SwitchCam] switchCamera called but C1 or C2 is null");
            return;
        }
        if (Camera1.activeSelf )
        {
            //
            Camera2.SetActive(true);
            Camera1.SetActive(false);
            MainCamera.SetActive(false);
            Camera3.SetActive(false);
            if (editButton != null) editButton.SyncUiByModes();

        }
        else if (Camera2.activeSelf) {
            //
            Camera2.SetActive(false);
            Camera1.SetActive(true);
            MainCamera.SetActive(false);
            Camera3.SetActive(false);
          
            editButton.SyncUiByModes();
        }
        Debug.Log($"[SwitchCam] After toggle: C1={(Camera1 ? Camera1.activeSelf : false)} C2={(Camera2 ? Camera2.activeSelf : false)}");


    }
}
