using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateMesh : MonoBehaviour
{
    public float height;
    public bool skew = false;

    private Vector3 previousHandlePos;
    // Start is called before the first frame update
    void Start()
    {
        previousHandlePos = transform.GetChild(0).position;
    }

    // Update is called once per frame
    void Update()
    {
        if (height != 0)
        {
            Vector3 delta = transform.GetChild(0).position - previousHandlePos;
            MeshCreator.ExtrudeMesh(GetComponent<MeshFilter>().mesh, delta);
            previousHandlePos = transform.GetChild(0).position;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.transform.name);
    }
}
