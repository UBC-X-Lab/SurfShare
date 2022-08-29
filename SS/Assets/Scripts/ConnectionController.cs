using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.WebRTC.Unity;

public class ConnectionController : MonoBehaviour
{
    public PeerConnection myPeerConnection;
    public NodeDssSignaler myNodeDssSignaler;

    private string localPeerId;
    private string remotePeerId;

    private bool connection_started = false;

    // Start is called before the first frame update
    void Start()
    {
        this.localPeerId = this.myNodeDssSignaler.LocalPeerId;
        this.remotePeerId = this.myNodeDssSignaler.RemotePeerId;
    }

    public void StartConnection()
    {
        if (RemoteSpaceControl.STATE == RemoteSpaceControl.PLACE_COMPLETE)
        {
            if (!string.IsNullOrEmpty(localPeerId) && !string.IsNullOrEmpty(remotePeerId))
            {
                if (!connection_started)
                {
                    connection_started = true;
                    myPeerConnection.StartConnection();
                    Debug.Log("Starting connection...");
                }
                else
                {
                    Debug.Log("Connection has already established!");
                }
            }
            else
            {
                Debug.Log("[START CONNECTION] peer ids not provided!");
            }
        }
    }
}
