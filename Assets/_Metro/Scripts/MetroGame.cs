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

public delegate void GameSelectionDelegateDef(bool selected);

/**
 * SpaceMetro aims to clone mini metro in VR
 * This singleton object initializes and handles global game state and events
 */
public class MetroGame : MonoBehaviour, IMixedRealityPointerHandler
{
  private Random random;
  private int seed = 21;
  public uint gameId;

  public float score = 0.0f;
  public float time = 0.0f;
  public float gameSpeed = 0.0f;

  public float targetGameSpeed = 1.0f;
  public float dt = 0f;
  public float gameEfficiency = 0;

  public int tickCount = 0;

  public int passengersDelivered = 0;
  public float totalPassengerWaitTime = 0;
  public float totalPassengerTravelTime = 0;

  public int freeTrains = 3;
  public Train selectedTrain = null;
  public int freeCars = 0;

  public List<Station> stations = new List<Station>();
  public List<TransportLine> lines = new List<TransportLine>();

  public bool containsStarStation = false;
  public bool containsOtherStation = false;

  public bool paused = false;
  public bool isGameover = false;

  public bool addingTrain = false;
  public bool addedTrain = false;

  public bool Ai_paused = false;

  public float clockTime;
  public int hour;
  public int day;
  public int week;
  public bool routesNeedUpdating = true;

  // TransportLine edit state
  public static bool editing = false;
  public static bool editingInsert = false;
  public static TransportLine editingLine = null;
  public static int editingIndex = 0;
  public static float editingDist = 1.0f;

  public List<float> trackLengths = new List<float>();
  public float totalTrackLength;

  #region Organizational Scene Objects

  public GameObject stationsOrganizer;
  public GameObject transportLinesOrganizer;
  public GameObject trainOrganizer;
  public GameObject alertCylinder;

  private bool setAlert = false;
  private bool alertValue = false;

  #endregion

  public int daysPerTrain = int.MaxValue;
  public int daysPerLine = int.MaxValue;

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

  // Replaced normal Unity actions with this delegate so that we don't need to
  // know about the game instance from where we define the logic for the action.
  // EX: In Server.cs, we can define actions using the delegate parameter
  // MetroGame, and when we invoke here, we simply pass in ourselves. Needed now
  // that we don't have singleton access the game.
  public delegate void MetroGameAction(MetroGame game);
  private Queue ActionQueue = Queue.Synchronized(new Queue());
  private Queue<Action> mainThreadQueue = new Queue<Action>();

  public bool blockRequest = false; // DEBUG: block sync requests

  // Used to link an action and id together so we can later indicate to
  // MetroManager when we complete the action.
  private struct TrackedMetroGameAction
  {
    public MetroGameAction action;
    public uint id;
  }

  private int insertRecommendationStationId = -1;
  private int insertRecommendationIndex = -1;
  private int insertRecommendationLineId = -1;

  #endregion

  private volatile bool needReset = false;
  public bool simGame = false;
  private float simLength = 0;

  public float maxCost = 0;
  public float minCost = 0;

  public Station maxCostStation = null;

  public Light pointLight;

  private static Color[] lineColors = {
    Color.red,
    Color.blue,
    Color.yellow,
    Color.green,
    new Color(107f / 255f, 47f / 255f, 247f / 255f),
  };
  private int addedLines = 0;

  void OnEnable()
  {
    CoreServices.InputSystem?.RegisterHandler<IMixedRealityPointerHandler>(
        this);
  }
  void OnDisable()
  {
    CoreServices.InputSystem?.UnregisterHandler<IMixedRealityPointerHandler>(
        this);
  }

  // Start is called before the first frame update
  void Awake()
  {
    stationsOrganizer = new GameObject("Stations");
    stationsOrganizer.transform.SetParent(this.transform, false);
    transportLinesOrganizer = new GameObject("Transport Lines");
    transportLinesOrganizer.transform.SetParent(this.transform, false);
    trainOrganizer = new GameObject("Trains");
    trainOrganizer.transform.SetParent(this.transform,
                                       worldPositionStays: false);

    alertCylinder = GameObject.Instantiate(
        Resources.Load("Prefabs/AlertCylinder") as GameObject, this.transform);
    alertCylinder.SetActive(false);
    gameSpeed = 0.0f;
  }

