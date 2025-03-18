using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

using System.IO;

public class Server : MonoBehaviour
{

  public void Start()
  {
    SetupLogs();
    var wssv = new WebSocketServer("ws://0.0.0.0:3000");

    wssv.AddWebSocketService<MetroService>("/metro");
    wssv.AddWebSocketService<MetroService>("/multiplayer");
    wssv.Start();

    // remove timeout
    // wssv.WaitTime = System.Threading.Timeout.InfiniteTimeSpan;

    Debug.Log("[Server] WebSocket opened for MetroService at url " +
              "ws://0.0.0.0:3000/metro");
    // wssv.Stop ();
  }

  public static StreamWriter sw;
  public static void SetupLogs()
  {

    string filePath = Path.Combine(Application.persistentDataPath, "Logs/");

    if (!Directory.Exists(filePath))
      Directory.CreateDirectory(filePath);
    if (File.Exists(filePath + "ServerLatest.txt"))
      File.Copy(filePath + "ServerLatest.txt",
                filePath + "ServerPrevious.txt", true);

    // backup previous log

    // start new log
    sw = new StreamWriter(filePath + "ServerLatest.txt");
  }
  void OnDisable() { sw.Flush(); }
}

public class MetroService : WebSocketBehavior
{

  bool enableLogs = true;
  protected override void OnOpen()
  {
    Debug.Log("[Server][Metro Service] Client connected.");
  }
  protected override void OnClose(CloseEventArgs e)
  {
    Debug.Log("[Server][Metro Service] Client disconnected.");
  }

