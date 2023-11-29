using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WebSocketSharp;

public class ServerConnection : MonoBehaviour
{
    WebSocket ws;
    public UIManager ui;
    public float polling_period;
    private float timer;
    private bool shouldSendAlert = false;
    int gameToAlert;


    // Start is called before the first frame update
    void Start()
    { 
        timer = 0;
        Debug.Log("attempting to connect to server");
        ws = new WebSocket("ws://localhost:3000/multiplayer");
        ws.OnMessage += (sender, e) => {
            if(timer > polling_period){
                //poll games:
                var message = new JSONObject();
                message.AddField("command", "get_all_states");
                ws.Send(message.ToString());
                timer = 0;
            }
            else{
                var message = new JSONObject();
                var args = new JSONObject();
                if(shouldSendAlert){
                    shouldSendAlert = false;
                    message.AddField("command", "set_alert");
                    args.AddField("value", true);
                    args.AddField("game_id", gameToAlert);
                    message.AddField("arguments", args);
                    Debug.Log("Alert Sent!");
                }
                else
                    message.AddField("command", "noop");
                ws.Send(message.ToString());
               //Send noop 
            }
            JSONObject json = new JSONObject(e.Data);
            Debug.Log(json.ToString());
            if(!json.HasField("Games"))
                return;

            uint count = (uint)json["Count"].i;
            Debug.Log(count + ", Games Recieved");
            Debug.Log(e.Data);
            gameStruct[] games = new gameStruct[count];
            for(int i = 0; i<count; i++){
                JSONObject gameData = json["Games"][i];
                games[i] = (ParseGame(gameData)); 
                games[i].id = i;
            }
            ui.SetGameData(games);
        };
        ws.OnOpen += (sender, e) => {
            Debug.Log("Connected to Server");
            var json = new JSONObject();
            json.AddField("command", "get_all_states");
            ws.Send(json.ToString());
        };
        ws.Connect();
    }

    void OnDisable(){
        ws.Close();
    }

    gameStruct ParseGame(JSONObject data){
        gameStruct game = new gameStruct();
        JSONObject stations = data["stations"];
        foreach(var s  in stations.list){
            if(s.HasField("cnt_sphere"))
                game.p_sphere+=(int)s["cnt_sphere"].i;
            if(s.HasField("cnt_cone"))
                game.p_cone+=(int)s["cnt_cone"].i;
            if(s.HasField("cnt_cube"))
                game.p_cube+=(int)s["cnt_cube"].i;
            if(s.HasField("cnt_star"))
                game.p_star+=(int)s["cnt_star"].i;
            
            string shape = s["shape"].str;
            switch(shape){
            case "Sphere": game.s_sphere++; break;
            case "Cone": game.s_cone++; break;
            case "Cube": game.s_cube++; break;
            case "Star": game.s_star++; break;
            default: Debug.LogError("Unknown shape: " + shape); break;
            }
        }

        List<JSONObject> lines = data["lines"].list;
        JSONObject segments = data["segments"];
        foreach(var s in segments.list){
            var lineID = s["which_line"].i;
            foreach(var line in lines){
                if(line["unique_id"].i == lineID){
                    lines.Remove(line);
                    break;
                }
                continue;
            }
            if(lines.Count <= 0)
                break;
        }
        game.free_lines = lines.Count; //free lines
        game.free_trains = (int) data["freeTrains"].i;
        return game;
    }


    

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        
    }

    public void Alert(int gameID){
        Debug.Log("Set alert should send");
        shouldSendAlert = true;
        gameToAlert = gameID;
    }


}
