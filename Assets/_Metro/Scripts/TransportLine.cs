using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;


public class TransportLine : MonoBehaviour
{

    public bool isDeployed = false;
    public List<Station> stops = new List<Station>();
    // public List<TrackSegment> tracks = new List<TrackSegment>();
    public TrackSpline track = null;
    public TrackSpline tempTrack = null;
    public List<Train> trains = new List<Train>();

    public Color color;
    // public TrackSegment selectedTrack = null;
    public int nextIndex = 0;

    public int lengthOfLines = 20;


    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // if(selectedTrack != null)
        //     selectedTrack.end = MetroManager.Instance.cursor;

    }

    // append station as last stop on line
    public void AddStation(Station station){
        InsertStation(stops.Count, station);
    }
    
    // insert station as stop at arbitrary index
    public void InsertStation(int stopIndex, Station station){
        if(track == null){
            Debug.Log("create track spline");
            var go = new GameObject();
            go.name = "TrackSpline";
            var t = go.AddComponent<TrackSpline>();
            t.line = this;
            t.color = color;
            track = t;
        }
        if(this.stops.Contains(station)){
            // only allow if making looping line
            // TODO
            return;
        }
        Debug.Log("insert station");
        stops.Insert(stopIndex, station);
        track.points.Insert(stopIndex, station.transform.position);
        if(!station.lines.Contains(this)) station.lines.Add(this);
        isDeployed = true;
        if(stops.Count >= 2 && trains.Count == 0){
            AddTrain(0.0f,1.0f);
        }
    }

    public void RemoveStation(Station station){
        station.lines.Remove(this);
        stops.Remove(station);
        track.points.Remove(station.transform.position);

    }

    public void RemoveAll(){
        foreach(var s in stops){
            s.lines.Remove(this);
        }
        stops.Clear();
        track.points.Clear();
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

    public TrackSpline CreateTemporarySegment(GameObject obj){
        return null;
    }

    // public void UpdateTrackSegments(){
    //     Debug.Log("UpdateTrackSegments");
    //     for(int i=0; i < stops.Count - 1; i++){
    //         var a = stops[i];
    //         var b = stops[i+1];
    //         UpdateTrackSegment(i,a,b);
    //     }
    // }

    // public void UpdateTrackSegment(int index, Station a, Station b){
    //     if(index >= tracks.Count) AddTrackSegment(index);

    //     var track = tracks[index];
    //     track.editing = true;
    //     track.start = a.transform.position;
    //     track.cp1 = a.transform.position;
    //     track.end = b.transform.position;
    //     track.cp2 = b.transform.position;
    //     if(index > 0){
    //         track.cp1 = track.start + (track.start - tracks[index-1].start).normalized;
    //     }
    //     if(index < tracks.Count - 1){
    //         track.cp2 = track.end - (tracks[index+1].end - track.end).normalized;
    //     }

    // }

    // public void AddTrackSegment(int index){
    //     Debug.Log("AddTrackSegment");

    //     var go = new GameObject();
    //     var track = go.AddComponent<TrackSegment>();
    //     track.line = this;
    //     track.linkIndex = index;
    //     track.color = color;
    //     // selectedTrack = track;
    //     tracks.Insert(index, track);
    //     // GameObject.Instantiate(go, new Vector3(0,0,0), Quaternion.identity);
    // }


    
}
