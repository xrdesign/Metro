using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Train : MonoBehaviour
{

    public float position; // index position along line
    public float speed;
    public float direction;
    public int cars = 0;
    public int nextStop = 0;
    public List<Passenger> passengers = new List<Passenger>();
    
    public TransportLine line;

    private GameObject prefab;
    private GameObject train;

    // Start is called before the first frame update
    void Start()
    {
        prefab = Resources.Load("Prefabs/Train") as GameObject;
        train = GameObject.Instantiate(prefab, new Vector3(0,0,0), prefab.transform.rotation) as GameObject;
        // go.transform.position = new Vector3(x, y, 0);
        // go.transform.localScale = new Vector3(0.1f,0.1f,0.1f);
        // this.gameObject.transform.rotation = prefab.transform.rotation;        
        train.transform.SetParent(this.gameObject.transform, false);
    }

    // Update is called once per frame
    void Update()
    {

        this.gameObject.transform.position = line.track.GetPosition(position);
        var v = line.track.GetVelocity(position);
        this.gameObject.transform.rotation = Quaternion.LookRotation(v);

        // var factor = v.magnitude;
        // if(factor == 0.0f) factor = 1.0f;
        speed = 0.01f / 60.0f; // / factor;

        position += direction * speed;
        if(position <= 0.0f){
            position = 0.0f;
            direction = 1.0f;
        } else if(position >= 1.0f){
            position = 1.0f;
            direction = -1.0f;
        }

        var d = position * (line.stops.Count - 1);
        var closestStopIndex = System.Math.Round(d);
        var dist = System.Math.Abs(d - closestStopIndex);
        if(dist < 0.05 && nextStop == closestStopIndex){
            Debug.Log("stop " + nextStop);

            var station = line.stops[nextStop];
            PassengerDrop(station);
            PassengerPickup(station);

            if(closestStopIndex == line.stops.Count - 1) nextStop -= 1;
            else if(closestStopIndex == 0) nextStop += 1;
            else nextStop += (int)direction;


        }
    }

    int PassengerDrop(Station station){

        // passengers.RemoveAll(p => p.destination == station.type)
        var newList = new List<Passenger>();
        var dropCount = 0;
        foreach(var p in passengers){
            if(p.destination == station.type)
                dropCount += 1;
            else
                newList.Add(p);
        }

        passengers = newList;
        MetroManager.AddScore(dropCount);

        return 0; //todo animate?
    }

    void PassengerPickup(Station station){
        foreach(var p in station.passengers){
            if(passengers.Count < 6 + 6*cars){

                // if()

            }
        }
    }


    public void AddCar(){

    }

    public void RemoveCar(){

    }


    
}
