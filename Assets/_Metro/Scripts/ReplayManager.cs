using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class ReplayManager : MonoBehaviour
{
  const int VERSION = 1;

  [SerializeField]
  MetroManager metroManager;

  [SerializeField]
  float _timePerTick = .02f; // Time.fixedDeltaTime;
  int currTick = 0;
  float currTime;
  int nextEventTick;
  JSONObject nextEvent;

  [SerializeField]
  int subjectID;
  [SerializeField]
  int sessionID;
  [SerializeField]
  int instanceIdx;

  string dataFolder;
  string replayFilePath;
  string positionReplayFile;

  StreamReader eventStream;
  bool useEyeTracking = false;
  StreamReader positionStream;

  bool initialized = false;
  public bool shouldExecuteTick = false;
  public bool playing = false;

  public float selectedTime;
  public bool jumpToSelectedTime;

  bool loading = false;
  List<MetroGame> games = new List<MetroGame>();

  [SerializeField]
  GameObject headMarkerPrefab;
  [SerializeField]
  GameObject gazeMarkerPrefab;
  GameObject headMarker;
  GameObject gazeMarker;
  [SerializeField]
  LineRenderer gazeLine;
  [SerializeField]
  Material fixationMat;
  [SerializeField]
  Material gazePointMat;
  [SerializeField]
  bool shouldCreateGazePointCSV = false;

  [SerializeField]
  string eyetrackingOutputFile;

  public bool getFixationGameStates = false;
  [SerializeField]
  string fixationInputFile;
  [SerializeField]
  string fixationOutputFile;

  [Header("Replay - Game State Recording")]
  [SerializeField]
  bool shouldRecordGameStates = false;

  [Header("\tEvents To Record Game States")]
  [SerializeField]
  bool onFixation;
  [SerializeField]
  List<string> onEventTypes;

  /*
  public IEnumerator GetGameStates(string filename)
  {
    Reset();

    // Open fixation data if needed:
    StreamReader fixationReader = null;
    if (onFixation)
    {
      yield return new WaitForEndOfFrame();
      string inputPath = Path.Join(dataFolder, fixationInputFile);
      if (!File.Exists(inputPath))
      {
        Debug.LogError("input file doesn't exists");
        yield return null;
      }
      fixationReader = new StreamReader(inputPath);
    }

    // Open output file:
    string filepath = Path.Join(dataFolder, filename);
    if (Directory.Exists(filepath))
    {
      Debug.LogError("output file already exists");
      yield return null;
    }
    StreamWriter sw = new StreamWriter(filepath);

    Queue<JSONObject> fixations = new Queue<JSONObject>();
    if (onFixation)
    {
      string line = "";
      bool isFixation = false;
      while ((line = fixationReader.ReadLine()) != null)
      {
        JSONObject obj = new JSONObject(line);
        if (obj == null)
        {
          Debug.LogError("NULL JSON Object");
          continue;
        }
        bool currentFixation = obj["FIXATION_IDX"].b;
        if (isFixation && !currentFixation)
        {
          isFixation = false;
        }
        else if (!isFixation && currentFixation)
        {
          isFixation = true;
          fixations.Enqueue(obj);
        }
      }
      fixationReader.Close();
    }

    JSONObject nextFixation = null;
    float nextFixationTimeStamp = -1;
    while (nextEventTick >= 0 ||
           (currTime < nextFixationTimeStamp &&
            nextFixationTimeStamp != Mathf.Infinity))
    {
      if (currTime >= nextFixationTimeStamp)
      {
        if (nextFixation != null)
        {
          // Log Fixation:
          //@TODO
        }
        if (fixations.Count > 0)
        {
          nextFixation = fixations.Dequeue();
          nextFixationTimeStamp = nextFixation["TIME"].f;
        }
      }

      if (currTime + _timePerTick >= nextEventTimeStamp)
      {
        if (IsDesiredEvent())
        {
          JSONObject eventTriggered = new JSONObject();
          eventTriggered.AddField("event", nextEvent);

          var gamesBefore = new JSONObject(JSONObject.Type.ARRAY);
          uint numGames = MetroManager.GetNumGames();
          for (uint i = 0; i < numGames; i++)
          {
            gamesBefore.Add(MetroManager.SerializeGame(i));
          }
          eventTriggered.AddField("games_before", gamesBefore);

          SimulateTick(_timePerTick);

          var gamesAfter = new JSONObject(JSONObject.Type.ARRAY);
          for (uint i = 0; i < numGames; i++)
          {
            gamesAfter.Add(MetroManager.SerializeGame(i));
          }
          eventTriggered.AddField("games_after", gamesAfter);
        }
        else
        {
          SimulateTick(_timePerTick);
        }
      }
      else
      {
        SimulateTick(_timePerTick);
      }
    }

    foreach (var fixation in fixations)
    {
      float time = fixation["TIME"].f;
      Debug.Log($"Loading TIME {time}");
      yield return StartCoroutine(LoadTime(time));
      Debug.Log("LOADED TIME");
      JSONObject state = new JSONObject();
      uint numGames = MetroManager.GetNumGames();
      var games = new JSONObject(JSONObject.Type.ARRAY);
      for (uint i = 0; i < numGames; i++)
      {
        games.Add(MetroManager.SerializeGame(i));
      }
      state.AddField("games", games);
      JSONObject objects = new JSONObject(JSONObject.Type.ARRAY);
      JSONObject fixationData = new JSONObject();
      var o = ParseVector(fixation["GAZE_POSITION"].str);
      RaycastHit[] hits = Physics.SphereCastAll(o, .1f, Vector3.forward, 0);
      foreach (var hit in hits)
      {
        var go = hit.collider.gameObject;
        Train t = go.GetComponent<Train>();
        Station s = go.GetComponent<Station>();
        TrackSegment l = go.GetComponent<TrackSegment>();
        if (t != null)
        {
          var obj = new JSONObject();
          obj.AddField("type", "train");
          obj.AddField("uuid", t.uuid);
          objects.Add(obj);
        }
        else if (s != null)
        {
          var obj = new JSONObject();
          obj.AddField("type", "station");
          obj.AddField("id", s.id);
          objects.Add(obj);
        }
        else if (l != null)
        {
          var obj = new JSONObject();
          obj.AddField("type", "segment");
          obj.AddField("id", l.line.uuid);
          objects.Add(obj);
        }
      }
      state.AddField("objects", objects);
      sw.WriteLine(state.ToString());
    }

    sw.Flush();
    sw.Close();
  }

  private bool IsDesiredEvent()
  {
    string type = nextEvent["TYPE"].str;
    foreach (string t in onEventTypes)
    {
      if (t == type)
      {
        return true;
      }
    }
    return false;
  }
  */

  /* Built-Ins / LifeCycle */
  void Awake()
  {
    string logFolder = Path.Join(Application.persistentDataPath, "Logs");
    string subjectFolder = Path.Join(logFolder, $"{subjectID}");
    string sessionFolder = Path.Join(subjectFolder, $"{sessionID}");

    if (!Directory.Exists(sessionFolder))
    {
      Debug.LogError("[ReplayManager] No such subject/session folder");
      return;
    }

    string[] directories = Directory.GetDirectories(
        sessionFolder, $"{instanceIdx}*", SearchOption.AllDirectories);
    if (directories.Length > 1)
    {
      Debug.LogWarning("[ReplayManager] Multiple instances matched search " +
                       "pattern, viewing first replay");
    }
    if (directories.Length <= 0)
    {
      Debug.LogError("[ReplayManager] Error, no replays match search pattern.");
      return;
    }

    // Load FILE
    dataFolder = directories[0];
    string replayFile = Path.Join(dataFolder, "game.replay");
    if (!File.Exists(replayFile))
    {
      Debug.LogError(
          $"[ReplayManager] No replay file found at \"{replayFile}\"");
      return;
    }
    eventStream = new StreamReader(replayFile);

    var positionReplayFilePath = Path.Join(dataFolder, "game.replayposition");
    if (File.Exists(positionReplayFilePath))
    {
      Debug.Log("Using Eye Tracking Replay");
      positionStream = new StreamReader(positionReplayFilePath);
      useEyeTracking = true;
      headMarker = GameObject.Instantiate(headMarkerPrefab);
      headMarker.name = "Head";
      gazeMarker = GameObject.Instantiate(gazeMarkerPrefab);
      gazeMarker.name = "Gaze";
      gazeLine.positionCount = 2;

      // Try to setup the GameObjectFollower for the main camera if there is one
      // Find GameObjectFollower
      GameObjectFollower follower =
          Camera.main.GetComponent<GameObjectFollower>();
      if (follower)
      {
        follower.SetTarget(headMarker);
      }
    }
  }

  void Start() { Reset(); }

  void FixedUpdate()
  {
    if (playing)
    {
      SimulateTick(Time.fixedDeltaTime);
    }
  }
  void Update()
  {
    if (playing)
    {

    }
    else if (shouldExecuteTick)
    {
      SimulateTick(Time.fixedDeltaTime);
      shouldExecuteTick = false;
    }
    //@TODO remove
    else if (jumpToSelectedTime)
    {
      StartCoroutine(LoadTime(selectedTime));
      jumpToSelectedTime = false;
    }
    else if (shouldCreateGazePointCSV)
    {
      shouldCreateGazePointCSV = false;
      StartCoroutine(CreateGazePointCSV(eyetrackingOutputFile));
    }
    /*
    else if (getFixationGameStates)
    {
      getFixationGameStates = false;
      StartCoroutine(GetFixationGameStates(fixationOutputFile));
    }
    */
  }

  /* Interface */
  public void Pause() { playing = false; }
  public void Play() { playing = true; }

  public IEnumerator LoadTime(float targetTime)
  {
    if (!loading)
    {
      loading = true;
      playing = false;

      int targetTick =
          (int)(targetTime /
                Time.fixedDeltaTime); // Truncate time to nearest tick

      if (targetTick < currTick)
      {
        Reset();
        yield return new WaitForEndOfFrame();
      }
      if (targetTick <= 0)
      {
        yield return null;
      }
      while (currTick < targetTick && nextEventTick >= 0)
      {
        SimulateTick(_timePerTick);
      }
      loading = false;
    }
  }

  void SimulateTick(float dt)
  {
    if (!initialized)
    {
      Debug.LogError(
          "[ReplayManager] Trying to simulate tick on unitialized replay");
      return;
    }
    if (nextEventTick < 0)
    {
      Debug.Log("[ReplayManager] Reached end of replay");
      Pause();
      return;
    }

    currTick++;
    currTime = currTick * Time.fixedDeltaTime;
    while (currTick >= nextEventTick)
    {
      ExecuteNextEvent();
      GetNextEvent();
      if (nextEventTick < 0)
      {
        break;
      }
    }

    foreach (var g in games)
    {
      g.ProcessTick(dt);
    }

    if (useEyeTracking)
    {
      // Place head and gaze markers
      while (currTick > nextPositionTick)
      {
        PlaceMarkers();
        GetNextMarker();
        if (nextPositionTick < 0)
        {
          break;
        }
      }
    }
  }
  int nextPositionTick = 0;
  JSONObject nextPositionObject;
  void PlaceMarkers()
  {
    if (!useEyeTracking)
    {
      return;
    }

    if (nextPositionObject == null)
    {
      return;
    }
    // Handle different formats
    if (nextPositionObject["HEAD"])
    {
      headMarker.transform.position =
          ParseVector(nextPositionObject["HEAD"].str);
    }
    else if (nextPositionObject["HEAD_POSITION"])
    {
      headMarker.transform.position =
          ParseVector(nextPositionObject["HEAD_POSITION"].str);
    }

    if (nextPositionObject["GAZE"])
    {
      gazeMarker.transform.position =
          ParseVector(nextPositionObject["GAZE"].str);
    }
    else if (nextPositionObject["GAZE_POSITION"])
    {
      gazeMarker.transform.position =
          ParseVector(nextPositionObject["GAZE_POSITION"].str);
    }

    // Debug.Log("head: " + headMarker.transform.position);

    bool isFixation = nextPositionObject["FIXATION_IDX"] &&
                      nextPositionObject["FIXATION_IDX"].b;
    gazeMarker.GetComponent<Renderer>().material =
        isFixation ? fixationMat : gazePointMat;

    if (nextPositionObject["HEAD_ROTATION"])
    {
      headMarker.transform.rotation =
          ParseQuaternion(nextPositionObject["HEAD_ROTATION"].str);
    }
    else
    {
      // For the old data, this is the best we can have because we don't have
      // head rotation recorded but for the new data, we have head rotation
      // recorded
      headMarker.transform.LookAt(gazeMarker.transform);
    }

    Vector3[] markers = { headMarker.transform.position,
                          gazeMarker.transform.position };
    gazeLine.SetPositions(markers);
  }

  public IEnumerator CreateGazePointCSV(string filename)
  {
    string path =
        Path.Join(Application.persistentDataPath, "EyetrackingCoordinates");
    if (!Directory.Exists(path))
    {
      Directory.CreateDirectory(path);
    }
    string filepath = Path.Join(path, filename);
    if (Directory.Exists(filepath))
    {
      Debug.LogError("output file already exists");
      yield return null;
    }
    StreamWriter sw = new StreamWriter(filepath);

    Reset();
    yield return new WaitForEndOfFrame();
    while (nextPositionTick >= 0)
    {
      SimulateTick(Time.fixedDeltaTime);
      if (gazeMarker != null)
      {
        Vector3 screenPoint =
            Camera.main.WorldToScreenPoint(gazeMarker.transform.position);
        Vector2 normalizedScreenPoint = new Vector2(
            screenPoint.x / Screen.width, screenPoint.y / Screen.height);
        sw.WriteLine(
            $"{currTick * Time.fixedDeltaTime}, {normalizedScreenPoint.x}, {normalizedScreenPoint.y}");
      }
      // Wait for camera position to update
      yield return new WaitForEndOfFrame();
    }
    sw.Flush();
    sw.Close();
  }

  void GetNextMarker()
  {
    if (!useEyeTracking)
    {
      return;
    }
    Debug.Log("Getting next position");
    var l = positionStream.ReadLine();

    // reached EOF
    if (l == "")
    {
      Debug.Log("TEST! EOF");
      nextPositionTick = -1;
      nextPositionObject = null;
      return;
    }

    nextPositionObject = new JSONObject(l);
    if (!nextPositionObject.HasField("TIME"))
    {
      Debug.Log("TEST! NO TIME");
      Debug.Log(l);
      nextPositionObject = null;
      nextPositionTick = -1;
      return;
    }
    nextPositionTick = (int)nextPositionObject["TICK"].i;
  }

  /* Helpers */
  void Reset()
  {
    // Delete any gameobjects
    foreach (MetroGame game in games)
    {
      Destroy(game.gameObject);
    }
    games.Clear();

    // Load header
    eventStream.DiscardBufferedData();
    eventStream.BaseStream.Seek(0, SeekOrigin.Begin);
    if (useEyeTracking)
    {
      positionStream.DiscardBufferedData();
      positionStream.BaseStream.Seek(0, SeekOrigin.Begin);
    }
    string headerRaw = eventStream.ReadLine();

    // Read header
    JSONObject header = new JSONObject(headerRaw);

    int replayVersion = (int)header["VERSION"].i;
    if (replayVersion != VERSION)
    {
      Debug.LogWarning(
          $"[ReplayManager] Loaded replay is of version {replayVersion}, current version is {VERSION}.");
      Debug.LogWarning(
          "[ReplayManager] This may cause issues: backward compatibility is done by manually computing the tick.");
      // return;
    }

    JSONObject managerParams = header["MANAGER_PARAMS"];

    metroManager.daysPerNewTrain = (uint)managerParams["DAYS_PER_TRAIN"].i;
    metroManager.daysPerNewLine = (uint)managerParams["DAYS_PER_LINE"].i;
    metroManager.timeoutDurationOverride = (uint)managerParams["TIMEOUT"].i;

    //@TODO load manager params...
    int numGames = (int)managerParams["NUM_GAMES"].i;
    metroManager.games.Clear();
    metroManager.numGamesToSpawn = (uint)numGames;
    metroManager.SpawnGames();
    foreach (var game in metroManager.games)
    {
      game.simGame = true;
      game.StartGame();
      games.Add(game);
    }
    if (numGames > 0)
    {
      metroManager.SelectGame(games[0]);
    }

    // Load first event:
    GetNextEvent();
    GetNextMarker();

    currTime = 0;
    initialized = true;
  }

  void ExecuteNextEvent()
  {
    Debug.Log("EXECUTING EVENT");
    if (nextEvent == null)
    {
      nextEventTick = -1;
      return;
    }
    int gameID = (int)nextEvent["GAME_ID"].i;
    MetroGame g = games[gameID];
    JSONObject e = nextEvent["EVENT"]; // event parameters

    string eventType = nextEvent["EVENT_TYPE"].str;
    switch (eventType)
    {
      case "StationSpawnedEvent":
        // Type
        string shape = e["TYPE"].str;
        g.SpawnStation(ParseStationType(shape));

        // Position
        g.stations[g.stations.Count - 1].transform.localPosition =
            ParseVector(e["POSITION"].str);
        break;
      case "PassengerSpawnedEvent":
        int stationID = (int)e["STATION_ID"].i;
        StationType type = ParseStationType(e["DESIRED_STATION"].str);
        g.stations[stationID].SpawnPassenger(type);
        break;
      case "LineInsertStationEvent":
        int lineInsertID = (int)e["LINE_ID"].i;
        int stationInserted = (int)e["STATION_ID"].i;
        int insertIndex = (int)e["STOP_INDEX"].i;
        g.lines[lineInsertID].InsertStation(insertIndex,
                                            g.stations[stationInserted], false);
        break;
      case "LineRemoveStationEvent":
        int lineRemoveID = (int)e["LINE_ID"].i;
        int stationRemoved = (int)e["STATION_ID"].i;
        g.lines[lineRemoveID].RemoveStation(g.stations[stationRemoved]);
        break;
      case "LineAddTrainEvent":
        int lineToAddTrain = (int)e["LINE_ID"].i;
        g.lines[lineToAddTrain].AddTrain(0, 1);
        break;
      case "LineRemoveTrainEvent":
        int lineToRemoveTrain = (int)e["LINE_ID"].i;
        g.lines[lineToRemoveTrain].RemoveTrain();
        break;
      case "LineClearedEvent":
        int lineToClear = (int)e["LINE_ID"].i;
        g.lines[lineToClear].RemoveAll();
        break;
      case "BaseEvent":
        Debug.Log("FOOTER");
        break;
      default:
        Debug.LogWarning($"[ReplayManager] Unhandled event type: {eventType}");
        break;
    }
  }

  Vector3 ParseVector(string pStr)
  {
    string trimmed = pStr.Trim('(', ')');
    var p = trimmed.Split(',');
    float x = float.Parse(p[0]);
    float y = float.Parse(p[1]);
    float z = float.Parse(p[2]);
    return new Vector3(x, y, z);
  }

  Quaternion ParseQuaternion(string pStr)
  {
    string trimmed = pStr.Trim('(', ')');
    var p = trimmed.Split(',');
    float x = float.Parse(p[0]);
    float y = float.Parse(p[1]);
    float z = float.Parse(p[2]);
    float w = float.Parse(p[3]);
    return new Quaternion(x, y, z, w);
  }
  StationType ParseStationType(string shape)
  {
    StationType type = StationType.Cone;
    if (shape == "Sphere")
      type = StationType.Sphere;
    else if (shape == "Cube")
      type = StationType.Cube;
    else if (shape == "Star")
      type = StationType.Star;
    return type;
  }

  void GetNextEvent()
  {
    Debug.Log("Getting next event");
    var l = eventStream.ReadLine();

    // reached EOF
    if (l == "")
    {
      nextEventTick = -1;
      nextEvent = null;
      return;
    }

    nextEvent = new JSONObject(l);
    if (!nextEvent.HasField("TIME"))
    {
      nextEvent = null;
      nextEventTick = -1;
      return;
    }
    // if version == 0, then we don't have TICK field, so we need to calculate by TIME / 0.02
    if (nextEvent.HasField("TICK"))
    {
      nextEventTick = (int)nextEvent["TICK"].i;
    }
    else
    {
      nextEventTick = (int)(nextEvent["TIME"].f / _timePerTick);
    }
    // nextEventTick = (int)nextEvent["TICK"].i;
  }
}
