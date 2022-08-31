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
                    Debug.Log("Assigning collided object authority!");
                }
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (transform.parent.GetComponent<UpdateMesh>().hasAuthority)// && transform.parent.GetComponent<UpdateMesh>().manipulating)
        {
            // only detect collision with networked objects
            if (collision.gameObject.tag == "NetworkedObjects")
            {
                Debug.Log("Bounce off speed:" + collision.gameObject.GetComponent<Rigidbody>().velocity.magnitude);
                // fire it away!
                if (collision.gameObject.GetComponent<Rigidbody>().velocity.magnitude * 10 < 2)
                {
                    collision.gameObject.GetComponent<Rigidbody>().velocity = collision.gameObject.GetComponent<Rigidbody>().velocity * 10;
                }
                else
                {
                    collision.gameObject.GetComponent<Rigidbody>().velocity = collision.gameObject.GetComponent<Rigidbody>().velocity.normalized * 10;
                }
            }
        }
    }
}
