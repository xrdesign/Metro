using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectFollower : MonoBehaviour
{

    private GameObject target;
    private bool follow = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void LateUpdate()
    {
        if(follow && target){
            this.transform.position = target.transform.position;
            this.transform.rotation = target.transform.rotation;
        }
    }

    public void SetTarget(GameObject target){
        this.target = target;
    }

    public void SetFollow(bool follow){
        this.follow = follow;
    }
}
