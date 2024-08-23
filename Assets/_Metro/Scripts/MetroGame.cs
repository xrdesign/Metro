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


using TMPro;
using Random = System.Random;
using Fusion;
using UnityEditor.Build;
using ExitGames.Client.Photon.StructWrapping;
using System.Linq;


public delegate void GameSelectionDelegateDef(bool selected);

/**
* SpaceMetro aims to clone mini metro in VR
* This singleton object initializes and handles global game state and events
*/
public class MetroGame : NetworkBehaviour, IMixedRealityPointerHandler
{
    private Random random;
    public uint gameId;

    [Networked] public float score { get; set; } = 0.0f;
    [Networked] public float time { get; set; } = 0.0f;
    [Networked] public float gameSpeed { get; set; } = 0.0f;
    public float dt = 0f;

    [Networked] public int passengersDelivered { get; set; } = 0;
    [Networked] public float totalPassengerWaitTime { get; set; } = 0;
    [Networked] public float totalPassengerTravelTime { get; set; } = 0;

    [Networked] public int freeTrains { get; set; } = 3;
    public Train selectedTrain = null;
    [Networked] public int freeCars { get; set; } = 0;


    [Networked, Capacity(40)] public NetworkArray<Station> stations => default;
    [Networked] public int stationCount { get; set; } = 0;
    [Networked, Capacity(20)] public NetworkArray<TransportLine> lines => default;
    [Networked] public int lineCount { get; set; } = 0;

    // ChangeDetector
    private ChangeDetector _changeDetector;

    [Networked] public bool containsStarStation { get; set; } = false;
    [Networked] public bool containsOtherStation { get; set; } = false;


    [Networked] public bool paused { get; set; } = false;
    [Networked] public bool isGameover { get; set; } = false;

    public bool addingTrain = false;
    public bool addedTrain = false;

    [Networked] public bool Ai_paused { get; set; } = false;

    public float clockTime { get; set; }
    public int hour { get; set; }
    public int day { get; set; }
    public int week { get; set; }

    // TransportLine edit state
    public bool editing = false;
    public bool editingInsert = false;
    public TransportLine editingLine = null;
    public int editingIndex = 0;
    public float editingDist = 1.0f;

    [Networked, Capacity(80)] public NetworkArray<float> trackLengths => default;
    [Networked] public int trackLengthCount { get; set; } = 0;
    [Networked] public float totalTrackLength { get; set; } = 0;


    #region Organizational Scene Objects


    [Networked] public NetworkObject stationsOrganizer { get; set; }
    [Networked] public NetworkObject transportLinesOrganizer { get; set; }
    [Networked] public NetworkObject trainOrganizer { get; set; }
    public GameObject alertCylinder;

    private bool setAlert = false;
    private bool alertValue = false;

    #endregion


    public int daysPerTrain { get; set; } = int.MaxValue;
    public int daysPerLine { get; set; } = int.MaxValue;


    #region Delegates

    #region UI

    // Invoke this to tell Manager to update UI if this game is selected.
    public Action uiUpdateDelegate;

    #endregion

    public GameSelectionDelegateDef GameSelectionDelegate;

    #endregion

    public int insertions;
    public int deletions;
    public int linesRemoved;
    public int linesCreated;

    public int trainsAdded;
    public int trainsRemoved;

    #region Action Queue

    // Replaced normal Unity actions with this delegate so that we don't need to know about the game instance from where
    // we define the logic for the action. EX: In Server.cs, we can define actions using the delegate parameter MetroGame,
    // and when we invoke here, we simply pass in ourselves. Needed now that we don't have singleton access the game.
    public delegate void MetroGameAction(MetroGame game);
    private Queue ActionQueue = Queue.Synchronized(new Queue());

    // Used to link an action and id together so we can later indicate to MetroManager when we complete the action.
    private struct TrackedMetroGameAction
    {
        public MetroGameAction action;
        public uint id;
    }

    #endregion

    private volatile bool needReset = false;
    private bool simGame = false;
    private float simLength = 0;


