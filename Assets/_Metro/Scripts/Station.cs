using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Physics;

public enum StationType {
    Sphere,
    Cone,
    Cube
}

public class Station : MonoBehaviour, IMixedRealityPointerHandler, IMixedRealityFocusHandler
{

    public int id;
    public int uuid;
    public StationType type;
    public Vector3 position;
    public float timer = 0.0f; // max 45 seconds for animation + 2s grace period

    public List<Passenger> passengers = new List<Passenger>();

    // Reference to attached Lines for easier pathfinding along lines
    public List<TransportLine> lines;


    // public List<GameObject> passengerObject;
    private Image[] seats;


    static bool dragging = false; 
    bool firstDrag = false;

    public Image timerImage;

    float cooldown = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        seats = gameObject.GetComponentsInChildren<Image>(true);
    }

    // Update is called once per frame
    void Update()
    {
        position = transform.position;
        cooldown -= Time.deltaTime;

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

        // Update overcrowding status
        if(passengers.Count > 6){ 
            timer += MetroManager.dt;

        } else {
            timer -= MetroManager.dt;
            if(timer < 0f) timer = 0f;
        }
        timerImage.enabled = true;
        timerImage.fillAmount = timer / 45.0f;
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

        if(passengers.Count >= 30) return;
        Passenger p = new Passenger();
        p.destination = type;
        passengers.Add(p);
       
    }

    public void StartOvercrowdedTimer(){

    }
    public void NotifyCircleAnimation(){

    }


    void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData){
    
        var line = MetroManager.SelectFreeLine();
        if( line != null){
            Debug.Log("station down");
            line.AddStation(this);

            var dist = eventData.Pointer.Result.Details.RayDistance;
            MetroManager.StartEditingLine(line, 0, dist, false);

            eventData.Pointer.IsFocusLocked = false;
            eventData.Pointer.IsTargetPositionLockedOnFocusLock = false;
            // FocusDetails details;
            // CoreServices.FocusProvider.TryGetFocusDetails(eventData.Pointer, out details);
            // Debug.Log(details.Object);
            // details.Object = null; //MetroManager.Instance.stations[1].gameObject;
            // details.Point = MetroManager.Instance.stations[1].gameObject.transform.position;
            // bool ret = CoreServices.FocusProvider.TryOverrideFocusDetails(eventData.Pointer, details);
            firstDrag = false;

            var hapticController = eventData.Pointer?.Controller as IMixedRealityHapticFeedback;
            hapticController?.StartHapticImpulse(0.4f, 0.05f);
        } else {
            // TODO no free line feedback
            // maybe instead create a NUllLine that is returned from SelectFreeLine
            // grey segment with X icon
        }

    }
    
    void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData){
        // var line = MetroManager.selectedLine;
        // if( line != null){
        //     if(line.stops.Count == 1) line.RemoveAll();

        // }
        // Debug.Log("station up");
        // MetroManager.DeselectLine();

        
    }

    void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData){
        if(!firstDrag) return;
        if(cooldown > 0.0f) return;
        firstDrag = false;

        var line = MetroManager.editingLine;
        var index = MetroManager.editingIndex;
        var dist = eventData.Pointer.Result.Details.RayDistance;
        var insert = MetroManager.editingInsert;

        if( line != null){
            // add if not in line (unless closing loop TODO)
            if(!line.stops.Contains(this)){
                line.InsertStation(index+1, this);
                var insrt = index+1 < line.stops.Count-1;
                MetroManager.StartEditingLine(line, index+1, dist, insrt);
            
            // remove if adjacent to editingIndex
            } else if(line.stops.Count > 1){
                if (line.stops[index] == this){
                    line.RemoveStation(this);
                    var insrt = index-1 >= 0 && index-1 < line.stops.Count-1;
                    MetroManager.StartEditingLine(line, index-1, dist, insrt);

                }else if(insert && line.stops[index+1] == this){
                    line.RemoveStation(this);
                    var insrt = index < line.stops.Count-1;
                    MetroManager.StartEditingLine(line, index, dist, insrt);

                } 
            }
            
            var hapticController = eventData.Pointer?.Controller as IMixedRealityHapticFeedback;
            hapticController?.StartHapticImpulse(0.4f, 0.05f);
            cooldown = 1.0f;
            // TODO trigger add/remove event viz

        }
    }

    void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData){
    
        // Line selected
        // Add Station to selectedLines next index
        // if(MetroManager.selectedLine != null){
        //     Debug.Log("Selected line addStation");
        //     MetroManager.selectedLine.AddStation(this);

        // } else {
        //     // No line selected and no lines attached
        //     // SelectFreeLine and place        
        //     if(lines.Count == 0){
        //         Debug.Log("Select free line, addStation");
        //         var line = MetroManager.SelectFreeLine();
        //         if(line != null){
        //             line.AddStation(this);
        //         } // else TODO visualize no free lines somehow
        //     }

        //     // No line selected and one line attached? multiple lines?
        // }
    }

    void IMixedRealityFocusHandler.OnFocusEnter(FocusEventData eventData){
        firstDrag = true;
    }
    void IMixedRealityFocusHandler.OnFocusExit(FocusEventData eventData){

    }

    
}
