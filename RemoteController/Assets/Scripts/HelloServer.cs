using UnityEngine;

public class HelloServer : MonoBehaviour
{
    private HelloResponser _helloRequester;
    private void Start()
    {
        _helloRequester = new HelloResponser();
        _helloRequester.Start();
    }
    private void OnDestroy()
    {
        _helloRequester.Stop();
    }
}