    private static Color[] lineColors = {
        Color.red,
        Color.blue,
        Color.yellow,
        Color.green,
        new Color(107f/255f, 47f/255f, 247f/255f),
    };
    private int addedLines = 0;


    void OnEnable()
    {
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityPointerHandler>(this);
    }
    void OnDisable()
    {
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityPointerHandler>(this);
    }

    // override Spawned()
    public override void Spawned()
    {
        base.Spawned();
        // Awake
        if (HasStateAuthority)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/NetworkObjectEmpty");
            stationsOrganizer = Runner.Spawn(prefab,
                onBeforeSpawned: (runner, obj) =>
                {
                    obj.GetComponent<NetworkName>().syncedName = "Stations";
                });
            stationsOrganizer.transform.SetParent(this.transform, false);
            // transportLinesOrganizer = new GameObject("Transport Lines");
            transportLinesOrganizer = Runner.Spawn(prefab,
                onBeforeSpawned: (runner, obj) =>
                {
                    obj.GetComponent<NetworkName>().syncedName = "Transport Lines";
                });
            transportLinesOrganizer.transform.SetParent(this.transform, false);
            // trainOrganizer = new GameObject("Trains");
            trainOrganizer = Runner.Spawn(prefab,
                onBeforeSpawned: (runner, obj) =>
                {
                    obj.GetComponent<NetworkName>().syncedName = "Trains";
                });
            trainOrganizer.transform.SetParent(this.transform, worldPositionStays: false);
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        alertCylinder = GameObject.Instantiate(Resources.Load("Prefabs/AlertCylinder") as GameObject, this.transform);
        alertCylinder.SetActive(false);
        gameSpeed = 0.0f;


        // Start
        if (random == null)
        {
            random = new Random();
        }
        GameObject floor = GameObject.Instantiate(Resources.Load("Prefabs/GameFloor") as GameObject);
        floor.transform.SetParent(this.transform, worldPositionStays: false);
        floor.GetComponent<MeshRenderer>().material.SetInt("_IsEven", (gameId % 2 == 0 || gameId == 0) ? 1 : 0);
        floor.transform.Find("Canvas/ID Display").GetComponent<TMP_Text>().text = $"{gameId}";

        // Change self name to Game ${gameId}
        this.gameObject.name = $"Game {gameId}";

        Debug.Log("Game " + gameId + " spawned");

        if (!simGame)
            StartGame();
    }

    public void StartGame()
    {

        if (HasStateAuthority)
        {
            Debug.Log("Start Game " + gameId);
            Debug.Log("freeTrains: " + this.freeTrains);
            this.ResetGameState();
            this.InitializeGameState();
        }
        else
        {
            Debug.Log("Joinning Game " + gameId);
        }

    }

