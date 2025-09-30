using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MODESWITCH : MonoBehaviour
{
    public GameObject WM;
    public GameObject EM;
    private Button WMButton;
    private Button EMButton;


    public GameObject Camera3;
    public GameObject Camera2;
    public GameObject Camera1;
    public GameObject MainCamera;

    //UI items
    public GameObject Button1;
    public GameObject Button2;
    public GameObject Button3;
    public GameObject Button4;
    public GameObject Button5;
    public GameObject Dropdown;
    public GameObject Joystick;
    public GameObject HeightSlider;
    public GameObject AngleSlider;

    [SerializeField] private EditButton editButton;
    [SerializeField] private Walkmode_button walkButton;
    private BoxCollider Camera3Collider;


    private bool loaded = false;
    public string mode = "EM";
    // Start is called before the first frame update

    void Awake()
    {
        if (WM!=null) WMButton = WM.GetComponent<Button>();
        if (EM!=null) EMButton = EM.GetComponent<Button>();
        if (WMButton!=null)
        { 
            WMButton.onClick.RemoveAllListeners();
            WMButton.onClick.AddListener(SetWM);
        }

        if (EMButton != null)
        {
            EMButton.onClick.RemoveAllListeners();
            EMButton.onClick.AddListener(SetEM);
        }
    }

    void Start()
    {
        if (WMButton != null) WMButton.interactable = loaded;
        if (EMButton != null) EMButton.interactable = loaded;
    }

    // Update is called once per frame
    void Update()
        {
        
        }

    private void SetWM() 
    {
        mode = "WM";
        if (!loaded)
        {
            Debug.LogWarning("[MODESWITCH] SetWM ignored: not loaded yet");
            return;
        }
        if (walkButton != null) walkButton.Walkmode = true;
        if (editButton != null)
        {
            editButton.isEdit = false;
            editButton.editMode = false;
            editButton.DeleteMode = false;
            editButton.SyncUiByModes();
        }

        SetCamerasForWM();
        SetUIForWM();

        Debug.Log($"[MODESWITCH] WM -> C1={(Camera1 && Camera1.activeSelf)} C2={(Camera2 && Camera2.activeSelf)} C3={(Camera3 && Camera3.activeSelf)} " +
                  $"Main={(MainCamera && MainCamera.activeSelf)} isEdit={editButton?.isEdit} Walk={walkButton?.Walkmode}");


    }

    private void SetEM()
    {
        mode = "EM";
        if (!loaded)
        {
            Debug.LogWarning("[MODESWITCH] SetWM ignored: not loaded yet");
            return;
        }

        if (walkButton != null) walkButton.Walkmode = false;
        if (editButton != null)
        {
            editButton.isEdit = true;
            editButton.editMode = false;
            editButton.DeleteMode = false;
            editButton.SyncUiByModes();
        }

        SetCamerasForEM();
        SetUIForEM();
        Debug.Log($"[MODESWITCH] EM -> C1={(Camera1 && Camera1.activeSelf)} C2={(Camera2 && Camera2.activeSelf)} C3={(Camera3 && Camera3.activeSelf)} " +
          $"Main={(MainCamera && MainCamera.activeSelf)} isEdit={editButton?.isEdit} Walk={walkButton?.Walkmode}");
    }

    public string GetMode() {
        Debug.Log("[MODESWITCH] return mode:" + mode);
        return mode;
    }

    public void JsSetWM() {
        SetWM();
        Debug.Log("[MODESWITCH] Switched to Walk mode");
    }

    public void JsSetEM()
    {
        SetEM();
        Debug.Log("[MODESWITCH] Switched to Edit mode");
    }






    private void SetCamerasForWM()
    {
        
        if (Camera2) Camera2.SetActive(false);
        if (Camera1) Camera1.SetActive(false);
        if (MainCamera) MainCamera.SetActive(false);
        if (Camera3) Camera3.SetActive(true);
        
    }

    private void SetCamerasForEM()
    {

        if (Camera2) Camera2.SetActive(false);
        if (Camera1) Camera1.SetActive(true);
        if (MainCamera) MainCamera.SetActive(false);
        if (Camera3) Camera3.SetActive(false);

    }

    private void SetUIForWM()
    {
        
        SetActiveIf(Button1, false);
        SetActiveIf(Button2, false);
        SetActiveIf(Button3, false);
        SetActiveIf(Button4, false);
        SetActiveIf(Button5, false);
        SetActiveIf(Dropdown, false);
        
        SetActiveIf(Joystick, true);
        SetActiveIf(HeightSlider, true);
        SetActiveIf(AngleSlider, true);
    }

    private void SetUIForEM()
    {

        SetActiveIf(Button1, true);
        SetActiveIf(Button2, true);
        SetActiveIf(Button3, true);
        SetActiveIf(Button4, true);
        SetActiveIf(Button5, true);
        SetActiveIf(Dropdown, true);

        SetActiveIf(Joystick, false);
        SetActiveIf(HeightSlider, false);
        SetActiveIf(AngleSlider, false);
    }

    private void SetActiveIf(GameObject go, bool on)
    {
        if (go == null) return;
        if (go.activeSelf != on) go.SetActive(on);
    }

    public void SetLoaded(bool isLoaded, GameObject cam1 = null)
    {
        loaded = isLoaded;
        if (WMButton != null)
        {
            WMButton.interactable = isLoaded;
        }
        if (EMButton != null)
        {
            EMButton.interactable = isLoaded;
        }

        if (walkButton == null) walkButton = FindObjectOfType<Walkmode_button>(true);
        if (walkButton != null) walkButton.loaded = isLoaded;

        if (isLoaded )
        {
            if (cam1 != null) Camera1 = cam1;
            if (walkButton != null)
            {
                walkButton.findWallHeight();
                if (HeightSlider != null)
                {
                    var s = HeightSlider.GetComponent<Slider>();
                    if (s != null) s.value = 0.5f;
                }
            }

            if (Camera3 != null) Camera3Collider = Camera3.GetComponent<BoxCollider>();

            // 預設進入 Edit 或 Walk，可依需要改
            // 這裡我們依現有 mode 決定
            if (mode == "WM") SetWM();
            else SetEM();
        }
        else
        {
            if (walkButton != null) walkButton.Walkmode = false;
            if (Camera3) Camera3.SetActive(false);
        }
        Debug.Log("[Walkmode_button] SetLoaded -> " + isLoaded + ", Camera1=" + (Camera1 ? Camera1.name : "null"));
    }


}
