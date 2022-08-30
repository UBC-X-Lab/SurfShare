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
            if (collision.gameObject.tag == "NetworkedObjects")
            {
                Debug.Log(collision.gameObject.name);
                if (!collision.gameObject.GetComponentInParent<UpdateMesh>().hasAuthority)
                {
                    collision.gameObject.GetComponentInParent<UpdateMesh>().AssignAuthority();
                }
                Debug.Log("Assigning collided object authority!");
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (transform.parent.GetComponent<UpdateMesh>().hasAuthority && transform.parent.GetComponent<UpdateMesh>().manipulating)
        {
            if (collision.gameObject.tag == "NetworkedObjects")
            {
                // fire it away!
                if (collision.gameObject.GetComponent<Rigidbody>().velocity.magnitude * 3 < 1)
                {
                    collision.gameObject.GetComponent<Rigidbody>().velocity = collision.gameObject.GetComponent<Rigidbody>().velocity * 3;
                }
                else
                {
                    collision.gameObject.GetComponent<Rigidbody>().velocity = collision.gameObject.GetComponent<Rigidbody>().velocity.normalized * 1;
                }
                
            }
        }
    }
}
