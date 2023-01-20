using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MyNetWorkManager : NetworkManager
{
    public bool isServer = false;

    public bool isHost = false;

    public override void Start()
    {
        if (isServer)
        {
            singleton.StartServer();
        }
        else if (isHost)
        {
            singleton.StartHost();
        }
    }

    private void Update()
    {
        if (Main.WebRTCSetupComplete && !isServer)
        {
            singleton.StartClient();
            Main.WebRTCSetupComplete = false;
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
