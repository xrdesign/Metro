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
using UnityEngine.InputSystem.Controls;
using UnityEditor.Search;

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

  public float cost;
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

  private bool isResetting = false; // Flag to track if the coroutine is already running

  private bool isShowingCost = false;

  public Image timerImage;

  float cooldown = 0.0f;
  Material instancedMaterial;
  Color origColor = Color.white;

  private Renderer rend;
  private MaterialPropertyBlock block;

  Interactable interactable;
  InteractableShaderTheme shaderTheme;

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

    // create an instance of the material for color highlighting
    interactable = GetComponent<Interactable>();
    if (interactable != null)
    {
      // // instancedMaterial = renderer.material;
      // // var colorTheme = interactable.ActiveThemes[0];
      // var profile = interactable.Profiles[0];
      // var originalTheme = profile.Themes[0];                         // Theme SO
      // var themeClone = ScriptableObject.Instantiate(originalTheme);  // deep-clones Definitions too
      // themeClone.name += "_Instance";

      // // 2. Swap in your clone
      // profile.Themes[0] = themeClone; // build a Theme from our Definition :contentReference[oaicite:1]{index=1}

      shaderTheme = interactable.ActiveThemes
                       .OfType<InteractableShaderTheme>()
                       .FirstOrDefault();
      // origColor = shaderTheme.StateProperties[0].Values[0].Color;
    }

    rend = GetComponent<Renderer>();
    block = new MaterialPropertyBlock();

    // create a point light source for the station
  }

  private void Awake()
  {
    interactable = GetComponent<Interactable>();

    // 1. Deep‐clone every Theme SO in the profile
    foreach (var profile in interactable.Profiles)
    {
      for (int i = 0; i < profile.Themes.Count; i++)
      {
        var original = profile.Themes[i];
        // JSON‐clone ensures nested Definitions are copied too :contentReference[oaicite:1]{index=1}
        var json = JsonUtility.ToJson(original);
        var clone = ScriptableObject.CreateInstance<Theme>();
        JsonUtility.FromJsonOverwrite(json, clone);
        clone.name = original.name + "_Instance";
        profile.Themes[i] = clone;
      }
    }

    // 2. Trigger Interactable to tear down & rebuild its theme engines
    //    so it uses your clones instead of the shared assets
    interactable.enabled = false;
    interactable.enabled = true;
  }

  public void SetThemeColor(int stateIndex, Color color)
  {
    if (shaderTheme == null) return;

    // Ensure the theme’s StateProperties list is valid
    var prop = shaderTheme.StateProperties.FirstOrDefault();
    if (prop == null) return;

    // Update the color value for that state
    prop.Values[stateIndex].Color = color;

    // reset the state to default 
    interactable.SetState(InteractableStates.InteractableStateEnum.Default, true);
    interactable.ResetAllStates();
  }

  public void SetColor(Color color)
  {
    // Get current block, modify its "_Color" property, then apply it
    rend.GetPropertyBlock(block);
    block.SetColor("_Color", color);
    rend.SetPropertyBlock(block);
  }

  void FixedUpdate()
  {
    // Update overcrowding status
    if (passengers.Count > 6)
    {
      timer += gameInstance.dt;
      // Debug.Log("Overcrowded station: " + id);
    }
    else
    {
      timer -= gameInstance.dt;
      if (timer < 0f)
        timer = 0f;
    }
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

    timerImage.enabled = true;
    // when it's endless mode, no timeout timer icon
    if (MetroManager.Instance.endlessMode)
    {
      timerImage.fillAmount = 0;
    }
    else
    {
      timerImage.fillAmount = timer / MaxTimeoutDuration;
    }

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


    // TODO: station cost display
    _stationText.text = stationName;
    // instancedMaterial.color = origColor;
    if (MetroManager.Instance.showCosts)
    {
      // Debug.Log("Station cost: " + cost);
      isShowingCost = true;
      ShowCost();
    }
    else
    {
      if (isShowingCost)
      {
        isShowingCost = false;
        _stationText.text = stationName; // Reset to original name
        SetThemeColor(0, origColor);
        SetColor(origColor);
      }
    }
  }

  public void ShowCost()
  {
    switch (MetroManager.Instance.costDisplayMode)
    {
      case MetroManager.CostDisplayMode.Name:
        _stationText.text = stationName + " : " + cost.ToString("F2");
        break;
      case MetroManager.CostDisplayMode.Highlight:

        break;
      case MetroManager.CostDisplayMode.Color:
        float norm_cost = gameInstance.GetNormalizedStationCost(cost);
        //-1 * float.Parse(stationName);
        // instancedMaterial.color = GetColor(norm_cost);
        SetThemeColor(0, GetColor(norm_cost));
        SetColor(GetColor(norm_cost));
        break;
    }

    // Start a coroutine to turn off the display mode after 8 seconds only if it's not already running
    if (!isResetting)
    {
      isResetting = true; // Set the flag to true to prevent multiple calls
      StartCoroutine(ResetCostDisplayMode());
    }
  }




  private IEnumerator ResetCostDisplayMode()
  {
    // Wait for 8 seconds
    yield return new WaitForSeconds(8);
    _stationText.text = stationName;
    // instancedMaterial.color = origColor;
    SetThemeColor(0, origColor);
    SetColor(origColor);
    MetroManager.Instance.showCosts = false;
    isResetting = false; // Reset the flag so it can be called again
  }

  public Color GetColor(float value)
  {
    // Check if the value is infinite
    if (value <= -1)
    {
      // Return black for infinite values
      // return Color.black;
      // Debug color
      // -1 returns black, -10 returns white, intermediate values interpolate between black and white
      if (value <= -10)
      {
        return Color.white;
      }
      else if (value <= -1)
      {
        // Interpolate between black (-1) and white (-10)
        float t = Mathf.InverseLerp(-1, -10, value);
        return Color.Lerp(Color.black, Color.white, t);
      }
    }

    // Clamp value to ensure it's within the range [0,1]
    value = Mathf.Clamp01(value);

    // Interpolating between green (0,1,0) and red (1,0,0)
    float r = value;          // Increases from 0 (green) to 1 (red)
    float g = 1 - value;      // Decreases from 1 (green) to 0 (red)
    float b = 0f;             // No blue component

    return new Color(r, g, b);
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
        // Debug.Log("HERE");
        // Debug.Log("ROUTECOUNT: " + route.Count);
        Station current = this;
        typeScore = 0;
        int lineID = -1;
        float waitTime = 0;
        float distOnCurrLine = 0;
        for (int i = 0; i < route.Count; i++)
        {
          Station next = route[i];
          var neighbors = current.GetNeighbors();
          // Debug.Log("Neighbors n: " + neighbors.Count);
          foreach (var neighborPair in current.GetNeighbors())
          {
            if (neighborPair.Key != next)
            {
              continue;
            }
            else
            {
              // Debug.Log("GOTHERE");
            }
            if (lineID == -1)
            {
              lineID = neighborPair.Value;
              // Debug.Log("LINEID: " + lineID);
            }
            else if (neighborPair.Value != lineID)
            {
              float totalDistance =
                  gameInstance.lines[lineID].tracks.totalLength;

              // account for number of trains
              int numTrains = gameInstance.lines[lineID].trains.Count;

              float worstCase =
                  ((totalDistance * 2) / numTrains) + distOnCurrLine;
              float bestCase = distOnCurrLine;

              float avg = (worstCase + bestCase) / 2;
              waitTime += avg / .75f; // account for train speed
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

  // when application exit, set the color (in case of MRTK) to the original
  void OnDestroy()
  {
    if (shaderTheme != null)
    {
      // Reset the color to the original color
      SetThemeColor(0, origColor);
      SetColor(origColor);
    }
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