  void Start()
  {
    // Create Display Plane:
    if (random == null)
    {
      random = new Random();
    }
    GameObject floor = GameObject.Instantiate(
        Resources.Load("Prefabs/GameFloor") as GameObject);
    floor.transform.SetParent(this.transform, worldPositionStays: false);
    floor.GetComponent<MeshRenderer>().material.SetInt(
        "_IsEven", (gameId % 2 == 0 || gameId == 0) ? 1 : 0);
    floor.transform.Find("Canvas/ID Display").GetComponent<TMP_Text>().text =
        $"{gameId}";

    if (!simGame)
      StartGame();

    // Create a point light for highlighting
    string light_name = "Point Light " + gameId;
    pointLight = new GameObject(light_name).AddComponent<Light>();
    pointLight.type = LightType.Point;
    pointLight.range = 10;
    pointLight.intensity = 0.5f;

  }

  public void StartGame()
  {
    Debug.Log("Start Game " + gameId);
    Debug.Log("freeTrains: " + this.freeTrains);
    this.ResetGameState();
    this.InitializeGameState();
  }

  public void SetAlert(bool active)
  {
    Debug.Log("Setting Alert!");
    setAlert = true;
    alertValue = true;
    return;

    /*
    if(IsGameSelected())
        alertCylinder.SetActive(false);
    else
        alertCylinder.SetActive(active);
        */
  }

  #region Notifications

  // Only called from MetroManager when the selected game has changed.
  public void OnSelectionChange(bool selected)
  {
    /*
    if(selected)
        SetAlert(false);
        */

    // All we do is invoke our delegate for other objects atm.
    if (GameSelectionDelegate !=
        null)
    { // Delegate is null unless assigned to a method.
      GameSelectionDelegate.Invoke(selected);
    }
    if (uiUpdateDelegate != null)
      uiUpdateDelegate.Invoke();
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
    foreach (var station in stations)
    {
      if (station.stationName == stationName)
        return station;
    }

    return null;
  }

  #endregion


  public void SetStationCosts(JSONObject costs)
  {
    // Reset min max
    maxCost = 0;
    minCost = float.PositiveInfinity;

    // costs is the station_costs array
    // update costs for each station from the station_costs array
    for (int i = 0; i < costs.Count; i++)
    {
      JSONObject cost = costs[i];
      int station_id = (int)cost.GetField("station_id").n;
      float station_cost = cost.GetField("cost").n;
      // Find the station with the id
      Station station = stations.Find(x => x.id == station_id);
      if (station != null)
      {
        station.cost = station_cost;
        // if not infinity, update the min and max cost
        if (station_cost != float.PositiveInfinity)
        {
          if (station_cost > maxCost)
          {
            maxCost = station_cost;
            maxCostStation = station;
          }
          if (station_cost < minCost)
            minCost = station_cost;
        }
      }
    }
  }

  public void SetInsertionRecommendation(int station_id, int index, int line_id)
  {
    this.insertRecommendationStationId = station_id;
    this.insertRecommendationIndex = index;
    this.insertRecommendationLineId = line_id;
  }

  public void VisualizeInsertionRecommendation(int station_id, int index, int line_id)
  {
    // TODO: 

    if (MetroManager.Instance.showRecommendation)
    {
      MetroManager.Instance.recomendationLine.gameObject.SetActive(true);
    }
    else
    {
      MetroManager.Instance.recomendationLine.gameObject.SetActive(false);
    }
    // get the line with line_id
    TransportLine line = lines.Find(x => x.id == line_id);
    Tracks tracks = line.tracks;

    // if the tracks.segments is empty then the line is not deployed yet, ignore
    if (tracks.segments.Count == 0)
      return;

    // get the station with station_id
    Station station = stations.Find(x => x.id == station_id);

    // Then the end position is the station position
    Vector3 endPos = station.transform.position;

    TrackSegment startSeg = null;
    // now for the startPos there are two cases: edge and middle
    if (index == 0)
    {
      // get the head of the track
      startSeg = tracks.head;
    }
    else if (index == line.stops.Count)
    {
      // get the tail of the track
      startSeg = tracks.tail;
    }
    else
    {
      // get the segment at index
      // if index within the range of segments, get the segment at index
      // otherwise return
      if (index < 1 || index >= tracks.segments.Count)
        return;
      startSeg = tracks.segments[index - 1];
    }

    // get the middle position of the segment
    LineRenderer lineRenderer = startSeg.lineRenderer;
    Vector3 startPos = lineRenderer.GetPosition((int)(lineRenderer.positionCount / 2)); // middle of the segment

    // set the insertion recommendation
    MetroManager.Instance.recomendationLine.SetPositions(new Vector3[] { startPos, endPos });
  }

