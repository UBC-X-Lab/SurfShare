using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    // Mesh Creation
    public GameObject RemoteObject;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SpawnMesh(Vector2[] poly_vertices, Vector3[] world_vertices, Vector3 heightNormal)
    {
        CmdSpawnMesh(poly_vertices, world_vertices, heightNormal, GetComponent<NetworkIdentity>());
    }

    [Command]
    void CmdSpawnMesh(Vector2[] poly_vertices, Vector3[] world_vertices, Vector3 heightNormal, NetworkIdentity netid)
    {
        GameObject meshObj = Instantiate(RemoteObject);
        NetworkServer.Spawn(meshObj, netid.connectionToClient);
        meshObj.GetComponent<UpdateMesh>().sync_poly_vertices.AddRange(poly_vertices);
        meshObj.GetComponent<UpdateMesh>().sync_world_vertices.AddRange(world_vertices);
        meshObj.GetComponent<UpdateMesh>().heightNormal = heightNormal;
        meshObj.GetComponent<UpdateMesh>().vertices_initialized = true;
    }
}
