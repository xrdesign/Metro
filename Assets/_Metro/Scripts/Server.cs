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

        // 
        switch(command){
            case "get_state":
                res = MetroManager.SerializeGameState().ToString();
                break;

            case "take_action":
                var args = json["arguments"];
                res = QueueAction(args);              
                break;
            case "get_actions":
                // TODO: I don't know what this is for.
                JSONObject actions = new JSONObject(JSONObject.Type.ARRAY);
                actions.Add("insert_station");
                actions.Add("remove_station");
                actions.Add("remove_track");
                res = actions.ToString();
                break;
            case "reset_game":
                MetroManager.StartGame();
                res = MetroManager.SerializeGameState().ToString();
                break;
            default:
                Debug.LogError("[Server][Metro Service] Received: " + e.Data);
                res = "Error, I don't understand your command.";
                break;
        }

        Send(res);
    }

    protected string QueueAction(JSONObject args)
    {
        var action = args["action"].str;        
        Debug.Log("[Server][Metro Service] take action: " + action);
        
        switch(action){
            case "insert_station":
                var lineIndex = (int)args["line"].n;
                var stationIndex = (int)args["station"].n;
                var index = (int)args["index"].n;
                MetroManager.QueueAction(() => {
                    var line = MetroManager.Instance.lines[lineIndex];
                    var station = MetroManager.Instance.stations[stationIndex];
                    line.InsertStation(index, station);
                });
                
                break;
            case "remove_station":
                lineIndex = (int)args["line"].n;
                stationIndex = (int)args["station"].n;
                MetroManager.QueueAction(() =>
                {
                    var line = MetroManager.Instance.lines[lineIndex];
                    var station = line.stops[stationIndex];
                    line.RemoveStation(station);
                });
                break;
            case "remove_track":
                lineIndex = (int)args["line"].n;
                MetroManager.QueueAction(() =>
                {
                    var line = MetroManager.Instance.lines[lineIndex];
                    line.RemoveAll();
                });
                break;

            default:
                return "Error, action didn't match any valid actions.";
        }
        return "";
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
