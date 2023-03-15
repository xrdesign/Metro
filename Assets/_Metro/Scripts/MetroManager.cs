using System;
using System.Collections;
using System.Collections.Generic;
using LSL;
using Oculus.Platform.Models;
using UnityEngine;

public class MetroManager : MonoBehaviour
{
    #region Singleton
    
    public static MetroManager Instance;
    
    #endregion
    
    #region LibLSL

    private liblsl.StreamOutlet markerStream;

    #endregion


    #region Set In Editor

    private uint numGamesToSpawn = 1;

    #endregion


    #region Privates

    private List<MetroGame> games = new List<MetroGame>();
    
    // Used for UI stuff. The game that the player is currently "selecting". I.E. what they can interact with, add to, change, etc.
    private MetroGame selectedGame = null;

    #endregion
    
    
    
    // Start is called before the first frame update
    void Start()
    {
        liblsl.StreamInfo inf =
            new liblsl.StreamInfo("EventMarker", "Markers", 1, 0, liblsl.channel_format_t.cf_string);
        markerStream = new liblsl.StreamOutlet(inf);

        if (numGamesToSpawn <= 0) {
            Debug.LogError("No games set to spawn!");
        }
        
        for (int i = 0; i < numGamesToSpawn; i++) {
            var newMetroGame = Instantiate(new GameObject("Game " + games.Count)).AddComponent<MetroGame>();
            games.Add(newMetroGame);
            
            // todo: Change later so that we can switch between games we want to control.
            if (i == 0) {
                selectedGame = newMetroGame;
            }
        }
    }
    
    public static void SendEvent(string eventString)
    {
        string[] tempSample;
        tempSample = new string[] { eventString };
        Instance.markerStream.push_sample(tempSample);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Awake() {
        if (Instance is null) Instance = this;
        else {
            Destroy(this);
            Debug.LogError("More than one MetroManager initialized!");
        }
    }

    public static void ResetGame(uint gameID) {
        GetGameWithID(gameID).ScheduleReset();
    }

    public static JSONObject SerializeGame(uint gameID) {
        return GetGameWithID(gameID).SerializeGameState();
    }

    private static MetroGame GetGameWithID(uint gameID) {
        return Instance.games.Find(game => game.gameId == gameID);
    }

    public static void QueueGameAction(MetroGame.MetroGameAction action, uint gameID) {
        GetGameWithID(gameID).QueueAction(action);
    }

    public static MetroGame GetSelectedGame() {
        return Instance.selectedGame;
    }

    
    // Starts every game simultaneously.
    public static void StartGames() {
        foreach (var metroGame in Instance.games) {
            metroGame.StartGame();
        }
    }
    
}
