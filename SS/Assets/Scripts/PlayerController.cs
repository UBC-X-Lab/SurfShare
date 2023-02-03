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

    private GameObject RemoteVideoPlayer;

    private GameObject RemoteAudio;

    [SyncVar]
    public bool headInitialized = false;

    private Transform PeerWorldOrigin;

    // Start is called before the first frame update
    void Start()
    {
        Head = transform.GetChild(0).gameObject;
        PeerWorldOrigin = GameObject.Find("PeerWorldOrigin").transform;
        RemoteVideoPlayer = GameObject.Find("RemoteVideoPlayer");
        RemoteAudio = GameObject.Find("RemoteAudio");
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasAuthority && RemoteSpaceControl.remoteSpaceSetupCompleted && !headInitialized)
        {
            Debug.Log("Peer Head initialized!");
            RemoteVideoPlayer.GetComponent<BoxCollider>().enabled = false;
            CmdSetHeadInitilized();
            Head.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().enabled = true;
            transform.position = PeerWorldOrigin.position;
            transform.rotation = PeerWorldOrigin.rotation;
        }

        // continue to track peer world origin
        if (!hasAuthority && headInitialized)
        {
            if (Head.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().enabled != Main.head_on)
            {
                Head.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().enabled = Main.head_on;
            }

            if (RemoteAudio.GetComponent<AudioSource>().spatialize != Main.head_on)
            {
                RemoteAudio.GetComponent<AudioSource>().spatialize = Main.head_on;
            }

            if (!transform.position.Equals(PeerWorldOrigin.position))
            {
                transform.position = PeerWorldOrigin.position;
            }

            if (!transform.rotation.Equals(PeerWorldOrigin.rotation))
            {
                transform.rotation = PeerWorldOrigin.rotation;
            }

            if (Main.head_on)
            {
                RemoteAudio.transform.position = Head.transform.position;
                RemoteAudio.transform.rotation = Head.transform.rotation;
            }
        }

        if (hasAuthority && headInitialized)
        {
            // Debug.Log("X:" + Camera.main.transform.position.x + ", Y:" + Camera.main.transform.position.y + ", Z:" + Camera.main.transform.position.z);
            Head.transform.position = Camera.main.transform.position;
            Head.transform.rotation = Camera.main.transform.rotation;
        }
    }

    public void SpawnMesh(Vector2[] poly_vertices, int[] vertices_count, Vector3[] world_vertices, Vector3 heightNormal, Color mesh_color, bool KinematicCreation)
    {
        CmdSpawnMesh(poly_vertices, vertices_count, world_vertices, heightNormal, mesh_color, KinematicCreation, GetComponent<NetworkIdentity>());
    }

    [Command (requiresAuthority = false)]
    void CmdSetHeadInitilized()
    {
        headInitialized = true;
    }

    [Command]
    void CmdSpawnMesh(Vector2[] poly_vertices, int[] vertices_count, Vector3[] world_vertices, Vector3 heightNormal, Color mesh_color, bool KinematicCreation, NetworkIdentity netid)
    {
        GameObject meshObj = Instantiate(RemoteObject);
        NetworkServer.Spawn(meshObj, netid.connectionToClient);
        meshObj.GetComponent<UpdateMesh>().sync_poly_vertices.AddRange(poly_vertices);
        meshObj.GetComponent<UpdateMesh>().sync_world_vertices.AddRange(world_vertices);
        meshObj.GetComponent<UpdateMesh>().sync_vertices_count.AddRange(vertices_count);
        meshObj.GetComponent<UpdateMesh>().heightNormal = heightNormal;
        meshObj.GetComponent<UpdateMesh>().mesh_color = mesh_color;
        meshObj.GetComponent<UpdateMesh>().has_rigidbody = !KinematicCreation;
        meshObj.GetComponent<UpdateMesh>().vertices_initialized = true;

        if (KinematicCreation)
        {
            Debug.Log("This object has no rigidbody");
        }
    }
}
