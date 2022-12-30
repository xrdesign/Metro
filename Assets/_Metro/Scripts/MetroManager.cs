using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Physics;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit;


/**
* SpaceMetro aims to clone mini metro in VR
* This singleton object initializes and handles global game state and events
*/
public class MetroManager : MonoBehaviour, IMixedRealityPointerHandler
{
    public static MetroManager Instance;

    public float score = 0.0f;
    public float time = 0.0f;
    public float gameSpeed = 0.0f;
    public static float dt = 0f;

    public int freeTrains = 3;
    public int freeCars = 0;

    public List<Station> stations = new List<Station>();
    public List<TransportLine> lines = new List<TransportLine>();
    
    public bool paused = false;
    public bool addingTrain = false;
    public bool addedTrain = false;
    public float clockTime;
    public int hour;
    public int day;
    public int week; 

    // TransportLine edit state
    public static bool editing = false;
    public static bool editingInsert = false;
    public static TransportLine editingLine = null;
    public static int editingIndex = 0;
    public static float editingDist = 1.0f;

    public List<float> trackLengths = new List<float>();
    public float totalTrackLength;

    public GameObject menuUI;
    public GameObject metroUI;
    TransportLineUI[] lineUIs;

    private static Queue ActionQueue = Queue.Synchronized(new Queue());

    private void Awake(){
        if (Instance is null) Instance = this;
        else Destroy(this);
    }

