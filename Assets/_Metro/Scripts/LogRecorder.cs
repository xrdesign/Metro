using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

// Centralized location for creating replay files
public class LogRecorder : MonoBehaviour
{
  [SerializeField]
  bool enabled = true;
  private Queue<Tuple<uint, BaseEvent>> eventsThisFrame;
  private float currentTime;

  private StreamWriter eventWriter;
  private StreamWriter playerPositionWriter;

  private DateTime date;

  static LogRecorder instance;

  // Constants:
  const int VERSION = 0;

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
    }
    if (!enabled)
    {
      return;
    }

    date = DateTime.Now;
    // Create File
    string logFolder = Path.Join(Application.persistentDataPath, "Logs");
    Debug.Log(date.Year);
    string d = string.Join(
        "-", new string[] { date.Year.ToString(), date.Month.ToString(),
                            date.Day.ToString(), date.Hour.ToString(),
                            date.Minute.ToString(), date.Second.ToString() });
    string logName = d;
    string filePath = Path.Join(logFolder, logName);

    if (!Directory.Exists(logFolder))
    {
      Directory.CreateDirectory(logFolder);
    }
    while (File.Exists(filePath + ".replay"))
    {
      filePath += " (1)";
    }
    filePath += ".replay";

    // Create and open file stream for writing
    eventWriter = new StreamWriter(File.Create(filePath));
    playerPositionWriter =
        new StreamWriter(File.Create(filePath + ("positio" + "n")));

    eventsThisFrame = new Queue<Tuple<uint, BaseEvent>>();
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
    currentTime += Time.deltaTime;
    while (eventsThisFrame.Count > 0)
    {
      Tuple<uint, BaseEvent> pair = eventsThisFrame.Dequeue();
      JSONObject log = new JSONObject();
      log.AddField("TIME", currentTime);
      log.AddField("GAME_ID", pair.Item1);
      log.AddField("EVENT_TYPE", pair.Item2.EventType());
      log.AddField("EVENT", pair.Item2.ToJson());

      eventWriter.WriteLine(log.ToString());
    }
  }

  public static void RecordPosition(Vector3 headPos, Quaternion headRot,
                                    Vector3 gazePoint)
  {
    JSONObject logStep = new JSONObject();
    logStep.AddField("TIME", instance.currentTime);
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
}

public class BaseEvent
{
  public BaseEvent() { }
  public virtual string EventType() { return "BaseEvent"; }
  public virtual JSONObject ToJson() { return new JSONObject(); }
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
