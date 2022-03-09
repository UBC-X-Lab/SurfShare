using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.WebRTC.Unity;

public class Main : MonoBehaviour
{
    public NodeDssSignaler myNodeDssSignaler;

    private PeerConnection myPeerConnection;

    private string localPeerId;

    private string remotePeerId;

    // Start is called before the first frame update
    void Start()
    {
        myPeerConnection = myNodeDssSignaler.PeerConnection;
        localPeerId = myNodeDssSignaler.LocalPeerId;
        remotePeerId = myNodeDssSignaler.RemotePeerId;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartConnection() {
        if (!string.IsNullOrEmpty(localPeerId) && !string.IsNullOrEmpty(remotePeerId))
        {
            myPeerConnection.StartConnection();
        }
        else {
            Debug.Log("[START CONNECTION] peer ids not provided!");
        }
    }
}
