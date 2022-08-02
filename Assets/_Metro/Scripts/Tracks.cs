using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;


public class Tracks : MonoBehaviour
{

    public TransportLine line;

    // public List<Vector3> points = new List<Vector3>();
    public List<Vector3> cp = new List<Vector3>();
    public List<float> lengths = new List<float>();

    public TrackSegment head;
    public TrackSegment tail;
    public List<TrackSegment> segments = new List<TrackSegment>();
    public List<TrackSegment> ghostSegments = new List<TrackSegment>();

    public List<TrackSegment> uiSegments = new List<TrackSegment>(); 


    public bool needsUpdate = true;

    

    // Start is called before the first frame update
    void Start()
    {
        for(int i = 0; i < 2; i++){
            uiSegments.Add(CreateSegment(-10));
            // uiSegments[i].gameObject.SetActive(false);
        }
        head = CreateSegment(-1);
        // head.gameObject.SetActive(false);
        tail = CreateSegment(9999);
        // tail.gameObject.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {   
        if(!needsUpdate) return;
        // if(line.stops.Count == 0) return;

        UpdateControlPoints();
        UpdateSegments();
        UpdateLengths();

        needsUpdate = false;
    }

    void UpdateLengths(){
        lengths.Clear();
        for(int i = 0; i < line.stops.Count - 1; i++){
            var p0 = line.stops[i].transform.position;
            var p1 = line.stops[i+1].transform.position;

            lengths.Add(Vector3.Distance(p0,p1));
        }
    }

    void UpdateControlPoints(){
        cp.Clear();
        if(line.stops.Count == 0) return;

        cp.Add(line.stops[0].transform.position);

        for(int i = 0; i < line.stops.Count - 1; i++){
            var p0 = line.stops[i].transform.position;
            var p1 = line.stops[i+1].transform.position;

            // default line
            var cp0 = p0 + (p1-p0)*0.15f;
            var cp1 = p1 + (p0-p1)*0.15f;

            // look back to i-1 
            if(i > 0){ 
                var p00 = line.stops[i-1].transform.position;
                var v = Vector3.Normalize(p1 - p00);
                cp0 = p0 + v * 0.1f;

            }
            // look forward to i+2
            if(i < line.stops.Count - 2){
                var p2 = line.stops[i+2].transform.position;
                var v = Vector3.Normalize(p2 - p0);
                cp1 = p1 - v * 0.1f;
            }
            cp.Add(cp0);
            cp.Add(cp1);
            cp.Add(p1);
        }
    }

    void UpdateSegments(){
        foreach(var s in segments) GameObject.Destroy(s.gameObject);
        segments.Clear();

        for(int i = 0; i < line.stops.Count - 1; i++){
            var track = CreateSegment(i);

            track.cp[0] = cp[3*i];
            track.cp[1] = cp[3*i+1];
            track.cp[2] = cp[3*i+2];
            track.cp[3] = cp[3*i+3];
            track.needsUpdate = true;

            segments.Add(track);

        }
        if(line.stops.Count > 1){
            head.cp[0] = cp[0];
            head.cp[1] = cp[0];
            head.cp[2] = cp[0] - (cp[1]-cp[0]).normalized*0.15f;
            head.cp[3] = cp[0] - (cp[1]-cp[0]).normalized*0.15f;
            head.gameObject.SetActive(true);
            head.needsUpdate = true;
            tail.cp[0] = cp[cp.Count-1];
            tail.cp[1] = cp[cp.Count-1];
            tail.cp[2] = cp[cp.Count-1] - (cp[cp.Count-2]-cp[cp.Count-1]).normalized*0.15f;
            tail.cp[3] = cp[cp.Count-1] - (cp[cp.Count-2]-cp[cp.Count-1]).normalized*0.15f;
            tail.gameObject.SetActive(true);
            tail.index = segments.Count;
            tail.needsUpdate = true;
        } else {
            head.gameObject.SetActive(false);
            tail.gameObject.SetActive(false);
        }


    }

    TrackSegment CreateSegment(int index){
        var go = new GameObject();
        go.name = "Track";
        go.transform.SetParent(line.gameObject.transform);
        var track = go.AddComponent<TrackSegment>();
        track.line = line;
        track.index = index;
        return track;
    }

    public void UpdateUISegment(int i, Vector3 pos, int stopIndex){
        // Debug.Log("UpdateUISegment");
        // Debug.Log(i + " " + stopIndex);
        // Debug.Log(uiSegments.Count + " " + cp.Count);
        uiSegments[i].gameObject.SetActive(true);
        if(stopIndex == -1) stopIndex = 0;
        uiSegments[i].cp[0] = cp[3*stopIndex];
        uiSegments[i].cp[1] = cp[3*stopIndex];
        uiSegments[i].cp[2] = pos;
        uiSegments[i].cp[3] = pos;
        uiSegments[i].needsUpdate = true;

    }

    public void DisableUISegments(){
        foreach(var s in uiSegments)
            s.gameObject.SetActive(false);
    }

    public Vector3 GetPosition(float p){
        if(segments.Count == 0) return new Vector3(0f,-1000f,0f);
        var x = p * segments.Count;
        var i = (int)x;
        var v = x - i;
        Vector3 pos;
        if(i == segments.Count) pos = segments[segments.Count-1].Interpolate(1.0f);
        else pos = segments[i].Interpolate(v);
        return pos;
    }

    public Vector3 GetVelocity(float p){
        var x = p * segments.Count;
        var i = (int)x;
        var v = x - i;
        Vector3 vel;
        if(i == segments.Count) vel = segments[segments.Count-1].Derivative(1.0f);
        else vel = segments[i].Derivative(v);
        return vel;
    }

  



    
}
