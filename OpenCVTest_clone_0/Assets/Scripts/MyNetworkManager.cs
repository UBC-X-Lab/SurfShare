using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MyNetworkManager : NetworkManager
{
    public bool isServer = false;

    public override void Start()
    {
        if (isServer)
        {
            StartHost();
        }
        else
        {
            StartClient();
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("Start Server");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Start Client");
    }
}
