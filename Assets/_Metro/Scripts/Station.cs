using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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


    public List<GameObject> passengerObject;


    static bool dragging = false; 

    // Start is called before the first frame update
    void Start()
    {
        // SetupBody();
    }

    // Update is called once per frame
    void Update()
    {
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
        GameObject go;
        switch(type){
            case StationType.Sphere:
                var prefab = Resources.Load("Prefabs/Sphere");
                go = GameObject.Instantiate(prefab, new Vector3(0,0,0), Quaternion.identity) as GameObject;
                break;
            case StationType.Cone:
                prefab = Resources.Load("Prefabs/Cone");
                go = GameObject.Instantiate(prefab, new Vector3(0,0,0), Quaternion.identity) as GameObject;
                break;
            case StationType.Cube:
                prefab = Resources.Load("Prefabs/Cube");
                go = GameObject.Instantiate(prefab, new Vector3(0,0,0), Quaternion.identity) as GameObject;
                break;
            default:
                prefab = Resources.Load("Prefabs/Cub");
                go = GameObject.Instantiate(prefab, new Vector3(0,0,0), Quaternion.identity) as GameObject;
                break;
        }
        
        var n = passengers.Count;
        float x = 0.15f * n - 0.5f ;
        float y = 0.85f;

        go.transform.position = new Vector3(x, y, 0);
        go.transform.localScale = new Vector3(0.1f,0.1f,0.1f);
        go.transform.SetParent(this.gameObject.transform, false);


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
