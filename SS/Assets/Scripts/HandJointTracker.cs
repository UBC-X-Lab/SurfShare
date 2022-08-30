using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandJointTracker : MonoBehaviour
{
    public bool isLeft;

    string right_hand_name = "Right_RiggedHandRight(Clone)";
    string left_hand_name = "Left_RiggedHandLeft(Clone)";

    int JointCount = 24;

    Transform[] joints;
    int[] jointTransformIndices;

    bool joints_initialized = false;

    // Start is called before the first frame update
    void Start()
    {
        joints = new Transform[JointCount];
        jointTransformIndices = new int[JointCount];

        for (int i = 0; i < JointCount; i++)
        {
            joints[i] = transform.GetChild(i);
            joints[i].gameObject.GetComponent<MeshRenderer>().enabled = false;
            jointTransformIndices[i] = int.Parse(joints[i].gameObject.name) + 1;
        }
        // Debug.Log(transform.parent.GetComponent<PlayerController>().hasAuthority);
    }

    // Update is called once per frame
    void Update()
    {
        // keep tracking local player's hand
        if (transform.parent.GetComponent<PlayerController>().hasAuthority && transform.parent.GetComponent<PlayerController>().headInitialized)
        {
            GameObject hand = GameObject.Find(isLeft? left_hand_name : right_hand_name);
            if (hand != null)
            {
                for (int i = 0; i < JointCount; i++)
                {
                    joints[i].position = hand.transform.GetChild(jointTransformIndices[i]).position;
                    joints[i].rotation = hand.transform.GetChild(jointTransformIndices[i]).rotation;
                }
            }
        }

        // for remote hand, just enable mesh renderer
        if (!transform.parent.GetComponent<PlayerController>().hasAuthority && transform.parent.GetComponent<PlayerController>().headInitialized && !joints_initialized)
        {
            for (int i = 0; i < JointCount; i++)
            {
                joints[i].gameObject.GetComponent<MeshRenderer>().enabled = true;
            }
            joints_initialized = true;
        }
    }
}
