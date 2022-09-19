using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Train : MonoBehaviour
{

    public float position; // index position along line
    public float speed;
    public float direction;
    public int cars = 0;
    public int nextStop = 0;
    public List<Passenger> passengers = new List<Passenger>();

    private Image[] seats;
    
    public TransportLine line;

    private GameObject prefab;
    private GameObject train;

    // Start is called before the first frame update
    void Start()
    {
        prefab = Resources.Load("Prefabs/Train") as GameObject;
        train = GameObject.Instantiate(prefab, new Vector3(0,0,0), prefab.transform.rotation) as GameObject;
        train.transform.SetParent(this.gameObject.transform, false);

        // seat[0] is image frame..
        seats = train.GetComponentsInChildren<Image>(true);
    }

    // Update is called once per frame
    void Update()
    {

        this.gameObject.transform.position = line.tracks.GetPosition(position);
        var v = line.tracks.GetVelocity(position);
        this.gameObject.transform.rotation = Quaternion.LookRotation(v);


        // show passengers
        foreach(var s in seats) s.enabled = false;
        if(passengers.Count > 0){
            seats[0].enabled = true;
            var dir = Vector3.Normalize(Camera.main.transform.position - transform.position);
            var quat = Quaternion.LookRotation(dir, Camera.main.transform.up);
            seats[0].transform.parent.rotation = quat;
        }
        for(int i = 0; i < passengers.Count; i++){
            seats[i+1].enabled = true;
            var dest = passengers[i].destination;
            if(dest == StationType.Cube)
                seats[i+1].sprite = Resources.Load<Sprite>("Images/square");
            else if(dest == StationType.Cone)
                seats[i+1].sprite = Resources.Load<Sprite>("Images/triangle");
            else if(dest == StationType.Sphere)
                seats[i+1].sprite = Resources.Load<Sprite>("Images/circle");
        }

        // var factor = v.magnitude;
        // if(factor == 0.0f) factor = 1.0f;
        speed = 0.15f * MetroManager.dt; /// 60.0f; // / factor;

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
        // var dist = System.Math.Abs(d - closestStopIndex);
        var dist = direction*(closestStopIndex - d); // if overshot then negative
        if(dist < 0.05 && nextStop == closestStopIndex){
            // Debug.Log("stop " + nextStop);

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
        var newList = new List<Passenger>();

        foreach(var p in station.passengers){
            if(passengers.Count < 6 + 6*cars){

                passengers.Add(p);
                

            }else
                newList.Add(p);
        }

        station.passengers = newList;
    }


    public void AddCar(){

    }

    public void RemoveCar(){

    }


    
}
