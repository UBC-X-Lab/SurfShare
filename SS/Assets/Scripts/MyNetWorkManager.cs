using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MyNetWorkManager : NetworkManager
{
    public bool isServer = false;

    public override void Start()
    {
        if (isServer)
        {
            singleton.StartHost();
        }
        else
        {
            singleton.StartClient();
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
