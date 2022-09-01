using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // collision should be an object currently manipulated by a user
        if (transform.parent.GetComponent<UpdateMesh>().hasAuthority && transform.parent.GetComponent<UpdateMesh>().manipulating)
        {
            // only detect collision with networked objects
            if (collision.gameObject.tag == "NetworkedObjects" && !collision.gameObject.GetComponentInParent<UpdateMesh>().manipulating)
            {
                // Debug.Log(collision.gameObject.name);
                if (!collision.gameObject.GetComponentInParent<UpdateMesh>().hasAuthority)
                {
                    collision.gameObject.GetComponentInParent<UpdateMesh>().AssignAuthority();
                    Debug.Log("Controlled object assigning collided object authority!");
                }
                collision.gameObject.GetComponent<Rigidbody>().AddForce((FrameHandler.corners[0] - FrameHandler.corners[2]).normalized * 0.5f, ForceMode.VelocityChange);
                Debug.Log("Bounce off speed: " + collision.gameObject.GetComponent<Rigidbody>().velocity.magnitude);
            }
        }
        else if (!transform.parent.GetComponent<UpdateMesh>().manipulating)
        {
            // on collusion, a free object with higher speed assign other objects its authority
            if (collision.gameObject.tag == "NetworkedObjects" && !collision.gameObject.GetComponentInParent<UpdateMesh>().manipulating)
            {
                if (gameObject.gameObject.GetComponent<Rigidbody>().velocity.magnitude > collision.gameObject.GetComponent<Rigidbody>().velocity.magnitude)
                {
                    if (!collision.gameObject.GetComponentInParent<UpdateMesh>().hasAuthority)
                    {
                        collision.gameObject.GetComponentInParent<UpdateMesh>().AssignAuthority();
                        Debug.Log("Faster object assigning collided object authority!");
                    }
                }
            }
        }
    }

    //private void OnCollisionExit(Collision collision)
    //{
    //    if (transform.parent.GetComponent<UpdateMesh>().hasAuthority)// && transform.parent.GetComponent<UpdateMesh>().manipulating)
    //    {
    //        // only detect collision with networked objects
    //        if (collision.gameObject.tag == "NetworkedObjects")
    //        {
    //            // fire it away!
    //            if (collision.gameObject.GetComponent<Rigidbody>().velocity.magnitude * 10 < 2)
    //            {
    //                collision.gameObject.GetComponent<Rigidbody>().velocity = collision.gameObject.GetComponent<Rigidbody>().velocity * 10;
    //            }
    //            else
    //            {
    //                collision.gameObject.GetComponent<Rigidbody>().velocity = collision.gameObject.GetComponent<Rigidbody>().velocity.normalized * 10;
    //            }
    //        }
    //        Debug.Log("Bounce off speed:" + collision.gameObject.GetComponent<Rigidbody>().velocity.magnitude);
    //    }
    //}
}
