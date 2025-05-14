using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMenu : MonoBehaviour
{
    private MetroGame selectedGame;

    public void SetSelectedGame(MetroGame game)
    {
        selectedGame = game;
    }

    private void updateUI()
    {
        if (selectedGame != null)
        {
            
        }
    }
}
