using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugHandler : MonoBehaviour
{
    public TMP_Text DebugMessageComp;
    private string msgToDisplay;
    private string prevMsgDisplayed;
    // Start is called before the first frame update
    void Start()
    {
        //this.DebugMessageComp = this.GetComponent<TMP_Text>();
        //Debug.Log(this.DebugMessageComp.text);
        this.msgToDisplay = this.DebugMessageComp.text;
        this.prevMsgDisplayed = this.DebugMessageComp.text;
    }

    private void OnEnable()
    {
        Application.logMessageReceivedThreaded += LogMessage;
    }

    private void OnDisable()
    {
        Application.logMessageReceivedThreaded -= LogMessage;
    }

    public void LogMessage(string message, string stackTrace, LogType type)
    {
        //if (textMesh.text.Length > 300)
        //{
        //    textMesh.text = message + "\n";
        //}
        //else
        //{
        //    textMesh.text += message + "\n";
        //}
        if (this.DebugMessageComp != null)
        {
            if (!message.Contains("Received SDP message:"))
            {
                this.msgToDisplay = message + "\n" + this.msgToDisplay;
                if (msgToDisplay.Length > 5000)
                {
                    this.msgToDisplay = this.msgToDisplay.Substring(0, 5000);
                }
            }
        }
    }

    void Update()
    {
        if (this.msgToDisplay.Length != this.prevMsgDisplayed.Length && !this.msgToDisplay.Equals(this.prevMsgDisplayed))
        {
            this.DebugMessageComp.text = this.msgToDisplay;
            this.prevMsgDisplayed = this.msgToDisplay;
        }
    }
}
