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
using UnityEngine.Events;
using UnityEngine.UI;

using UnityEngine.InputSystem.EnhancedTouch;


/**
* SpaceMetro aims to clone mini metro in VR
* This singleton object initializes and handles global game state and events
*/
public class MetroGame : MonoBehaviour, IMixedRealityPointerHandler {
    public uint gameId;
    
    public float score = 0.0f;
    public float time = 0.0f;
    public float gameSpeed = 0.0f;
    public static float dt = 0f;

    public int freeTrains = 3;
    public int freeCars = 0;

    public List<Station> stations = new List<Station>();
    public List<TransportLine> lines = new List<TransportLine>();
    
    public bool paused = false;
    public bool isGameover = false;

    public bool addingTrain = false;
    public bool addedTrain = false;

    public bool Ai_paused = false;

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


    #region Organizational Scene Objects

    private GameObject stationsOrganizer;
    private GameObject transportLinesOrganizer;

    #endregion
    
    


    #region Delegates

    #region UI

    // Invoke this to tell Manager to update UI if this game is selected.
    public Action uiUpdateDelegate;

    #endregion

    #endregion

    

    #region Action Queue

    // Replaced normal Unity actions with this delegate so that we don't need to know about the game instance from where
    // we define the logic for the action. EX: In Server.cs, we can define actions using the delegate parameter MetroGame,
    // and when we invoke here, we simply pass in ourselves. Needed now that we don't have singleton access the game.
    public delegate void MetroGameAction(MetroGame game);
    private static Queue ActionQueue = Queue.Synchronized(new Queue());

    #endregion

    private volatile bool needReset = false;
    
    void OnEnable(){
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityPointerHandler>(this);
    }
    void OnDisable(){
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityPointerHandler>(this);
    }

    // Start is called before the first frame update
    void Start() {
        stationsOrganizer = new GameObject("Stations");
        stationsOrganizer.transform.SetParent(this.transform, false);
        transportLinesOrganizer = new GameObject("Transport Lines");
        transportLinesOrganizer.transform.SetParent(this.transform, false);
        
        gameSpeed = 0.0f;
        
        StartGame();
    }

 

    public void StartGame(){
        Debug.Log("Start Game " + gameId);
        Debug.Log("freeTrains: " + this.freeTrains);
        this.ResetGameState();
        this.InitializeGameState();

    }

    public void SetPaused(bool shouldPause) {
        this.paused = shouldPause;

        // send toggling event
        MetroManager.SendEvent((this.paused ? "Game Paused: ": "Game Resumed: ") + gameId);
    }

    public void SetAIEnabled(bool aiEnabled)
    {
        this.Ai_paused = aiEnabled;

        // send toggling event
        MetroManager.SendEvent((this.Ai_paused ? "Ai Paused: " : "Ai Resumed: ") + gameId);
    }

    public void QueueAction(MetroGameAction gameAction){
        if (this.Ai_paused) return; // don't accept actions from AI if AI is paused
        if (this.paused) return; // don't accept actions if game is paused
        lock (ActionQueue.SyncRoot){
            ActionQueue.Enqueue(gameAction);
        }
    }


    // Update is called once per frame
    void Update()
    {
        // Execute Server Actions
        while(ActionQueue.Count > 0){
            MetroGameAction action;
            lock(ActionQueue.SyncRoot){
                action = (MetroGameAction)ActionQueue.Dequeue();
            }
            action(this);
        }

        if (needReset)
        {
            needReset = false;
            StartGame();
            // End this Update step early
            return;
        }

        // Update Passenger's route
        UpdatePassengerRoute();

        // Time progression
        CheckStationTimers(); // check for lose condition
        UpdateClock(); // update clock, grant weekly reward

        UpdatePointerState();

    }

    public void ScheduleReset()
    {
        this.needReset = true;
    }