  // Old API (Pre Instanced Games)
  /**
   * Receives json command structure
   * {
   *   command:"",
   *   arguments:{}
   * }
   *
   * commands without arguments: get_state, reset_game, get_actions
   * commands with arguments: take_action, set_data
   *
   * {   // insert station 1 at beggining of line 0
   *   command:"take_action",
   *   arguments: {
   *       action: "insert_station",
   *       line: 0,
   *       station: 1,
   *       index: 0
   *   }
   * {
   *  command: "take_action",
   *  arguments: {
   *      action: "remove_station",
   *      line: 0,
   *      station: 1
   * }
   * }
   * * {
   *  command: "take_action",
   *  arguments: {
   *      action: "remove_track", // remove entire line
   *      line: 0
   * }
   * }
   *
   */
  protected override void OnMessage(MessageEventArgs e)
  {
    if (enableLogs)
    {
      Server.sw.WriteLine(e.Data);
    }
    var res = new JSONObject();
    var json = new JSONObject(e.Data);
    var command = json["command"].str;
    var delaySend = false;

    try
    {
      switch (command)
      {
        case "enable_logs":
          this.enableLogs = true;
          res.AddField("Status", "Success");
          break;
        case "set_alert":
          var args = json["arguments"];
          uint id = (uint)args["game_id"].i;
          bool val = args["value"].b;
          if (MetroManager.SetAlert(id, val))
            res.AddField("Status", "Success");
          else
            res.AddField("Status", "Failure");
          break;

        // Agent can set station cost with this command
        case "set_station_costs":
          uint gameIDSetStationCosts = (uint)json["game_id"].i;
          var stationCosts = json["station_costs"];
          MetroManager.SetStationCosts(gameIDSetStationCosts, stationCosts);
          break;

        // Agent can set recommendation with this command
        case "recommend_insertion":
          var gameIDSetRecommendation = (uint)json["game_id"].i;
          // self.send_and_recieve(json.dumps({
          //   'command': 'recommend_insertion',
          //       'game_id': self.game_id,
          //       'arguments': {
          //     'station_id': best_candidate_station_id,
          //           'insert_position': best_insert_postion,
          //           'line_index': best_chosen_path_index
          //       }
          // }))
          var recommendation = json["arguments"];
          Debug.Log("[Server][Metro Service] Set Insertion Recommendation: " +
                    recommendation.ToString());
          MetroManager.SetInsertionRecommendation(
              gameIDSetRecommendation,
              (int)recommendation["station_id"].i,
              (int)recommendation["insert_position"].i,
              (int)recommendation["line_index"].i);
          break;
        case "get_state":
          uint gameIDGetState = (uint)json["game_id"].i;
          res = MetroManager.SerializeGame(gameIDGetState);
          res.AddField("new_instructions", MetroManager.HasInstructions());
          res.AddField("instruction_text", MetroManager.GetInstructions());
          break;
        case "get_state_sync":
          uint gameIDGetStateSync = (uint)json["game_id"].i;
          delaySend = true;

          // Enqueue request to be processed on the main thread
          MetroManager.RunOnMainThread(() =>
          {
            var stateRes = new JSONObject();
            try
            {
              stateRes = MetroManager.SerializeGame(gameIDGetStateSync);
              stateRes.AddField("new_instructions", MetroManager.HasInstructions());
              stateRes.AddField("instruction_text", MetroManager.GetInstructions());

              Send(stateRes.ToString()); // Respond after Update() runs
            }
            catch (Exception exception)
            {
              stateRes.Clear();
              stateRes.AddField("Status", "Error");
              stateRes.AddField("Error Message", exception.Message);
              stateRes.AddField("Stack Trace", exception.StackTrace);
              Send(stateRes.ToString());
            }

            if (stateRes.ToString().Length > 0)
            {
              // if (enableLogs)
              // {
              //   Server.sw.WriteLine(stateRes.ToString());
              // }
            }
          }, gameIDGetStateSync);
          break;
        case "set_response":
          var response = json["arguments"];
          // MetroManager.SetResponse(response);
          break;


        case "get_all_states":
          var games = new JSONObject(JSONObject.Type.ARRAY);
          uint count = MetroManager.GetNumGames();
          for (uint i = 0; i < count; i++)
          {
            games.Add(MetroManager.SerializeGame(i));
          }
          res.AddField("Status", "Success");
          res.AddField("Games", games);
          res.AddField("Count", count);
          break;
        case "get_action_queue":
          List<uint> queuedActions = MetroManager.GetQueuedActions();
          JSONObject queuedActionsJson = new JSONObject(JSONObject.Type.ARRAY);
          foreach (var queuedAction in queuedActions)
          {
            queuedActionsJson.Add(queuedAction);
          }
          res = queuedActionsJson;
          break;

        case "get_action_finished":
          uint actionIDGetActionStatus = (uint)json["action_id"].i;
          bool actionFinished =
              MetroManager.IsActionFinished(actionIDGetActionStatus);
          res = new JSONObject(actionFinished);
          break;

        case "take_action":
          MetroManager.LogServerMessage(json);
          uint gameIDTakeAction = (uint)json["game_id"].i;
          args = json["arguments"];
          res = QueueAction(args, gameIDTakeAction);
          MetroManager.LogServerMessage(res);
          break;
        case "get_potential_actions":
          JSONObject actions = new JSONObject(JSONObject.Type.ARRAY);
          actions.Add("insert_station");
          actions.Add("remove_station");
          actions.Add("remove_track");
          res = actions;
          break;
        case "reset_game": // TODO: Maybe this should an option for take_action?
          uint gameIDResetGame = (uint)json["game_id"].i;
          res = MetroManager.SerializeGame(gameIDResetGame);
          MetroManager.ResetGame(gameIDResetGame);
          break;
        case "reset_scene":
          for (uint i = 0; i < MetroManager.GetNumGames(); i++)
          {
            MetroManager.ResetGame(i);
          }
          break;
        case "noop":
          res.AddField("Status", "None");
          break;
        default:
          Debug.LogError("[Server][Metro Service] Received: " + e.Data);
          throw new Exception("Unrecognized Command");
      }
    }
    catch (Exception exception)
    {
      res.Clear();
      res.AddField("Status", "Error");
      res.AddField("Error Message", exception.Message);
      res.AddField("Stack Trace", exception.StackTrace);
    }

    if (delaySend)
    {
      return;
    }

    Send(res.ToString());
    if (res.ToString().Length > 0)
    {
      if (enableLogs)
      {
        Server.sw.WriteLine(res.ToString());
      }
    }
  }

  protected void QueueRequest(JSONObject args, uint gameID)
  {

  }

