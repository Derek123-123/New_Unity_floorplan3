using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Walkmode_button : MonoBehaviour
{

    //Cameras
    public GameObject Camera3;
    //public GameObject Camera2;
    //public GameObject Camera1;
    //public GameObject MainCamera;

    //UI items
    //public GameObject Button1;
    //public GameObject Button2;
    //public GameObject Button3;
    //public GameObject Button4;
    //public GameObject Button5;
    //public GameObject Dropdown;
    public GameObject Joystick;
    public GameObject HeightSlider;
    public GameObject AngleSlider;

    //private EditButton editButton;

    private float WallHeight;
    

    public FixedJoystick joyhandler;
    
    public bool Walkmode;
    public bool loaded;
    private Button button;

    private Rigidbody CAMR;
    private float walkSpeed = 1;
    public float rotationSensit = 5.0f;
    private float SpinDegree = 0f;


    private BoxCollider Camera3Collider;
    //private bool camera3blocked = false;
    //private Vector3? blockNormal = null;
    public float fronBlockDotThreshold = -0.1f;
    // Start is called before the first frame update

    void Awake()
    {
        button = gameObject.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("[Walkmode_button] No Button component found on this GameObject. Place this script on the UI Button object or wire up clicks manually.");
        }
        /*if (button != null)
        {
            button.onClick.RemoveListener(toggleWalkMode);
            button.onClick.AddListener(toggleWalkMode);
            button.interactable = false;
        }*/
        

    }
 
    void Start()
    {
        RefreshCamera3Rigidbody();
        if (joyhandler == null)
        {
            joyhandler = FindObjectOfType<FixedJoystick>(true);
            if (joyhandler == null)
                Debug.LogWarning("[Walkmode_button] FixedJoystick not found. Movement will not work.");
        }
        
        
           

    }
   
    // Update is called once per frame
    void Update()
    {
        if (!loaded)
        {
            if (Camera3 != null) Camera3.SetActive(false);
            if (Joystick) Joystick.SetActive(false);
            if (HeightSlider) HeightSlider.SetActive(false);
            if (AngleSlider) AngleSlider.SetActive(false);
            return;

        }
        if (Walkmode)
        {
            
            /*if (joyhandler != null)
            {
                if (CAMR == null) RefreshCamera3Rigidbody();
                if (CAMR != null)
                {
                    CAMR.velocity = new Vector3(joyhandler.Horizontal * walkSpeed, CAMR.velocity.y, joyhandler.Vertical * walkSpeed);
                }
            }*/

            if (Input.GetMouseButton(0) && !IsPointerOverUI() && joyhandler.Horizontal==0 && joyhandler.Vertical==0) {
                float m_x = Input.GetAxis("Mouse X");//* rotationSensit;
                doCAM3Spin(m_x);
            }

            Camera3.GetComponent<Camera>().fieldOfView = (1+AngleSlider.GetComponent<Slider>().value)*100;
        }
        else
        {
            
        }
        
        
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

    private void doCAM3Spin(float x) {
        if (Camera3 != null) {
            float rotationAmount = (x / Screen.width) * rotationSensit* Screen.width;
            SpinDegree -= rotationAmount;
            var rot = Quaternion.Euler(0, SpinDegree, 0);

            CAMR.MoveRotation(rot);
        }
    }

    void FixedUpdate() {
        if (!loaded || !Walkmode) return;
        if (CAMR == null) { RefreshCamera3Rigidbody(); if (CAMR == null) return; }

        float h = joyhandler.Horizontal;
        float v = joyhandler.Vertical;

        if (Camera3 != null) {
            Vector3 forwardFlat = Vector3.forward;
            Vector3 rightFlat = Vector3.right;

            Quaternion yRot = Quaternion.Euler(0, SpinDegree, 0);
            forwardFlat = yRot * Vector3.forward;
            rightFlat = yRot * Vector3.right;

            Vector3 moveDir = (rightFlat * h + forwardFlat * v);
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

            Vector3 desired = moveDir * walkSpeed * Time.fixedDeltaTime;
            desired.y = 0f;

            if (desired.sqrMagnitude > 0.000001f)
            {
                Vector3 origin = CAMR.position + Vector3.up * 0.5f;
                Vector3 dir = desired.normalized;
                float distance = desired.magnitude + 0.05f;

                float radius = 0.5f;
                if (Physics.SphereCast(origin, radius, dir, out RaycastHit hit, distance))
                {
                    if (Mathf.Abs(hit.normal.y) < 0.75f)
                    {
                        if (Vector3.Dot(desired, hit.normal) < 0f)
                        {
                            desired = Vector3.ProjectOnPlane(desired, hit.normal);
                        }
                    }
                    else { 
                    
                    }
                }
            }

            float targetY = CAMR.position.y;
            if (HeightSlider)
            {
                targetY = WallHeight * HeightSlider.GetComponent<Slider>().value;
            }

            Vector3 nextPos = CAMR.position + desired;
            nextPos.y = targetY;

            CAMR.MovePosition(nextPos);

        }

    }
    

    private void RefreshCamera3Rigidbody() {
        CAMR = null;
        if (Camera3 != null)
        {
            CAMR = Camera3.GetComponent<Rigidbody>();
            if (CAMR == null) Debug.Log("[Walkmode_button] Camera3 does not have a Rigidbody");
        }
        else {
            Debug.Log("[Walkmode_button] Camera3 Not found");
        }
    }

    public void findWallHeight() {
        GameObject Wall = GameObject.Find("Wall_0");
        if (Wall == null)
        {
            Debug.LogWarning("[EditButton] Wall_0 not found, wallHeight default=1");
            WallHeight = 2.5f;
        }
        else
        {
            var wallrenderer = Wall.GetComponent<MeshRenderer>();
            if (wallrenderer != null) WallHeight = wallrenderer.bounds.size.y;
            else { WallHeight = 2.5f; Debug.LogWarning("[EditButton] Wall_0 has no MeshRenderer, wallHeight default=2.5"); }
        }
    }
    
    /*void OnCollisionEnter(Collision collision)
    {
        if (collision.collider!= null && collision.collider != Camera3Collider) {
            camera3blocked = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.collider != null && collision.collider != Camera3Collider)
        {
            camera3blocked = false ;
        }
    }*/

}
