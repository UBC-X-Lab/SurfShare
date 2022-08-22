using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;

public class ExtrusionController : MonoBehaviour
{
    public List<int> topVerticesIndices = new List<int>();
    public Mesh myMesh;
    public Vector3 previousLocalPostion;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.parent.gameObject.GetComponent<NearInteractionGrabbable>().enabled = !Main.toggleExtrusion;
        this.transform.parent.gameObject.GetComponent<ObjectManipulator>().enabled = !Main.toggleExtrusion;

        this.GetComponent<SphereCollider>().enabled = Main.toggleExtrusion;
        this.GetComponent<MeshRenderer>().enabled = Main.toggleExtrusion;
        this.GetComponent<NearInteractionGrabbable>().enabled = Main.toggleExtrusion;
        this.GetComponent<ObjectManipulator>().enabled = Main.toggleExtrusion;

        if (Main.toggleExtrusion)
        {
            Vector3 delta = this.transform.localPosition - previousLocalPostion;
            Debug.Log("LocalPosition:" + this.transform.localPosition + ", previous:" + previousLocalPostion);

            if (delta.magnitude > 0)
            {
                Vector3[] new_vertics = myMesh.vertices;
                foreach (int vertex_index in topVerticesIndices)
                {
                    new_vertics[vertex_index] += delta;
                }
                myMesh.vertices = new_vertics;
                previousLocalPostion = this.transform.localPosition;
            }
        }
    }

    public static void UpdateMeshHeight(Mesh mesh, float height)
    {
        Vector3[] new_vertices = mesh.vertices;
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            if (new_vertices[i].y != 0)
            {
                new_vertices[i].y = height;
            }
        }
        mesh.vertices = new_vertices;
    }
}
