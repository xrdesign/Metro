using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct gameStruct{
    public int id;
    public int cnt_p;
    public int cnt_t;
    public int cnt_l;
};


public class UIManager : MonoBehaviour
{
    private GameObject gamesRoot;
    private GameObject leftScrollButton;
    private GameObject rightScrollButton;

    private bool dirtyTable;
    private List<GameObject> gameInstances;
    [SerializeField] private GameObject GameInstancePrefab;
    [SerializeField] private int GamesPerPage = 16;
    [SerializeField] private int numGames = 10;


    public List<gameStruct> games;
    /* Builtin Methods */
    void Awake(){
        dirtyTable = false;
        gameInstances = new List<GameObject>();
        games = new List<gameStruct>();

        //Obtain references:
        gamesRoot = transform.Find("GamesView").gameObject;

        //@TODO buttons...
    }
    void Start(){
    }
    void Update(){
        if (dirtyTable){
            PopulateGamesTable();
        }
    }

    /*Methods*/
    private void PopulateGamesTable(){
        foreach(var game in gameInstances){
            Destroy(game);
        }
        gameInstances.Clear();
        this.dirtyTable = false;
        for(int i = 0; i<games.Count; i++){
            Debug.Log($"Adding game {i} to table");
            GameObject inst = GameObject.Instantiate(GameInstancePrefab, gamesRoot.transform);
            gameInstances.Add(inst);
            var cell = inst.GetComponent<GameCell>();
            if(cell != null)
                cell.data = games[i];
        }
    }

    /*
    private void ReadGameData(JSONObject gameInfo){
        gameStruct game;
        game.id = games.Count;
        game.cnt_t = 0;
        game.cnt_l = 0;
        game.cnt_p = (int)(gameInfo["cnt_sphere"].i) +(int)(gameInfo["cnt_cube"].i) + (int)(gameInfo["cnt_cone"].i) + (int)(gameInfo["cnt_star"].i);
        games.Add(game);
        //sort
    }
    */

    /*Public Interface*/
    public void ScrollGames(bool scrollRight){}

    public void SetGameData(gameStruct[] gameList){
        Debug.Log("loading games");
        this.games = new List<gameStruct>(gameList);
        this.dirtyTable = true;
    }


}
