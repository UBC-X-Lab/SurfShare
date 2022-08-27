using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RemoteSpaceControl : NetworkBehaviour
{
    public static bool localSet = false;
    private float localWidth;
    private float localHeight;

    public static bool remoteSet = true;
    public static float remoteWidth = 0.5f;
    public static float remoteHeight = 0.3f;

    private bool remoteVideoSet = false;
    private Vector3[] RemoteCorners = new Vector3[4];
    public GameObject RemoteVideoPlane;
    public GameObject RemoteVideoFrame;

    public static Vector3 RemoteSpaceOrigin;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (FrameHandler.corners.Count == 4 && !localSet)
        {
            localSet = true;
            localWidth = (FrameHandler.corners[1] - FrameHandler.corners[0]).magnitude;
            localHeight = (FrameHandler.corners[2] - FrameHandler.corners[0]).magnitude;
            CmdSync(NetworkClient.localPlayer.GetComponent<NetworkIdentity>(), localWidth, localHeight);
        }

        if (localSet && remoteSet && !remoteVideoSet)
        {
            remoteVideoSet = true;
            SetRemoteVideo();
        }
    }

    void SetRemoteVideo()
    {
        remoteVideoSet = true;
        Vector3 remoteFrameOrigin = FrameHandler.corners[3];
        Vector3 remoteXAxis = (FrameHandler.corners[0] - FrameHandler.corners[1]).normalized * remoteWidth;
        Vector3 remoteYAxis = (FrameHandler.corners[0] - FrameHandler.corners[2]).normalized * remoteHeight;
        RemoteCorners[0] = remoteFrameOrigin;
        RemoteCorners[1] = remoteFrameOrigin + remoteXAxis;
        RemoteCorners[2] = remoteFrameOrigin + remoteYAxis;
        RemoteCorners[3] = remoteFrameOrigin + remoteXAxis + remoteYAxis;

        // we want the centers of the frames to be placed together!
        Vector3 frameCenter = (FrameHandler.corners[0] + FrameHandler.corners[1] + FrameHandler.corners[2] + FrameHandler.corners[3]) / 4;
        Vector3 remoteCenter = (RemoteCorners[0] + RemoteCorners[1] + RemoteCorners[2] + RemoteCorners[3]) / 4;
        RemoteCorners[0] += frameCenter - remoteCenter;
        RemoteCorners[1] += frameCenter - remoteCenter;
        RemoteCorners[2] += frameCenter - remoteCenter;
        RemoteCorners[3] += frameCenter - remoteCenter;

        // place video plane
        RemoteVideoPlane.GetComponent<RemoteVideoPlayerController>().PositionRemotePlayer(RemoteCorners);
        Debug.Log("remote video plane set!");

        // draw frame
        Transform remoteVideoTransform = RemoteVideoPlane.GetComponent<Transform>();
        LineRenderer myLineRenderer = RemoteVideoFrame.GetComponent<LineRenderer>();
        myLineRenderer.SetWidth(0.005f, 0.005f);
        myLineRenderer.positionCount = 4;

        myLineRenderer.SetPosition(0, remoteVideoTransform.InverseTransformPoint(RemoteCorners[0]));
        myLineRenderer.SetPosition(1, remoteVideoTransform.InverseTransformPoint(RemoteCorners[1]));
        myLineRenderer.SetPosition(2, remoteVideoTransform.InverseTransformPoint(RemoteCorners[3]));
        myLineRenderer.SetPosition(3, remoteVideoTransform.InverseTransformPoint(RemoteCorners[2]));

        Debug.Log("remote frame set!");
    }

    [Command(requiresAuthority = false)]
    void CmdSync(NetworkIdentity originId, float localWidth, float localHeight)
    {
        // Debug.Log("Origin:" + originId.connectionToClient);
        foreach (NetworkConnectionToClient netid in NetworkServer.connections.Values)
        {
            if (netid != originId.connectionToClient)
            {
                TargetSync(netid, localWidth, localHeight);
            }
        }
    }

    [TargetRpc]
    void TargetSync(NetworkConnection targetConnection, float localWidth, float localHeight)
    {
        Debug.Log("Remote width and height set!");
        remoteWidth = localWidth;
        remoteHeight = localHeight;
        remoteSet = true;
    }

    //[TargetRpc]
    //void TargetSync(NetworkConnection targetConnection, bool remoteSetSuccess)
    //{
    //    remoteSet = remoteSetSuccess;
    //}
}
