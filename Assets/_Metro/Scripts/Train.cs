using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Fusion;
using System.Linq;

public class Train : NetworkBehaviour, IMixedRealityPointerHandler
{
    [Networked] public int uuid { get; set; }
    public float position; // index position along line
    public float speed;
    public float direction;
    public int cars = 0;
    public int nextStop = 0;

    [Networked, Capacity(8)] public NetworkArray<Passenger> passengers => default;
    [Networked] public int passengerCount { get; set; } = 0;

    public Color color;

    private Image[] seats;

    public TransportLine line = null;
    public bool shouldRemove = false;

    private GameObject prefab;
    private GameObject train;
    public GameObject ghost = null;


    [Networked] public MetroGame gameInstance { get; set; }


    public List<string> passengerRoutes = new List<string>();
    // Start is called before the first frame update
    public override void Spawned()
    {
        /*
        prefab = Resources.Load("Prefabs/Train") as GameObject;
        train = GameObject.Instantiate(prefab, new Vector3(0,0,0), prefab.transform.rotation) as GameObject;
        train.transform.SetParent(this.gameObject.transform, false);
        */
        train = this.gameObject;
        SetColor(color);

        // seat[0] is image frame..
        seats = train.GetComponentsInChildren<Image>(true);
    }

    public override void Render()
    {
        DisplayPickedupPassengers();
    }

    // Update is called once per frame
    public override void FixedUpdateNetwork()
    {

        passengerRoutes.Clear();

        for (int i = 0; i < passengerCount; i++)
        {
            var p = passengers[i];
            string s = "";
            for (int j = 0; j < p.routeCount; j++)
            {
                var r = Runner.FindObject(p.routes[j]);
                s += r.GetComponent<Station>().id + " ";
            }
            passengerRoutes.Add(s);
        }

        for (int i = 0; i < passengerCount; i++)
        {
            var p = passengers[i];
            p.travelTime += Runner.DeltaTime * gameInstance.gameSpeed;
            passengers.Set(i, p);
        }
        if (line == null)
        {
            return;
        }
        if (line.stopCount <= 1)
        {
            return;
        }




        this.gameObject.transform.position = line.tracks.GetPosition(position);
        var v = line.tracks.GetVelocity(position);
        this.gameObject.transform.rotation = Quaternion.LookRotation(v);
        float speedMult = line.tracks.GetTrainDistanceSpeedMultiplier(position, 5.0f);



        // var factor = v.magnitude;
        // if(factor == 0.0f) factor = 1.0f;
        speed = 0.15f * gameInstance.dt * speedMult; /// 60.0f; // / factor;

        position += direction * speed;
        if (position <= 0.0f)
        {
            position = 0.0f;
            direction = 1.0f;
        }
        else if (position >= 1.0f)
        {
            position = 1.0f;
            direction = -1.0f;
        }

        var d = position * (line.stopCount - 1);
        var closestStopIndex = System.Math.Round(d);
        // var dist = System.Math.Abs(d - closestStopIndex);
        var dist = direction * (closestStopIndex - d); // if overshot then negative
        if (dist < 0.05)
        {
            if (nextStop >= line.stopCount || nextStop < 0)
            {
                nextStop = (int)closestStopIndex;
            }
            if (nextStop == closestStopIndex)
            {
                // Debug.Log("stop " + nextStop);

                var station = line.stops[nextStop];

                if (closestStopIndex == line.stopCount - 1) nextStop -= 1;
                else if (closestStopIndex == 0) nextStop += 1;
                else nextStop += (int)direction;

                var nextStation = line.stops[nextStop];

                PassengerDropWithRoute(station, nextStation);
                if (shouldRemove)
                {
                    DropAllPassengers(station);

                    this.gameInstance.freeTrains++;
                    // this.line.trains.Remove(this);
                    line.trainCount = FusionUtils.Remove(line.trains, line.trainCount, this);
                    Runner.Despawn(this.Object);
                    return;
                }
                PassengerPickupWithRoute(station, nextStation);
            }

        }
    }

