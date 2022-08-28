using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RemoteSpaceControl : NetworkBehaviour
{
    public static bool localSet = false;
    private float localWidth;
    private float localHeight;

    public static bool remoteSet = false;
    public static float remoteWidth = 0.5f;
    public static float remoteHeight = 0.3f;

    public bool setRemoteVideoPlane = false;
    private Vector3[] RemoteCorners = new Vector3[4];
    public GameObject RemoteVideoPlane;
    public GameObject RemoteVideoFrame;
    public GameObject FrameUpIndicator;

    public static Vector3 RemoteSpaceOrigin;

    // STATES (Note this implementation is not atomic! Make sure the users communicate before hand)
    public static readonly int PLACE_LOCAL = 0, PLACE_REMOTE = 1, PLACE_COMPLETE = 2;
    public static int STATE = PLACE_LOCAL;

    public int DEBUG_STATE;

    public Transform LocalVideoPlayer;
    public Transform WorldOrigin;

    [SyncVar]
    Vector3 space_offset = new Vector3(0, 0, 0); // this should be the local offset, so we can directly apply

    [SyncVar]
    Quaternion quaternion_offset = new Quaternion();

    // Start is called before the first frame update
    void Start()
    {
        FrameUpIndicator.GetComponent<LineRenderer>().SetWidth(0.005f, 0.005f);
        FrameUpIndicator.GetComponent<LineRenderer>().positionCount = 2;
    }

    // Update is called once per frame
    void Update()
    {
        DEBUG_STATE = STATE;

        if (FrameHandler.corners.Count == 4 && !localSet)
        {
            localSet = true;
            Vector3 localXAxis = FrameHandler.corners[1] - FrameHandler.corners[0];
            Vector3 localYAxis = FrameHandler.corners[2] - FrameHandler.corners[0];
            localWidth = (FrameHandler.corners[1] - FrameHandler.corners[0]).magnitude;
            localHeight = (FrameHandler.corners[2] - FrameHandler.corners[0]).magnitude;

            Vector3 localFrameCenter = (FrameHandler.corners[0] + FrameHandler.corners[1] + FrameHandler.corners[2] + FrameHandler.corners[3]) / 4.0f;
            Quaternion localFrameQuaternion = Quaternion.LookRotation(-Vector3.Cross(localXAxis, localYAxis), -localYAxis);
            LocalVideoPlayer.position = localFrameCenter;
            LocalVideoPlayer.rotation = localFrameQuaternion;

            if (setRemoteVideoPlane)
            {
                STATE = PLACE_COMPLETE;
                // sync space offset so the peer can set this peer's frame
                // Debug.Log("Offset:" + (RemoteVideoPlane.GetComponent<Transform>().position - localFrameCenter).ToString());
                // space_offset = RemoteVideoPlane.GetComponent<Transform>().InverseTransformVector(RemoteVideoPlane.GetComponent<Transform>().position - localFrameCenter);

                space_offset = LocalVideoPlayer.InverseTransformPoint(RemoteVideoPlane.GetComponent<Transform>().position); // this is now directly the remote video relative postion
                quaternion_offset = Quaternion.Inverse(localFrameQuaternion) * RemoteVideoPlane.GetComponent<Transform>().rotation;
                CmdInitializeSpaceOffset(space_offset, quaternion_offset);
                Debug.Log(space_offset);
                Debug.Log("Client setup complete!");
            }

            CmdSync(NetworkClient.localPlayer.GetComponent<NetworkIdentity>(), localWidth, localHeight);
        }

        if (STATE == PLACE_REMOTE)
        {
            if (remoteSet)
            {
                // if local is not set, then I was the second user, I set remote and then decide our initial relative position
                if (!localSet)
                {
                    Vector3 CursorLookAt = HandEventsHandler.handray_cursor_orientation * Vector3.forward;
                    Vector3 Handray = HandEventsHandler.handray_cursor_position - HandEventsHandler.handray_start_position;
                    float angle = Vector3.Angle(CursorLookAt, Handray);
                    Vector3 Up = CursorLookAt.magnitude / Mathf.Cos(angle) * Handray.normalized + CursorLookAt;
                    if (Vector3.Dot(HandEventsHandler.handray_cursor_orientation * Vector3.up, Up) < 0)
                    {
                        Up = -Up;
                    }
                    Quaternion FrameQuaternion = Quaternion.LookRotation(CursorLookAt, Up.normalized);

                    //Debug.Log(Up);

                    //Debug.Log(HandEventsHandler.handray_cursor_orientation * Vector3.up);
                    // Quaternion FrameQuaternion = HandEventsHandler.handray_cursor_orientation;

                    RemoteVideoFrame.GetComponent<LineRenderer>().positionCount = 4;
                    RemoteVideoFrame.GetComponent<LineRenderer>().SetWidth(0.005f, 0.005f);
                    RemoteVideoFrame.GetComponent<LineRenderer>().SetPosition(0, RemoteVideoFrame.GetComponentInParent<Transform>()
                        .InverseTransformPoint(HandEventsHandler.handray_cursor_position + FrameQuaternion * (remoteWidth * new Vector3(1, 0, 0) + remoteHeight * new Vector3(0, 1, 0)) / 2));
                    RemoteVideoFrame.GetComponent<LineRenderer>().SetPosition(1, RemoteVideoFrame.GetComponentInParent<Transform>()
                        .InverseTransformPoint(HandEventsHandler.handray_cursor_position + FrameQuaternion * (remoteWidth * new Vector3(-1, 0, 0) + remoteHeight * new Vector3(0, 1, 0)) / 2));
                    RemoteVideoFrame.GetComponent<LineRenderer>().SetPosition(2, RemoteVideoFrame.GetComponentInParent<Transform>()
                        .InverseTransformPoint(HandEventsHandler.handray_cursor_position + FrameQuaternion * (remoteWidth * new Vector3(-1, 0, 0) + remoteHeight * new Vector3(0, -1, 0)) / 2));
                    RemoteVideoFrame.GetComponent<LineRenderer>().SetPosition(3, RemoteVideoFrame.GetComponentInParent<Transform>()
                        .InverseTransformPoint(HandEventsHandler.handray_cursor_position + FrameQuaternion * (remoteWidth * new Vector3(1, 0, 0) + remoteHeight * new Vector3(0, -1, 0)) / 2));


                    FrameUpIndicator.GetComponent<LineRenderer>().SetPosition(0, RemoteVideoFrame.GetComponentInParent<Transform>()
                        .InverseTransformPoint(HandEventsHandler.handray_cursor_position + FrameQuaternion * (remoteWidth * new Vector3(1, 0, 0) + remoteHeight * new Vector3(0, -1.1f, 0)) / 2));
                    FrameUpIndicator.GetComponent<LineRenderer>().SetPosition(1, RemoteVideoFrame.GetComponentInParent<Transform>()
                        .InverseTransformPoint(HandEventsHandler.handray_cursor_position + FrameQuaternion * (remoteWidth * new Vector3(-1, 0, 0) + remoteHeight * new Vector3(0, -1.1f, 0)) / 2));

                    if (setRemoteVideoPlane)
                    {
                        Transform remoteVideoTransform = RemoteVideoPlane.GetComponent<Transform>();
                        RemoteCorners[0] = remoteVideoTransform.TransformPoint(RemoteVideoFrame.GetComponent<LineRenderer>().GetPosition(2));
                        RemoteCorners[1] = remoteVideoTransform.TransformPoint(RemoteVideoFrame.GetComponent<LineRenderer>().GetPosition(3));
                        RemoteCorners[2] = remoteVideoTransform.TransformPoint(RemoteVideoFrame.GetComponent<LineRenderer>().GetPosition(1));
                        RemoteCorners[3] = remoteVideoTransform.TransformPoint(RemoteVideoFrame.GetComponent<LineRenderer>().GetPosition(0));
                        SetRemoteVideo(RemoteCorners);
                        STATE = PLACE_LOCAL;
                    }
                }
                else // if local is already set, then I was the first user, I wait for the second user to place their remote frame for me
                {
                    SetRemoteVideo(null, true);

                    Vector3 localXAxis = FrameHandler.corners[1] - FrameHandler.corners[0];
                    Vector3 localYAxis = FrameHandler.corners[2] - FrameHandler.corners[0];
                    Quaternion localFrameQuaternion = Quaternion.LookRotation(-Vector3.Cross(localXAxis, localYAxis), -localYAxis);
                    RemoteVideoPlane.GetComponent<Transform>().rotation = localFrameQuaternion * quaternion_offset;

                    // Vector3 world_space_offset = RemoteVideoPlane.GetComponent<Transform>().TransformVector(space_offset);
                    Vector3 world_space_offset = LocalVideoPlayer.TransformPoint(space_offset);
                    RemoteVideoPlane.GetComponent<Transform>().position = world_space_offset;

                    Debug.Log("Offset:" + world_space_offset.ToString());

                    STATE = PLACE_COMPLETE;
                }
            }
        }
    }

    void SetRemoteVideo(Vector3[] RemoteCorners = null, bool matchCenter = false)
    {
        if (RemoteCorners == null)
        {
            RemoteCorners = new Vector3[4];
            Vector3 remoteFrameOrigin = FrameHandler.corners[3];
            Vector3 remoteXAxis = (FrameHandler.corners[0] - FrameHandler.corners[1]).normalized * remoteWidth;
            Vector3 remoteYAxis = (FrameHandler.corners[0] - FrameHandler.corners[2]).normalized * remoteHeight;
            RemoteCorners[0] = remoteFrameOrigin;
            RemoteCorners[1] = remoteFrameOrigin + remoteXAxis;
            RemoteCorners[2] = remoteFrameOrigin + remoteYAxis;
            RemoteCorners[3] = remoteFrameOrigin + remoteXAxis + remoteYAxis;
        }

        // place video plane
        RemoteVideoPlane.GetComponent<RemoteVideoPlayerController>().PositionRemotePlayer(RemoteCorners);
        Debug.Log("remote video plane set!");

        // draw frame
        Transform remoteVideoTransform = RemoteVideoPlane.GetComponent<Transform>();
        LineRenderer myLineRenderer = RemoteVideoFrame.GetComponent<LineRenderer>();
        LineRenderer videoUpSide = FrameUpIndicator.GetComponent<LineRenderer>();

        if (RemoteVideoFrame.GetComponent<LineRenderer>().positionCount != 4)
        {
            RemoteVideoFrame.GetComponent<LineRenderer>().positionCount = 4;
            RemoteVideoFrame.GetComponent<LineRenderer>().SetWidth(0.005f, 0.005f);
        }

        myLineRenderer.SetPosition(0, remoteVideoTransform.InverseTransformPoint(RemoteCorners[0]));
        myLineRenderer.SetPosition(1, remoteVideoTransform.InverseTransformPoint(RemoteCorners[1]));
        myLineRenderer.SetPosition(2, remoteVideoTransform.InverseTransformPoint(RemoteCorners[3]));
        myLineRenderer.SetPosition(3, remoteVideoTransform.InverseTransformPoint(RemoteCorners[2]));

        videoUpSide.SetPosition(0, remoteVideoTransform.InverseTransformPoint(RemoteCorners[0] + (RemoteCorners[0] - RemoteCorners[2]) * 0.05f));
        videoUpSide.SetPosition(1, remoteVideoTransform.InverseTransformPoint(RemoteCorners[1] + (RemoteCorners[0] - RemoteCorners[2]) * 0.05f));

        if (matchCenter)
        {
            // match the centers
            Vector3 localCenter = (FrameHandler.corners[0] + FrameHandler.corners[1] + FrameHandler.corners[2] + FrameHandler.corners[3]) / 4.0f;
            Vector3 remoteCenter = (RemoteCorners[0] + RemoteCorners[1] + RemoteCorners[2] + RemoteCorners[3]) / 4.0f;

            remoteVideoTransform.position += (localCenter - remoteCenter);
        }

        Debug.Log("remote frame set!");
    }


    //===============================   Sync space offset ==============================//
    [Command(requiresAuthority = false)]
    void CmdInitializeSpaceOffset(Vector3 new_space_offset, Quaternion new_quaternion_offset)
    {
        space_offset = new_space_offset;
        quaternion_offset = new_quaternion_offset;
    }

    public void ChangeSpaceOffset()
    {
        if (STATE == PLACE_COMPLETE)
        {
            Vector3 localXAxis = FrameHandler.corners[1] - FrameHandler.corners[0];
            Vector3 localYAxis = FrameHandler.corners[2] - FrameHandler.corners[0];
            Quaternion localFrameQuaternion = Quaternion.LookRotation(-Vector3.Cross(localXAxis, localYAxis), -localYAxis);
            quaternion_offset = Quaternion.Inverse(localFrameQuaternion) * RemoteVideoPlane.GetComponent<Transform>().rotation;

            Vector3 localFrameCenter = (FrameHandler.corners[0] + FrameHandler.corners[1] + FrameHandler.corners[2] + FrameHandler.corners[3]) / 4.0f;
            space_offset = LocalVideoPlayer.InverseTransformPoint(RemoteVideoPlane.GetComponent<Transform>().position);

            CmdChangeSpaceOffset(NetworkClient.localPlayer.GetComponent<NetworkIdentity>(), space_offset, quaternion_offset);
        }
    }

    [Command(requiresAuthority = false)]
    void CmdChangeSpaceOffset(NetworkIdentity originId, Vector3 new_space_offset, Quaternion new_quaternion_offset)
    {
        space_offset = new_space_offset;
        quaternion_offset = new_quaternion_offset;
        foreach (NetworkConnectionToClient netid in NetworkServer.connections.Values)
        {
            if (netid != originId.connectionToClient)
            {
                TargetApplySpaceOffset(netid, new_space_offset, new_quaternion_offset);
            }
        }
    }

    [TargetRpc]
    void TargetApplySpaceOffset(NetworkConnection netid, Vector3 new_space_offset, Quaternion new_quaternion_offset)
    {
        Vector3 localXAxis = FrameHandler.corners[1] - FrameHandler.corners[0];
        Vector3 localYAxis = FrameHandler.corners[2] - FrameHandler.corners[0];
        Quaternion localFrameQuaternion = Quaternion.LookRotation(-Vector3.Cross(localXAxis, localYAxis), -localYAxis);
        RemoteVideoPlane.GetComponent<Transform>().rotation = localFrameQuaternion * new_quaternion_offset;

        Vector3 localFrameCenter = (FrameHandler.corners[0] + FrameHandler.corners[1] + FrameHandler.corners[2] + FrameHandler.corners[3]) / 4.0f;
        Vector3 world_space_offset = LocalVideoPlayer.TransformPoint(new_space_offset);
        RemoteVideoPlane.GetComponent<Transform>().position = world_space_offset;
        
        Debug.Log("Remote Space Reset!");
    }

    //===============================   Notify set local ==============================//
    public void NotifySetLocal()
    {
        CmdNotifySetLocal(NetworkClient.localPlayer.GetComponent<NetworkIdentity>());
    }

    [Command(requiresAuthority = false)] // set local starts, let the other user enter remote mode
    void CmdNotifySetLocal(NetworkIdentity originId)
    {
        foreach (NetworkConnectionToClient netid in NetworkServer.connections.Values)
        {
            if (netid != originId.connectionToClient)
            {
                TargetWaitRemote(netid);
            }
        }
    }

    [TargetRpc]
    void TargetWaitRemote(NetworkConnection targetConnection)
    {
        Debug.Log("Peer setting local, switch to remote placement!");
        STATE = PLACE_REMOTE;
    }
    //================================================================================//


    //===============================   Sync Width And Height ==============================//
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
    //================================================================================//

    //[TargetRpc]
    //void TargetSync(NetworkConnection targetConnection, bool remoteSetSuccess)
    //{
    //    remoteSet = remoteSetSuccess;
    //}
}
