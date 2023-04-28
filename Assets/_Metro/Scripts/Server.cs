using System;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;



public class Server : MonoBehaviour
{

    public void Start(){
        var wssv = new WebSocketServer ("ws://localhost:3000");

        wssv.AddWebSocketService<MetroService> ("/metro");
        wssv.Start();

        Debug.Log("[Server] WebSocket opened for MetroService at url ws://localhost:3000/metro");
        // wssv.Stop ();
    }

}




public class MetroService : WebSocketBehavior
{


    protected override void OnOpen(){
        Debug.Log("[Server][Metro Service] Client connected.");
    }
    protected override void OnClose(CloseEventArgs e){
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
    protected override void OnMessage (MessageEventArgs e)
    {
        
        var res = "";
        var json = new JSONObject(e.Data);
        var command = json["command"].str;

        try {
            switch (command) {
                case "get_state":
                    uint gameIDGetState = (uint)json["game_id"].i;
                    res = MetroManager.SerializeGame(gameIDGetState).ToString();
                    break;

                case "take_action":
                    uint gameIDTakeAction = (uint)json["game_id"].i;
                    var args = json["arguments"];
                    res = QueueAction(args, gameIDTakeAction);
                    break;
                case "get_actions":
                    // TODO: I don't know what this is for.
                    JSONObject actions = new JSONObject(JSONObject.Type.ARRAY);
                    actions.Add("insert_station");
                    actions.Add("remove_station");
                    actions.Add("remove_track");
                    res = actions.ToString();
                    break;
                case "reset_game": // TODO: Maybe this should an option for take_action?
                    uint gameIDResetGame = (uint)json["game_id"].i;
                    res = MetroManager.SerializeGame(gameIDResetGame).ToString();
                    MetroManager.ResetGame(gameIDResetGame);
                    break;
                default:
                    Debug.LogError("[Server][Metro Service] Received: " + e.Data);
                    res = "Error, I don't understand your command.";
                    break;
            }
        }
        catch (Exception exception) {
            res = "ERROR: " + exception;
        }


        Send(res);
    }

    // Queues an action for a specific game instance.
    protected string QueueAction(JSONObject args, uint gameID)
    {
        var action = args["action"].str;        
        Debug.Log("[Server][Metro Service] take action: " + action);
        Debug.Log("[Server][Metro Service] full action: " + args);
        
        switch(action){
            case "insert_station":
                // Validating Arguments
                if (!args.HasField("line_index"))
                    throw new Exception("insert_station action missing required input: line_index");
                
                if (!(args.HasField("station_name") || args.HasField("station_index")))
                    throw new Exception("insert_station action missing required input: station_name or station_index");
                
                if (!args.HasField("insert_index"))
                    throw new Exception("insert_station action missing required input: insert_index");
                

                
                bool useStationNameInsert = true;
                string stationNameInsert = "";
                int stationIndexInsert = -1;
                
                if (args.HasField("station_name")) {
                    useStationNameInsert = true;
                    stationNameInsert = args["station_name"].str;
                }
                else {  // Must include station_index due to above checks.
                    useStationNameInsert = false;
                    stationIndexInsert = (int)args["station_index"].i;
                }
                
                var lineIndex = (int)args["line_index"].i;
                var index = (int)args["insert_index"].i;
                MetroManager.QueueGameAction((game) => {
                    var line = game.lines[lineIndex];
                    Station station = null;
                    if (useStationNameInsert) {
                        station = game.GetStationFromName(stationNameInsert);
                        if (!station) throw new Exception("Unable to retrieve station with station name: " + stationNameInsert);
                    }
                    else {
                        station = game.stations[stationIndexInsert];
                        if (!station) throw new Exception("Unable to retrieve station with station index: " + stationIndexInsert);
                    }

                    line.InsertStation(index, station);
                }, gameID);
                
                
                
                break;
            case "remove_station":
                // Validating Arguments
                if (!args.HasField("line_index"))
                    throw new Exception("insert_station action missing required input: line_index");
                
                if (!(args.HasField("station_name") || args.HasField("station_index")))
                    throw new Exception("insert_station action missing required input: station_name or station_index");

                bool useStationNameRemove = true;
                string stationNameRemove = "";
                int stationIndexRemove = -1;
                
                if (args.HasField("station_name")) {
                    useStationNameRemove = true;
                    stationNameRemove = args["station_name"].str;
                }
                else {  // Must include station_index due to above checks.
                    useStationNameRemove = false;
                    stationIndexRemove = (int)args["station_index"].i;
                }

                lineIndex = (int)args["line"].i;
                stationIndexRemove = (int)args["station"].i;
                MetroManager.QueueGameAction((game) =>
                {
                    var line = game.lines[lineIndex];
                    Station station = null;
                    if (useStationNameRemove) {
                        station = game.GetStationFromName(stationNameRemove);
                        if (!station) throw new Exception("Unable to retrieve station with station name: " + stationNameRemove);
                    }
                    else {
                        station = line.stops[stationIndexRemove];
                        if (!station) throw new Exception("Unable to retrieve station with station index: " + stationIndexRemove);
                    }
                    
                    line.RemoveStation(station);
                }, gameID);
                break;
            
            
            case "remove_track":
                // Validating Arguments
                if (!args.HasField("line_index"))
                    throw new Exception("insert_station action missing required input: line_index");
                
                
                lineIndex = (int)args["line_index"].i;
                MetroManager.QueueGameAction((game) =>
                {
                    var line = game.lines[lineIndex];
                    line.RemoveAll();
                }, gameID);
                break;

            default:
                throw new Exception("Error, action didn't match any valid actions.");
        }
        return "Success";
    }

    protected override void OnError (ErrorEventArgs e)
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
