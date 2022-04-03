using AsyncIO;
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
                    string[] message_parsed = message.Split(',');
                    WorldCursor.remote_cursor_x = float.Parse(message_parsed[0]);
                    WorldCursor.remote_cursor_y = float.Parse(message_parsed[1]);
                    WorldCursor.remote_cursor_clicked = int.Parse(message_parsed[2]);

                    // DEBUG
                    //Debug.Log(WorldCursor.remote_cursor_x);
                    //Debug.Log(WorldCursor.remote_cursor_y);
                    //Debug.Log(WorldCursor.remote_cursor_clicked);
                }
            }
        }

        NetMQConfig.Cleanup(); // this line is needed to prevent unity freeze after one use, not sure why yet
    }
}