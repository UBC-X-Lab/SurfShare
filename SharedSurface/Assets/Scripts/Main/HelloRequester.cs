﻿using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;

/// <summary>
///     Example of requester who only sends Hello. Very nice guy.
///     You can copy this class and modify Run() to suits your needs.
///     To use this class, you just instantiate, call Start() when you want to start and Stop() when you want to stop.
/// </summary>
public class HelloRequester : RunAbleThread
{
    /// <summary>
    ///     Request Hello message to server and receive message back. Do it 10 times.
    ///     Stop requesting when Running=false.
    /// </summary>
    /// 
    public string serverIP;
    public string serverPort;

    protected override void Run()
    {
        ForceDotNet.Force(); // this line is needed to prevent unity freeze after one use, not sure why yet
        using (RequestSocket client = new RequestSocket())
        {
            client.Connect("tcp://" + this.serverIP + ":" + this.serverPort);

            while (Running)
            {
                client.SendFrame("Hello");
                // only send non-ping message once

                string message = null;
                bool gotMessage = false;
                while (Running)
                {
                    gotMessage = client.TryReceiveFrameString(out message); // this returns true if it's successful
                    if (gotMessage) break;
                }

                // parse message
                if (gotMessage)
                {
                    string[] message_parsed;
                    if (message.Length > 0) // ignore empty messages
                    {
                        if (message.Contains(";"))
                        {
                            message_parsed = message.Split(';');
                        }
                        else
                        {
                            message_parsed = new string[1];
                            message_parsed[0] = message;
                        }

                        lock (WorldCursor.myLock)
                        {
                            foreach (string cursor_info in message_parsed) // cursor_x, cursor_y, cursor_state, r, g, b
                            {
                                WorldCursor.buffer.Enqueue(cursor_info);
                                //Debug.Log(cursor_info);
                            }
                        }
                    }
                }
            }
        }

        NetMQConfig.Cleanup(); // this line is needed to prevent unity freeze after one use, not sure why yet
    }
}