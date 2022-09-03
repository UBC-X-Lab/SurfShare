using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Mirror;

public class UpdateMesh : NetworkBehaviour
{
    public List<int> topVerticesIndices = new List<int>();
    public Mesh myMesh;
    public Vector3 handlePreviousPosition;

    private GameObject BaseMesh;
    private GameObject Handle;

    private Transform PeerWorldOrigin;

    // network
    public readonly SyncList<Vector2> sync_poly_vertices = new SyncList<Vector2>();
    public readonly SyncList<Vector3> sync_world_vertices = new SyncList<Vector3>();

    [SyncVar]
    public Color mesh_color = new Color(1, 1, 1);

    [SyncVar]
    public Vector3 heightNormal;

    [SyncVar]
    public bool vertices_initialized = false;

    [SyncVar]
    public bool manipulating = false;

    public bool canUpdateMesh = false; // have we initilized, can we update?

    [SyncVar]
    public bool stay_kinematic = false;

    [SyncVar]
    public bool isKinematic = true;

    [SyncVar]
    public bool useGravity = true;

    private bool isOwner;


    void Start()
    {
        BaseMesh = transform.GetChild(0).gameObject;
        Handle = BaseMesh.transform.GetChild(0).gameObject;
        PeerWorldOrigin = GameObject.Find("PeerWorldOrigin").transform;
        // Debug.Log("Lalala");
    }

    void Update()
    {
        Handle.GetComponent<SphereCollider>().enabled = Main.toggleExtrusion;
        Handle.GetComponent<MeshRenderer>().enabled = Main.toggleExtrusion;
        //Handle.GetComponent<NearInteractionGrabbable>().enabled = Main.toggleExtrusion;
        //Handle.GetComponent<ObjectManipulator>().enabled = Main.toggleExtrusion;

        // call once for initilization
        if (vertices_initialized && !canUpdateMesh)
        {
            canUpdateMesh = true;
            CreateMesh();
            Debug.Log("Authority:" + hasAuthority);
        }

        if (canUpdateMesh && !isOwner) // mesh has been created
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

        if (Main.toggleExtrusion && canUpdateMesh)
        {
            Vector3 delta = Handle.transform.localPosition - handlePreviousPosition;

            if (delta.magnitude > 0)
            {
                // Debug.Log("LocalPosition:" + this.transform.localPosition);
                Vector3[] new_vertices = myMesh.vertices;
                foreach (int vertex_index in topVerticesIndices)
                {
                    new_vertices[vertex_index] += delta;
                }
                myMesh.vertices = new_vertices;
                BaseMesh.GetComponent<MeshCollider>().sharedMesh = myMesh;
            }
        }

        if (BaseMesh.GetComponent<Rigidbody>().isKinematic != isKinematic && !stay_kinematic)
        {
            BaseMesh.GetComponent<Rigidbody>().isKinematic = isKinematic;
        }

        if (BaseMesh.GetComponent<Rigidbody>().useGravity != useGravity)
        {
            BaseMesh.GetComponent<Rigidbody>().useGravity = useGravity;
        }

        handlePreviousPosition = Handle.transform.localPosition;
    }

    public void CreateMesh()
    {
        Vector2[] poly_vertices = new Vector2[sync_poly_vertices.Count];
        sync_poly_vertices.CopyTo(poly_vertices, 0);
        Vector3[] world_vertices = new Vector3[sync_world_vertices.Count];
        sync_world_vertices.CopyTo(world_vertices, 0);

        Mesh newMesh = MeshCreator.CreateMesh(poly_vertices, world_vertices, heightNormal);
        BaseMesh.GetComponent<MeshFilter>().mesh = newMesh;
        BaseMesh.GetComponent<MeshCollider>().sharedMesh = newMesh;
        this.GetComponent<UpdateMesh>().myMesh = newMesh;

        // get top vertices indices (after optimization)
        for (int j = 0; j < newMesh.vertexCount; j++)
        {
            Vector3 cur_vertex = newMesh.vertices[j];
            bool isTop = true;
            for (int k = 0; k < world_vertices.Length; k++)
            {
                if ((world_vertices[k]).Equals(cur_vertex))
                {
                    isTop = false;
                    break;
                }
            }
            if (isTop)
            {
                this.GetComponent<UpdateMesh>().topVerticesIndices.Add(j);
                Handle.transform.localPosition += cur_vertex;
            }
        }
        //Debug.Log("Number of top:" + MyObject.GetComponent<UpdateMesh>().topVerticesIndices.Count);
        //Debug.Log("Number of vertices:" + newMesh.vertexCount);
        Handle.transform.position /= this.GetComponent<UpdateMesh>().topVerticesIndices.Count;
        Handle.transform.position += 0.05f * heightNormal;
        this.GetComponent<UpdateMesh>().handlePreviousPosition = Handle.transform.localPosition;

        if (!hasAuthority)
        {
            isOwner = false;
            transform.position = PeerWorldOrigin.position;
            transform.rotation = PeerWorldOrigin.rotation;
        }
        else
        {
            isOwner = true;
        }

        BaseMesh.GetComponent<Renderer>().material.color = mesh_color;
    }

    [Command(requiresAuthority = false)]
    public void CmdSyncKinematic(bool value)
    {
        isKinematic = value;
    }

    [Command(requiresAuthority = false)]
    void CmdSyncGravity(bool value)
    {
        useGravity = value;
    }

    [Command]
    void CmdsyncManipulation(bool value)
    {
        manipulating = value;
    }

    public void OnManipulationStart()
    {
        AssignAuthority();
        // if assign success, start manipulation
        if (hasAuthority)
        {
            if (isKinematic != false)
            {
                CmdSyncKinematic(false);
            }
            CmdSyncGravity(false);
            CmdsyncManipulation(true);
        }
    }

    public void OnExtrusionStart()
    {
        AssignAuthority();
        if (hasAuthority)
        {
            CmdsyncManipulation(true);
        }
    }


    public void AssignAuthority()
    {
        CmdAssignAuthority(GetComponent<NetworkIdentity>(), NetworkClient.localPlayer.GetComponent<NetworkIdentity>());
    }

    [Command(requiresAuthority = false)]
    void CmdAssignAuthority(NetworkIdentity myId, NetworkIdentity netId)
    {
        // can only assign authority if no one is manipulating
        if (!manipulating)
        {
            myId.RemoveClientAuthority();
            if (myId.AssignClientAuthority(netId.connectionToClient))
            {
                Debug.Log("Assign Success!");
                //manipulating = true;
                //useGravity = false;
                //isKinematic = false;
            }
            else
            {
                Debug.Log("Assign Failed!");
            }
        }

        // Debug.Log(hasAuthority);
    }

    public void OnManipulationEnd()
    {
        if (hasAuthority)
        {
            CmdManipulationEnd();
        }
        // Debug.Log("Release Speed:" + BaseMesh.GetComponent<Rigidbody>().velocity.magnitude);
        // BaseMesh.GetComponent<Rigidbody>().AddForce((FrameHandler.corners[0] - FrameHandler.corners[2]).normalized * 0.5f, ForceMode.VelocityChange);
        // Debug.Log("Release Speed:" + BaseMesh.GetComponent<Rigidbody>().velocity.magnitude);
    }


    public void OnExtrusionEnd()
    {
        if (hasAuthority)
        {
            CmdManipulationEnd();
        }
    }
    // only the user with authority can set manipulating to false
    [Command]
    void CmdManipulationEnd()
    {
        manipulating = false;
        useGravity = true;
    }
}
