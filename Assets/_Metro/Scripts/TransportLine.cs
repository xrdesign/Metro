using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;


public class TransportLine : MonoBehaviour
{
    public int id;
    public int uuid;
    public bool isDeployed = false;
    public List<Station> stops = new List<Station>();
    public List<Train> trains = new List<Train>();

    public Tracks tracks = null;


    public Color color;
    public int nextIndex = 0;



    // Start is called before the first frame update
    void Start()
    {
        var go = new GameObject();
        go.name = "Tracks";
        tracks = go.AddComponent<Tracks>();
        tracks.line = this;
    }

    // Update is called once per frame
    void Update()
    {
    }

    // append station as last stop on line
    public void AddStation(Station station){
        InsertStation(stops.Count, station);
    }
    
    // insert station as stop at arbitrary index
    public void InsertStation(int stopIndex, Station station){
        if(this.stops.Contains(station)){
            // only allow if making looping line
            // TODO
            return;
        }
        Debug.Log("insert station");
        stops.Insert(stopIndex, station);
        if(!station.lines.Contains(this)) station.lines.Add(this);
        isDeployed = true;
        if(stops.Count >= 2 && trains.Count == 0){
            AddTrain(0.0f,1.0f);
        }
        tracks.needsUpdate = true;
    }

    public void RemoveStation(Station station){
        station.lines.Remove(this);
        stops.Remove(station);
        tracks.needsUpdate = true;

    }

    public void RemoveAll(){
        foreach(var s in stops){
            s.lines.Remove(this);
        }
        stops.Clear();
        tracks.needsUpdate = true;
        foreach(var t in trains){
            Destroy(t.gameObject);
        }
        MetroManager.Instance.freeTrains += trains.Count;
        trains.Clear();
        isDeployed = false;
    }

    public void AddTrain(float position, float direction){
        if(MetroManager.Instance.freeTrains == 0) return;
        MetroManager.Instance.freeTrains -= 1;

        var go = new GameObject();
        go.name = "Train";
        var t = go.AddComponent<Train>();
        t.position = position;
        t.direction = direction;
        t.speed = 0.0f;
        t.line = this;
        t.uuid = MetroManager.Instance.GetInstanceID();

        trains.Add(t);
    }

    public Station FindDestination(int from, int direction, StationType type){
        var distance = 999;
        var index = -1;
        Station station = null;

        for(var i=0; i < stops.Count; i++){
            var stop = stops[i];
            var d = (i - from);

            if(stop.type == type && System.Math.Abs(d) < distance && d * direction > 0){
                station = stop;
                index = i;
                distance = d;
            }
        }
        return station;
    }

    public Station FindTransfer(int from, int direction, StationType type){
        return null;
    }





    
}