    void ResetGameState(){
        Debug.Log("Resetting game state for " + gameObject.name);
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
        
        // It looks like delegates are only instantiated when methods are assigned to them, so all but one of the games will have null here.
        if (uiUpdateDelegate != null) 
            uiUpdateDelegate.Invoke();
    }
    
    void InitializeGameState(){
        print("Initializing game state for " + gameObject.name);
        SpawnStation(StationType.Cube);
        SpawnStation(StationType.Cone);
        SpawnStation(StationType.Sphere);

        AddTransportLine(Color.red);
        AddTransportLine(Color.blue);
        AddTransportLine(Color.yellow);
        
        paused = false;
        gameSpeed = 1.0f;
        isGameover = false;
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
        paused = true;
        

        MetroManager.SendEvent("Game Over: " + gameId);
        isGameover = true;

    }

    public void UpdateClock()
    {
        float lengthOfDay = 20.0f; // 1 day 20 seconds

        if (paused) {
            gameSpeed = 0.0f;
        }
        else
        {
            gameSpeed = 1.0f;
        }

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
        go.transform.SetParent(transportLinesOrganizer.transform);
        line.color = color;
        line.id = lines.Count;
        line.uuid = line.GetInstanceID();
        line.gameInstance = this;
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

    public void SpawnStation(StationType type){
        print(this.gameObject.name + " spawning station of type " + type.ToString());
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

        var radius = 1.5f + 0.5f * this.week + 0.1f * this.day;
        var offset = new Vector3(0f,0f,2f);
        var pos = new Vector3(0f,1.0f,0f) + offset;
        while(StationTooClose(pos)){
            pos = UnityEngine.Random.insideUnitSphere * radius + offset;
            radius += 0.02f;
            if(pos.y < 0.5f) pos.Set(pos.x, 0.5f, pos.z);
            if(pos.y > 2.0f) pos.Set(pos.x, 2.0f, pos.z);
        }
        obj.transform.SetParent(stationsOrganizer.transform);
        obj.transform.localPosition = pos;
        obj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        station.id = this.stations.Count;
        station.uuid = station.GetInstanceID();
        station.gameInstance = this;  // Pass in ourselves for access
        this.stations.Add(station);
    }

    public void SpawnRandomStation(){
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

    public bool StationTooClose(Vector3 pos){
        foreach(var station in this.stations){
            var d = Vector3.Distance(pos, station.transform.localPosition);
            if(d < 0.5f) return true;
        }
        return false;
    }

    public TransportLine SelectFreeLine(){
        foreach(var line in this.lines){
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

    public void AddScore(int inc){
        this.score += inc;
        // 
    }

    public void UpdatePassengerRoute()
    {
        // for all station
        for (int i = 0; i < stations.Count; i++)
        {
            Station currentStation = stations[i];
            // for all passenger in the station
            for (int j = 0; j < currentStation.passengers.Count; j++)
            {
                Passenger currentPassenger = currentStation.passengers[j];
                
                // TODO: should not always update
                //if (currentPassenger.route == null)
                //{
                    // find a route
                    currentPassenger.route = FindRouteClosest(currentStation, currentPassenger.destination);

                    // Debug print
                    string routeString = "";
                    for (int k = 0; k < currentPassenger.route.Count; k++)
                    {
                        routeString += currentPassenger.route[k].id + " ";
                    }
                    //Debug.Log("Passenger is going from " + currentStation.uuid + " to " + currentPassenger.destination + " via [ " + routeString + "]");
                //}
                //else
                //{
                    // // if passenger is current at the end of the route, null it
                    // if (currentPassenger.route[currentPassenger.route.Count - 1] == currentStation)
                    // {
                    //     currentPassenger.route = null;
                    //     //continue;
                    // }
                //}
            }
        }
    }

    public List<Station> ReconstructRoute(Station start, Station end, Dictionary<Station, Station> cameFrom)
    {
        List<Station> route = new List<Station>();
        Station current = end;
        while (current != start)
        {
            route.Add(current);
            current = cameFrom[current];
        }
        //route.Add(current);
        route.Reverse();
        return route;
    }

    public List<Station> FindRouteClosest(Station start, StationType goal)
    {
        var result = FindRoute(start, (x) => x.type == goal);
        if (result.Item1.Count == 0)
        {
            // Failed to find a connected route, find the closest station to the closet goal station
            // Find the closest station that is goal type
            float minDist = Single.PositiveInfinity;
            int minIndex = -1;
            for (int i = 0; i < stations.Count; i++)
            {
                if (stations[i].type == goal)
                {
                    float dist = Vector3.Distance(start.transform.position, stations[i].transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        minIndex = i;
                    }
                }
            }

            if (minIndex == -1) {
                print("Failure to find path:\nStart Station Type: " + start.type.ToString() + "\nGoal: " + goal.ToString() + "Available Types: " + stations.ToString());
            }

            // Find the station in closedset that is closest to the goal station
            Station closest = stations[minIndex];
            minDist = Single.PositiveInfinity;
            Station closestConnected = null;
            foreach(var item in result.Item2)
            {
                float dist = Vector3.Distance(closest.transform.position, item.Key.transform.position) + item.Value; // TODO: weight the fScore and distance
                if (dist < minDist)
                {
                    minDist = dist;
                    closestConnected = item.Key;
                }
            }
            result = FindRoute(start, (x) => x == closestConnected);
        }

        return result.Item1;
    }

    // pass in a functor for criteria
    public Tuple<List<Station>, Dictionary<Station, float>> FindRoute(Station start, Func<Station, bool> criteria)
    {
        // use A * to find a shortest route, if no route found, find the route to the closest station to the target
        List<Station> route = new List<Station>();
        List<Station> closedSet = new List<Station>();
        // openSet is a sorted List with fScore as priority, lowest fScore is the first element
        SortedList<float, Station> openSet = new SortedList<float, Station>();
        Dictionary<Station, Station> cameFrom = new Dictionary<Station, Station>();
        Dictionary<Station, float> gScore = new Dictionary<Station, float>();
        Dictionary<Station, float> fScore = new Dictionary<Station, float>();
        gScore.Add(start, 0);
        fScore.Add(start, HeuristicCostEstimate(start, start));

        openSet.Add(fScore[start], start);

        while (openSet.Count > 0)
        {
            // get first station in the openSet
            Station current = openSet.Values[0];
            if (criteria(current))
            {
                // if the station is the goal, reconstruct the route
                route = ReconstructRoute(start, current, cameFrom);
                break;
            }

            openSet.RemoveAt(0);
            closedSet.Add(current);
            // for all neighbor of the current station
            // Find all neighboring stations
            List<KeyValuePair<Station, int>> neighbors = current.GetNeighbors();

            for (int i = 0; i < neighbors.Count; i++)
            {
                Station neighbor = neighbors[i].Key;
                int lineId = neighbors[i].Value;
                // if the neighbor is in the closedSet, skip
                if (closedSet.Contains(neighbor))
                {
                    continue;
                }
                // if the new path to the neighbor is shorter, update the path
                float tentative_gScore = gScore[current] + Vector3.Distance(current.transform.position, neighbor.transform.position);

                float neighborgScore = Single.PositiveInfinity;
                if (gScore.ContainsKey(neighbor))
                {
                    neighborgScore = gScore[neighbor];
                }
                if (tentative_gScore < neighborgScore)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative_gScore;
                    fScore[neighbor] = gScore[neighbor] + HeuristicCostEstimate(start, neighbor);
                    if (!openSet.ContainsValue(neighbor))
                    {
                        openSet.Add(fScore[neighbor], neighbor);
                    }
                }
            }
        }

        return new Tuple<List<Station>, Dictionary<Station, float>>(route, fScore);
    }
    public float HeuristicCostEstimate(Station start, Station goal)
    {
        // TODO for more complicated weighting
        // currently favor crowd to less crowd
        int left = start.passengers.Count;
        int right = goal.passengers.Count;
        if (left > right)
        {
            return 1.0f; 
        }
        else
        {
            return 3.0f;
        }
    }

    void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData){
        Debug.Log("Pointer Clicked");
        MetroManager.SendEvent("Controller clicked: " + gameId);
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


    public JSONObject SerializeGameState(){
        // JSONObject json = new JSONObject(JsonUtility.ToJson(Instance));
        JSONObject json = new JSONObject();

        json.AddField("score", this.score);
        json.AddField("time", this.time);
        json.AddField("isPause", this.paused);
        json.AddField("isGameover", this.isGameover);
        json.AddField("freeTrains", this.freeTrains);
        json.AddField("stations", SerializeStations());
        json.AddField("lines", SerializeTransportLines());
        json.AddField("trains", SerializeTrains());
        json.AddField("segments", SerializeSegments());
        return json;
    }
    

    public JSONObject SerializeStations(){
        JSONObject json = new JSONObject(JSONObject.Type.ARRAY);
        foreach( var s in this.stations){
            JSONObject sjson = new JSONObject();
            sjson.AddField("unique_id", s.uuid);
            sjson.AddField("type", "station");
            sjson.AddField("shape", s.type.ToString());
            sjson.AddField("x", s.position.x);
            sjson.AddField("y", s.position.y);
            sjson.AddField("z", s.position.z);
            sjson.AddField("timer", s.timer);
            var passenger_counts = GetPassengerCounts(s.passengers);
            foreach(var destination in passenger_counts.Keys) {
                sjson.AddField("cnt_" + destination.ToLower(), passenger_counts[destination]);
            }
            json.Add(sjson);
        }
        return json;
    }

    public JSONObject SerializeTransportLines(){
        JSONObject json = new JSONObject(JSONObject.Type.ARRAY);
        foreach( var l in this.lines){
            JSONObject ljson = new JSONObject();
            ljson.AddField("unique_id", l.uuid);
            ljson.AddField("type", "line");
            json.Add(ljson);        
        }
        return json;
    }

    public JSONObject SerializeSegments(){
        JSONObject json = new JSONObject(JSONObject.Type.ARRAY);
        foreach( var l in this.lines){            
            for (int i=0; i<l.stops.Count-1; i++) {
                JSONObject segment_json = new JSONObject();
                var s = l.stops[i];
                var next_s = l.stops[i+1];
                segment_json.AddField("type", "segment");
                segment_json.AddField("length", 20);
                segment_json.AddField("which_line", l.uuid);
                segment_json.AddField("from_station", s.uuid);
                segment_json.AddField("to_station", next_s.uuid);
                json.Add(segment_json);
            }
        }
        return json;
    }

    public static Dictionary<string, int> GetPassengerCounts(List<Passenger> passengers){
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach(var p in passengers) {
            try {
                counts[p.destination.ToString()] += 1;
            } catch (KeyNotFoundException) {
                counts.Add(p.destination.ToString(), 1);
            }
        }
        return counts;
    }

    public JSONObject SerializeTrains(){
        JSONObject trains_json = new JSONObject(JSONObject.Type.ARRAY);
        foreach(var l in this.lines) {
            JSONObject json = new JSONObject();
            foreach( var t in l.trains){
                json.AddField("unique_id", t.uuid);
                json.AddField("type", "train");
                json.AddField("position", t.position);
                json.AddField("speed", t.speed);
                json.AddField("direction", t.direction);
                json.AddField("line_id", l.uuid);
                // is there currently no capacity limit?
                var passenger_counts = GetPassengerCounts(t.passengers);
                foreach(var destination in passenger_counts.Keys) {
                    json.AddField("cnt_" + destination.ToLower(), passenger_counts[destination]);
                }
            }
            trains_json.Add(json);
        }
        return trains_json;
    }
}