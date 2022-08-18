using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateMesh : MonoBehaviour
{
    public float height = 0.01f;
    public bool skew = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (height != 0)
        {
            MeshCreator.UpdateMeshHeight(GetComponent<MeshFilter>().mesh, height);
        }

        if (skew)
        {
            MeshCreator.SkewMesh(GetComponent<MeshFilter>().mesh);
        }
    }
}
