using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using LSL;

// Centralized location for creating replay files
public class LogRecorder : MonoBehaviour
{
  [SerializeField]
  bool enabled = true;
  [SerializeField]
  int subjectNumber = 0;
  [SerializeField]
  int sessionID = -1;

  private Queue<Tuple<uint, BaseEvent>> eventsThisFrame;
  private Queue<Tuple<uint, BaseEvent>> lossEventsThisFrame;
  private float currentTime;

  private StreamWriter eventWriter;
  private StreamWriter playerPositionWriter;

  private DateTime date;

  static LogRecorder instance;

  private liblsl.StreamOutlet markerStream;
  private liblsl.StreamOutlet stationLossStream;


  private string _logDir = "";
  public static string logDir
  {
    get { return instance._logDir; }
  }

  // Constants:
  const int VERSION =
      1; // V0 uses time.deltaTime accumulation causing rounding error
         // V1 - new replays store integer TICK count where each tick is
         //      time.fixedDeltaTime to try and avoid rounding errors

  ///
  /// Functions
  ///

  /* Unity Built-Ins */

  // Setup File Access
  void Awake()
  {
    // Singleton
    if (instance != null)
    {
      Debug.LogError("[LogRecorder] Multiple LogRecorders present in scene " +
                     "destroying extra...");
      Destroy(this);
      return;
    }
    else
    {
      instance = this;
#if Unity_EDITOR_OSX 
#else
      liblsl.StreamInfo inf = new liblsl.StreamInfo(
          "ReplayMarkers", "Markers", 1, 0, liblsl.channel_format_t.cf_string);
      markerStream = new liblsl.StreamOutlet(inf);

      liblsl.StreamInfo inf2 = new liblsl.StreamInfo(
          "StationsLoss", "Markers", 1, 0, liblsl.channel_format_t.cf_string);
      stationLossStream = new liblsl.StreamOutlet(inf2);
#endif
    }

    if (!enabled)
    {
      return;
    }

    // Setup Logging Directory:
    date = DateTime.Now;
    string logFolder = Path.Join(Application.persistentDataPath, "Logs");
    if (!Directory.Exists(logFolder))
    {
      Directory.CreateDirectory(logFolder);
    }
    string subjectFolder = Path.Join(logFolder, $"{subjectNumber}");
    if (!Directory.Exists(subjectFolder))
    {
      Directory.CreateDirectory(subjectFolder);
    }
    string sessionFolder = Path.Join(subjectFolder, $"{sessionID}");
    if (!Directory.Exists(sessionFolder))
    {
      Directory.CreateDirectory(sessionFolder);
    }
    string[] subdirectories = Directory.GetDirectories(sessionFolder);
    int matchNumber = subdirectories.Length;
    string d = string.Join(
        "-", new string[] { date.Year.ToString(), date.Month.ToString(),
                            date.Day.ToString(), date.Hour.ToString(),
                            date.Minute.ToString(), date.Second.ToString() });
    _logDir = Path.Join(sessionFolder, $"{matchNumber}--{d}");
    int i = 1;
    while (Directory.Exists(_logDir))
    {
      _logDir += $"({i})";
      i++;
    }
    Directory.CreateDirectory(_logDir);
    string filePath = Path.Join(_logDir, "game.replay");
    // Create and open file stream for writing
    eventWriter = new StreamWriter(File.Create(filePath));
    playerPositionWriter =
        new StreamWriter(File.Create(filePath + ("position")));

    eventsThisFrame = new Queue<Tuple<uint, BaseEvent>>();
    lossEventsThisFrame = new Queue<Tuple<uint, BaseEvent>>();
    currentTime = 0;
  }

  // Print Header
  //@TODO (METRO MANAGER PARAMS)
  void Start()
  {
    if (!enabled)
    {
      return;
    }
    JSONObject header = new JSONObject();
    header.AddField("VERSION", VERSION);
    header.AddField("DATE", date.ToString());
    JSONObject m = new JSONObject();
    m.AddField("NUM_GAMES", MetroManager.Instance.numGamesToSpawn);
    m.AddField("TIMEOUT", MetroManager.Instance.timeoutDurationOverride);
    m.AddField("DAYS_PER_TRAIN", MetroManager.Instance.daysPerNewTrain);
    m.AddField("DAYS_PER_LINE", MetroManager.Instance.daysPerNewLine);
    header.AddField("MANAGER_PARAMS", m);

    eventWriter.WriteLine(header.ToString());
  }