    public void SetAlert(bool active)
    {
        if (HasStateAuthority)
        {
            Debug.Log("Setting Alert!");
            setAlert = true;
            alertValue = true;

            RPC_SetAlert(active);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetAlert(bool active)
    {
        setAlert = true;
        alertValue = active;
    }

    #region Notifications

    // Only called from MetroManager when the selected game has changed.
    public void OnSelectionChange(bool selected)
    {

        // All we do is invoke our delegate for other objects atm.
        if (GameSelectionDelegate != null)
        {  // Delegate is null unless assigned to a method.
            GameSelectionDelegate.Invoke(selected);
        }
        if (uiUpdateDelegate != null) uiUpdateDelegate.Invoke();
    }

    #endregion


    // Getters. Mostly for info that needs to poll other objects.
    #region Getters

    public bool IsGameSelected()
    {
        return this == MetroManager.GetSelectedGame();
    }

    public Station GetStationFromName(string stationName)
    {
        for (int i = 0; i < stationCount; i++)
        {
            Station station = stations[i];
            if (station.stationName == stationName)
                return station;
        }

        return null;
    }

    #endregion

    public void SetPaused(bool shouldPause)
    {
        this.paused = shouldPause;

        // send toggling event
        MetroManager.SendEvent((this.paused ? "Game Paused: " : "Game Resumed: ") + gameId);
    }

    public void SetAIEnabled(bool aiEnabled)
    {
        this.Ai_paused = aiEnabled;

        // send toggling event
        MetroManager.SendEvent((this.Ai_paused ? "Ai Paused: " : "Ai Resumed: ") + gameId);
    }

    /// <summary>
    /// Queues an action to be executed. Throws exceptions when AI is paused or game is paused.
    /// </summary>
    /// <param name="gameAction"></param>
    /// <returns></returns>
    public uint QueueAction(MetroGameAction gameAction)
    {
        if (this.Ai_paused) throw new Exception("Cannot Queue Action, AI is paused"); // don't accept actions from AI if AI is paused
        if (this.paused) throw new Exception("Cannot Queue Action, Game is paused"); // don't accept actions if game is paused
        Debug.Log("Action Queued on MetroGame: " + this.gameId);
        uint newID = MetroManager.RequestQueueID();
        TrackedMetroGameAction trackedMetroGameAction;
        trackedMetroGameAction.action = gameAction;
        trackedMetroGameAction.id = newID;
        lock (ActionQueue.SyncRoot)
        {
            ActionQueue.Enqueue(trackedMetroGameAction);
        }
        return newID;
    }


    // Update is called once per frame
    public override void FixedUpdateNetwork()
    {

        if (setAlert)
            alertCylinder.SetActive(alertValue);

        // Execute Server Actions
        while (ActionQueue.Count > 0)
        {
            TrackedMetroGameAction action;
            lock (ActionQueue.SyncRoot)
            {
                action = (TrackedMetroGameAction)ActionQueue.Dequeue();
            }
            action.action(this);
            Debug.Log("Fulfilling action on game: " + gameId);
            MetroManager.FulfillQueueAction(action.id);
        }

        if (needReset)
        {
            StartGame();
            needReset = false;
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

    void ResetGameState()
    {
        Debug.Log("Resetting game state for " + gameObject.name);
        for (int i = 0; i < stationCount; i++)
        {
            var s = stations[i].Object;
            Runner.Despawn(s);
        }
        containsStarStation = false;
        stations.Clear();
        stationCount = 0;

        for (int i = 0; i < lineCount; i++)
        {
            var l = lines[i].Object;
            var t = l.GetComponent<TransportLine>();
            t.RemoveAll();
            Runner.Despawn(t.tracks.Object);
            Runner.Despawn(l);
        }
        lines.Clear();
        lineCount = 0;
        score = 0;
        passengersDelivered = 0;
        totalPassengerWaitTime = 0;
        totalPassengerTravelTime = 0;
        addedLines = 0;


    }

    void InitializeGameState()
    {
        print("Initializing game state for " + gameObject.name);
        paused = false;
        gameSpeed = 1.0f;
        isGameover = false;

        // It looks like delegates are only instantiated when methods are assigned to them, so all but one of the games will have null here.
        if (uiUpdateDelegate != null)
            uiUpdateDelegate.Invoke();

        if (HasStateAuthority)
        {
            SpawnStation(StationType.Cube);
            SpawnStation(StationType.Cone);
            SpawnStation(StationType.Sphere);
        }
        else
        {
            return;
        }

        stations[0].stationName = "1";
        stations[1].stationName = "2";
        stations[2].stationName = "3";

        this.addedLines = 0;
        AddTransportLine();
        AddTransportLine();
        AddTransportLine();


    }

    public void CheckStationTimers()
    {
        for (int i = 0; i < stationCount; i++)
        {
            Station station = stations[i];
            float overcrowdedTimerLimit = station.MaxTimeoutDuration + 2.0f; // MaxTimeoutDuration from station + 2 second grace period
            if (station.timer > overcrowdedTimerLimit)
            {
                GameOver();
            }
        }
    }

    public void GameOver()
    {
        if (HasStateAuthority)
        {
            // Debug.Log("TODO: Game Over!");
            // SceneManager.LoadScene(0);
            gameSpeed = 0.0f;
            paused = true;


            MetroManager.SendEvent("Game Over: " + gameId);
            isGameover = true;

            // Send RPC to all clients to replicate the game over state
            RPC_GameOver();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_GameOver()
    {
        gameSpeed = 0.0f;
        paused = true;
        MetroManager.SendEvent("Game Over: " + gameId);
        isGameover = true;
    }

    public void UpdateClock()
    {
        float lengthOfDay = 20.0f; // 1 day 20 seconds

        float gameSpeed = this.gameSpeed;
        if (paused)
        {
            gameSpeed = 0.0f;
        }

        dt = Runner.DeltaTime * gameSpeed;

        if (dt == 0.0f) return;

        time += dt;

        clockTime = (time % lengthOfDay) / lengthOfDay * 24;
        int newHour = (int)clockTime;
        int newDay = (int)((time / lengthOfDay) % 7);
        int newWeek = (int)(time / (lengthOfDay * 7));

        if (newHour != hour)
        {
            // new hour event
            hour = newHour;
            if (hour % 2 == 0)
            {
                SpawnPassengers(); // spawn random passengers if needed
            }
        }
        if (newDay != day)
        {
            // new day event
            day = newDay;
            if (!simGame)
                SpawnStations(); // spawn random stations if needed
            if (day % daysPerTrain == 0)
            {
                freeTrains++;
                if (uiUpdateDelegate != null)
                    uiUpdateDelegate.Invoke();
            }
            if (day % daysPerLine == 0)
            {
                AddTransportLine();
                if (uiUpdateDelegate != null)
                    uiUpdateDelegate.Invoke();
            }
        }

        if (newWeek != week)
        {
            // new week event
            week = newWeek;
        }
    }

    public void AddTransportLine()
    {
        if (addedLines < 0 || addedLines >= lineColors.Length)
        {
            return;
        }
        Color color = lineColors[addedLines];
        // var go = new GameObject();
        // go.name = "TransportLine";
        // var line = go.AddComponent<TransportLine>();
        // go.transform.SetParent(transportLinesOrganizer.transform);
        if (HasStateAuthority)
        {
            var line = Runner.Spawn(Resources.Load<GameObject>("Prefabs/TransportLine"),
                onBeforeSpawned: (runner, no) =>
                {
                    no.GetComponent<NetworkName>().syncedName = "TransportLine";
                    no.transform.SetParent(transportLinesOrganizer.transform);
                    no.GetComponent<TransportLine>().id = lineCount;
                    no.GetComponent<TransportLine>().color = color;
                    no.GetComponent<TransportLine>().uuid = no.GetInstanceID();
                    no.GetComponent<TransportLine>().gameInstance = this;
                }).GetComponent<TransportLine>();
            lines.Set(lineCount, line);
            lineCount++;
            addedLines++;
        }
    }
    public float GetRandomFloat()
    {
        return random.Next(0, 10000) / 10000f;
    }
    public void SetSeed(int seed)
    {
        Debug.Log($"Setting seed: {seed}");
        random = new Random(seed);
    }

    public void SpawnPassengers()
    {
        for (int i = 0; i < stationCount; i++)
        {
            Station station = stations[i];
            var p = GetRandomFloat();
            if (p < 0.15f)
            {
                station.SpawnRandomPassenger();
            }
        }
    }

    public void SpawnStations()
    {
        var p = GetRandomFloat();
        if (p < 0.8f)
        {
            SpawnRandomStation();
        }
    }

    public void SpawnStation(StationType type)
    {
        if (HasStateAuthority)
        {
            GameObject prefab;
            switch (type)
            {
                case StationType.Sphere:
                    prefab = Resources.Load("Prefabs/StationSphere") as GameObject;
                    break;
                case StationType.Cone:
                    prefab = Resources.Load("Prefabs/StationCone") as GameObject;
                    break;
                case StationType.Cube:
                    prefab = Resources.Load("Prefabs/StationCube") as GameObject;
                    break;
                case StationType.Star:
                    prefab = Resources.Load("Prefabs/StationStar") as GameObject;
                    containsStarStation = true;
                    break;
                default:
                    prefab = Resources.Load("Prefabs/StationCube") as GameObject;
                    break;
            }

            // Instantiate the object on the host side
            NetworkObject networkObject = Runner.Spawn(prefab,
                onBeforeSpawned: (runner, no) =>
                {
                    GameObject obj = no.gameObject;

                    // Random position calculations only on the host
                    var radius = 1.5f + 0.5f * this.week + 0.1f * this.day;
                    var offset = new Vector3(0f, 0f, 2f);
                    var pos = new Vector3(0f, 1.0f, 0f) + offset;

                    while (StationTooClose(pos))
                    {
                        float theta = GetRandomFloat() * 360f;
                        float w = GetRandomFloat() * 360f;
                        float r = GetRandomFloat();
                        Vector3 ran = Vector3.forward * r;
                        ran = Quaternion.AngleAxis(theta, Vector3.up) * ran;
                        ran = Quaternion.AngleAxis(w, Vector3.right) * ran;
                        pos = ran * radius + offset;
                        radius += 0.02f;

                        // Clamp position y
                        pos.y = Mathf.Clamp(pos.y, 0.5f, 2.0f);
                    }

                    obj.transform.localPosition = pos;

                    // Set the parent and organize
                    obj.transform.SetParent(stationsOrganizer.transform);

                    // Prepare the station data to be sent to clients
                    Station station = obj.GetComponentInChildren<Station>();
                    station.id = this.stationCount;
                    station.uuid = station.GetInstanceID();
                    station.gameInstance = this;  // Pass in ourselves for access to game state
                });


            // Add to the host's station list
            stations.Set(stationCount, networkObject.GetComponent<Station>());
            this.stationCount++;

            // Send RPC to all clients to replicate the station locally
            // RPC_SpawnStationClient(type, pos, station.id, station.uuid);
        }
    }

    public void SpawnRandomStation()
    {
        if (HasStateAuthority)
        {
            var p = GetRandomFloat();
            var type = StationType.Sphere;

            if (p < 0.1f && !containsStarStation)
                type = StationType.Star;
            else if (p < 0.45f)
                type = StationType.Sphere;
            else if (p < 0.85f)
                type = StationType.Cone;
            else
                type = StationType.Cube;

            // Host will call the SpawnStation and broadcast the result
            // RPC_SpawnStation(type);
            SpawnStation(type);
        }
    }

    public bool StationTooClose(Vector3 pos)
    {
        for (int i = 0; i < stationCount; i++)
        {
            var station = stations[i];
            var d = Vector3.Distance(pos, station.transform.localPosition);
            if (d < 0.5f) return true;
        }
        return false;
    }

    public TransportLine SelectFreeLine()
    {
        for (int i = 0; i < lineCount; i++)
        {
            var line = lines[i];
            Debug.Log(line);
            if (!line.isDeployed)
            {
                Debug.Log("select");
                editingLine = line;
                return line;
            }
        }
        return null;
    }

    public void DeselectLine()
    {
        var line = editingLine;
        if (line != null)
        {
            line.tracks.DisableUISegments();
            var color = line.color;
            color.a = 0.75f;
            if (line.stopCount == 1) line.RemoveAll();
            else
            {
                if (editingIndex == -1) line.tracks.head.SetColor(color);
                else if (editingIndex == line.tracks.segmentCount) line.tracks.tail.SetColor(color);
                else line.tracks.segments[editingIndex].SetColor(color);
            }
        }
        editingLine = null;
    }

    public void StartEditingLine(TransportLine line, int trackIndex, float dist, bool insert)
    {
        editingLine = line;
        editingIndex = trackIndex;
        editingDist = dist;
        editingInsert = insert;
        editing = true;
    }


    public static Vector3 PointerTarget = new Vector3(0, 0, 0);
    void UpdatePointerState()
    {
        // Set PointerTarget vector from primaryPointer 
        var p = CoreServices.InputSystem.FocusProvider.PrimaryPointer;
        if (p != null && p.Result != null)
        {
            var startPoint = p.Position;
            var endPoint = p.Result.Details.Point;
            var hitObject = p.Result.Details.Object;

            if (hitObject != null)
            {
                // Debug.Log("Hit object: " + hitObject);
                var dist = p.Result.Details.RayDistance;
                var offset = 0.1f;
                PointerTarget = RayStep.GetPointByDistance(p.Rays, dist - offset);
            }
            else
            {
                PointerTarget = RayStep.GetPointByDistance(p.Rays, 2.5f);
            }

        }
    }

    public void AddScore(int inc)
    {
        this.score += inc;
        // 
    }

    public void UpdatePassengerRoute()
    {
        // for all station
        for (int i = 0; i < stationCount; i++)
        {
            Station currentStation = stations[i];
            // for all passenger in the station
            for (int j = 0; j < currentStation.passengerCount; j++)
            {
                Passenger currentPassenger = currentStation.passengers[j];

                // TODO: should not always update
                //if (currentPassenger.route == null)
                //{
                // find a route
                // currentPassenger.route = FindRouteClosest(currentStation, currentPassenger.destination);
                var route = FindRouteClosest(currentStation, currentPassenger.destination);
                // convert to list of networkID with Linq map
                var routeId = route.Select(x => x.Object.Id).ToList();

                currentPassenger.routes.CopyFrom(routeId, 0, routeId.Count);
                currentPassenger.routeCount = routeId.Count;

                // Debug print
                string routeString = "";
                for (int k = 0; k < currentPassenger.routeCount; k++)
                {
                    var station = Runner.FindObject(currentPassenger.routes[k]).GetComponent<Station>();
                    routeString += station.id + " ";
                }

                currentStation.passengers.Set(j, currentPassenger);

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
            for (int i = 0; i < stationCount; i++)
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

            if (minIndex == -1)
            {
                print("Failure to find path:\nStart Station Type: " + start.type.ToString() + "\nGoal: " + goal.ToString() + "Available Types: " + stations.ToString());
            }

            // Find the station in closedset that is closest to the goal station
            Station closest = stations[minIndex];
            minDist = Single.PositiveInfinity;
            Station closestConnected = null;
            foreach (var item in result.Item2)
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
        int left = start.passengerCount;
        int right = goal.passengerCount;
        if (left > right)
        {
            return 1.0f;
        }
        else
        {
            return 3.0f;
        }
    }

    void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData)
    {
        Debug.Log("Pointer Clicked");
        MetroManager.SendEvent("Controller clicked: " + gameId);
    }

    void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData)
    {
        //Debug.Log("MetroManager pointer up");
        DeselectLine();

    }

    void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        var point = RayStep.GetPointByDistance(eventData.Pointer.Rays, editingDist);
        if (editingLine != null)
        {
            if (editingIndex <= editingLine.tracks.segmentCount && editingLine.stopCount > 1)
            {
                TrackSegment segment;
                if (editingIndex < 0) segment = editingLine.tracks.head;
                else if (editingIndex == editingLine.tracks.segmentCount) segment = editingLine.tracks.tail;
                else segment = editingLine.tracks.segments[editingIndex];
                var color = editingLine.color;
                color.a = 0.2f;
                // Debug.Log("drag " + editingIndex + " " + segment + " " + color);
                segment.SetColor(color);
            }
            if (editingLine.tracks)
            {
                editingLine.tracks.UpdateUISegment(0, point, editingIndex);
                if (editingInsert)
                    editingLine.tracks.UpdateUISegment(1, point, editingIndex + 1);
            }

        }
    }

    void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData)
    {

    }


    public JSONObject SerializeGameState()
    {

        while (needReset) { }


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
        json.AddField("stationsInserted", insertions);
        insertions = 0;
        json.AddField("stationsRemoved", deletions);
        deletions = 0;
        json.AddField("linesRemoved", linesRemoved);
        linesRemoved = 0;
        json.AddField("linesCreated", linesCreated);
        linesCreated = 0;
        json.AddField("trainsAdded", trainsAdded);
        trainsAdded = 0;
        json.AddField("trainsRemoved", trainsRemoved);
        trainsRemoved = 0;
        return json;
    }


    public JSONObject SerializeStations()
    {
        JSONObject json = new JSONObject(JSONObject.Type.ARRAY);
        for (int i = 0; i < stationCount; i++)
        {
            var s = this.stations[i];
            JSONObject sjson = new JSONObject();
            sjson.AddField("id", s.id);
            sjson.AddField("unique_id", s.uuid);
            sjson.AddField("type", "station");
            sjson.AddField("shape", s.type.ToString());
            sjson.AddField("x", s.position.x);
            sjson.AddField("y", s.position.y);
            sjson.AddField("z", s.position.z);
            sjson.AddField("timer", s.timer);
            sjson.AddField("human_name", s.stationName);
            var passenger_counts = GetPassengerCounts(s.passengers, s.passengerCount);
            foreach (var destination in passenger_counts.Keys)
            {
                sjson.AddField("cnt_" + destination.ToLower(), passenger_counts[destination]);
            }
            json.Add(sjson);
        }
        return json;
    }

    public JSONObject SerializeTransportLines()
    {
        JSONObject json = new JSONObject(JSONObject.Type.ARRAY);
        for (int i = 0; i < lineCount; i++)
        {
            var l = this.lines[i];
            JSONObject ljson = new JSONObject();
            ljson.AddField("id", l.id);
            ljson.AddField("unique_id", l.uuid);
            ljson.AddField("type", "line");
            json.Add(ljson);
        }
        return json;
    }

    public JSONObject SerializeSegments()
    {
        JSONObject json = new JSONObject(JSONObject.Type.ARRAY);
        {
            foreach (var l in this.lines.GetFilledElements(this.lineCount))
            {
                for (int i = 0; i < l.stopCount - 1; i++)
                {
                    JSONObject segment_json = new JSONObject();
                    var s = l.stops[i];
                    var next_s = l.stops[i + 1];
                    segment_json.AddField("type", "segment");
                    segment_json.AddField("length", 20);
                    segment_json.AddField("which_line", l.uuid);
                    segment_json.AddField("from_station", s.uuid);
                    segment_json.AddField("to_station", next_s.uuid);
                    json.Add(segment_json);
                }
            }
        }
        return json;
    }

    public static Dictionary<string, int> GetPassengerCounts(NetworkArray<Passenger> passengers, int count)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        for (int i = 0; i < count; i++)
        {
            var p = passengers[i];
            try
            {
                counts[p.destination.ToString()] += 1;
            }
            catch (KeyNotFoundException)
            {
                counts.Add(p.destination.ToString(), 1);
            }
        }

        return counts;
    }

    public JSONObject SerializeTrains()
    {
        JSONObject trains_json = new JSONObject(JSONObject.Type.ARRAY);
        foreach (var l in this.lines.GetFilledElements(this.lineCount))
        {
            JSONObject json = new JSONObject();
            foreach (var t in l.trains.GetFilledElements(l.trainCount))
            {
                json.AddField("unique_id", t.uuid);
                json.AddField("type", "train");
                json.AddField("position", t.position);
                json.AddField("speed", t.speed);
                json.AddField("direction", t.direction);
                json.AddField("line_id", l.uuid);
                // is there currently no capacity limit?
                var passenger_counts = GetPassengerCounts(t.passengers, t.passengerCount);
                foreach (var destination in passenger_counts.Keys)
                {
                    json.AddField("cnt_" + destination.ToLower(), passenger_counts[destination]);
                }
            }
            trains_json.Add(json);
        }
        return trains_json;
    }

    public void StartSimGame(JSONObject gameState, float gameSpeed, float simLength)
    {
        Debug.Log("Starting Sim Game");
        DeserializeGameState(gameState);
        this.score = 0;
        this.gameSpeed = gameSpeed;
        this.simGame = true;
        this.simLength = simLength;
        Debug.Log("Done");
    }

    public void DeserializeGameState(JSONObject gameState)
    {
        //Reset Game Params
        ResetGameState();
        this.time = gameState["time"].f;
        this.paused = gameState["isPause"].b;
        this.isGameover = gameState["isGameover"].b;

        //Spawn Stations
        var jsonStations = gameState["stations"].list;
        for (int i = 0; i < jsonStations.Count; i++)
        {
            var station = jsonStations[i];

            //Create station
            StationType type = StationType.Cone;
            if (station["shape"].str == "Sphere")
                type = StationType.Sphere;
            else if (station["shape"].str == "Cube")
                type = StationType.Cube;
            else if (station["shape"].str == "Star")
                type = StationType.Star;
            this.SpawnStation(type);

            //SetupStation params
            var actualStation = this.stations[i];
            actualStation.transform.localPosition = new Vector3(station["x"].f, station["y"].f, station["z"].f);
            actualStation.id = (int)station["id"].i;
            actualStation.uuid = (int)station["unique_id"].i;

            //Uncomment to deserialize with passengers aswell.
            //Don't include passengers for scoring individual states
            //
            //actualStation.timer = station["timer"].f; //Gameover Timeout
            /*
            if(station["cnt_cone"] != null){
                Debug.Log(station["cnt_cone"]);
                int cnt_cone = (int)station["cnt_cone"].i;
                for(int x = 0; x<cnt_cone; x++)
                    actualStation.SpawnPassenger(StationType.Cone);
            }
            if(station["cnt_sphere"] != null){
                int cnt_sphere = (int)station["cnt_sphere"].i;
                for(int x = 0; x<cnt_sphere; x++)
                    actualStation.SpawnPassenger(StationType.Sphere);
            }
            if(station["cnt_cube"] != null){
                int cnt_cube = (int)station["cnt_cube"].i;
                for(int x = 0; x<cnt_cube; x++)
                    actualStation.SpawnPassenger(StationType.Cube);
            }
            if(station["cnt_star"] != null){
                int cnt_star = (int)station["cnt_star"].i;
                for(int x = 0; x<cnt_star; x++)
                    actualStation.SpawnPassenger(StationType.Star);
            }
            */
        }


        //Recreate Transit Lines
        var jsonLines = gameState["lines"].list;
        for (int i = 0; i < jsonLines.Count; i++)
        {
            this.AddTransportLine();
            lines[i].uuid = (int)jsonLines[i]["unique_id"].i;
        }

        int current_line_id = 0;
        TransportLine currLine = null;
        foreach (var segment in gameState["segments"].list)
        {
            int which_line = (int)segment["which_line"].i;

            //Check if looking at new line:
            if (which_line != current_line_id)
            {
                // currLine = lines.Find(l => l.uuid == (int)segment["which_line"].i);
                currLine = FusionUtils.Find(lines, lineCount, l => l.uuid == (int)segment["which_line"].i);
                if (currLine == null)
                    continue;

                // Station first = this.stations.Find(s => s.uuid == (int)segment["from_station"].i);
                // Station second = this.stations.Find(s => s.uuid == (int)segment["to_station"].i);
                Station first = FusionUtils.Find(stations, stationCount, s => s.uuid == (int)segment["from_station"].i);
                Station second = FusionUtils.Find(stations, stationCount, s => s.uuid == (int)segment["to_station"].i);

                currLine.AddStation(first);
                currLine.AddStation(second);
            }
            else
            {
                if (currLine == null) continue;

                // Station station = this.stations.Find(s => s.uuid == (int)segment["to_station"].i);
                Station station = FusionUtils.Find(stations, stationCount, s => s.uuid == (int)segment["to_station"].i);
                currLine.AddStation(station);
            }
        }
        for (int i = 0; i < lineCount; i++)
        {
            var line = lines[i];
            for (int j = 0; j < line.trainCount; j++)
            {
                var train = line.trains[j];
                // Destroy(train);
                Runner.Despawn(train.Object);
            }
            line.trains.Clear();
            line.trainCount = 0;
        }

        //Arbritary amount of free trains to place
        this.freeTrains = 1000;
        var jsonTrains = gameState["trains"].list;
        foreach (var train in jsonTrains)
        {
            if (train.IsNull)
            {
                Debug.Log("Skipping Null Train");
                continue;
            }
            // var line = lines.Find(l => l.uuid == (int)train["line_id"].i);
            var line = FusionUtils.Find(lines, lineCount, l => l.uuid == (int)train["line_id"].i);
            if (line == null)
            {
                Debug.Log("Can't find line");
                continue;
            }
            Debug.Log("Adding train to the " + line.color + "line");
            line.AddTrain(train["position"].f, train["direction"].f);
        }
        //Reset free trains to actual value
        this.freeTrains = (int)gameState["freeTrains"].i;

    }
}
