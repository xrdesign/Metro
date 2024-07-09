using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class ReplayManager : MonoBehaviour
{
  const int VERSION = 0;
  [SerializeField]
  MetroManager metroManager;

  [SerializeField]
  float _timePerTick = .05f;
  float currTime;
  float nextEventTimeStamp;
  JSONObject nextEvent;

  [SerializeField]
  string replayFilePath;
  StreamReader sr;

  bool initialized = false;
  public bool shouldExecuteTick = false;
  public bool playing = false;

  public float selectedTime;
  public bool jumpToSelectedTime;

  bool loading = false;
  List<MetroGame> games = new List<MetroGame>();

  /* Built-Ins / LifeCycle */

  void Awake()
  {
    // Load FILE
    string replayFile =
        Path.Join(Application.persistentDataPath, "Logs", replayFilePath);
    if (!File.Exists(replayFile))
    {
      Debug.LogError(
          $"[ReplayManager] No replay file found at \"{replayFile}\"");
      return;
    }
    sr = new StreamReader(replayFile);
  }

  void Start() { Reset(); }

  void Update()
  {
    if (playing)
    {
      SimulateTick(Time.deltaTime);
    }
    else if (shouldExecuteTick)
    {
      SimulateTick(_timePerTick);
      shouldExecuteTick = false;
    }
    //@TODO remove
    else if (jumpToSelectedTime)
    {
      StartCoroutine(LoadTime(selectedTime));
      jumpToSelectedTime = false;
    }
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

      if (targetTime < currTime)
      {
        Reset();
        yield return new WaitForEndOfFrame();
      }
      if (targetTime < 0)
      {
        yield return null;
      }
      while (currTime < targetTime && nextEventTimeStamp >= 0)
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
    if (nextEventTimeStamp < 0)
    {
      Debug.Log("[ReplayManager] Reached end of replay");
      Pause();
      return;
    }

    currTime += dt;
    while (currTime > nextEventTimeStamp)
    {
      ExecuteNextEvent();
      GetNextEvent();
      if (nextEventTimeStamp < 0)
      {
        break;
      }
    }

    foreach (var g in games)
    {
      g.ProcessTick(dt);
    }
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
    sr.DiscardBufferedData();
    sr.BaseStream.Seek(0, SeekOrigin.Begin);
    string headerRaw = sr.ReadLine();

    // Read header
    JSONObject header = new JSONObject(headerRaw);

    int replayVersion = (int)header["VERSION"].i;
    if (replayVersion != VERSION)
    {
      Debug.LogError(
          $"[ReplayManager] Loaded replay is of version {replayVersion}, expected version {VERSION}.");
      return;
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

    currTime = 0;
    initialized = true;
  }

  void ExecuteNextEvent()
  {
    Debug.Log("EXECUTING EVENT");
    if (nextEvent == null)
    {
      nextEventTimeStamp = -1;
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
        Debug.LogError($"[ReplayManager] Unhandled event type: {eventType}");
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
    var l = sr.ReadLine();

    // reached EOF
    if (l == "")
    {
      nextEventTimeStamp = -1;
      nextEvent = null;
      return;
    }

    nextEvent = new JSONObject(l);
    if (!nextEvent.HasField("TIME"))
    {
      nextEvent = null;
      nextEventTimeStamp = -1;
      return;
    }
    nextEventTimeStamp = nextEvent["TIME"].f;
  }
}
