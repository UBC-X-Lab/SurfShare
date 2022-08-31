using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class HandJointTracker : NetworkBehaviour
{
#if UNITY_EDITOR
    string right_hand_name = "Right_RiggedHandRight(Clone)";
    string left_hand_name = "Left_RiggedHandLeft(Clone)";
#endif

#if UNITY_WSA && !UNITY_EDITOR
    string right_hand_name = "Right_HandSkeleton(Clone)";
    string left_hand_name = "Left_HandSkeleton(Clone)";
#endif

    Transform remote_left_hand;
    Transform remote_right_hand;

    [SyncVar]
    bool render_left = false;
    
    [SyncVar]
    bool render_right = false;

    public bool db_render_left;
    public bool db_render_right;

    int JointCount = 24;

    Transform[] left_joints;
    Transform[] right_joints;
    int[] jointTransformIndices;

    Vector3[] local_left_hand_position;
    Quaternion[] local_left_hand_rotation;

    Vector3[] local_right_hand_position;
    Quaternion[] local_right_hand_rotation;

    public readonly SyncList<Vector3> remote_left_hand_position = new SyncList<Vector3>();
    public readonly SyncList<Quaternion> remote_left_hand_rotation = new SyncList<Quaternion>();

    public readonly SyncList<Vector3> remote_right_hand_position = new SyncList<Vector3>();
    public readonly SyncList<Quaternion> remote_right_hand_rotation = new SyncList<Quaternion>();

    // controls rate for sync
    int frameCount = 0;
    public int jointUpdateRate = 2;

    // Start is called before the first frame update
    void Start()
    {
        // Debug.Log(syncInterval);
        remote_left_hand = transform.GetChild(1);
        remote_right_hand = transform.GetChild(2);

        left_joints = new Transform[JointCount];
        right_joints = new Transform[JointCount];
        jointTransformIndices = new int[JointCount];

        local_left_hand_position = new Vector3[JointCount];
        local_left_hand_rotation = new Quaternion[JointCount];
        local_right_hand_position = new Vector3[JointCount];
        local_right_hand_rotation = new Quaternion[JointCount];

        for (int i = 0; i < JointCount; i++)
        {
            left_joints[i] = remote_left_hand.transform.GetChild(i);
            right_joints[i] = remote_right_hand.transform.GetChild(i);

#if UNITY_EDITOR
            jointTransformIndices[i] = int.Parse(left_joints[i].gameObject.name) + 1;
#endif

#if UNITY_WSA && !UNITY_EDITOR
            jointTransformIndices[i] = int.Parse(left_joints[i].gameObject.name) - 1;
#endif
        }

        if (hasAuthority)
        {
            CmdInitializeRemoteData();
        }
        // Debug.Log(remote_left_hand_position.Count);
        // Debug.Log(transform.parent.GetComponent<PlayerController>().hasAuthority);
    }

    // Update is called once per frame
    void Update()
    {
        db_render_left = render_left;
        db_render_right = render_right;

        // keep tracking local player's hand
        if (hasAuthority && GetComponent<PlayerController>().headInitialized)
        {
            GameObject my_left_hand = GameObject.Find(left_hand_name);
            GameObject my_right_hand = GameObject.Find(right_hand_name);

            // turn off remote left hand if null
            if (my_left_hand == null && render_left)
            {
                CmdSetLeftHandVisibility(false);
            }

            // turn off remote right if null
            if (my_right_hand == null && render_right)
            {
                CmdSetRightHandVisibility(false);
            }

            // track local left hand position
            if (my_left_hand != null)
            {
                if (!render_left)
                {
                    CmdSetLeftHandVisibility(true);
                    // Debug.Log("Turn on left");
                }

                for (int i = 0; i < JointCount; i++)
                {
#if UNITY_WSA && !UNITY_EDITOR
                    if (my_left_hand.transform.childCount != JointCount + 2)
                    {
                        break;
                    }
#endif
                    local_left_hand_position[i] = my_left_hand.transform.GetChild(jointTransformIndices[i]).position;
                    local_left_hand_rotation[i] = my_left_hand.transform.GetChild(jointTransformIndices[i]).rotation;
                }

                if (frameCount % jointUpdateRate == 0)
                {
                    CmdLeftUpdate(local_left_hand_position, local_left_hand_rotation);
                }
            }

            // track local right hand position
            if (my_right_hand != null)
            {
                if (!render_right)
                {
                    CmdSetRightHandVisibility(true);
                    // Debug.Log("Turn on right");
                }

                for (int i = 0; i < JointCount; i++)
                {
#if UNITY_WSA && !UNITY_EDITOR
                    if (my_right_hand.transform.childCount != JointCount + 2)
                    {
                        break;
                    }
#endif
                    local_right_hand_position[i] = my_right_hand.transform.GetChild(jointTransformIndices[i]).position;
                    local_right_hand_rotation[i] = my_right_hand.transform.GetChild(jointTransformIndices[i]).rotation;
                }

                if (frameCount % jointUpdateRate == 0)
                {
                    CmdRightUpdate(local_right_hand_position, local_right_hand_rotation);
                }
            }

            if (frameCount % jointUpdateRate == 0)
            {
                frameCount = 0;
            }
            frameCount += 1;
        }

        // for remote hand, just enable mesh renderer
        if (!hasAuthority && GetComponent<PlayerController>().headInitialized)
        {
            // Debug.Log(render_right);
            for (int i = 0; i < JointCount; i++)
            {
                left_joints[i].gameObject.GetComponent<MeshRenderer>().enabled = render_left;
                right_joints[i].gameObject.GetComponent<MeshRenderer>().enabled = render_right;

                if (render_left)
                {
                    left_joints[i].localPosition = remote_left_hand_position[i];
                    left_joints[i].localRotation = remote_left_hand_rotation[i];
                }
                
                if (render_right)
                {
                    right_joints[i].localPosition = remote_right_hand_position[i];
                    right_joints[i].localRotation = remote_right_hand_rotation[i];
                }
            }
        }
    }

    [Command]
    void CmdSetLeftHandVisibility(bool visibility)
    {
        render_left = visibility;
        //if (visibility)
        //{
        //    Debug.Log("Turn on left hand");
        //}
    }

    [Command]
    void CmdSetRightHandVisibility(bool visibility)
    {
        render_right = visibility;
        //if (visibility)
        //{
        //    Debug.Log("Turn on right hand");
        //}
    }

    [Command]
    void CmdInitializeRemoteData()
    {
        for (int i = 0; i < JointCount; i++)
        {
            remote_left_hand_position.Add(new Vector3());
            remote_left_hand_rotation.Add(new Quaternion());
            remote_right_hand_position.Add(new Vector3());
            remote_right_hand_rotation.Add(new Quaternion());
        }
    }

    [Command]
    void CmdLeftUpdate(Vector3[] positions, Quaternion[] rotations)
    {
        for (int i = 0; i < JointCount; i++)
        {
            if (!remote_left_hand_position[i].Equals(positions[i]))
            {
                remote_left_hand_position[i] = positions[i];
            }
            if (!remote_left_hand_rotation[i].Equals(rotations[i]))
            {
                remote_left_hand_rotation[i] = rotations[i];
            }
        }
    }

    [Command]
    void CmdRightUpdate(Vector3[] positions, Quaternion[] rotations)
    {
        for (int i = 0; i < JointCount; i++)
        {
            if (!remote_right_hand_position[i].Equals(positions[i]))
            {
                remote_right_hand_position[i] = positions[i];
            }
            if (!remote_right_hand_rotation[i].Equals(rotations[i]))
            {
                remote_right_hand_rotation[i] = rotations[i];
            }
        }
    }
}
