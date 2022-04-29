using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.WebRTC.Unity;

public class ConnectionControl : MonoBehaviour
{
    public PeerConnection myPeerConnection;
    public NodeDssSignaler myNodeDssSignaler;

    private string localPeerId;
    private string remotePeerId;

    // Start is called before the first frame update
    void Start()
    {
        this.localPeerId = this.myNodeDssSignaler.LocalPeerId;
        this.remotePeerId = this.myNodeDssSignaler.RemotePeerId;
    }

    public void StartConnection()
    {
        if (!string.IsNullOrEmpty(localPeerId) && !string.IsNullOrEmpty(remotePeerId))
        {
            myPeerConnection.StartConnection();
        }
        else
        {
            Debug.Log("[START CONNECTION] peer ids not provided!");
        }
    }
}
