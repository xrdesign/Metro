using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Recommender : MonoBehaviour {
    [SerializeField] MetroManager metroManager;
    [SerializeField] TMP_Text output;
    [SerializeField] float pollingPeriod;

    Coroutine pollingRoutine;

    void Start(){
        pollingRoutine = StartCoroutine(PollGames());
    }
    void OnDisable(){
        StopCoroutine(pollingRoutine);
    }
    void OnEnable(){
        pollingRoutine = StartCoroutine(PollGames());
    }

    struct GameIssues{
        public int unusedTrains;
        public int unusedLines;
        public int disconnectedStations;
        public int overCrowdedStations;
        public int gameID;
    };
    GameIssues NewGameIssues(){
        GameIssues gIssues;
        gIssues.unusedTrains = 0;
        gIssues.unusedLines  = 0;
        gIssues.disconnectedStations = 0;
        gIssues.overCrowdedStations  = 0;
        gIssues.gameID = -1;
        return gIssues;
    }
    List<GameIssues> AnalyzeGames () {
        var games = metroManager.games;
        List<GameIssues> issues = new List<GameIssues>();
        var numGames = games.Count;
        //Loop through games and store metrics
        for (int g = 0; g<numGames; g++){
            MetroGame game = games[g];
            GameIssues gIssues = NewGameIssues();
            gIssues.gameID = g;
            foreach(var station in game.stations){
                if(station.lines.Count == 0){
                    gIssues.disconnectedStations++;
                }
                if(station.passengers.Count > 10){
                    gIssues.overCrowdedStations++;
                }
            }
            foreach(var line in game.lines){
                if(line.stops.Count <= 0){
                    gIssues.unusedLines++;
                }
            }
            gIssues.unusedTrains = game.freeTrains;
            issues.Add(gIssues);
        }
        return issues;
    }

    bool IsGreaterThan(GameIssues g1, GameIssues g2){
        if(g1.overCrowdedStations > g2.overCrowdedStations){
            return true;
        }
        else if(g1.overCrowdedStations == g2.overCrowdedStations){
            if(g1.disconnectedStations > g2.disconnectedStations){
                return true;
            }
            else if( g1.disconnectedStations == g2.disconnectedStations){
                if( g1.unusedLines > g2.unusedLines){
                    return true;
                }
                else if( g1.unusedLines == g2.unusedLines){
                    if(g1.unusedTrains > g2.unusedTrains){
                        return true;
                    }
                }
            }
        }
        return false;
    }
    bool IsEqual(GameIssues g1, GameIssues g2){
        return g1.unusedLines == g2.unusedLines 
            && g1.unusedTrains == g2.unusedTrains 
            && g1.overCrowdedStations == g2.overCrowdedStations 
            && g1.disconnectedStations == g2.disconnectedStations;
    }
    int compare (GameIssues g1, GameIssues g2){
        if(IsGreaterThan(g1, g2)){
            return 1;
        }
        if(IsEqual(g1,g2)){
                return 0;
        }
        return -1;
    }


    void PrintIssues(List<GameIssues> issues){
        string output = "";
        foreach(var i in issues){
            output += toString(i);
        }
        Debug.Log(output);

    }
    string toString(GameIssues g){
        return $"{g.gameID}, {g.overCrowdedStations}, {g.disconnectedStations}, {g.unusedLines}, {g.unusedTrains}\n";
    }
    string CreateRecommendation(){
        var issues = AnalyzeGames();

        Debug.Log("Pre-Sort:");
        PrintIssues(issues);
        issues.Sort(compare);
        Debug.Log("Post-Sort:");
        PrintIssues(issues);

        if(issues.Count <= 0){
            return "ERROR no games to analyze";
        }

        var i = issues[issues.Count - 1];
        if(i.overCrowdedStations > 0){
            return $"Game {i.gameID} has {i.overCrowdedStations} crowded stations";
        }
        if(i.disconnectedStations > 0){
            return $"Game {i.gameID} has {i.disconnectedStations} disconnected stations";
        }
        if(i.unusedLines> 0){
            return $"Game {i.gameID} has {i.unusedLines} available line(s) to use!";
        }
        if(i.unusedLines > 0){
            return $"Game {i.gameID} has {i.unusedTrains} available train(s) to add!";
        }
        return "Everything looks good, great job!";
    }

    IEnumerator PollGames(){
        while(true){
            yield return new WaitForSeconds(pollingPeriod);

            string recommendation = CreateRecommendation();
            this.output.text = recommendation;
        }
    }
}