  // Queues an action for a specific game instance.
  protected JSONObject QueueAction(JSONObject args, uint gameID)
  {
    var action = args["action"].str;
    Debug.Log("[Server][Metro Service] take action: " + action);
    Debug.Log("[Server][Metro Service] full action: " + args);

    uint queueID = 0;

    switch (action)
    {
      case "insert_station":
        // Validating Arguments
        if (!args.HasField("line_index"))
          throw new Exception(
              "insert_station action missing required input: line_index");

        if (!(args.HasField("station_name") || args.HasField("station_index")))
          throw new Exception("insert_station action missing required input: " +
                              "station_name or station_index");

        if (!args.HasField("insert_index"))
          throw new Exception(
              "insert_station action missing required input: insert_index");

        bool useStationNameInsert = true;
        string stationNameInsert = "";
        int stationIndexInsert = -1;

        if (args.HasField("station_name"))
        {
          useStationNameInsert = true;
          stationNameInsert = args["station_name"].str;
        }
        else
        { // Must include station_index due to above checks.
          useStationNameInsert = false;
          stationIndexInsert = (int)args["station_index"].i;
        }

        var lineIndex = (int)args["line_index"].i;
        var index = (int)args["insert_index"].i;
        queueID = MetroManager.QueueGameAction((game) =>
        {
          var line = game.lines[lineIndex];
          Station station = null;
          if (useStationNameInsert)
          {
            station = game.GetStationFromName(stationNameInsert);
            if (!station)
              throw new Exception(
                  "Unable to retrieve station with station name: " +
                  stationNameInsert);
          }
          else
          {
            station = game.stations[stationIndexInsert];
            if (!station)
              throw new Exception(
                  "Unable to retrieve station with station index: " +
                  stationIndexInsert);
          }

          line.InsertStation(index, station);
        }, gameID);

        break;
      case "remove_station":
        // Validating Arguments
        if (!args.HasField("line_index"))
          throw new Exception(
              "insert_station action missing required input: line_index");

        if (!(args.HasField("station_name") || args.HasField("station_index")))
          throw new Exception("insert_station action missing required input: " +
                              "station_name or station_index");

        bool useStationNameRemove = true;
        string stationNameRemove = "";
        int stationIndexRemove = -1;

        if (args.HasField("station_name"))
        {
          useStationNameRemove = true;
          stationNameRemove = args["station_name"].str;
        }
        else
        { // Must include station_index due to above checks.
          useStationNameRemove = false;
          stationIndexRemove = (int)args["station_index"].i;
        }

        lineIndex = (int)args["line_index"].i;
        queueID = MetroManager.QueueGameAction((game) =>
        {
          var line = game.lines[lineIndex];
          Station station = null;
          if (useStationNameRemove)
          {
            station = game.GetStationFromName(stationNameRemove);
            if (!station)
              throw new Exception(
                  "Unable to retrieve station with station name: " +
                  stationNameRemove);
          }
          else
          {
            station = line.stops[stationIndexRemove];
            if (!station)
              throw new Exception(
                  "Unable to retrieve station with station index: " +
                  stationIndexRemove);
          }

          line.RemoveStation(station);
        }, gameID);
        break;

      case "remove_track":
        // Validating Arguments
        if (!args.HasField("line_index"))
          throw new Exception(
              "insert_station action missing required input: line_index");

        lineIndex = (int)args["line_index"].i;
        queueID = MetroManager.QueueGameAction((game) =>
        {
          var line = game.lines[lineIndex];
          line.RemoveAll();
        }, gameID);
        break;

      case "add_train":
        if (!args.HasField("line_index"))
          throw new Exception(
              "insert_train action missing required input: line_index");
        lineIndex = (int)args["line_index"].i;
        queueID = MetroManager.QueueGameAction((game) =>
        {
          var line = game.lines[lineIndex];
          line.AddTrain(0, 0);
        }, gameID);
        break;
      case "remove_train":
        if (!args.HasField("line_index"))
          throw new Exception(
              "remove_train action missing required input: line_index");
        lineIndex = (int)args["line_index"].i;
        queueID = MetroManager.QueueGameAction((game) =>
        {
          var line = game.lines[lineIndex];
          line.RemoveTrain();
        }, gameID);
        break;
      default:
        throw new Exception("Error, action didn't match any valid actions.");
    }
    JSONObject res = new JSONObject();
    res.AddField("Status", "Success");
    res.AddField("ActionID", queueID);
    return res;
  }

  protected override void OnError(WebSocketSharp.ErrorEventArgs e)
  {
    Debug.LogError(e.Message);
    Debug.LogError(e.Exception);
  }

  // string InsertStation(int lineIndex, int stationIndex, int index){

  //     // try{
  //         var line = MetroManager.Instance.lines[lineIndex];
  //         var station = MetroManager.Instance.stations[stationIndex];
  //         line.InsertStation(index, station);
  //     // } catch {
  //     //     return "Error in InsertStation";
  //     // }
  //     return "";
  // }
}
