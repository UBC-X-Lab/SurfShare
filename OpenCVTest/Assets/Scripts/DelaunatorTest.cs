using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelaunatorTest : MonoBehaviour
{
    public GameObject baseObject;
    // Start is called before the first frame update
    void Start()
    {
        Vector2[] poly_vertices = new Vector2[4];
        poly_vertices[0] = new Vector2(0, 0);
        poly_vertices[1] = new Vector2(0, 1);
        poly_vertices[2] = new Vector2(1, 1);
        poly_vertices[3] = new Vector2(1, 0);

        GameObject newObject = Instantiate(baseObject);
        newObject.GetComponent<MeshFilter>().mesh = MeshCreator.CreateMesh(poly_vertices);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
