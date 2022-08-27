using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class UpdateMesh : NetworkBehaviour
{
    private GameObject BaseMesh;

    public readonly SyncList<Vector2> vertices = new SyncList<Vector2>();

    [SyncVar]
    public bool vertices_initialized = false;

    [SyncVar]
    public bool manipulating = false;

    public bool canUpdateMesh = false; // have we initilized, can we update?

    private Vector3 previousHandlePos;

    public bool isKinematic;

    // Start is called before the first frame update
    void Start()
    {
        // vertices.Callback += onVerticesUpdated;
        BaseMesh = transform.GetChild(0).gameObject;
        isKinematic = BaseMesh.GetComponent<Rigidbody>().isKinematic;
    }

    public void CreateMesh()
    {
        GameObject image = GameObject.Find("Quad");
        float image_width = image.transform.localScale.x;
        float image_height = image.transform.localScale.y;
        Vector3 image_origin = image.transform.position - new Vector3(image_width / 2, 0, image_height / 2);

        //obj.GetComponent<UpdateMesh>().enabled = true;
        transform.position = image_origin;
        Vector2[] MeshVertices = new Vector2[vertices.Count];
        vertices.CopyTo(MeshVertices, 0);
        BaseMesh.GetComponent<MeshFilter>().mesh = MeshCreator.CreateMesh(MeshVertices);
        BaseMesh.GetComponent<MeshCollider>().sharedMesh = BaseMesh.GetComponent<MeshFilter>().mesh;
        for (int i = 0; i < MeshVertices.Length; i++)
        {
            BaseMesh.transform.GetChild(0).localPosition += new Vector3(MeshVertices[i].x, 0.2f, MeshVertices[i].y);
        }
        BaseMesh.transform.GetChild(0).localPosition /= MeshVertices.Length;
        previousHandlePos = BaseMesh.transform.GetChild(0).localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        // call once for initilization
        if (vertices_initialized && !canUpdateMesh)
        {
            canUpdateMesh = true;
            CreateMesh();
        }

        if (canUpdateMesh)
        {
            Vector3 delta = BaseMesh.transform.GetChild(0).localPosition - previousHandlePos; // can we just use delta?
            if (delta.magnitude > 0)
            {
                previousHandlePos = BaseMesh.transform.GetChild(0).localPosition;
                MeshCreator.ExtrudeMesh(BaseMesh.GetComponent<MeshFilter>().mesh, delta);
                BaseMesh.GetComponent<MeshCollider>().sharedMesh = BaseMesh.GetComponent<MeshFilter>().mesh;
                // Debug.Log("Hey!");
            }
        }

        if (isKinematic != BaseMesh.GetComponent<Rigidbody>().isKinematic)
        {
            isKinematic = BaseMesh.GetComponent<Rigidbody>().isKinematic;
            CmdSyncKinematic(NetworkClient.localPlayer.GetComponent<NetworkIdentity>(), isKinematic);
        }
    }

    [Command(requiresAuthority = false)]
    void CmdSyncKinematic(NetworkIdentity originId, bool value)
    {
        // Debug.Log("Origin:" + originId.connectionToClient);
        foreach (NetworkConnectionToClient netid in NetworkServer.connections.Values)
        {
            TargetSyncKinematic(netid, value);
        }
    }

    [TargetRpc]
    void TargetSyncKinematic(NetworkConnection targetConnection, bool value)
    {
        isKinematic = value;
        BaseMesh.GetComponent<Rigidbody>().isKinematic = value;
    }

    //[Command]
    //void CmdUpdateVertices(Vector2[] new_vertices)
    //{
    //    vertices.Clear();
    //    vertices.AddRange(new_vertices);
    //}

    //[ClientRpc (includeOwner = false)]
    //void UpdateClientMesh()
    //{
    //    Vector2[] MeshVertices = new Vector2[vertices.Count];
    //    vertices.CopyTo(MeshVertices, 0);
    //    GetComponent<MeshFilter>().mesh = MeshCreator.CreateMesh(MeshVertices);
    //    GetComponent<MeshCollider>().sharedMesh = GetComponent<MeshFilter>().mesh;
    //}

    public void AssignAuthority()
    {
        CmdAssignAuthority(GetComponent<NetworkIdentity>(), NetworkClient.localPlayer.GetComponent<NetworkIdentity>());
    }

    [Command(requiresAuthority = false)]
    void CmdAssignAuthority(NetworkIdentity myId, NetworkIdentity netId)
    {
        if (!manipulating)
        {
            myId.RemoveClientAuthority();
            if (myId.AssignClientAuthority(netId.connectionToClient))
            {
                Debug.Log("Assign Success!");
                manipulating = true;
            }
            else
            {
                Debug.Log("Assign Failed!");
            }
        }

        // Debug.Log(hasAuthority);
    }

    // only the user with authority can set manipulating to false
    [Command]
    public void CmdManipulationEnd()
    {
        manipulating = false;
    }

    public void turnOffKinematicOnPickUp()
    {
        CmdSyncKinematic(NetworkClient.localPlayer.GetComponent<NetworkIdentity>(), false);
    }

    // TODO
    private void OnCollisionEnter(Collision collision)
    {
        // collision should be an object currently manipulated by a user
        if (hasAuthority && manipulating)
        {
            Debug.Log(collision.gameObject.name);
            // only detect collision with networked objects
            if (collision.gameObject.tag == "NetworkedObjects")
            {
                if (!collision.gameObject.GetComponentInParent<UpdateMesh>().hasAuthority)
                {
                    collision.gameObject.GetComponentInParent<UpdateMesh>().AssignAuthority();
                }
                Debug.Log("Assigning collided object authority!");
            }
        }
    }

    //void onVerticesUpdated(SyncList<Vector2>.Operation op, int index, Vector2 oldItem, Vector2 newItem)
    //{
    //    switch (op)
    //    {
    //        case SyncList<Vector2>.Operation.OP_ADD:
    //            // index is where it was added into the list
    //            // newItem is the new item
    //            Debug.Log("Vertice changed!");
    //            break;
    //        case SyncList<Vector2>.Operation.OP_INSERT:
    //            // index is where it was inserted into the list
    //            // newItem is the new item
    //            break;
    //        case SyncList<Vector2>.Operation.OP_REMOVEAT:
    //            // index is where it was removed from the list
    //            // oldItem is the item that was removed
    //            break;
    //        case SyncList<Vector2>.Operation.OP_SET:
    //            // index is of the item that was changed
    //            // oldItem is the previous value for the item at the index
    //            // newItem is the new value for the item at the index
    //            break;
    //        case SyncList<Vector2>.Operation.OP_CLEAR:
    //            // list got cleared
    //            break;
    //    }
    //}
}
