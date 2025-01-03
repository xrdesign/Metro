using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

using System.IO;

public class SimServer : MonoBehaviour
{

    WebSocketServer wssv;
    public void Start()
    {
        metroManager = GameObject.FindGameObjectWithTag("MetroManager").GetComponent<MetroManager>();
        wssv = new WebSocketServer("ws://0.0.0.0:3000");

        wssv.AddWebSocketService<SimService>("/metroSim");
        wssv.Start();

        Debug.Log("[Server] WebSocket opened for MetroService at url ws://0.0.0.0:3000/metro");
        SimServer.setupGames = false;
    }

    public static bool setupGames;
    public static bool alreadySetupGames = false;
    public static JSONObject setupArgs;
    public static MetroManager metroManager;

    public void Update()
    {
        if (SimServer.setupGames)
            this.SetupGames(SimServer.setupArgs);
    }

    public void OnDisable()
    {
        wssv.Stop();
    }


    public void SetupGames(JSONObject args)
    {
        Debug.Log("Setting up Games");
        var games = args["games"].list;
        SimServer.metroManager.enabled = true;
        SimServer.metroManager.numGamesToSpawn = (uint)games.Count;
        SimServer.metroManager.SetupSim(games, 10, 120);
        setupGames = false;
        Debug.Log("Finished Setting Up Games");
    }
}

public class SimService : WebSocketBehavior
{

    protected override void OnOpen()
    {
        Debug.Log("[Server][Sim Service] Client connected.");
    }
    protected override void OnClose(CloseEventArgs e)
    {
        Debug.Log("[Server][Sim Service] Client disconnected.");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        var res = new JSONObject();
        var json = new JSONObject(e.Data);
        var command = json["command"].str;
        Debug.Log(command);

        res.AddField("Status", "None");
        try
        {
            switch (command)
            {
                case "noop":
                    if (SimServer.metroManager.isDone)
                    {
                        res.Clear();
                        res.AddField("Status", "Complete");
                        res.AddField("Scores", SimServer.metroManager.GetSimScores());
                    }
                    break;
                case "set_games":
                    Debug.Log("Setting games");
                    SimServer.setupArgs = json["args"];
                    SimServer.setupGames = true;
                    res.SetField("Status", "Success");
                    Debug.Log(SimServer.setupGames);
                    break;
                case "reset_scene":
                    Debug.Log("Resetting Scene");
                    MetroManager.ResetScene();
                    res.SetField("Status", "Reset");
                    break;
                default:
                    break;
            }
        }
        catch (Exception exception)
        {
            res.Clear();
            res.AddField("Status", "Error");
            res.AddField("Error Message", exception.Message);
        }


        Send(res.ToString());
        if (res.ToString().Length > 0)
        {
            Server.sw.WriteLineAsync(res.ToString());
            Server.sw.FlushAsync();
        }
    }

}

