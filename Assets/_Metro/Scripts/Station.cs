using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Physics;
using UnityEngine.Serialization;
using System;

public enum StationType { Sphere, Cone, Cube, Star }

public class Station : MonoBehaviour,
                       IMixedRealityPointerHandler,
                       IMixedRealityFocusHandler
{
  #region Identifiers

  // The station ID is "per game", meaning that stations from different games
  // can have the same ID.
  public int id;

  // This will be unique because it's set to its unity instance ID.
  public int uuid;
  public StationType type;

  // This is a randomly generated human recognizable name. Unique within game.
  public string stationName = "";

  #endregion

  public Vector3 position;
  public float timer = 0.0f; // max 45 seconds for animation + 2s grace period

  // todo: Change to delegates in the future?
  public MetroGame gameInstance;

  public List<Passenger> passengers = new List<Passenger>();
  public Dictionary<StationType, List<Station>> routes =
      new Dictionary<StationType, List<Station>>();
  public float stationEfficiency =
      0; // Passengers Delivered Per Second / Passengers Spawned Per Second

  public string[] passengersRoutes; // Debug purpose

  // Reference to attached Lines for easier pathfinding along lines
  public List<TransportLine> lines;

  // Placed as instanced parameter here. Maybe later on timeout is a per station
  // thing?
  public float MaxTimeoutDuration = 45.0f;

  #region Canvas References

  // public List<GameObject> passengerObject;
  private Image[] seats;

  private Text _stationText;

  #endregion

  static bool dragging = false;
  bool firstDrag = false;

  public Image timerImage;

  float cooldown = 0.0f;

  // Start is called before the first frame update
  public void Init()
  {
    seats = gameObject.GetComponentsInChildren<Image>(true);
    _stationText = gameObject.GetComponentInChildren<Text>(true);
    // Get timeout override from manager.
    MaxTimeoutDuration = MetroManager.Instance.timeoutDurationOverride;
    // Get random station name from manager.
    if (stationName == "")
    {
      stationName =
          $"{id}"; // MetroManager.Instance.GenerateRandomStationName(gameInstance.gameId);
    }

    _stationText.text = stationName;
  }

  // Update is called once per frame
  void Update()
  {
    // TODO: Why have a position variable defined like this?
    // Great question...
    position = transform.localPosition;
    cooldown -= Time.deltaTime;

    foreach (var p in passengers)
    {
      p.waitTime += Time.deltaTime * gameInstance.gameSpeed;
    }

    // show passengers
    foreach (var s in seats)
      s.enabled = false;
    if (passengers.Count > 0)
    {
      seats[0].enabled = true;
      var dir = Vector3.Normalize(Camera.main.transform.position -
                                  transform.position);
      var quat = Quaternion.LookRotation(dir, Camera.main.transform.up);
      seats[0].transform.parent.rotation = quat;
    }
    for (int i = 0; i < passengers.Count; i++)
    {
      seats[i + 1].enabled = true;
      var dest = passengers[i].destination;
      if (dest == StationType.Cube)
        seats[i + 1].sprite = Resources.Load<Sprite>("Images/square");
      else if (dest == StationType.Cone)
        seats[i + 1].sprite = Resources.Load<Sprite>("Images/triangle");
      else if (dest == StationType.Sphere)
        seats[i + 1].sprite = Resources.Load<Sprite>("Images/circle");
      else if (dest == StationType.Star)
        seats[i + 1].sprite = Resources.Load<Sprite>("Images/star");
    }

    // Update overcrowding status
    if (passengers.Count > 6)
    {
      timer += gameInstance.dt;

    }
    else
    {
      timer -= gameInstance.dt;
      if (timer < 0f)
        timer = 0f;
    }
    timerImage.enabled = true;
    timerImage.fillAmount = timer / MaxTimeoutDuration;

    // Update passenger routes
    passengersRoutes = new string[passengers.Count];
    for (int i = 0; i < passengers.Count; i++)
    {
      passengersRoutes[i] = "";
      if (passengers[i].route != null)
      {
        foreach (var s in passengers[i].route)
        {
          passengersRoutes[i] += s.id + " ";
        }
      }
    }
  }

  public void SpawnRandomPassenger()
  {
    // TODO implement better way to set probabilities
    List<StationType> possibleTypes = new List<StationType>();
    if (!(type == StationType.Sphere))
      possibleTypes.Add(StationType.Sphere);
    if (!(type == StationType.Cube))
      possibleTypes.Add(StationType.Cube);
    if (!(type == StationType.Cone))
      possibleTypes.Add(StationType.Cone);
    if (!(type == StationType.Star) && gameInstance.containsStarStation)
      possibleTypes.Add(StationType.Star);

    var pSphere = type == StationType.Sphere ? 0.0f : 0.55f;
    var pCone = type == StationType.Cone ? 0.0f : 0.55f;
    var pCube = type == StationType.Cube ? 0.0f : 0.45f;

    var p = gameInstance.GetRandomFloat();
    int idx = (int)(p * possibleTypes.Count);
    if (idx == possibleTypes.Count)
      idx--;
    SpawnPassenger(possibleTypes[idx]);
  }

  public void SpawnPassenger(StationType type)
  {

    if (passengers.Count >= 30)
      return;
    Passenger p = new Passenger();
    p.destination = type;
    p.gameInstance = this.gameInstance;
    p.waitTime = 0;
    p.travelTime = 0;
    passengers.Add(p);

    // LOG IT!
    PassengerSpawnedEvent e = new PassengerSpawnedEvent(id, type);
    LogRecorder.SendEvent(gameInstance.gameId, e);
  }

  public void StartOvercrowdedTimer() { }
  public void NotifyCircleAnimation() { }

  public float UpdateRoutes()
  {
    this.routes.Clear();
    StationType[] types = (StationType[])Enum.GetValues(typeof(StationType));
    float totalStationScore = 0;
    foreach (StationType type in types)
    {
      float typeScore = 999999999;
      if (type == this.type)
      {
        continue;
      }
      else if (type == StationType.Star &&
                 !gameInstance.containsStarStation)
      {
        continue;
      }

      var result = gameInstance.FindRoute(this, (x) => x.type == type);
      var route = result.Item1; //@TODO Verify if includes start station...
      if (route.Count != 0)
      {
        Debug.Log("HERE");
        Debug.Log("ROUTECOUNT: " + route.Count);
        Station current = this;
        typeScore = 0;
        int lineID = -1;
        float waitTime = 0;
        float distOnCurrLine = 0;
        for (int i = 0; i < route.Count; i++)
        {
          Station next = route[i];
          var neighbors = current.GetNeighbors();
          Debug.Log("Neighbors n: " + neighbors.Count);
          foreach (var neighborPair in current.GetNeighbors())
          {
            if (neighborPair.Key != next)
            {
              continue;
            }
            else
            {
              Debug.Log("GOTHERE");
            }
            if (lineID == -1)
            {
              lineID = neighborPair.Value;
              Debug.Log("LINEID: " + lineID);
            }
            else if (neighborPair.Value != lineID)
            {
              float totalDistance =
                  gameInstance.lines[lineID].tracks.totalLength;
              float worstCase = totalDistance * 2 + distOnCurrLine;
              float bestCase = distOnCurrLine;
              float avg = (worstCase + bestCase) / 2;
              waitTime += avg / .75f;
              distOnCurrLine = 0;
            }
            lineID = neighborPair.Value;
            distOnCurrLine += Vector3.Distance(current.transform.position,
                                               next.transform.position);
          }
          current = route[i];
        }
        if (lineID >= 0)
        {
          float totalDistanceFinal =
              gameInstance.lines[lineID].tracks.totalLength;
          float worstCaseFinal = totalDistanceFinal * 2 + distOnCurrLine;
          float bestCaseFinal = distOnCurrLine;
          float avgFinal = (worstCaseFinal + bestCaseFinal) / 2;
          waitTime += avgFinal / .75f;
        }
        typeScore += waitTime;
      }
      else
      { // @TODO Find route closest
        // Failed to find a connected route, find the closest station to the
        // closet goal station Find the closest station that is goal type
        var goal = type;
        var start = this;
        float minDist = Single.PositiveInfinity;
        int minIndex = -1;
        for (int i = 0; i < gameInstance.stations.Count; i++)
        {
          if (gameInstance.stations[i].type == goal)
          {
            float dist =
                Vector3.Distance(start.transform.position,
                                 gameInstance.stations[i].transform.position);
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
                "Available Types: " + gameInstance.stations.ToString());
        }

        // Find the station in closedset that is closest to the goal station
        Station closest = gameInstance.stations[minIndex];
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
        result = gameInstance.FindRoute(start, (x) => x == closestConnected);
      }
      totalStationScore += typeScore;
      this.routes.Add(type, result.Item1);
    }

    float factor = gameInstance.containsStarStation ? (.25f) : (1.0f / 3.0f);
    totalStationScore *= factor;

    // Train speed ~ .75 units per second...
    // passengers delivered per second =
    // passenger spawn rate ~

    foreach (var passenger in passengers)
    {
      passenger.route = this.routes[passenger.destination];
    }

    this.stationEfficiency = totalStationScore;
    return totalStationScore;
  }

  public List<KeyValuePair<Station, int>> GetNeighbors()
  {
    // for each transport line, find the index of this station on the line
    // store the ref to previous and next station if exists, in the format <ref,
    // line.id>
    List<KeyValuePair<Station, int>> neighbors =
        new List<KeyValuePair<Station, int>>();
    foreach (var line in lines)
    {
      var index = line.stops.IndexOf(this);
      if (index > 0)
      {
        neighbors.Add(
            new KeyValuePair<Station, int>(line.stops[index - 1], line.id));
      }
      if (index < line.stops.Count - 1)
      {
        neighbors.Add(
            new KeyValuePair<Station, int>(line.stops[index + 1], line.id));
      }
    }
    return neighbors;
  }

  void IMixedRealityPointerHandler.OnPointerDown(
      MixedRealityPointerEventData eventData)
  {

    var line = gameInstance.SelectFreeLine();
    if (line != null)
    {
      Debug.Log("station down");
      line.AddStation(this);

      /*
      MetroManager.SendEvent("Select Station: " + "station - " + id +
                             ";line - " + line.id);
     */
      Debug.Log("Select Station: " + "station - " + id + ";line - " + line.id);

      var dist = eventData.Pointer.Result.Details.RayDistance;
      MetroGame.StartEditingLine(line, 0, dist, false);

      eventData.Pointer.IsFocusLocked = false;
      eventData.Pointer.IsTargetPositionLockedOnFocusLock = false;
      // FocusDetails details;
      // CoreServices.FocusProvider.TryGetFocusDetails(eventData.Pointer, out
      // details); Debug.Log(details.Object); details.Object = null;
      // //MetroManager.Instance.stations[1].gameObject; details.Point =
      // MetroManager.Instance.stations[1].gameObject.transform.position; bool
      // ret =
      // CoreServices.FocusProvider.TryOverrideFocusDetails(eventData.Pointer,
      // details);
      firstDrag = false;

      var hapticController =
          eventData.Pointer?.Controller as IMixedRealityHapticFeedback;
      hapticController?.StartHapticImpulse(0.4f, 0.05f);
    }
    else
    {
      // TODO no free line feedback
      // maybe instead create a NUllLine that is returned from SelectFreeLine
      // grey segment with X icon
    }
  }

  void IMixedRealityPointerHandler.OnPointerUp(
      MixedRealityPointerEventData eventData)
  {
    // var line = MetroManager.selectedLine;
    // if( line != null){
    //     if(line.stops.Count == 1) line.RemoveAll();

    // }
    // Debug.Log("station up");
    // MetroManager.DeselectLine();
  }

  void IMixedRealityPointerHandler.OnPointerDragged(
      MixedRealityPointerEventData eventData)
  {
    if (!firstDrag)
      return;
    if (cooldown > 0.0f)
      return;
    firstDrag = false;

    var line = MetroGame.editingLine;
    var index = MetroGame.editingIndex;
    var dist = eventData.Pointer.Result.Details.RayDistance;
    var insert = MetroGame.editingInsert;

    if (line != null)
    {
      /*
      MetroManager.SendEvent("Add Station: " + "station - " + id + ";line - " +
                             line.id);
     */
      Debug.Log("Add Station: " + "station - " + id + ";line - " + line.id);
      Debug.Log("Index: " + index + ", insert: " + insert +
                "Stops Count: " + line.stops.Count);

      // add if not in line (unless closing loop TODO)
      if (!line.stops.Contains(this))
      {
        Debug.Log("Adding to line");
        line.InsertStation(index + 1, this);
        var insrt = index + 1 < line.stops.Count - 1;
        MetroGame.StartEditingLine(line, index + 1, dist, insrt);

        // remove if adjacent to editingIndex
      }
      else if (line.stops.Count > 1)
      {
        Debug.Log("Removing from line");
        if (index == -1)
        {
          Debug.Log("Removing First Station of line");
          if (line.stops[0] == this)
          {
            line.RemoveStation(this);
            if (!line.isDeployed)
              MetroGame.DeselectLine();
          }
        }
        else if (line.stops[index] == this)
        {
          Debug.Log("Top");
          line.RemoveStation(this);
          var insrt = index - 1 >= 0 && index - 1 < line.stops.Count - 1;
          MetroGame.StartEditingLine(line, index - 1, dist, insrt);
          if (!line.isDeployed)
            MetroGame.DeselectLine();
        }
        else if (insert && line.stops[index + 1] == this)
        {
          Debug.Log("Bottom");
          line.RemoveStation(this);
          var insrt = index < line.stops.Count - 1;
          MetroGame.StartEditingLine(line, index, dist, insrt);
          if (!line.isDeployed)
            MetroGame.DeselectLine();
        }
      }

      var hapticController =
          eventData.Pointer?.Controller as IMixedRealityHapticFeedback;
      hapticController?.StartHapticImpulse(0.4f, 0.05f);
      cooldown = 1.0f;
      // TODO trigger add/remove event viz
    }
  }

  void IMixedRealityPointerHandler.OnPointerClicked(
      MixedRealityPointerEventData eventData)
  {

    // Line selected
    // Add Station to selectedLines next index
    // if(MetroManager.selectedLine != null){
    //     Debug.Log("Selected line addStation");
    //     MetroManager.selectedLine.AddStation(this);

    // } else {
    //     // No line selected and no lines attached
    //     // SelectFreeLine and place
    //     if(lines.Count == 0){
    //         Debug.Log("Select free line, addStation");
    //         var line = MetroManager.SelectFreeLine();
    //         if(line != null){
    //             line.AddStation(this);
    //         } // else TODO visualize no free lines somehow
    //     }

    //     // No line selected and one line attached? multiple lines?
    // }
  }

  void IMixedRealityFocusHandler.OnFocusEnter(FocusEventData eventData)
  {
    firstDrag = true;
  }
  void IMixedRealityFocusHandler.OnFocusExit(FocusEventData eventData) { }
}
