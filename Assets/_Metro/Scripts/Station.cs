using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;

public enum StationType {
    Sphere,
    Cone,
    Cube
}

public class Station : MonoBehaviour, IMixedRealityPointerHandler
{

    public StationType type;
    public Vector3 position {
        get { return transform.position; }
        set { transform.position = value;}
    }
    public float timer = 0.0f; // max 45 seconds for animation + 2s grace period

    public List<Passenger> passengers;

    // Reference to attached Lines for easier pathfinding along lines
    public List<TransportLine> lines;


    // public List<GameObject> passengerObject;
    private Image[] seats;


    static bool dragging = false; 

    // Start is called before the first frame update
    void Start()
    {
        seats = gameObject.GetComponentsInChildren<Image>(true);
    }

    // Update is called once per frame
    void Update()
    {
        // show passengers
        foreach(var s in seats) s.enabled = false;
        if(passengers.Count > 0){
            seats[0].enabled = true;
            var dir = Vector3.Normalize(Camera.main.transform.position - transform.position);
            var quat = Quaternion.LookRotation(dir);
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
    }

    public void SpawnRandomPassenger(){
        // TODO implement better way to set probabilities
        var pSphere = type == StationType.Sphere ? 0.0f : 0.55f;
        var pCone = type == StationType.Cone ? 0.0f : 0.55f;
        var pCube = type == StationType.Cube ? 0.0f : 0.45f;
        
        var p = Random.value;

        if(p < pSphere){
            SpawnPassenger(StationType.Sphere);

        } else if(p < pSphere + pCone){
            SpawnPassenger(StationType.Cone);

        } else if(p < pSphere + pCone + pCube){
            SpawnPassenger(StationType.Cube);

        }
    }

    public void SpawnPassenger(StationType type){

        Passenger p = new Passenger();
        p.destination = type;
        passengers.Add(p);
       
    }


    void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData){
    
    }
    
    void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData){
    
    }

    void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData){
        // if first drag and no lines already connected, spawn next line
        // Debug.Log("drag");

    }

    void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData){
    
        // Line selected
        // Add Station to selectedLines next index
        if(MetroManager.selectedLine != null){
            Debug.Log("Selected line addStation");
            MetroManager.selectedLine.AddStation(this);

        } else {
            // No line selected and no lines attached
            // SelectFreeLine and place        
            if(lines.Count == 0){
                Debug.Log("Select free line, addStation");
                var line = MetroManager.SelectFreeLine();
                if(line != null){
                    line.AddStation(this);
                } // else TODO visualize no free lines somehow
            }

            // No line selected and one line attached? multiple lines?
        }
    }

    
}