  public float GetNormalizedStationCost(float cost)
  {
    if (cost == float.PositiveInfinity)
      return -1;
    return (cost - minCost) / (maxCost - minCost);
  }

  public void SetPaused(bool shouldPause)
  {
    this.paused = shouldPause;

    // send toggling event
    MetroManager.SendEvent((this.paused ? "Game Paused: " : "Game Resumed: ") +
                           gameId);
  }

  public void SetAIEnabled(bool aiEnabled)
  {
    this.Ai_paused = aiEnabled;

    // send toggling event
    MetroManager.SendEvent((this.Ai_paused ? "Ai Paused: " : "Ai Resumed: ") +
                           gameId);
  }

  /// <summary>
  /// Queues an action to be executed. Throws exceptions when AI is paused or
  /// game is paused.
  /// </summary>
  /// <param name="gameAction"></param>
  /// <returns></returns>
  public uint QueueAction(MetroGameAction gameAction)
  {
    if (this.Ai_paused)
      throw new Exception(
          "Cannot Queue Action, AI is paused"); // don't accept actions from AI
                                                // if AI is paused
    if (this.paused)
      throw new Exception(
          "Cannot Queue Action, Game is paused"); // don't accept actions if
                                                  // game is paused
    Debug.Log("Action Queued on MetroGame: " + this.gameId);
    uint newID = MetroManager.GetNextActionQueueID();
    TrackedMetroGameAction trackedMetroGameAction;
    trackedMetroGameAction.action = gameAction;
    trackedMetroGameAction.id = newID;
    lock (ActionQueue.SyncRoot) { ActionQueue.Enqueue(trackedMetroGameAction); }
    return newID;
  }

  public void RunOnMainThread(Action action)
  {
    lock (mainThreadQueue)
    {
      // make sure limit to 256 actions, otherwise drop early actions
      if (mainThreadQueue.Count > 256)
      {
        mainThreadQueue.Dequeue();
      }
      mainThreadQueue.Enqueue(action);
    }
  }

  // Update is called once per frame
  void Update()
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

    if (!blockRequest)
    {
      // Debug.Log("Invoked requests: " + mainThreadQueue.Count);
      while (mainThreadQueue.Count > 0)
      {
        var action = mainThreadQueue.Dequeue();
        action.Invoke();
      }
    }

    if (needReset)
    {
      freeTrains = 3; // todo: need proper reset
      StartGame();
      needReset = false;
      // End this Update step early
      return;
    }

    // if showCost and mode == highlight, move the light to the station
    if (MetroManager.Instance.showCosts &&
        MetroManager.Instance.costDisplayMode == MetroManager.CostDisplayMode.Highlight)
    {
      if (maxCostStation != null)
      {
        pointLight.transform.position = maxCostStation.transform.position;
        pointLight.gameObject.SetActive(true);
      }
      else
      {
        pointLight.gameObject.SetActive(false);
      }
    }
    else
    {
      pointLight.gameObject.SetActive(false);
    }