  // Log anything sent this frame:
  void LateUpdate()
  {
    if (!enabled)
    {
      return;
    }
    currentTime = Time.time;
    while (eventsThisFrame.Count > 0)
    {
      Tuple<uint, BaseEvent> pair = eventsThisFrame.Dequeue();
      JSONObject log = new JSONObject();
      log.AddField("TIME", currentTime);
      log.AddField("TICK",
                   MetroManager.Instance.games[(int)pair.Item1].tickCount);
      log.AddField("GAME_ID", pair.Item1);
      log.AddField("EVENT_TYPE", pair.Item2.EventType());
      log.AddField("EVENT", pair.Item2.ToJson());
      if (pair.Item2.EventType() == "StationSpawnedEvent")
      {
        log.AddField(
            "EFFICIENCY_BEFORE",
            MetroManager.Instance.games[(int)pair.Item1].gameEfficiency);
      }
      eventWriter.WriteLine(log.ToString());
#if Unity_EDITOR_OSX 
#else
      markerStream.push_sample(new string[] { log.ToString() });
#endif
    }
    while (lossEventsThisFrame.Count > 0)
    {
      Tuple<uint, BaseEvent> pair = lossEventsThisFrame.Dequeue();
      JSONObject log = new JSONObject();
      log.AddField("TIME", currentTime);
      log.AddField("TICK",
                   MetroManager.Instance.games[(int)pair.Item1].tickCount);
      log.AddField("GAME_ID", pair.Item1);
      log.AddField("EVENT_TYPE", pair.Item2.EventType());
      log.AddField("EVENT", pair.Item2.ToJson());
#if UNITY_EDITOR_OSX
#else
      stationLossStream.push_sample(new string[] { log.ToString() });
      // TODO: DEBUG
      Debug.Log("Pushing sample: " + log.ToString());
#endif
    }
  }

  public static void RecordPosition(Vector3 headPos, Quaternion headRot,
                                    Vector3 gazePoint)
  {
    JSONObject logStep = new JSONObject();
    logStep.AddField("TIME", Time.time);
    logStep.AddField("TICK", MetroManager.Instance.games[0].tickCount);
    logStep.AddField("HEAD_POSITION", headPos.ToString());
    logStep.AddField("HEAD_ROTATION", headRot.ToString());
    logStep.AddField("GAZE_POSITION", gazePoint.ToString());
    instance.playerPositionWriter.WriteLine(logStep.ToString());
  }

  // (print footer?) flush and close
  void OnDestroy()
  {
    if (!enabled)
    {
      return;
    }
    // Footer:
    JSONObject log = new JSONObject();
    BaseEvent e = new BaseEvent();
    log.AddField("TIME", currentTime);
    log.AddField("GAME_ID", 0);
    log.AddField("EVENT_TYPE", e.EventType());
    log.AddField("EVENT", e.ToJson());
    eventWriter.WriteLine(log.ToString());

    eventWriter.Flush();
    eventWriter.Close();

    playerPositionWriter.Flush();
    playerPositionWriter.Close();
  }

  /* Public Interface */

  public static void SendEvent(uint gameID, BaseEvent e)
  {
    if (instance == null)
    {
      Debug.LogError(
          "[LogRecorder] Attempting to log data with no LogRecorder in scene");
      return;
    }
    if (!instance.enabled)
    {
      return;
    }
    instance.eventsThisFrame.Enqueue(new Tuple<uint, BaseEvent>(gameID, e));
  }

  public static void SendLossEvent(uint gameID, BaseEvent e)
  {
    if (instance == null)
    {
      Debug.LogError(
          "[LogRecorder] Attempting to log data with no LogRecorder in scene");
      return;
    }
    if (!instance.enabled)
    {
      return;
    }
    instance.lossEventsThisFrame.Enqueue(new Tuple<uint, BaseEvent>(gameID, e));
  }
}

