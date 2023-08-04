using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro.EditorUtilities;

public class RemoteSpaceControl : NetworkBehaviour
{
    public static bool localSet = false;
    private float localWidth;
    private float localHeight;

    public static bool remoteSet = false;
    public static float remoteWidth = 0.5f;
    public static float remoteHeight = 0.3f;

    public bool setRemoteVideoPlane = false;
    public static Vector3[] RemoteCorners = new Vector3[4];
    public GameObject RemoteVideoPlane;
    public GameObject RemoteVideoFrame;
    public GameObject FrameUpIndicator;

    // STATES (Note this implementation is not atomic! Make sure the users communicate before hand)
    public static readonly int PLACE_LOCAL = 0, PLACE_REMOTE = 1, PLACE_COMPLETE = 2;
    public static int STATE = PLACE_LOCAL;

    // public int DEBUG_STATE;

    // frame placement
    public Transform LocalVideoPlayer;
    Vector3 space_offset = new Vector3(0, 0, 0); // from remote to me
    Quaternion quaternion_offset = new Quaternion();

    // world placement
    public Transform MyWorldOrigin;
    public Transform PeerWorldOrigin;

    [SyncVar]
    bool SetupWorld = true;

    Vector3 world_offset = new Vector3(0, 0, 0);
    Quaternion world_quaternion_offset = new Quaternion();
    bool PeerWorldSet = false;

    static public bool remoteSpaceSetupCompleted = false;

    public Transform Menu;




    // Start is called before the first frame update
    void Start()
    {
        FrameUpIndicator.GetComponent<LineRenderer>().SetWidth(0.005f, 0.005f);
        FrameUpIndicator.GetComponent<LineRenderer>().positionCount = 2;
    }

