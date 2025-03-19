using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;


public class SceneLoader : MonoBehaviour
{

    public TMP_Text scoreLabel;
    public GameObject menuUI;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update() {
        var game = MetroManager.GetSelectedGame();
        if (!game) return;
        
        if(MetroManager.GetSelectedGame().score > 0)
            scoreLabel.text = "Last game score: " + MetroManager.GetSelectedGame().score;
        
    }

    public void StartGame()
    {
        // SceneManager.LoadScene(1);
        menuUI.SetActive(false);
        MetroManager.StartGames();
    }

    public void QuitGame()
    {
        Application.Quit();

    }
}
