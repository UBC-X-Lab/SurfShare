using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;
using System.Threading;

/// <summary>
///     Example of requester who only sends Hello. Very nice guy.
///     You can copy this class and modify Run() to suits your needs.
///     To use this class, you just instantiate, call Start() when you want to start and Stop() when you want to stop.
/// </summary>
public class HelloResponser : RunAbleThread
{
    /// <summary>
    ///     Request Hello message to server and receive message back. Do it 10 times.
    ///     Stop requesting when Running=false.
    /// </summary>
    /// 
    protected override void Run()
    {
        ForceDotNet.Force(); // this line is needed to prevent unity freeze after one use, not sure why yet
        using (ResponseSocket server = new ResponseSocket())
        {
            server.Bind("tcp://*:8000");

            while (Running)
            {
                string message = null;
                bool gotMessage = false;
                while (Running)
                {
                    gotMessage = server.TryReceiveFrameString(out message); // this returns true if it's successful
                    if (gotMessage) break;
                }

                // parse message
                if (gotMessage)
                {
                    //Debug.Log("Message received: " + message);
                    string toSend = "";
                    lock (Paintable.myLock)
                    {
                        while (Paintable.buffer.Count > 0)
                        {
                            toSend += Paintable.buffer.Dequeue() + ";";
                        }
                    } 
                    server.SendFrame(toSend.TrimEnd(';'));
                    //Debug.Log(toSend);
                }
            }
        }

        NetMQConfig.Cleanup(); // this line is needed to prevent unity freeze after one use, not sure why yet
    }
}