    // Update is called once per frame
    void Update()
    {
        // DEBUG_STATE = STATE;

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
            LocalVideoPlayer.localScale = new Vector3(localXAxis.magnitude, localYAxis.magnitude, 1);

            // Setup world position
            world_offset = LocalVideoPlayer.InverseTransformPoint(MyWorldOrigin.position);
            world_quaternion_offset = Quaternion.Inverse(LocalVideoPlayer.GetComponent<Transform>().rotation) * MyWorldOrigin.rotation; // from localvideo to world

            // setup menu position
            Menu.gameObject.SetActive(true);
            Menu.position = (FrameHandler.corners[2] + FrameHandler.corners[3] + localYAxis) / 2 + Vector3.Cross(localXAxis, localYAxis).normalized * 0.05f;
            Menu.rotation = LocalVideoPlayer.rotation;
            Menu.SetParent(LocalVideoPlayer, true);

            // set the lines to local space so they move with the portal
            foreach (LineRenderer lr in FrameHandler.lineRenderers)
            {
                Vector3 local_start = LocalVideoPlayer.InverseTransformPoint(lr.GetPosition(0));
                Vector3 local_end = LocalVideoPlayer.InverseTransformPoint(lr.GetPosition(1)) ;

                lr.useWorldSpace = false;
                lr.SetPosition(0, local_start);
                lr.SetPosition(1, local_end);
            }

            CmdSync(NetworkClient.localPlayer.GetComponent<NetworkIdentity>(), localWidth, localHeight); // this toggles peer's remote set

            if (setRemoteVideoPlane) // second user calls this
            {
                ChangeSpaceOffset();
                STATE = PLACE_COMPLETE;
                Debug.Log("Client setup complete!");
            }
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
            }
        }

        if (STATE == PLACE_COMPLETE && SetupWorld && !PeerWorldSet)
        {
            PeerWorldSet = true;
            CmdSetupPeerWorld(NetworkClient.localPlayer.GetComponent<NetworkIdentity>(), world_offset, world_quaternion_offset);
        }
    }

    void SetRemoteVideo(Vector3[] RemoteCorners = null, bool matchCenter = false)
    {
        // remote corners are null for the first user, so set automatically
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
            RemoteSpaceControl.RemoteCorners = RemoteCorners;
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


    //===============================   Setup Peer World ==============================//
    //public void TogglePeerWorldSetup()
    //{
    //    Debug.Log("Local Called Toggle!");
    //    CmdTogglePeerWorldSetup();
    //}

    [Command(requiresAuthority = false)]
    public void CmdTogglePeerWorldSetup()
    {
        // Debug.Log("Server called toggle!");
        SetupWorld = true;
    }

    [Command(requiresAuthority = false)]
    void CmdSetupPeerWorld(NetworkIdentity originId, Vector3 new_world_position, Quaternion new_world_rotation)
    {
        foreach (NetworkConnectionToClient netid in NetworkServer.connections.Values)
        {
            if (netid != originId.connectionToClient)
            {
                TargetSetupPeerWorld(netid, new_world_position, new_world_rotation);
            }
        }
    }

    [TargetRpc]
    void TargetSetupPeerWorld(NetworkConnection originId, Vector3 new_world_position, Quaternion new_world_rotation)
    {
        // the relative global offsets are directly the local position and rotation
        PeerWorldOrigin.localPosition = new_world_position;
        PeerWorldOrigin.localRotation = new_world_rotation;

        PeerWorldOrigin.SetParent(null, true);

        remoteSpaceSetupCompleted = true;
        //Debug.Log("Peer to frame distance:" + (PeerWorldOrigin.position - PeerWorldOrigin.parent.position).magnitude);
        Debug.Log("Peer World Set!");
    }


    ////===============================   Sync local (their remote) portal ==============================//
    //public void LocalPortalMoved() // should only allow movement after initial setup is completed
    //{
    //    Debug.Log("Local Portal Moved!");
    //    // update the global coordinates of the corners
    //    LineRenderer topBorder = FrameHandler.lineRenderers[0];
    //    LineRenderer bottomBorder = FrameHandler.lineRenderers[3];

    //    FrameHandler.corners[0] = LocalVideoPlayer.TransformPoint(topBorder.GetPosition(0));
    //    FrameHandler.corners[1] = LocalVideoPlayer.TransformPoint(topBorder.GetPosition(1));
    //    FrameHandler.corners[2] = LocalVideoPlayer.TransformPoint(bottomBorder.GetPosition(0));
    //    FrameHandler.corners[3] = LocalVideoPlayer.TransformPoint(bottomBorder.GetPosition(1));

    //    //Quaternion local_portal_quaternion_offset = Quaternion.Inverse(MyWorldOrigin.rotation) * LocalVideoPlayer.rotation;
    //    CmdLocalPortalUpdate(NetworkClient.localPlayer.GetComponent<NetworkIdentity>(), FrameHandler.corners[0], FrameHandler.corners[1], FrameHandler.corners[2], FrameHandler.corners[3]);
    //}

    //[Command(requiresAuthority = false)]
    //void CmdLocalPortalUpdate(NetworkIdentity originId, Vector3 new_corner_1, Vector3 new_corner_2, Vector3 new_corner_3, Vector3 new_corner_4)
    //{
    //    foreach (NetworkConnectionToClient netid in NetworkServer.connections.Values)
    //    {
    //        if (netid != originId.connectionToClient)
    //        {
    //            TargetSyncLocalPortal(netid, new_corner_1, new_corner_2, new_corner_3, new_corner_4);
    //        }
    //    }
    //}

    //[TargetRpc]
    //void TargetSyncLocalPortal(NetworkConnection netid, Vector3 new_corner_1, Vector3 new_corner_2, Vector3 new_corner_3, Vector3 new_corner_4)
    //{
    //    Vector3 corner1 = RemoteVideoFrame.transform.InverseTransformPoint(PeerWorldOrigin.position + PeerWorldOrigin.TransformVector(new_corner_1).normalized * new_corner_1.magnitude);
    //    Vector3 corner2 = RemoteVideoFrame.transform.InverseTransformPoint(PeerWorldOrigin.position + PeerWorldOrigin.TransformVector(new_corner_2).normalized * new_corner_2.magnitude);
    //    Vector3 corner3 = RemoteVideoFrame.transform.InverseTransformPoint(PeerWorldOrigin.position + PeerWorldOrigin.TransformVector(new_corner_3).normalized * new_corner_3.magnitude);
    //    Vector3 corner4 = RemoteVideoFrame.transform.InverseTransformPoint(PeerWorldOrigin.position + PeerWorldOrigin.TransformVector(new_corner_4).normalized * new_corner_4.magnitude);

    //    RemoteVideoFrame.GetComponent<LineRenderer>().SetPositions(new Vector3[] {corner1, corner2, corner4, corner3});
    //}

    //===============================   Sync space offset ==============================//

    public void ChangeSpaceOffset()
    {
        space_offset = RemoteVideoPlane.GetComponent<Transform>().InverseTransformPoint(LocalVideoPlayer.position);
        quaternion_offset = Quaternion.Inverse(RemoteVideoPlane.GetComponent<Transform>().rotation) * LocalVideoPlayer.rotation;

        CmdChangeSpaceOffset(NetworkClient.localPlayer.GetComponent<NetworkIdentity>(), space_offset, quaternion_offset);
    }

    [Command(requiresAuthority = false)]
    void CmdChangeSpaceOffset(NetworkIdentity originId, Vector3 new_space_offset, Quaternion new_quaternion_offset)
    {
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
        // Vector3 localFrameCenter = (FrameHandler.corners[0] + FrameHandler.corners[1] + FrameHandler.corners[2] + FrameHandler.corners[3]) / 4.0f;
        if (STATE == PLACE_REMOTE)
        {
            SetRemoteVideo(null, true);
            STATE = PLACE_COMPLETE;
        }
        Vector3 world_space_offset = LocalVideoPlayer.TransformPoint(new_space_offset);
        RemoteVideoPlane.transform.position = world_space_offset;
        RemoteVideoPlane.transform.rotation = LocalVideoPlayer.rotation * new_quaternion_offset;
        Debug.Log("Remote Space Reset!");

        //if (STATE == PLACE_REMOTE)
        //{
        //    CmdTogglePeerWorldSetp(); // this marks that all setup are done, so we do not have message arrive out of order error;
        //    STATE = PLACE_COMPLETE;
        //}
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