    void DropAllPassengers(Station station)
    {
        for (int i = passengerCount - 1; i >= 0; i--)
        {
            var p = passengers[i];
            if (p.destination == station.type)
            {
                // passengers.RemoveAt(i);
                passengerCount = FusionUtils.RemoveAt(passengers, passengerCount, i);
                continue;
            }
            // if station is on route, but the next Station is not, drop the passenger (add back to station!)
            if (p.routeCount > 0)
            {
                // passengers.RemoveAt(i);
                passengerCount = FusionUtils.RemoveAt(passengers, passengerCount, i);
                station.passengerCount = FusionUtils.Add(station.passengers, station.passengerCount, p);
                continue;
            }
        }
    }

    public void SwitchLine(TransportLine nextLine)
    {
        if (this.line.trainCount <= 1)
        {
            return;
        }
        this.gameInstance.freeTrains++;
        nextLine.AddTrain(0.0f, 1.0f);

        // Destroy(this.gameObject);
        Runner.Despawn(this.Object);
        Destroy(ghost);
    }

    void DisplayPickedupPassengers()
    {
        // show passengers
        foreach (var s in seats) s.enabled = false;
        if (passengerCount > 0)
        {
            seats[0].enabled = true;
            var dir = Vector3.Normalize(Camera.main.transform.position - transform.position);
            var quat = Quaternion.LookRotation(dir, Camera.main.transform.up);
            seats[0].transform.parent.rotation = quat;
        }
        for (int i = 0; i < passengerCount; i++)
        {
            seats[i + 1].enabled = true;
            var dest = passengers[i].destination;
            if (dest == StationType.Cube)
                seats[i + 1].sprite = Resources.Load<Sprite>("Images/square");
            else if (dest == StationType.Cone)
                seats[i + 1].sprite = Resources.Load<Sprite>("Images/triangle");
            else if (dest == StationType.Sphere)
                seats[i + 1].sprite = Resources.Load<Sprite>("Images/circle");
            else if (dest == StationType.Star)
                seats[i + 1].sprite = Resources.Load<Sprite>("Images/star");
        }
    }

    int PassengerDropWithRoute(Station station, Station nextStation)
    {
        var count = 0;
        var scoreCount = 0;
        float waitTime = 0;
        float travelTime = 0;
        for (int i = passengerCount - 1; i >= 0; i--)
        {
            var p = passengers[i];

            // if destination, drop (delete) the passenger
            if (p.destination == station.type)
            {
                // passengers.RemoveAt(i);
                passengerCount = FusionUtils.RemoveAt(passengers, passengerCount, i);
                count += 1;
                // only this get score
                scoreCount += 1;
                waitTime += p.waitTime;
                travelTime += p.travelTime;
                continue;
            }


            // if station is on route, but the next Station is not, drop the passenger (add back to station!)
            if (p.routeCount > 0)
            {
                // Debug: print all routes
                // string s = "";
                // for (int j = 0; j < p.routeCount; j++)
                // {
                //     var r = Runner.FindObject(p.routes[j]);
                //     s += r.GetComponent<Station>().id + " ";
                // }
                // Debug.Log("[PassengerDropWithRoute:251] Passenger route: " + s);

                if (Runner.FindObject(p.routes[0]) != station)
                {
                    Debug.Log("Error dropping off passenger, arrived at unexpected station, recomputing route");
                    // p.routes = gameInstance.FindRouteClosest(station, p.destination);
                    var next = gameInstance.FindRouteClosest(station, p.destination);
                    var nextIds = next.Select(x => x.Object.Id).ToList();
                    p.routes.CopyFrom(nextIds, 0, nextIds.Count);
                    p.routeCount = nextIds.Count;
                }
                else
                {
                    // p.route.RemoveAt(0);
                    p.routeCount = FusionUtils.RemoveAt(p.routes, p.routeCount, 0);
                    // Debug.Log("[PassengerDropWithRoute:257] Remove station from route"); 
                }
                if (p.routeCount <= 0 || Runner.FindObject(p.routes[0]) != nextStation)
                {
                    // passengers.RemoveAt(i);
                    passengerCount = FusionUtils.RemoveAt(passengers, passengerCount, i);
                    // station.passengers.Add(p); // add back
                    station.passengerCount = FusionUtils.Add(station.passengers, station.passengerCount, p);
                    // remove the station from the route
                    // p.route.Remove(station); // this should be at index 0 (hopefully)
                    p.routeCount = FusionUtils.Remove(p.routes, p.routeCount, station.Object.Id);
                    count += 1;
                    // Debug.Log("[PassengerDropWithRoute:271] Dropped off passenger at station " + station.id);
                    continue;
                }
            }
        }

        gameInstance.AddScore(scoreCount);
        gameInstance.passengersDelivered += scoreCount;
        gameInstance.totalPassengerWaitTime += waitTime;
        gameInstance.totalPassengerTravelTime += travelTime;

        return count;
    }

