using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RemoteFrameControl : NetworkBehaviour
{
    private float localWidth;
    private float localHeight;

    private float remoteWidth;
    private float remoteHeight;

    private bool localSet = false;
    private bool remoteSet = false;
    
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
