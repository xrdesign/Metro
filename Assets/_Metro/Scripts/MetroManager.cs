using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Physics;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit;


/**
* SpaceMetro aims to clone mini metro in VR
* This singleton object initializes and handles global game state and events
*/
public class MetroManager : MonoBehaviour
{
    public static MetroManager Instance;

    public float score = 0.0f;
    public float time = 0.0f;
    public int freeTrains = 3;
    public int freeCars = 0;

    public List<Station> stations = new List<Station>();
    public List<TransportLine> lines = new List<TransportLine>();
    
    public bool paused = false;
    public int hour;
    public int day;
    public int week; 

    // TransportLine edit state
    public static TransportLine selectedLine = null;



    private void Awake(){
        if (Instance is null) Instance = this;
        else Destroy(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        InitializeGameState();
    }

    // Update is called once per frame
    void Update()
    {
        // Time progression
        if(!paused){
            CheckStationTimers(); // check for lose condition
            UpdateClock(); // update clock, grant weekly reward


        }

        UpdatePointerState();

    }



    void InitializeGameState(){
        SpawnStation(StationType.Sphere);
        SpawnStation(StationType.Cone);
        SpawnStation(StationType.Cube);
        SpawnStation(StationType.Sphere);
        SpawnStation(StationType.Sphere);
        SpawnStation(StationType.Sphere);
        SpawnStation(StationType.Sphere);

        AddTransportLine(Color.red);
        AddTransportLine(Color.blue);
        AddTransportLine(Color.yellow);

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

    void GameOver(){
        Debug.Log("TODO: Game Over!");
    }

    public void UpdateClock(){
        float lengthOfDay = 20.0f;

        time += Time.deltaTime;
        
        float clockTime = (time % lengthOfDay) / lengthOfDay * 24;
        int newHour = (int) clockTime;
        int newDay = (int)((time / lengthOfDay) % 7);
        int newWeek = (int)(time / (lengthOfDay * 7));

        if(newHour != hour){
            // new hour event
            hour = newHour;
            if(hour % 6 == 0){
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
        lines.Add(line);
    }

    public void SpawnPassengers(){
        foreach(Station station in stations){
            var p = Random.value;
            if(p < 0.15f){
                station.SpawnRandomPassenger();
            }
        }
    }

    public void SpawnStations(){
        var p = Random.value;
        if(p < 0.6f){
            SpawnRandomStation();
        }
    }

    public static void SpawnStation(StationType type, float radius = 1.0f){
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

        var pos = Random.insideUnitSphere * radius;
        if(pos.y < 0.5f) pos.Set(pos.x, 0.5f, pos.z);
        obj.transform.position = pos;
        obj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        Instance.stations.Add(station);   
    }

    public static void SpawnRandomStation(){
        var p = Random.value;
        var type = StationType.Sphere;
        if( p < 0.5f)
            type = StationType.Sphere;
        else if(p < 0.9f)
            type = StationType.Cone;
        else if(p < 1.0f)
            type = StationType.Cube;

        // todo unique stations..
        SpawnStation(type);
    }

    public static TransportLine SelectFreeLine(){
        foreach(var line in Instance.lines){
            Debug.Log(line);
            if(!line.isDeployed){
                Debug.Log("select");
                selectedLine = line;
                return line;
            }
        }
        return null;
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


    
}
