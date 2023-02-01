using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerControl : NetworkBehaviour
{
    //SyncList<Vector2> spawn_vertices_sync = new SyncList<Vector2>();
    //public List<Vector2[]> spawn_vertices = new List<Vector2[]>();

    // Mesh Creation
    public GameObject MeshPrefab;

    // test
    public GameObject Prefab;
    public bool Spawn = false;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Spawn)
        {
            Spawn = false;
            CmdSpawnObject();
        }

        // Debug.Log(hasAuthority);
    }


    [Command]
    public void CmdSpawnMesh(Vector2[] vertices, int[] vertices_count, bool stayKinematic)
    {
        GameObject meshObj = Instantiate(MeshPrefab);
        NetworkServer.Spawn(meshObj);
        meshObj.GetComponent<UpdateMesh>().stayKinematic = stayKinematic;
        meshObj.GetComponent<UpdateMesh>().vertices.AddRange(vertices);
        meshObj.GetComponent<UpdateMesh>().vertices_count.AddRange(vertices_count);
        meshObj.GetComponent<UpdateMesh>().vertices_initialized = true;
    }

    [Command]
    void CmdSpawnObject()
    {
        GameObject obj = Instantiate(Prefab);
        NetworkServer.Spawn(obj);
    }
}

