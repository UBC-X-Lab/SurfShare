using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;

public class ExtrusionController : MonoBehaviour
{
    public List<int> topVerticesIndices = new List<int>();
    public Mesh myMesh;
    public Vector3 previousPosition;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //this.transform.parent.gameObject.GetComponent<NearInteractionGrabbable>().enabled = !Main.toggleExtrusion;
        //this.transform.parent.gameObject.GetComponent<ObjectManipulator>().enabled = !Main.toggleExtrusion;

        this.GetComponent<SphereCollider>().enabled = Main.toggleExtrusion;
        this.GetComponent<MeshRenderer>().enabled = Main.toggleExtrusion;
        this.GetComponent<NearInteractionGrabbable>().enabled = Main.toggleExtrusion;
        this.GetComponent<ObjectManipulator>().enabled = Main.toggleExtrusion;

        if (Main.toggleExtrusion)
        {
            Vector3 delta = this.transform.localPosition - previousPosition;

            if (delta.magnitude > 0)
            {
                // Debug.Log("LocalPosition:" + this.transform.localPosition);
                Vector3[] new_vertices = myMesh.vertices;
                foreach (int vertex_index in topVerticesIndices)
                {
                    new_vertices[vertex_index] += delta;
                }
                myMesh.vertices = new_vertices;
                this.transform.parent.gameObject.GetComponent<MeshCollider>().sharedMesh = myMesh;
            }

            //Vector3[] new_vertices = myMesh.vertices;
            //foreach (int vertex_index in topVerticesIndices)
            //{
            //    Debug.Log(vertex_index);
            //    new_vertices[vertex_index] += 0.01f * Vector3.up;
            //}
            //myMesh.vertices = new_vertices;
        }
        previousPosition = this.transform.localPosition;
    }

    //public static void UpdateMeshHeight(Mesh mesh, float height)
    //{
    //    Vector3[] new_vertices = mesh.vertices;
    //    for (int i = 0; i < mesh.vertices.Length; i++)
    //    {
    //        if (new_vertices[i].y != 0)
    //        {
    //            new_vertices[i].y = height;
    //        }
    //    }
    //    mesh.vertices = new_vertices;
    //}
}
