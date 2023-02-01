using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class UpdateMesh : NetworkBehaviour
{
    private GameObject BaseMesh;

    public readonly SyncList<Vector2> vertices = new SyncList<Vector2>();

    public readonly SyncList<int> vertices_count = new SyncList<int>();

    [SyncVar]
    public bool vertices_initialized = false;

    [SyncVar]
    public bool manipulating = false;

    public bool canUpdateMesh = false; // have we initilized, can we update?

    private Vector3 previousHandlePos;

    [SyncVar]
    public bool stayKinematic = false;

    [SyncVar]
    public bool isKinematic;

    [SyncVar]
    public bool useGravity;

    // Start is called before the first frame update
    void Start()
    {
        // vertices.Callback += onVerticesUpdated;
        BaseMesh = transform.GetChild(0).gameObject;
        isKinematic = BaseMesh.GetComponent<Rigidbody>().isKinematic;
        useGravity = BaseMesh.GetComponent<Rigidbody>().useGravity;
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
        int[] MeshVerticesCount = new int[vertices_count.Count];

        vertices.CopyTo(MeshVertices, 0);
        vertices_count.CopyTo(MeshVerticesCount, 0);

        BaseMesh.GetComponent<MeshFilter>().mesh = MeshCreator.CreateMesh(MeshVertices, MeshVerticesCount);
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

        if (BaseMesh.GetComponent<Rigidbody>().isKinematic != isKinematic && !stayKinematic) // don't update kinematic if staykinematic is true
        {
            BaseMesh.GetComponent<Rigidbody>().isKinematic = isKinematic;
        }

        if (BaseMesh.GetComponent<Rigidbody>().useGravity != useGravity)
        {
            BaseMesh.GetComponent<Rigidbody>().useGravity = useGravity;
        }
    }

    [Command(requiresAuthority = false)]
    void CmdSyncKinematic(bool value)
    {
        isKinematic = value;
    }

    [Command(requiresAuthority = false)]
    void CmdSyncGravity(bool value)
    {
        useGravity = value;
    }

    public void AssignAuthority()
    {
        CmdAssignAuthority(GetComponent<NetworkIdentity>(), NetworkClient.localPlayer.GetComponent<NetworkIdentity>());
        if (isKinematic != false)
        {
            CmdSyncKinematic(false); // turn off Kinematic on pick up
        }
        CmdSyncGravity(false);
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

    public void OnManipulationEnd()
    {
        BaseMesh.GetComponent<Rigidbody>().velocity = BaseMesh.GetComponent<Rigidbody>().velocity * 5;
        CmdManipulationEnd();
    }

    // only the user with authority can set manipulating to false
    [Command]
    public void CmdManipulationEnd()
    {
        manipulating = false;
        CmdSyncGravity(true);
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
