using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RemoteFrame : NetworkBehaviour
{
    public float localWidth;
    public float localHeight;

    public float remoteWidth;
    public float remoteHeight;

    public bool Sync = false;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Sync)
        {
            Sync = false;
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
    }
}
