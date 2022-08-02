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
    void Update()
    {
        if(MetroManager.Instance.score > 0)
            scoreLabel.text = "Last game score: " + MetroManager.Instance.score;
        
    }

    public void StartGame()
    {
        // SceneManager.LoadScene(1);
        menuUI.SetActive(false);
        MetroManager.StartGame();
    }

    public void QuitGame()
    {
        Application.Quit();

    }
}
