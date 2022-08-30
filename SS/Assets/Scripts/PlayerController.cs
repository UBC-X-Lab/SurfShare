using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    // Mesh Creation
    public GameObject RemoteObject;

    // Head
    private GameObject Head;

    [SyncVar]
    public bool headInitialized = false;

    private Transform PeerWorldOrigin;

    // Start is called before the first frame update
    void Start()
    {
        Head = transform.GetChild(0).gameObject;
        PeerWorldOrigin = GameObject.Find("PeerWorldOrigin").transform;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasAuthority && RemoteSpaceControl.remoteSpaceSetupCompleted && !headInitialized)
        {
            Debug.Log("Peer Head initialized!");
            CmdSetHeadInitilized();
            Head.GetComponent<MeshRenderer>().enabled = true;
            transform.position = PeerWorldOrigin.position;
            transform.rotation = PeerWorldOrigin.rotation;
        }

        // continue to track peer world origin
        if (!hasAuthority && headInitialized)
        {
            if (!transform.position.Equals(PeerWorldOrigin.position))
            {
                transform.position = PeerWorldOrigin.position;
            }

            if (!transform.rotation.Equals(PeerWorldOrigin.rotation))
            {
                transform.rotation = PeerWorldOrigin.rotation;
            }
        }

        if (hasAuthority && headInitialized)
        {
            // Debug.Log("X:" + Camera.main.transform.position.x + ", Y:" + Camera.main.transform.position.y + ", Z:" + Camera.main.transform.position.z);
            Head.transform.position = Camera.main.transform.position;
            Head.transform.rotation = Camera.main.transform.rotation;
        }
    }

    public void SpawnMesh(Vector2[] poly_vertices, Vector3[] world_vertices, Vector3 heightNormal, bool KinematicCreation)
    {
        CmdSpawnMesh(poly_vertices, world_vertices, heightNormal, KinematicCreation, GetComponent<NetworkIdentity>());
    }

    [Command]
    void CmdSetHeadInitilized()
    {
        headInitialized = true;
    }

    [Command]
    void CmdSpawnMesh(Vector2[] poly_vertices, Vector3[] world_vertices, Vector3 heightNormal, bool KinematicCreation, NetworkIdentity netid)
    {
        GameObject meshObj = Instantiate(RemoteObject);
        NetworkServer.Spawn(meshObj, netid.connectionToClient);
        meshObj.GetComponent<UpdateMesh>().sync_poly_vertices.AddRange(poly_vertices);
        meshObj.GetComponent<UpdateMesh>().sync_world_vertices.AddRange(world_vertices);
        meshObj.GetComponent<UpdateMesh>().heightNormal = heightNormal;
        meshObj.GetComponent<UpdateMesh>().stay_kinematic = KinematicCreation;
        meshObj.GetComponent<UpdateMesh>().vertices_initialized = true;

        if (KinematicCreation)
        {
            Debug.Log("This object stays kinematic");
        }
    }
}
