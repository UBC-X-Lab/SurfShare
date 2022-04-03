using UnityEngine;

public class HelloClient : MonoBehaviour
{
    public string serverIP;
    public string serverPort;
    private HelloRequester _helloRequester;
    private void Start()
    {
        _helloRequester = new HelloRequester();
        _helloRequester.serverIP = this.serverIP;
        _helloRequester.serverPort = this.serverPort;
        _helloRequester.Start();
    }
    private void OnDestroy()
    {
        _helloRequester.Stop();
    }
}