using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.Input;

public class Train : MonoBehaviour, IMixedRealityPointerHandler
{
    public int uuid;
    public float position; // index position along line
    public float speed;
    public float direction;
    public int cars = 0;
    public int nextStop = 0;
    public List<Passenger> passengers = new List<Passenger>();
    public Color color;

    private Image[] seats;
    
    public TransportLine line = null;

    private GameObject prefab;
    private GameObject train;
    
    public MetroGame gameInstance;

    // Start is called before the first frame update
    void Start()
    {
        prefab = Resources.Load("Prefabs/Train") as GameObject;
        train = GameObject.Instantiate(prefab, new Vector3(0,0,0), prefab.transform.rotation) as GameObject;
        train.transform.SetParent(this.gameObject.transform, false);

        SetColor(color);

        // seat[0] is image frame..
        seats = train.GetComponentsInChildren<Image>(true);
    }

    // Update is called once per frame
    void Update()
    {

        if(line == null){

            return;
        }


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
        speed = 0.15f * gameInstance.dt; /// 60.0f; // / factor;

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

            if(closestStopIndex == line.stops.Count - 1) nextStop -= 1;
            else if(closestStopIndex == 0) nextStop += 1;
            else nextStop += (int)direction;

            var nextStation = line.stops[nextStop];

            PassengerDropWithRoute(station, nextStation);
            PassengerPickupWithRoute(station, nextStation);

        }
    }

    int PassengerDropWithRoute(Station station, Station nextStation)
    {
        var count = 0;
        var scoreCount = 0;
        for (int i = passengers.Count - 1; i >= 0; i--)
        {
            var p = passengers[i];

            // if destination, drop (delete) the passenger
            if (p.destination == station.type)
            {
                passengers.RemoveAt(i);
                count += 1;
                // only this get score
                scoreCount += 1;
                continue;
            }

            // if station is on route, but the next Station is not, drop the passenger (add back to station!)
            if (p.route != null)
            {
                if (p.route.Contains(station) && !p.route.Contains(nextStation)) // this should always be index 0 and index 1 though if no error
                {
                    passengers.RemoveAt(i);
                    station.passengers.Add(p); // add back
                    // remove the station from the route
                    p.route.Remove(station); // this should be at index 0 (hopefully)
                    count += 1;
                    continue;
                }
            }
        }

        gameInstance.AddScore(scoreCount);

        return count;
    }

    void PassengerPickupWithRoute(Station station, Station nextStation)
    {
        var newList = new List<Passenger>();

        foreach (var p in station.passengers)
        {
            if (passengers.Count < 6 + 6 * cars) // TODO: should use a better way to manage capacity check
            {
                
                // if the nextStation is in route, pick up the passenger
                if (p.route != null)
                {

                    // TODO: if the train can arrive one of the station onroute, we should try it as well
                    // especially when closest route is so full (wait after certain time)
                    if (p.route.Contains(nextStation))
                    {
                        passengers.Add(p);
                        continue;
                    }
                }
            }

            // if not pickup, then throw back to station
            newList.Add(p);
        }

        station.passengers = newList;
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
        gameInstance.AddScore(dropCount);

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

    public void SetColor(Color color){
        var material = train.transform.Find("train").GetComponent<Renderer>().materials[0];
        // material.color = color;
        material.SetColor("_BaseColor", color);
    }

    public void IsClicked()
    {
        Debug.Log("Train clicked");
    }


    void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData)
    {
        Debug.Log("Train pointer down");
        Debug.Log(eventData.Pointer.Position);

    }

    void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData)
    {
        Debug.Log("Train pointer up");

    }

    void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        Debug.Log("Train pointer dragging");

    }
    void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        Debug.Log("Train pointer clicked");
        //speed = 0;

    }



}
