using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using Mirror;

public class ObjectSync : NetworkBehaviour
{
    [SyncVar]
    public bool manipulating = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AssignAuthority()
    {
        CmdAssignAuthority(GetComponent<NetworkIdentity>(), NetworkClient.localPlayer.GetComponent<NetworkIdentity>());
    }

    [Command(requiresAuthority = false)]
    void CmdAssignAuthority(NetworkIdentity myId, NetworkIdentity netId)
    {
        if (!manipulating)
        {
            myId.RemoveClientAuthority();
            if (myId.AssignClientAuthority(netId.connectionToClient))
            {
                manipulating = true;
            }
        }
    }

    // only the user with authority can set manipulating to false
    [Command]
    public void CmdManipulationEnd()
    {
        manipulating = false;
    }
}
