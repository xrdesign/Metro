using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

public class TransportLine : MonoBehaviour
{
  public int id;
  public int uuid;
  public bool isDeployed = false;
  public List<Station> stops = new List<Station>();
  public List<Train> trains = new List<Train>();

  public Tracks tracks = null;

  public Color color;
  public int nextIndex = 0;

  public MetroGame gameInstance;

  // Start is called before the first frame update
  public void Init()
  {
    GameObject go = new GameObject();
    go.name = "Tracks, Transport Line " + color.ToString();
    tracks = go.AddComponent<Tracks>();
    tracks.transform.SetParent(this.transform);
    tracks.line = this;
    tracks.gameInstance = gameInstance; // Pass down game instance reference.
    tracks.Init();
  }

  void Start() { }

  // Update is called once per frame
  void Update() { }
  public void ProcessTick()
  {
    for (int i = 0; i < trains.Count; i++)
    {
      var t = trains[i];
      if (t.deleted)
      {
        trains.Remove(t);
        Destroy(t.gameObject);
        i--;
      }
    }
    tracks.ProcessTick();
    foreach (var t in trains)
    {
      t.ProcessTick();
    }
  }

  // append station as last stop on line
  public void AddStation(Station station)
  {
    InsertStation(stops.Count, station);
  }

  // insert station as stop at arbitrary index
  public void InsertStation(int stopIndex, Station station,
                            bool fromSim = false)
  {
    if (this.stops.Contains(station))
    {
      // only allow if making looping line
      // TODO
      return;
    }

    gameInstance.routesNeedUpdating = true;
    Debug.Log("insert station");
    stops.Insert(stopIndex, station);
    if (stops.Count == 2)
    {
      MetroManager.SendEvent(
          $"Action: LineCreated, Game: {gameInstance.gameId}, Line{id}.");
    }
    if (!station.lines.Contains(this))
      station.lines.Add(this);
    if (!isDeployed)
    {
      gameInstance.linesCreated++;
    }
    isDeployed = true;
    if (stops.Count >= 2 && trains.Count == 0)
    {
      if (!fromSim)
        AddTrain(0.0f, 1.0f);
    }
    tracks.needsUpdate = true;
    gameInstance.insertions++;
    MetroManager.SendEvent(
        $"Action: InsertStation, Game: {gameInstance.gameId}, Station: {station.id}, Line: {id}.");

    // LOG IT!
    LineInsertStationEvent e =
        new LineInsertStationEvent(id, stopIndex, station.id);
    LogRecorder.SendEvent(gameInstance.gameId, e);
  }

  public void RemoveStation(Station station)
  {
    gameInstance.routesNeedUpdating = true;

    station.lines.Remove(this);
    stops.Remove(station);
    if (stops.Count <= 1)
      this.RemoveAll();
    tracks.needsUpdate = true;
    gameInstance.deletions++;
    MetroManager.SendEvent(
        $"Action: RemoveStation, Game: {gameInstance.gameId}, Station: {station.id}, Line: {id}.");

    // LOG IT!
    LineRemoveStationEvent e = new LineRemoveStationEvent(id, station.id);
    LogRecorder.SendEvent(gameInstance.gameId, e);
  }

  public void RemoveAll()
  {
    gameInstance.routesNeedUpdating = true;
    foreach (var s in stops)
    {
      s.lines.Remove(this);
    }
    stops.Clear();
    tracks.needsUpdate = true;
    foreach (var t in trains)
    {
      Destroy(t.gameObject);
    }
    gameInstance.freeTrains += trains.Count;
    trains.Clear();
    isDeployed = false;
    gameInstance.linesRemoved++;
    MetroManager.SendEvent(
        $"Action: RemoveLine, Game: {gameInstance.gameId}, Line: {id}");

    LineClearedEvent e = new LineClearedEvent(id);
    LogRecorder.SendEvent(gameInstance.gameId, e);
  }

  // add train at a specific location
  public void AddTrain(float position, float direction)
  {
    if (gameInstance.freeTrains == 0)
      return;
    gameInstance.freeTrains -= 1;

    var prefab = Resources.Load("Prefabs/Train") as GameObject;
    var go = GameObject.Instantiate(prefab, new Vector3(0, 0, 0),
                                    prefab.transform.rotation) as GameObject;
    go.name = "Train";
    var t = go.GetComponent<Train>();
    // var go = new GameObject();
    // go.name = "Train";
    // var t = go.AddComponent<Train>();
    t.gameInstance = gameInstance;
    t.position = position;
    t.direction = direction;
    t.speed = 0.0f;
    t.line = this;
    t.uuid = t.GetInstanceID();
    t.color = color;
    t.transform.SetParent(gameInstance.trainOrganizer.transform);

    trains.Add(t);
    gameInstance.trainsAdded++;
    MetroManager.SendEvent(
        $"Action: AddTrain, Game: {gameInstance.gameId}, Line: {id}.");
    t.Init();

    LineAddTrainEvent e = new LineAddTrainEvent(id);
    LogRecorder.SendEvent(gameInstance.gameId, e);
  }
  public void RemoveTrain()
  {
    if (this.trains.Count <= 0)
      return;
    trains[0].shouldRemove = true;
    gameInstance.trainsRemoved++;
    MetroManager.SendEvent(
        $"Action: RemoveTrain, Game: {gameInstance.gameId}, Line: {id}.");
    LineRemoveTrainEvent e = new LineRemoveTrainEvent(id);
    LogRecorder.SendEvent(gameInstance.gameId, e);
  }

  public Station FindDestination(int from, int direction, StationType type)
  {
    var distance = 999;
    var index = -1;
    Station station = null;

    for (var i = 0; i < stops.Count; i++)
    {
      var stop = stops[i];
      var d = (i - from);

      if (stop.type == type && System.Math.Abs(d) < distance &&
          d * direction > 0)
      {
        station = stop;
        index = i;
        distance = d;
      }
    }
    return station;
  }

  public Station FindTransfer(int from, int direction, StationType type)
  {
    return null;
  }
}
