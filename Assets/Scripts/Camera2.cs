using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Camera2 : MonoBehaviour
{
    public Transform target;
    public EditButton editButton;
    private float radius = 10;
    private float last_x;
    private float last_z;
    private Vector3 look_target;
    private float sensitivity = 0.15f;

    public float floorHeight = 0;
    public float floorWidth = 0;

    private float initialDistance;
    private float currentDistance;
    private bool both_touch = false;



    void OnEnable()
    {

        Debug.Log($"[Camera2::OnEnable] target={(target ? target.name : "null")} editButton={(editButton ? editButton.name : "null")}");
        TryInit();
    }

    public void TryInit()
    {
        if (target == null)
        {
            Debug.LogWarning("[Camera2::TryInit] target is null (waiting for injection).");
            return; // µÈ´ý CanvasUiManager ×¢Èë
        }
        var renderer = target.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("[Camera2::TryInit] target has no Renderer in children.");
            return;
        }

        var bounds = renderer.bounds;
        floorWidth = bounds.size.x;
        floorHeight = bounds.size.y;

        look_target = bounds.center;
        var initialPosition = new Vector3(look_target.x, radius, look_target.z);
        transform.position = initialPosition;
        last_x = initialPosition.x;
        last_z = initialPosition.z;
        transform.LookAt(look_target);
        //transform.rotation =  Quaternion.Euler(0, 90, 0);
        Debug.Log($"[Camera2::TryInit] initialized. floorW={floorWidth} floorH={floorHeight} look={look_target}");
    }

    // Start is called before the first frame update
    /*void Start()
    {
        // Initialize Floor target
        GameObject targetObj = GameObject.Find("Floor");
        Invoke("GetRectSet", 0.01f);
        if (targetObj == null)
        {
            Debug.LogError("Floor GameObject not found! Please ensure a GameObject named 'Floor' exists in the scene.");
        }
        else
        {
            target = targetObj.transform;
        }

        // Try to find the EditButton GameObject
        if (editButton == null)
        {
            Debug.LogWarning("editButton is not assigned in the Inspector. Attempting to find EditButton GameObject...");

            // Try common GameObject names
            
           
            editButton = GameObject.Find("edit");
            if (editButton != null)
             {
                 Debug.Log($"Found GameObject named '{editButton.name}'");
                  
             }
         }
        

        // Validate the EditButton script
        if (editButton != null)
        {
            editButton_Script = editButton.GetComponent<EditButton>();
            if (editButton_Script == null)
            {
                Debug.LogError($"EditButton script not found on GameObject: {editButton.name}. Ensure the EditButton script is attached!");
            }
            else
            {
                Debug.Log($"EditButton script found on GameObject: {editButton.name}");
            }
        }
        else
        {
            Debug.LogError("editButton GameObject not found! Please assign the editButton field in the Inspector or ensure a GameObject with the EditButton script exists and is active.");
        }
        
        

        Renderer renderer = target.GetComponentInChildren<Renderer>();
        Bounds bounds = renderer.bounds;
        float target_width = bounds.size.x;
        float target_height = bounds.size.y;

        floorHeight = target_height;
        floorWidth = target_width;


    }*/

    /*void GetRectSet()
    {
        Renderer renderer = target.GetComponentInChildren<Renderer>();
        Bounds bounds = renderer.bounds;
        float target_width = bounds.size.x;
        float target_height = bounds.size.y;

        

        look_target = bounds.center;
        Vector3 initialPosition =  new Vector3(look_target.x,  radius,look_target.z); // Offset along Z-axis
        transform.position = initialPosition;
        last_x = initialPosition.x;
        last_z = initialPosition.z;

        transform.LookAt(look_target);
        
        
    }*/

    void Handle_input(float x, float z)
    {
        last_x -= x * sensitivity;
        last_z -= z * sensitivity;
        transform.position = new Vector3(last_x, radius, last_z);
    }

    /*void moveCamera(float x, float z) {
        
        transform.position = new Vector3(x, radius, z);

    }*/
    // Update is called once per frame
    void Update()
    {
        if (target == null || editButton == null) return;
        bool edit = editButton.editMode;
        bool move = editButton.MoveMode;
        if (Input.GetMouseButton(0) && !IsPointerOverUI() && both_touch == false)
        {
            if (!edit && !move)
            {
                float m_x = Input.GetAxis("Mouse X");
                float m_y = Input.GetAxis("Mouse Y");
                Handle_input(m_x, m_y);
            }
            else
            {
                float m_x = 0;
                float m_y = 0;
                Handle_input(m_x, m_y);
            }



        }
        else if (Input.mouseScrollDelta.y > 0 && !IsPointerOverUI())
        {
            if (radius > 1 && !edit)
            {
                radius--;
            }

            Handle_input(0, 0);
        }
        else if (Input.mouseScrollDelta.y < 0 && !edit && !IsPointerOverUI())
        {
            if (radius <= Mathf.Max(floorWidth, floorHeight) * 5 && !edit)
            {
                radius++;
            }

            Handle_input(0, 0);
        }

        if (Input.touchCount == 2 && !edit && !IsPointerOverUI())
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
                    if (radius <= Mathf.Max(floorWidth, floorHeight) * 5)
                    {
                        radius++;
                    }

                    Handle_input(0, 0);
                }

                initialDistance = currentDistance;

            }
            else if (touch1.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Ended)
            {
                Invoke("Delay", 0.2f);
            }

        }

    }
    private void Delay()
    {
        both_touch = false;
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
}
