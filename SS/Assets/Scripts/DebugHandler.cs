using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugHandler : MonoBehaviour
{
    public TMP_Text DebugMessageComp;
    private string msgToDisplay = "Debug Messages Start";
    private bool msgUpdated = false;
    //private string prevMsgDisplayed;
    private readonly object logMsgLock = new object();
    // Start is called before the first frame update
    void Start()
    {

    }

    private void OnEnable()
    {
        Application.logMessageReceivedThreaded += LogMessage;
        Debug.Log("DebugHandlerEnabled");
    }

    private void OnDisable()
    {
        Application.logMessageReceivedThreaded -= LogMessage;
    }

    public void LogMessage(string message, string stackTrace, LogType type)
    {
        lock (this.logMsgLock)
        {
            if (!message.Contains("Received SDP message:"))
            {
                this.msgToDisplay = message + "\n" + this.msgToDisplay;
                if (msgToDisplay.Length > 1000)
                {
                    this.msgToDisplay = this.msgToDisplay.Substring(0, 1000);
                }
                this.msgUpdated = true;
                // Debug.Log(this.msgToDisplay);
            }
        }
    }

    void Update()
    {
        //if (this.msgToDisplay.Length != this.prevMsgDisplayed.Length && !this.msgToDisplay.Equals(this.prevMsgDisplayed))
        //{
        //    this.DebugMessageComp.text = this.msgToDisplay;
        //    this.prevMsgDisplayed = this.msgToDisplay;
        //}
        lock (this.logMsgLock)
        {
            if (this.msgUpdated)
            {
                this.DebugMessageComp.text = this.msgToDisplay;
            }
            this.msgUpdated = false;
        }
    }
}
