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

    public uint numGamesToSpawn = 1;

    #endregion


    #region Privates

    private List<MetroGame> games = new List<MetroGame>();
    
    // Used for UI stuff. The game that the player is currently "selecting". I.E. what they can interact with, add to, change, etc.
    // todo: Decide how to select. Should it just be nearest game? Should there be some UI for it? ETC. For now just select first game.
    private MetroGame selectedGame = null;

    #endregion
    
    #region UIs

    public GameObject menuUI;
    public GameObject metroUI;
    public GameObject addTrainUI;
    public GameObject LController;
    TransportLineUI[] lineUIs;

    #endregion



    // Start is called before the first frame update
    void Start()
    {
        gameObject.AddComponent<Server>();
        
        liblsl.StreamInfo inf =
            new liblsl.StreamInfo("EventMarker", "Markers", 1, 0, liblsl.channel_format_t.cf_string);
        markerStream = new liblsl.StreamOutlet(inf);

        if (numGamesToSpawn <= 0) {
            Debug.LogError("No games set to spawn!");
        }
        
        for (uint i = 0; i < numGamesToSpawn; i++) {
            var newMetroGame = (new GameObject("Game " + games.Count)).AddComponent<MetroGame>();
            newMetroGame.gameId = i;
            games.Add(newMetroGame);
            
            // todo: Change later so that we can switch between games we want to control.
            if (i == 0) {
                SelectGame(newMetroGame);
            }
        }
        
        lineUIs = metroUI.GetComponentsInChildren<TransportLineUI>(true);
    }

    // Refresh the UI. EX: When selected game is reset or when switching the selected game.
    private void RefreshUI() {
        foreach(var l in lineUIs){
            l.SetLine(null);
        }
        
        metroUI.SetActive(!selectedGame.isGameover);
        
        for(int i = 0; i < selectedGame.lines.Count; i++){
            lineUIs[i].SetLine(selectedGame.lines[i]);
        }
        
        if (addTrainUI)
        { 
            addTrainUI.SetActive(!selectedGame.isGameover); 
        }
        
    }

    // Only change selected game through this function, so that delegates are properly assigned.
    private void SelectGame(MetroGame game) {
        if (selectedGame) {
            selectedGame.uiUpdateDelegate -= RefreshUI;
            selectedGame = null;
        }

        selectedGame = game;
        selectedGame.uiUpdateDelegate += RefreshUI;
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
