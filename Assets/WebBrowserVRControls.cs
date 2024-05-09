using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoltstroStudios.UnityWebBrowser;
using VoltstroStudios.UnityWebBrowser.Core;

public class WebBrowserVRControls : MonoBehaviour
{
    [SerializeField] BaseUwbClientManager clientManager;

    private WebBrowserClient client;

    // Start is called before the first frame update
    void Start()
    {
        client = clientManager.browserClient;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
