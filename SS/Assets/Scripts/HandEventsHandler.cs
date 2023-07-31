using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class HandEventsHandler : MonoBehaviour
{
    public bool isLeftHanded = false;
    
    public static Vector3 handray_cursor_position; // cursor position
    public static Vector3 handray_start_position;
    public static Quaternion handray_cursor_orientation; // cursor orientation
    private bool handray_cursor_present = false; // is there a cursor?

    public GameObject RemoteSpaceControlObject;

    public FrameHandler frameHandler;

    // Start is called before the first frame update
    void Start()
    {
        if (this.isLeftHanded)
        {
            PointerUtils.SetHandRayPointerBehavior(PointerBehavior.AlwaysOff, Microsoft.MixedReality.Toolkit.Utilities.Handedness.Right);
        }
        else
        {
            PointerUtils.SetHandRayPointerBehavior(PointerBehavior.AlwaysOff, Microsoft.MixedReality.Toolkit.Utilities.Handedness.Left);
        }
    }

    // Update is called once per frame
    void Update()
    {
        getHandRayCursorPosition();
    }

    void getHandRayCursorPosition()
    {
        foreach (var source in CoreServices.InputSystem.DetectedInputSources)
        {
            // only look for hand
            if (source.SourceType == InputSourceType.Hand)
            {
                foreach (var pointer in source.Pointers)
                {
                    // ignore near pointers
                    if (pointer is IMixedRealityNearPointer || pointer is GGVPointer)
                    {
                        continue;
                    }

                    if (pointer.Result != null)
                    {
                        var endpoint = pointer.Result.Details.Point;
                        //Debug.Log("BaseCursor:" + pointer.BaseCursor.Position);
                        //Debug.Log("endpoint:" + endpoint);
                        this.handray_cursor_present = true;
                        //handray_cursor_position = endpoint - 0.2f * Vector3.forward; // for testing purpose
                        handray_cursor_position = endpoint;
                        handray_start_position = pointer.Result.StartPoint;
                        handray_cursor_orientation = pointer.BaseCursor.Rotation;
                        return;
                    }
                }
            }
        }
        this.handray_cursor_present = false;
        //Debug.Log("No cursor!");
    }

    public void HandRayPointerClicked()
    {
        if (this.handray_cursor_present)
        {
            if (RemoteSpaceControl.STATE == RemoteSpaceControl.PLACE_LOCAL)
            {
                frameHandler.SetLocalPinched();
                if (FrameHandler.corners.Count == 1)
                {
                    RemoteSpaceControlObject.GetComponent<RemoteSpaceControl>().NotifySetLocal(); // this sets the peer's state to PLACE_REMOTE
                }
            }
            else if (RemoteSpaceControl.STATE == RemoteSpaceControl.PLACE_REMOTE)
            {
                RemoteSpaceControlObject.GetComponent<RemoteSpaceControl>().setRemoteVideoPlane = true;
            }
        }
    }
}