    if (insertRecommendationStationId != -1)
    {
      VisualizeInsertionRecommendation(insertRecommendationStationId,
                                        insertRecommendationIndex,
                                        insertRecommendationLineId);
    }
  }

  void FixedUpdate()
  {
    if (simGame)
      return;
    ProcessTick(Time.fixedDeltaTime);
  }

  public void ProcessTick(float dt)
  {
    this.dt = dt * gameSpeed;
    tickCount++;
    this.time = tickCount * Time.fixedDeltaTime;

    // Update Passenger's route
    // Only update if tracks updated...
    if (routesNeedUpdating)
    {
      UpdatePassengerRoute();
    }

    // Time progression
    CheckStationTimers(); // check for lose condition

    UpdateClock(); // update clock, grant weekly reward

    UpdatePointerState();

    foreach (var line in lines)
    {
      line.ProcessTick();
    }
  }

  public void ScheduleReset() { this.needReset = true; }

  void ResetGameState()
  {
    tickCount = 0;
    Debug.Log("Resetting game state for " + gameObject.name);
    foreach (var s in stations)
    {
      Destroy(s.gameObject);
    }
    containsStarStation = false;
    stations.Clear();

    foreach (var t in lines)
    {
      t.RemoveAll();
      Destroy(t.tracks.gameObject);
      Destroy(t.gameObject);
    }
    lines.Clear();
    score = 0;
    passengersDelivered = 0;
    totalPassengerWaitTime = 0;
    totalPassengerTravelTime = 0;
    addedLines = 0;

    // want to create a new Random with the same seed to ensure the same
    // sequence of random numbers
    random = new Random(this.seed);
  }

  void InitializeGameState()
  {
    print("Initializing game state for " + gameObject.name);
    if (!simGame)
    {
      SpawnStation(StationType.Cube);
      SpawnStation(StationType.Cone);
      SpawnStation(StationType.Sphere);
    }

    this.addedLines = 0;
    AddTransportLine();
    AddTransportLine();
    AddTransportLine();

    paused = false;
    gameSpeed = targetGameSpeed;
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

    // It looks like delegates are only instantiated when methods are assigned
    // to them, so all but one of the games will have null here.
    if (uiUpdateDelegate != null)
      uiUpdateDelegate.Invoke();
  }

  // Spawn numbers of stations randomly
  public void SpawnStationsWithCount(int count)
  {
    for (int i = 0; i < count; i++)
    {
      var p = GetRandomFloat();
      var type = StationType.Sphere;
      if (p < 0.33f)
        type = StationType.Sphere;
      else if (p < 0.66f)
        type = StationType.Cone;
      else if (p < 1.0f)
        type = StationType.Cube;
      SpawnStation(type);
    }
    Debug.Log("###################### Spawned " + count + " stations ######################");
  }

  public void SpawnOneStarStation()
  {
    SpawnStation(StationType.Star);
    Debug.Log("###################### Spawned 1 star station ######################");
  }

  public void RemoveLongestLine()
  {
    if (lines.Count == 0)
      return;
    TransportLine longestLine = lines[0];
    for (int i = 1; i < lines.Count; i++)
    {
      if (lines[i].stops.Count > longestLine.stops.Count)
        longestLine = lines[i];
    }
    longestLine.RemoveAll();
    // Destroy(longestLine.tracks.gameObject);
    // Destroy(longestLine.gameObject);
    // lines.Remove(longestLine);
    Debug.Log("###################### Deleted longest line ######################");
  }

  public void CheckStationTimers()
  {
    // if the endlessMode is enable, then do not check station timer here
    if (MetroManager.Instance.endlessMode)
    {
      return;
    }

    foreach (Station station in stations)
    {
      float overcrowdedTimerLimit =
          station.MaxTimeoutDuration +
          2.0f; // MaxTimeoutDuration from station + 2 second grace period
      if (station.timer > overcrowdedTimerLimit)
      {
        GameOver();
      }
    }
  }

  public void GameOver()
  {
    // Debug.Log("TODO: Game Over!");
    // SceneManager.LoadScene(0);
    gameSpeed = 0.0f;
    paused = true;

    MetroManager.SendEvent("Game Over: " + gameId + ", " +
                           SerializeGameState().ToString());
    isGameover = true;

    MetroManager.Instance.OnGameover(gameId);
  }

  public void UpdateClock()
  {
    float lengthOfDay = 20.0f; // 1 day 20 seconds

    float gameSpeed = this.gameSpeed;
    if (paused)
    {
      gameSpeed = 0.0f;
    }

    if (dt == 0.0f)
      return;

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
        if (!simGame)
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
    var go = new GameObject();
    go.name = "TransportLine";
    var line = go.AddComponent<TransportLine>();
    go.transform.SetParent(transportLinesOrganizer.transform);
    line.color = color;
    line.id = lines.Count;
    line.uuid = line.GetInstanceID();
    line.gameInstance = this;
    lines.Add(line);
    line.Init();
    addedLines++;
  }
  public float GetRandomFloat() { return random.Next(0, 10000) / 10000f; }
  public void SetSeed(int seed)
  {
    this.seed = seed;
    Debug.Log($"Setting seed: {this.seed}");
    random = new Random(this.seed);
  }

  public void SpawnPassengers()
  {
    foreach (Station station in stations)
    {
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
    print(this.gameObject.name + " spawning station of type " +
          type.ToString());
    GameObject obj;
    switch (type)
    {
      case StationType.Sphere:
        GameObject prefab = Resources.Load("Prefabs/StationSphere") as GameObject;
        obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity)
                  as GameObject;
        break;
      case StationType.Cone:
        prefab = Resources.Load("Prefabs/StationCone") as GameObject;
        obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity)
                  as GameObject;
        break;
      case StationType.Cube:
        prefab = Resources.Load("Prefabs/StationCube") as GameObject;
        obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity)
                  as GameObject;
        break;
      case StationType.Star:
        prefab = Resources.Load("Prefabs/StationStar") as GameObject;
        obj = GameObject.Instantiate(prefab) as GameObject;
        containsStarStation = true;
        break;
      default:
        prefab = Resources.Load("Prefabs/StationCube") as GameObject;
        obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity)
                  as GameObject;
        break;
    }

    Station station = obj.GetComponentInChildren<Station>();

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
      if (pos.y < 0.5f)
        pos.Set(pos.x, 0.5f, pos.z);
      if (pos.y > 2.0f)
        pos.Set(pos.x, 2.0f, pos.z);
    }
    obj.transform.SetParent(stationsOrganizer.transform);
    obj.transform.localPosition = pos;
    // obj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

    station.id = this.stations.Count;
    station.uuid = station.GetInstanceID();
    station.gameInstance = this; // Pass in ourselves for access
    this.stations.Add(station);

    station.Init();

    // LOG IT!
    StationSpawnedEvent e =
        new StationSpawnedEvent(station.transform.localPosition, station.type);
    LogRecorder.SendEvent(gameId, e);
  }

  public void SpawnRandomStation()
  {
    var p = GetRandomFloat();
    var type = StationType.Sphere;

    // if willRandomlySpawnStarStation is false, then scale the p up to 0.1~1.0 so that no star station will be spawned
    if (!MetroManager.Instance.willRandomlySpawnStarStation)
    {
      p = 0.1f + p * 0.9f; // scale to 0.1~1.0
    }

    if (p < .1f && !containsStarStation)
      type = StationType.Star;
    else if (p < 0.45f)
      type = StationType.Sphere;
    else if (p < 0.85f)
      type = StationType.Cone;
    else if (p < 1.0f)
      type = StationType.Cube;
    SpawnStation(type);
  }

  public bool StationTooClose(Vector3 pos)
  {
    foreach (var station in this.stations)
    {
      var d = Vector3.Distance(pos, station.transform.localPosition);
      if (d < 0.5f)
        return true;
    }
    return false;
  }

  public TransportLine SelectFreeLine()
  {
    foreach (var line in this.lines)
    {
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

  public static void DeselectLine()
  {
    var line = editingLine;
    if (line != null)
    {
      line.tracks.DisableUISegments();
      var color = line.color;
      color.a = 0.75f;
      if (line.stops.Count == 1)
        line.RemoveAll();
      else
      {
        if (editingIndex == -1)
          line.tracks.head.SetColor(color);
        else if (editingIndex == line.tracks.segments.Count)
          line.tracks.tail.SetColor(color);
        else
          line.tracks.segments[editingIndex].SetColor(color);
      }
    }
    editingLine = null;
  }

  public static void StartEditingLine(TransportLine line, int trackIndex,
                                      float dist, bool insert)
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
    if (CoreServices.InputSystem == null)
    {
      return;
    }
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

    float score = 0;
    for (int i = 0; i < stations.Count; i++)
    {
      Station currentStation = stations[i];
      float stationScore = currentStation.UpdateRoutes();
      score += stationScore;
    }
    this.gameEfficiency = score / stations.Count;
    return;

    // OLD CODE BELOW CAN LIKELY REMOVE @TODO

    // for all station
    for (int i = 0; i < stations.Count; i++)
    {
      Station currentStation = stations[i];

      // for all passenger in the station
      for (int j = 0; j < currentStation.passengers.Count; j++)
      {
        Passenger currentPassenger = currentStation.passengers[j];

        // TODO: should not always update
        // if (currentPassenger.route == null)
        //{
        // find a route
        currentPassenger.route =
            FindRouteClosest(currentStation, currentPassenger.destination);

        // Debug print
        string routeString = "";
        for (int k = 0; k < currentPassenger.route.Count; k++)
        {
          routeString += currentPassenger.route[k].id + " ";
        }
        // Debug.Log("Passenger is going from " + currentStation.uuid + " to " +
        // currentPassenger.destination + " via [ " + routeString + "]");
        //}
        // else
        //{
        // // if passenger is current at the end of the route, null it
        // if (currentPassenger.route[currentPassenger.route.Count - 1] ==
        // currentStation)
        // {
        //     currentPassenger.route = null;
        //     //continue;
        // }
        //}
      }
    }
    /*
    foreach(var line in lines){
        foreach(var train in line.trains){
            Station station = line.stops[train.nextStop];
            foreach(var passenger in train.passengers){
                passenger.route = FindRouteClosest(station,
    passenger.destination); passenger.route.Insert(0, station);
            }
        }
    }
    */
  }

  public List<Station> ReconstructRoute(Station start, Station end,
                                        Dictionary<Station, Station> cameFrom)
  {
    List<Station> route = new List<Station>();
    Station current = end;
    while (current != start)
    {
      route.Add(current);
      current = cameFrom[current];
    }
    // route.Add(current);
    route.Reverse();
    return route;
  }

  public List<Station> FindRouteClosest(Station start, StationType goal)
  {
    var result = FindRoute(start, (x) => x.type == goal);
    if (result.Item1.Count == 0)
    {
      // Failed to find a connected route, find the closest station to the
      // closet goal station Find the closest station that is goal type
      float minDist = Single.PositiveInfinity;
      int minIndex = -1;
      for (int i = 0; i < stations.Count; i++)
      {
        if (stations[i].type == goal)
        {
          float dist = Vector3.Distance(start.transform.position,
                                        stations[i].transform.position);
          if (dist < minDist)
          {
            minDist = dist;
            minIndex = i;
          }
        }
      }

      if (minIndex == -1)
      {
        print("Failure to find path:\nStart Station Type: " +
              start.type.ToString() + "\nGoal: " + goal.ToString() +
              "Available Types: " + stations.ToString());
      }

      // Find the station in closedset that is closest to the goal station
      Station closest = stations[minIndex];
      minDist = Single.PositiveInfinity;
      Station closestConnected = null;
      foreach (var item in result.Item2)
      {
        float dist = Vector3.Distance(closest.transform.position,
                                      item.Key.transform.position) +
                     item.Value; // TODO: weight the fScore and distance
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
  public Tuple<List<Station>, Dictionary<Station, float>>
  FindRoute(Station start, Func<Station, bool> criteria)
  {
    // use A * to find a shortest route, if no route found, find the route to
    // the closest station to the target
    List<Station> route = new List<Station>();
    List<Station> closedSet = new List<Station>();
    // openSet is a sorted List with fScore as priority, lowest fScore is the
    // first element
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
        float tentative_gScore =
            gScore[current] + Vector3.Distance(current.transform.position,
                                               neighbor.transform.position);

        float neighborgScore = Single.PositiveInfinity;
        if (gScore.ContainsKey(neighbor))
        {
          neighborgScore = gScore[neighbor];
        }
        if (tentative_gScore < neighborgScore)
        {
          cameFrom[neighbor] = current;
          gScore[neighbor] = tentative_gScore;
          fScore[neighbor] =
              gScore[neighbor] + HeuristicCostEstimate(start, neighbor);
          if (!openSet.ContainsValue(neighbor))
          {
            while (openSet.ContainsKey(fScore[neighbor]))
            {
              fScore[neighbor] += 0.0001f;
            }
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


  void IMixedRealityPointerHandler.OnPointerDown(
      MixedRealityPointerEventData eventData)
  {
    Debug.Log("Pointer Clicked");
    MetroManager.SendEvent("Controller clicked: " + gameId);
  }

  void IMixedRealityPointerHandler.OnPointerUp(
      MixedRealityPointerEventData eventData)
  {
    // Debug.Log("MetroManager pointer up");
    DeselectLine();
  }

  void IMixedRealityPointerHandler.OnPointerDragged(
      MixedRealityPointerEventData eventData)
  {
    var point = RayStep.GetPointByDistance(eventData.Pointer.Rays, editingDist);
    if (editingLine != null)
    {
      if (editingIndex <= editingLine.tracks.segments.Count &&
          editingLine.stops.Count > 1)
      {
        TrackSegment segment;
        if (editingIndex < 0)
          segment = editingLine.tracks.head;
        else if (editingIndex == editingLine.tracks.segments.Count)
          segment = editingLine.tracks.tail;
        else
          segment = editingLine.tracks.segments[editingIndex];
        var color = editingLine.color;
        color.a = 0.2f;
        // Debug.Log("drag " + editingIndex + " " + segment + " " + color);
        segment.SetColor(color);
      }
      editingLine.tracks.UpdateUISegment(0, point, editingIndex);
      if (editingInsert)
        editingLine.tracks.UpdateUISegment(1, point, editingIndex + 1);
    }
  }

  void IMixedRealityPointerHandler.OnPointerClicked(
      MixedRealityPointerEventData eventData)
  { }

  public JSONObject SerializeGameState()
  {

    // while (needReset)
    // {
    // }

    // JSONObject json = new JSONObject(JsonUtility.ToJson(Instance));
    JSONObject json = new JSONObject();

    json.AddField("score", this.score);
    json.AddField("efficiency", this.gameEfficiency);
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
    foreach (var s in this.stations)
    {
      JSONObject sjson = new JSONObject();
      sjson.AddField("id", s.id);
      sjson.AddField("unique_id", s.uuid);
      sjson.AddField("type", "station");
      sjson.AddField("shape", s.type.ToString());
      sjson.AddField("efficiency", s.stationEfficiency);
      sjson.AddField("x", s.position.x);
      sjson.AddField("y", s.position.y);
      sjson.AddField("z", s.position.z);
      sjson.AddField("timer", s.timer);
      sjson.AddField("human_name", s.stationName);
      var passenger_counts = GetPassengerCounts(s.passengers);
      foreach (var destination in passenger_counts.Keys)
      {
        sjson.AddField("cnt_" + destination.ToLower(),
                       passenger_counts[destination]);
      }
      json.Add(sjson);
    }
    return json;
  }

  public JSONObject SerializeTransportLines()
  {
    JSONObject json = new JSONObject(JSONObject.Type.ARRAY);
    foreach (var l in this.lines)
    {
      JSONObject ljson = new JSONObject();
      ljson.AddField("id", l.id);
      ljson.AddField("unique_id", l.uuid);
      ljson.AddField("type", "line");
      JSONObject stops = new JSONObject(JSONObject.Type.ARRAY);
      foreach (var station in l.stops)
      {
        JSONObject stop = new JSONObject();
        stop.AddField("id", station.id);
        stop.AddField("uuid", station.uuid);
        stops.Add(stop);
      }
      ljson.AddField("stops", stops);
      json.Add(ljson);
    }
    return json;
  }

  public JSONObject SerializeSegments()
  {
    JSONObject json = new JSONObject(JSONObject.Type.ARRAY);
    foreach (var l in this.lines)
    {
      for (int i = 0; i < l.stops.Count - 1; i++)
      {
        JSONObject segment_json = new JSONObject();
        var s = l.stops[i];
        var next_s = l.stops[i + 1];
        segment_json.AddField("type", "segment");
        segment_json.AddField("length", 0);

        // Vector3.Distance(s.transform.position, next_s.transform.position));
        segment_json.AddField("which_line", l.uuid);
        segment_json.AddField("from_station", s.uuid);
        segment_json.AddField("to_station", next_s.uuid);
        json.Add(segment_json);
      }
    }
    return json;
  }

  public static Dictionary<string, int>
  GetPassengerCounts(List<Passenger> passengers)
  {
    Dictionary<string, int> counts = new Dictionary<string, int>();
    foreach (var p in passengers)
    {
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
    foreach (var l in this.lines)
    {
      JSONObject json = new JSONObject();
      foreach (var t in l.trains)
      {
        json.AddField("unique_id", t.uuid);
        json.AddField("type", "train");
        json.AddField("position", t.position);
        json.AddField("speed", t.speed);
        json.AddField("direction", t.direction);
        json.AddField("line_id", l.uuid);
        // is there currently no capacity limit?
        var passenger_counts = GetPassengerCounts(t.passengers);
        foreach (var destination in passenger_counts.Keys)
        {
          json.AddField("cnt_" + destination.ToLower(),
                        passenger_counts[destination]);
        }
      }
      trains_json.Add(json);
    }
    return trains_json;
  }

  /*
  public void StartSimGame(JSONObject gameState, float gameSpeed,
                           float simLength)
  {
    Debug.Log("Starting Sim Game");
    DeserializeGameState(gameState);
    this.score = 0;
    this.gameSpeed = gameSpeed;
    this.simGame = true;
    this.simLength = simLength;
    Debug.Log("Done");
  }
  */

  public void DeserializeGameState(JSONObject gameState)
  {
    // Reset Game Params
    ResetGameState();
    this.time = gameState["time"].f;
    this.paused = gameState["isPause"].b;
    this.isGameover = gameState["isGameover"].b;

    // Spawn Stations
    var jsonStations = gameState["stations"].list;
    for (int i = 0; i < jsonStations.Count; i++)
    {
      var station = jsonStations[i];

      // Create station
      StationType type = StationType.Cone;
      if (station["shape"].str == "Sphere")
        type = StationType.Sphere;
      else if (station["shape"].str == "Cube")
        type = StationType.Cube;
      else if (station["shape"].str == "Star")
        type = StationType.Star;
      this.SpawnStation(type);

      // SetupStation params
      var actualStation = this.stations[i];
      actualStation.transform.localPosition =
          new Vector3(station["x"].f, station["y"].f, station["z"].f);
      actualStation.id = (int)station["id"].i;
      actualStation.uuid = (int)station["unique_id"].i;

      // Uncomment to deserialize with passengers aswell.
      // Don't include passengers for scoring individual states
      //
      // actualStation.timer = station["timer"].f; //Gameover Timeout
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

    // Recreate Transit Lines
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

      // Check if looking at new line:
      if (which_line != current_line_id)
      {
        currLine = lines.Find(l => l.uuid == (int)segment["which_line"].i);
        if (currLine == null)
          continue;

        Station first =
            this.stations.Find(s => s.uuid == (int)segment["from_station"].i);
        Station second =
            this.stations.Find(s => s.uuid == (int)segment["to_station"].i);

        currLine.AddStation(first);
        currLine.AddStation(second);
      }
      else
      {
        if (currLine == null)
          continue;

        Station station =
            this.stations.Find(s => s.uuid == (int)segment["to_station"].i);
        currLine.AddStation(station);
      }
    }
    foreach (var line in lines)
    {
      foreach (var train in line.trains)
      {
        Destroy(train);
      }
      line.trains.Clear();
    }

    // Arbritary amount of free trains to place
    this.freeTrains = 1000;
    var jsonTrains = gameState["trains"].list;
    foreach (var train in jsonTrains)
    {
      if (train.IsNull)
      {
        Debug.Log("Skipping Null Train");
        continue;
      }
      var line = lines.Find(l => l.uuid == (int)train["line_id"].i);
      if (line == null)
      {
        Debug.Log("Can't find line");
        continue;
      }
      Debug.Log("Adding train to the " + line.color + "line");
      line.AddTrain(train["position"].f, train["direction"].f);
    }
    // Reset free trains to actual value
    this.freeTrains = (int)gameState["freeTrains"].i;
  }

  public void CalcuateScore()
  {
    StationType[] types = (StationType[])Enum.GetValues(typeof(StationType));

    float totalGameScore = 0;
    foreach (Station station in this.stations)
    {
      float totalStationScore = 0;
      foreach (StationType type in types)
      {
        float typeScore = 999999999;
        if (type == station.type)
        {
          continue;
        }
        else if (type == StationType.Star && !this.containsStarStation)
        {
          continue;
        }

        var result = FindRoute(station, (x) => x.type == type);
        var route = result.Item1; //@TODO Verify if includes start station...
        if (route.Count != 0)
        {
          typeScore = 0;
          for (int i = 1; i < route.Count; i++)
          {
            Station a = route[i - 1];
            Station b = route[i];
            typeScore +=
                Vector3.Distance(a.transform.position, b.transform.position);
          }
        }
        totalStationScore += typeScore;
      }

      float factor = containsStarStation ? (.25f) : (1f / 3f);
      totalStationScore *= factor;
    }
  }
}
