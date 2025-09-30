
using UnityEngine;

[DisallowMultipleComponent]
public class FloorScript : MonoBehaviour
{
    //[SerializeField] private Mesh customMesh;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log($"[Floorscript] {name} layer={gameObject.layer} hasCol={(GetComponent<Collider>() != null)}");
        if (GetComponent<Collider>() == null && GetComponent<MeshCollider>() == null)
        {
            var mc = gameObject.AddComponent<MeshCollider>();
            mc.convex = false;
        }



        // �O Layer������ "Floor"���˶������ "FloorLayer"
        int layerFloor = LayerMask.NameToLayer("FloorLayer");
        if (layerFloor != -1) gameObject.layer = layerFloor;
        else
        {
            Debug.LogWarning("[Floorscript] Layer 'FloorLayer' δ������Ո�� Project Settings > Tags and Layers ����ԓ�ӡ�");
        }

       

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