    void OnEnable(){
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityPointerHandler>(this);
    }
    void OnDisable(){
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityPointerHandler>(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        gameObject.AddComponent<Server>();

        gameSpeed = 0.0f;
        lineUIs = metroUI.GetComponentsInChildren<TransportLineUI>(true);
        StartGame();
    }


    public static void StartGame(){
        Debug.Log("Start Game");
        Debug.Log("freeTrains: " + MetroManager.Instance.freeTrains);
        Instance.ResetGameState();
        Instance.InitializeGameState();

    }

    public static void QueueAction(Action action){
        lock(ActionQueue.SyncRoot){
            ActionQueue.Enqueue(action);
        }
    }


    // Update is called once per frame
    void Update()
    {
        // Execute Server Actions
        while(ActionQueue.Count > 0){
            Action action;
            lock(ActionQueue.SyncRoot){
                action = (Action)ActionQueue.Dequeue();
            }
            action();
        }

        // Time progression
        CheckStationTimers(); // check for lose condition
        UpdateClock(); // update clock, grant weekly reward

        UpdatePointerState();

    }

    void ResetGameState(){
        Debug.Log("reset");
        foreach(var s in stations){
            Destroy(s.gameObject);
        }
        stations.Clear();
        
        foreach(var t in lines){
            t.RemoveAll();
            Destroy(t.tracks.gameObject);
            Destroy(t.gameObject);
        }
        lines.Clear();

        foreach(var l in lineUIs){
            l.SetLine(null);
        }
    }

    void InitializeGameState(){
        metroUI.SetActive(true);
        SpawnStation(StationType.Cube);
        SpawnStation(StationType.Cone);
        SpawnStation(StationType.Sphere);

        AddTransportLine(Color.red);
        AddTransportLine(Color.blue);
        AddTransportLine(Color.yellow);

        for(int i = 0; i < lines.Count; i++){
            lineUIs[i].SetLine(lines[i]);
        }
        paused = false;
        gameSpeed = 1.0f;
        // Debug.Log(lines[0]);
        // Debug.Log(stations[0]);

        // lines[0].AddStation(stations[0]);
        // lines[0].AddStation(stations[1]);
        // lines[0].AddStation(stations[2]);
        // lines[0].AddStation(stations[3]);
        // lines[0].AddStation(stations[4]);
        // lines[0].AddStation(stations[5]);
        // lines[0].AddStation(stations[6]);
    }

    public void CheckStationTimers(){
        float overcrowdedTimerLimit = 45.0f + 2.0f; //45 seconds for animation + 2 second grace period
        foreach(Station station in stations){
            if(station.timer > overcrowdedTimerLimit){
                GameOver();
            }
        }
    }

    public void GameOver(){
        // Debug.Log("TODO: Game Over!");
        // SceneManager.LoadScene(0);
        gameSpeed = 0.0f;
        metroUI.SetActive(false);
        menuUI.SetActive(true);

    }

    public void UpdateClock(){
        float lengthOfDay = 20.0f; // 1 day 20 seconds

        if(paused) gameSpeed = 0.0f;

        dt = Time.deltaTime * gameSpeed;

        if(dt == 0.0f) return;

        time += dt;
        
        clockTime = (time % lengthOfDay) / lengthOfDay * 24;
        int newHour = (int) clockTime;
        int newDay = (int)((time / lengthOfDay) % 7);
        int newWeek = (int)(time / (lengthOfDay * 7));

        if(newHour != hour){
            // new hour event
            hour = newHour;
            if(hour % 2 == 0){
                SpawnPassengers(); // spawn random passengers if needed
            }


        }
        if(newDay != day){
            // new day event
            day = newDay;

            SpawnStations(); // spawn random stations if needed

        }
        if(newWeek != week){
            // new week event
            week = newWeek;
        }
    }

    public void AddTransportLine(Color color){
        var go = new GameObject();
        go.name = "TransportLine";
        var line = go.AddComponent<TransportLine>();
        line.color = color;
        line.id = lines.Count;        
        lines.Add(line);
    }

    public void SpawnPassengers(){
        foreach(Station station in stations){
            var p = UnityEngine.Random.value;
            if(p < 0.15f){
                station.SpawnRandomPassenger();
            }
        }
    }

    public void SpawnStations(){
        var p = UnityEngine.Random.value;
        if(p < 0.8f){
            SpawnRandomStation();
        }
    }

    public static void SpawnStation(StationType type){
        GameObject obj;
        switch(type){
            case StationType.Sphere:
                GameObject prefab = Resources.Load("Prefabs/StationSphere") as GameObject;
                obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
                break;
            case StationType.Cone:
                prefab = Resources.Load("Prefabs/StationCone") as GameObject;
                obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
                break;
            case StationType.Cube:
                prefab = Resources.Load("Prefabs/StationCube") as GameObject;
                obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
                break;
            default:
                prefab = Resources.Load("Prefabs/StationCube") as GameObject;
                obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
                break;
        }

        Station station = obj.GetComponentInChildren<Station>();

        var radius = 1.5f + 0.5f * Instance.week + 0.1f * Instance.day;
        var offset = new Vector3(0f,0f,2f);
        var pos = new Vector3(0f,1.0f,0f) + offset;
        while(StationTooClose(pos)){
            pos = UnityEngine.Random.insideUnitSphere * radius + offset;
            radius += 0.02f;
            if(pos.y < 0.5f) pos.Set(pos.x, 0.5f, pos.z);
            if(pos.y > 2.0f) pos.Set(pos.x, 2.0f, pos.z);
        }

        obj.transform.position = pos;
        obj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        station.id = Instance.stations.Count;
        Instance.stations.Add(station);   
    }

    public static void SpawnRandomStation(){
        var p = UnityEngine.Random.value;
        var type = StationType.Sphere;
        if( p < 0.45f)
            type = StationType.Sphere;
        else if(p < 0.85f)
            type = StationType.Cone;
        else if(p < 1.0f)
            type = StationType.Cube;

        // todo unique stations..
        SpawnStation(type);
    }

    public static bool StationTooClose(Vector3 pos){
        foreach(var station in Instance.stations){
            var d = Vector3.Distance(pos, station.transform.position);
            if(d < 0.5f) return true;
        }
        return false;
    }

    public static TransportLine SelectFreeLine(){
        foreach(var line in Instance.lines){
            Debug.Log(line);
            if(!line.isDeployed){
                Debug.Log("select");
                editingLine = line;
                return line;
            }
        }
        return null;
    }

    public static void DeselectLine(){
        var line = editingLine;
        if( line != null){
            if(line.stops.Count == 1) line.RemoveAll();
            line.tracks.DisableUISegments();
            var color = line.color;
            color.a = 0.75f;
            if(editingIndex == -1) line.tracks.head.SetColor(color);
            else if(editingIndex == line.tracks.segments.Count) line.tracks.tail.SetColor(color);
            else line.tracks.segments[editingIndex].SetColor(color);
        }
        editingLine = null;
    }

    public static void StartEditingLine(TransportLine line, int trackIndex, float dist, bool insert){
        editingLine = line;
        editingIndex = trackIndex;
        editingDist = dist;
        editingInsert = insert;
        editing = true;
    }


    public static Vector3 PointerTarget = new Vector3(0,0,0);
    void UpdatePointerState(){
        // Set PointerTarget vector from primaryPointer 
        var p = CoreServices.InputSystem.FocusProvider.PrimaryPointer;
        if (p != null && p.Result != null)
        {
            var startPoint = p.Position;
            var endPoint = p.Result.Details.Point;
            var hitObject = p.Result.Details.Object;

            if(hitObject != null){
                // Debug.Log("Hit object: " + hitObject);
                var dist = p.Result.Details.RayDistance;
                var offset = 0.1f;
                PointerTarget = RayStep.GetPointByDistance(p.Rays, dist - offset);
            } else {
                PointerTarget = RayStep.GetPointByDistance(p.Rays, 2.5f);
            }

        }  
    }

    public static void AddScore(int inc){
        Instance.score += inc;
        // 
    }


    void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData){

    }
    
