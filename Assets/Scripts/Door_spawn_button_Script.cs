using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class button_Script : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject newDoor;
    public Button button;
    public bool clicked = false;



    void Start()
    {
        button = GameObject.Find("Button").GetComponent<Button>();
        if (newDoor == null)
        {
            newDoor = GameObject.Find("New_door_2");
            if (newDoor == null)
            {
                Debug.LogError("NewDoor GameObject not found in scene! Please assign in Inspector or ensure 'NewDoor' exists and is active.");
                return;
            }
        }

        button.onClick.AddListener(dothing);
    }

    void dothing()
    {
        if (!clicked)
        {
            MeshFilter newMeshFilter = newDoor.GetComponent<MeshFilter>(); //initial Mesh Filter and renderer
            MeshRenderer newMeshRenderer = newDoor.GetComponent<MeshRenderer>();
            if (newMeshFilter == null && newMeshRenderer == null)
            {
                Debug.LogError("No new Mesh Filter and Renderer!");
                return;
            }
            else if (newMeshRenderer == null)
            {
                Debug.LogError("No new Mesh  Renderer!");
                return;
            }
            else if (newMeshFilter == null)
            {
                Debug.LogError("No new Mesh Filter!");
                return;
            }
            Mesh newMesh = newMeshFilter.sharedMesh;
            Material newMaterial = newMeshRenderer.sharedMaterial;

            Vector3 newDoor_LongEdgeNormal = GetLEN(newMesh, newMeshRenderer.transform);

            MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
            foreach (MeshFilter meshFilter in meshFilters)
            {
                GameObject door = meshFilter.gameObject;
                if (door.name.StartsWith("Door_"))              //finding door
                {
                    MeshRenderer meshRenderer = door.GetComponent<MeshRenderer>();

                    //Vector3 originalPosition = door.transform.position;

                    Vector3 originalCenter = meshRenderer.bounds.center;

                    //Get original door long edge normal
                    Mesh originalMesh = meshFilter.sharedMesh;

                    Vector3 originalDoor_LongEdgeNormal = GetLEN(originalMesh, meshRenderer.transform);








                    //update door shape and material
                    meshFilter.sharedMesh = newMesh;
                    meshRenderer.sharedMaterial = newMaterial;

                    meshRenderer = door.GetComponent<MeshRenderer>();




                    Quaternion rotation = Quaternion.FromToRotation(newDoor_LongEdgeNormal, originalDoor_LongEdgeNormal);
                    door.transform.rotation = Quaternion.identity;
                    door.transform.rotation = rotation;
                    Vector3 newCenter = meshRenderer.bounds.center;
                    Vector3 offSet = originalCenter - newCenter;        //calculate the offset (all the components are assigned with position(0,0,0))
                    door.transform.position += offSet;

                    Debug.Log(door.name + " Position: " + door.transform.position);


                }
            }

            clicked = true;
        }

    }

    void UpdateDoor(GameObject Obj, Mesh newMesh, Material newMaterial)
    {
        MeshFilter meshFilter = Obj.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = Obj.GetComponent<MeshRenderer>();


    }

    Vector3 GetLEN(Mesh mesh, Transform transform)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3[] normals = mesh.normals;

        Dictionary<int, (Vector3[] verts, float z, int[] indices)> faceData = new Dictionary<int, (Vector3[], float, int[])>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int t0 = triangles[i];
            int t1 = triangles[i + 1];
            int t2 = triangles[i + 2];
            Vector3 v0 = transform.TransformPoint(vertices[t0]);
            Vector3 v1 = transform.TransformPoint(vertices[t1]);
            Vector3 v2 = transform.TransformPoint(vertices[t2]);
            float avgZ = (v0.z + v1.z + v2.z) / 3f;
            faceData[i / 3] = (new[] { v0, v1, v2 }, avgZ, new[] { t0, t1, t2 });
        }

        var topFace = faceData.OrderByDescending(f => f.Value.z).First();
        Vector3[] faceVerts = topFace.Value.verts;
        int[] faceIndices = topFace.Value.indices;
        Vector3[] edges = new Vector3[]
        {
            faceVerts[1] - faceVerts[0],
            faceVerts[2] - faceVerts[1],
            faceVerts[0] - faceVerts[2]
        };

        var edgeData = edges.Select((e, i) => new { Length = e.magnitude, index = i, Edge = e }).OrderByDescending(e => e.Length).First();
        Vector3 longestEdge = edgeData.Edge.normalized;
        Vector3 faceNormal = Vector3.Cross(edges[0], edges[1]).normalized;
        Vector3 edgeNormal = Vector3.Cross(longestEdge, faceNormal).normalized;

        edgeNormal = Filter(edgeNormal);


        return edgeNormal;
    }

    Vector3 Filter(Vector3 edgeNormal)
    {
        if ((edgeNormal.x >= 0.9 || edgeNormal.x <= -0.9) && (edgeNormal.z <= 0.1 || (edgeNormal.z >= -0.1 && edgeNormal.z < 0)))
        {
            edgeNormal.x = 1;
            edgeNormal.z = 0;
        }
        else if ((edgeNormal.z >= 0.9 || edgeNormal.z <= -0.9) && (edgeNormal.x <= 0.1 || (edgeNormal.x >= -0.1 && edgeNormal.x < 0)))
        {
            edgeNormal.z = 1;
            edgeNormal.x = 0;
        }

        edgeNormal.y = 0;
        return edgeNormal;
    }
    // Update is called once per frame
    void Update()
    {

    }
}
