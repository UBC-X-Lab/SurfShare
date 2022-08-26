using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoteVideoPlayerController : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        // transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PositionRemotePlayer(Vector3[] corners)
    {
        this.GetComponent<MeshRenderer>().enabled = true;

        Vector3 frame_origin = corners[0];
        Vector3 x_axis = corners[1] - frame_origin;
        Vector3 y_axis = corners[2] - frame_origin;

        // quad position
        this.transform.position = frame_origin + x_axis / 2 + y_axis / 2;

        // quad width and height (scale = 1 is 1 Unity unit for quad)
        float x_scale = x_axis.magnitude;
        float y_scale = y_axis.magnitude;
        this.transform.localScale = new Vector3(x_scale, y_scale, 1);

        Vector3 lookAt = Vector3.Cross(x_axis, y_axis); // unity is left-handed coordinate system
        transform.rotation = Quaternion.LookRotation(-lookAt, -y_axis); // quad lookAt is the reversed direction of the texture
        
    }
}