    void PassengerPickupWithRoute(Station station, Station nextStation)
    {
        // In-place NetworkArray modification
        for (int i = station.passengerCount - 1; i >= 0; i--)
        {
            var p = station.passengers[i];

            if (passengerCount < 6 + 6 * cars) // Adjust capacity check logic as needed
            {
                // Check if the passenger's route is valid
                if (p.routeCount > 0)
                {
                    // If the passenger's route contains the next station, pick them up
                    // if (p.route.Contains(nextStation))
                    if (FusionUtils.Contains(p.routes, p.routeCount, nextStation.Object.Id))
                    {
                        // passengers.Add(p);
                        passengerCount = FusionUtils.Add(passengers, passengerCount, p);
                        station.passengerCount = FusionUtils.RemoveAt(station.passengers, station.passengerCount, i);
                        continue;
                    }
                }
            }
        }

    }

    int PassengerDrop(Station station)
    {

        // In-place NetworkArray modification
        var dropCount = 0;
        float waitTime = 0;
        float travelTime = 0;

        // Iterate backward to avoid index shifting while removing items
        for (int i = passengerCount - 1; i >= 0; i--)
        {
            var p = passengers[i];

            if (p.destination == station.type)
            {
                dropCount += 1;
                waitTime += p.waitTime;
                travelTime += p.travelTime;

                // Remove the passenger from the list since they have reached the destination
                // passengers.RemoveAt(i);
                passengerCount = FusionUtils.RemoveAt(passengers, passengerCount, i);
            }
        }

        // Update game instance metrics
        gameInstance.AddScore(dropCount);
        gameInstance.totalPassengerWaitTime += waitTime;
        gameInstance.totalPassengerTravelTime += travelTime;

        return 0; //todo animate?
    }

    void PassengerPickup(Station station)
    {
        // In-place NetworkArray modification
        for (int i = station.passengerCount - 1; i >= 0; i--)
        {
            var p = station.passengers[i];

            if (passengerCount < 6 + 6 * cars)
            {
                // passengers.Add(p);
                passengerCount = FusionUtils.Add(passengers, passengerCount, p);
                station.passengerCount = FusionUtils.RemoveAt(station.passengers, station.passengerCount, i);
            }
        }
    }


    public void AddCar()
    {

    }

    public void RemoveCar()
    {

    }

    public void SetColor(Color color)
    {
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
        gameInstance.selectedTrain = this;
        //SpawnGhost Train to follow cursur
        GameObject prefab = Resources.Load("Prefabs/SelectedTrainIndicator") as GameObject;
        ghost = Instantiate(prefab);
        var indicator = ghost.GetComponent<SelectedTrainIndicator>();
        indicator.gameInstance = this.gameInstance;
        indicator.targetTrain = this;
        indicator.SetColor(this.color);

    }



}