    void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData){
        //Debug.Log("MetroManager pointer up");
        DeselectLine();
     
    }

    void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData){
        var point = RayStep.GetPointByDistance(eventData.Pointer.Rays, editingDist);
        if(editingLine != null){
            if(editingIndex <= editingLine.tracks.segments.Count && editingLine.stops.Count > 1){
                TrackSegment segment;
                if(editingIndex < 0) segment = editingLine.tracks.head;
                else if(editingIndex == editingLine.tracks.segments.Count) segment = editingLine.tracks.tail;
                else segment = editingLine.tracks.segments[editingIndex];
                var color = editingLine.color;
                color.a = 0.2f;
                // Debug.Log("drag " + editingIndex + " " + segment + " " + color);
                segment.SetColor(color);
            }
            editingLine.tracks.UpdateUISegment(0, point, editingIndex);
            if(editingInsert)
                editingLine.tracks.UpdateUISegment(1, point, editingIndex+1);

        }
    }

    void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData){
    
    }






    public static JSONObject SerializeGameState(){

        // JSONObject json = new JSONObject(JsonUtility.ToJson(Instance));
        JSONObject json = new JSONObject();

        json.AddField("score", Instance.score);
        json.AddField("time", Instance.time);
        json.AddField("freeTrains", Instance.freeTrains);
        
        json.AddField("stations", SerializeStations());
        json.AddField("lines", SerializeTransportLines());

        return json;
    }

    public static JSONObject SerializeStations(){
        JSONObject json = new JSONObject();
        foreach( var s in Instance.stations){
            JSONObject sjson = new JSONObject();
            sjson.AddField("type", s.type.ToString());
            sjson.AddField("x", s.position.x);
            sjson.AddField("y", s.position.y);
            sjson.AddField("z", s.position.z);
            sjson.AddField("timer", s.timer);
            sjson.AddField("passengers", SerializePassengers(s.passengers));

            json.AddField(s.id.ToString(), sjson);
        }
        return json;
    }

    public static JSONObject SerializeTransportLines(){
        JSONObject json = new JSONObject();
        foreach( var l in Instance.lines){
            JSONObject ljson = new JSONObject();
            ljson.AddField("isDeployed", l.isDeployed);
            
            JSONObject stops_json = new JSONObject(JSONObject.Type.ARRAY);
            foreach(var s in l.stops)
                stops_json.Add(s.id);
            ljson.AddField("stops", stops_json);
            
            ljson.AddField("trains", SerializeTrains(l.trains));

            json.AddField(l.id.ToString(), ljson);        
        }
        return json;
    }

    public static JSONObject SerializePassengers(List<Passenger> passengers){
        JSONObject json = new JSONObject(JSONObject.Type.ARRAY);
        foreach( var p in passengers){
            json.Add(p.destination.ToString());
        }
        return json;
    }

    public static JSONObject SerializeTrains(List<Train> trains){
        JSONObject json = new JSONObject();
        foreach( var t in trains){
            json.AddField("position", t.position);
            json.AddField("speed", t.speed);
            json.AddField("direction", t.direction);
            json.AddField("passengers", SerializePassengers(t.passengers));
        }
        return json;
    }






    
}