public class BaseEvent
{
  public BaseEvent() { }
  public virtual string EventType() { return "BaseEvent"; }
  public virtual JSONObject ToJson() { return new JSONObject(); }
}

public class StationsLossEvent : BaseEvent
{
  private int gameID;
  private string lossInfo;

  public StationsLossEvent(int gameID, JSONObject lossInfoJson)
  {
    this.gameID = gameID;
    this.lossInfo = lossInfoJson.ToString();
  }
  public override string EventType() { return "StationsLossEvent"; }
  public override JSONObject ToJson()
  {
    JSONObject o = new JSONObject();
    o.AddField("GAME_ID", gameID);
    o.AddField("LOSS_INFO", lossInfo);
    return o;
  }
}

public class PassengerSpawnedEvent : BaseEvent
{
  private int stationID;
  private StationType desiredStation;

  public PassengerSpawnedEvent(int stationID, StationType desiredStation)
  {
    this.stationID = stationID;
    this.desiredStation = desiredStation;
  }
  public override string EventType() { return "PassengerSpawnedEvent"; }
  public override JSONObject ToJson()
  {
    JSONObject o = new JSONObject();
    o.AddField("STATION_ID", stationID);
    o.AddField("DESIRED_STATION", desiredStation.ToString());
    return o;
  }
}

public class StationSpawnedEvent : BaseEvent
{
  private Vector3 position;
  private StationType type;
  public StationSpawnedEvent(Vector3 position, StationType type)
  {
    this.position = position;
    this.type = type;
  }
  public override string EventType() { return "StationSpawnedEvent"; }

  public override JSONObject ToJson()
  {
    JSONObject o = new JSONObject();
    o.AddField("POSITION", position.ToString());
    o.AddField("TYPE", type.ToString());
    return o;
  }
}

public class LineInsertStationEvent : BaseEvent
{
  private int lineID;
  private int stopIndex;
  private int stationID;

  public LineInsertStationEvent(int lineID, int stopIndex, int stationID)
  {
    this.lineID = lineID;
    this.stopIndex = stopIndex;
    this.stationID = stationID;
  }
  public override string EventType() { return "LineInsertStationEvent"; }
  public override JSONObject ToJson()
  {
    JSONObject o = new JSONObject();
    o.AddField("LINE_ID", lineID);
    o.AddField("STOP_INDEX", stopIndex);
    o.AddField("STATION_ID", stationID);
    return o;
  }
}

public class LineRemoveStationEvent : BaseEvent
{
  private int lineID;
  private int stationID;

  public LineRemoveStationEvent(int lineID, int stationID)
  {
    this.lineID = lineID;
    this.stationID = stationID;
  }
  public override string EventType() { return "LineRemoveStationEvent"; }
  public override JSONObject ToJson()
  {
    JSONObject o = new JSONObject();
    o.AddField("LINE_ID", lineID);
    o.AddField("STATION_ID", stationID);
    return o;
  }
}
public class LineClearedEvent : BaseEvent
{
  private int lineID;

  public LineClearedEvent(int lineID) { this.lineID = lineID; }
  public override string EventType() { return "LineClearedEvent"; }
  public override JSONObject ToJson()
  {
    JSONObject o = new JSONObject();
    o.AddField("LINE_ID", lineID);
    return o;
  }
}

public class LineAddTrainEvent : BaseEvent
{
  private int lineID;

  public LineAddTrainEvent(int lineID) { this.lineID = lineID; }
  public override string EventType() { return "LineAddTrainEvent"; }
  public override JSONObject ToJson()
  {
    JSONObject o = new JSONObject();
    o.AddField("LINE_ID", lineID);
    return o;
  }
}

public class LineRemoveTrainEvent : BaseEvent
{
  private int lineID;

  public LineRemoveTrainEvent(int lineID) { this.lineID = lineID; }
  public override string EventType() { return "LineRemoveTrainEvent"; }
  public override JSONObject ToJson()
  {

    JSONObject o = new JSONObject();
    o.AddField("LINE_ID", lineID);
    return o;
  }
}

public class SyncGamesEvent : BaseEvent
{
  public override string EventType() { return "SyncGamesEvent"; }
  public override JSONObject ToJson()
  {
    JSONObject o = new JSONObject();
    return o;
  }
}
