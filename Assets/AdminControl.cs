using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AdminControl : MonoBehaviour
{
    [SerializeField] MetroManager metroManager;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)){
            foreach (var game in metroManager.games){
                game.SetPaused(!game.paused);
            }
        }
        
        
    }
}
