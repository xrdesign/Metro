using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Passenger {

    public StationType destination = StationType.Cube;
    public MetroGame gameInstance;
    public float waitTime;
    public float travelTime;

    public float totalTime;

    public List<Station> route = null;

    // Start is called before the first frame update
    void Start()
    {
        totalTime = 0;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("gameSpeed" + gameInstance.gameSpeed);
        totalTime += Time.deltaTime * gameInstance.gameSpeed;
    }

}
