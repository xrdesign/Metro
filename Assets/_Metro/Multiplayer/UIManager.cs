using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct gameStruct{
    public int id;

    public int free_trains;
    public int free_lines;

    public int p_cube;
    public int p_sphere;
    public int p_cone;
    public int p_star;

    public int s_cube;
    public int s_sphere;
    public int s_cone;
    public int s_star;
};


public class UIManager : MonoBehaviour
{
    private GameObject gamesRoot;
    private GameObject leftScrollButton;
    private GameObject rightScrollButton;

    private bool dirtyTable;
    private List<GameObject> gameInstances;
    [SerializeField] private GameObject GameInstancePrefab;
    [SerializeField] private int gamesPerPage = 16;
    private uint page;
    private uint pages;

    public List<gameStruct> games;
    /* Builtin Methods */
    void Awake(){
        page = 0;
        dirtyTable = false;
        games = new List<gameStruct>();
        gamesRoot = transform.Find("GamesView").gameObject;
        gameInstances = new List<GameObject>();
        for(int i = 0; i<gamesPerPage; i++){
            GameObject inst = GameObject.Instantiate(GameInstancePrefab, gamesRoot.transform);
            gameInstances.Add(inst);
            inst.SetActive(false);
        }
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
        this.dirtyTable = false;
        for(int i = 0; i<gamesPerPage; i++){
            if(i+(page*gamesPerPage) >= games.Count){
                gameInstances[i].SetActive(false);
                break;
            }
            gameInstances[i].SetActive(true);
            Debug.Log($"Adding game {(gamesPerPage)*page + i} to table");
            var cell = gameInstances[i].GetComponent<GameCell>();
            if(cell != null){
                cell.SetData(games[(int)(i+(page*gamesPerPage))]);
            }
                
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
        pages = (uint)(gameList.Length / gamesPerPage);
        Debug.Log("loading games");
        this.games = new List<gameStruct>(gameList);
        for(int i = 0; i<games.Count; i++){
            //sort games:
        }
        this.dirtyTable = true;
    }



    //Should wrap around?
    public void nextPage(){
        page++;
        if(page >= pages)
            page = pages - 1;
    }
    public void previousPage(){
        page--;
        if(page < 0)
            page = 0;
    }

}
