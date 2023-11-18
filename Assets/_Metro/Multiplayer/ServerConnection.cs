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
                message.AddField("command", "noop");
                ws.Send(message.ToString());
               //Send noop 
            }
            JSONObject json = new JSONObject(e.Data);
            if(!json.HasField("Games"))
                return;

            uint count = (uint)json["Count"].i;
            Debug.Log(count + ", Games Recieved");
            Debug.Log(e.Data);
            gameStruct[] games = new gameStruct[count];
            for(int i = 0; i<count; i++){
                JSONObject gameData = json["Games"][i];
                games[i] = (ParseGame(gameData)); 
                Debug.Log("Parsing Game: " + gameData.ToString());
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
        game.cnt_p = 0;
        JSONObject stations = data["stations"];
        Debug.Log(stations);
        foreach(var s  in stations.list){
            int numPassengers = 0;
            if(s.HasField("cnt_sphere"))
                numPassengers+=(int)s["cnt_sphere"].i;
            if(s.HasField("cnt_cone"))
                numPassengers+=(int)s["cnt_cone"].i;
            if(s.HasField("cnt_cube"))
                numPassengers+=(int)s["cnt_cube"].i;
            if(s.HasField("cnt_star"))
                numPassengers+=(int)s["cnt_star"].i;
            game.cnt_p = numPassengers > game.cnt_p ? numPassengers : game.cnt_p;
            Debug.Log(s);
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
        game.cnt_l = lines.Count; //free lines
        game.cnt_t = (int) data["freeTrains"].i;
        Debug.Log(game.cnt_p);
        Debug.Log(game.cnt_t);
        return game;
    }


    

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        
    }


}
