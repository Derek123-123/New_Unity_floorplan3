using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class camera_drag : MonoBehaviour
{
    // Start is called before the first frame update
    //private bool mouse_f_p = false;
    //private float O_mouseX;
    //private float moved_x = 0;
    //private Vector3 targetPosition = Vector3.zero;
    public Transform target;
    public float rotationSensit = 5.0f;
    public float radius = 50;
    public float minPitch = 0f;
    public float maxPitch = 80.0f;
    public float floorHeight = 0;
    public float floorWidth = 0;

    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private Vector3 look_target;

    private float initialDistance;
    private float currentDistance;

    private bool both_touch = false;
    private bool initialized = false;
    void Start()
    {


        //target = GameObject.Find("Floor").transform;
        //distance = 10.0f; // Default distance;
        //Camera.main.backgroundColor = new Color(13,79,92,0.5f);
        
        //Update();
    }

    public void SetTarget(Transform floor)
    {
        target = floor;
        initialized = false; 
    }

    void TryInitIfReady()
    {
        if (initialized) return;
        if (target == null) return;

        var renderer = target.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            
            return;
        }

        Bounds bounds = renderer.bounds;
        floorWidth = bounds.size.x;
        floorHeight = bounds.size.y;

        look_target = bounds.center;

        // �O����ʼλ���c�Ƕ�
        Vector3 initialPosition = look_target + new Vector3(0, 0, radius);
        transform.position = initialPosition;
        transform.LookAt(look_target);

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        UpdateCameraPosition();

        initialized = true;
        Debug.Log("[camera_drag] initialized with target=" + target.name + $" bounds=({floorWidth},{floorHeight})");
    }
     void Update() {
        if (target == null)
        {
            return;
        }
        if (!initialized)
        {
            TryInitIfReady();
            if (!initialized) return;
        }
        //Vector3 rotationAngles = transform.eulerAngles;
        if (Input.GetMouseButton(0) && !IsPointerOverUI() && Input.touchCount < 2 && both_touch ==false)  //&& Input.TouchPhase!=began
        {

            float m_x = Input.GetAxis("Mouse X") * rotationSensit;
            float m_y = Input.GetAxis("Mouse Y") * rotationSensit;
            Handle_input(m_x, m_y);

        }
        else if (Input.mouseScrollDelta.y>0 && !IsPointerOverUI()) {
            if (radius > 1)
            {
                radius--;
            }
                
            Handle_input(0, 0);
        }
        else if (Input.mouseScrollDelta.y < 0 && !IsPointerOverUI())
        {
            if (radius <= Mathf.Max(floorWidth,floorHeight)*5) { 
                radius++;
            }
                
            Handle_input(0, 0);
        }
        if (Input.touchCount == 2)
        {
            both_touch = true;
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);
            if (touch1.phase == TouchPhase.Began && touch2.phase == TouchPhase.Began)
            {
                initialDistance = Vector2.Distance(touch1.position, touch2.position);
            }
            else if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
            {
                currentDistance = Vector2.Distance(touch1.position, touch2.position);
                if (currentDistance > initialDistance)
                {
                    if (radius > 1)
                    {
                        radius--;
                    }
                    Handle_input(0, 0);
                }
                else if (currentDistance < initialDistance)
                {
                    if (radius <= Mathf.Max(floorWidth, floorHeight) * 5) {
                        radius++;
                    }
                       
                    Handle_input(0, 0);
                }

                initialDistance = currentDistance;

            }
            else if (touch1.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Ended) {
                Invoke("Delay", 0.2f);
            }
        }
        
        

    }

    private void Delay() {
        both_touch = false;
        } 

    private void Handle_input(float x, float y) {
        yaw += x;
        pitch -= y;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        UpdateCameraPosition();
    }
    void UpdateCameraPosition() {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 direction = new Vector3(0, 0, -radius);
        Vector3 position = look_target  + rotation * direction;

        transform.position = position;
        transform.LookAt(look_target);
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


    /*void GetRectSet() {
        Renderer renderer = target.GetComponentInChildren<Renderer>();
        Bounds bounds = renderer.bounds;
        float target_width = bounds.size.x;
        float target_height = bounds.size.y;

        floorHeight = target_height;
        floorWidth = target_width;

        look_target = bounds.center;
        Vector3 initialPosition = look_target + new Vector3(0, 0, radius); // Offset along Z-axis
        transform.position = initialPosition;

        transform.LookAt(look_target);
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        UpdateCameraPosition();
    }*/
    // Update is called once per frame
    
}
