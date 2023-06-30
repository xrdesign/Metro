using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Similar to SelectedGameComponentCuller, but instead of disabling components, it instead sets alpha for every child canvasrenderer to 0 or 1
public class SelectedGameObjectCuller : MonoBehaviour
{
    
    [Serializable]
    public enum SelectedGameObjectCullerOption {
        EnableWhenSelected,
        DisableWhenSelected,
    }
    public SelectedGameObjectCullerOption disabledOption;
    
    void Start()
    {
        var game = GetComponentInParent<MetroGame>();
        game.GameSelectionDelegate += OnGameSelected;
        // Pretty sure we never need to remove our method
        // Edit: when deleting lines or removing trains need to remove
        // Done in OnDestroy atm 
        
        UpdateComponents(game.IsGameSelected());
    }

    void OnDestroy(){
        var game = GetComponentInParent<MetroGame>();
        if(!game)return;
        game.GameSelectionDelegate -= OnGameSelected;
    }
    
    void UpdateComponents(bool gameSelected) {
        gameObject.SetActive(gameSelected);
        
        switch (disabledOption) {
            case SelectedGameObjectCullerOption.EnableWhenSelected:
                gameObject.SetActive(gameSelected);
                break;
            case SelectedGameObjectCullerOption.DisableWhenSelected:
                gameObject.SetActive(!gameSelected);
                break;
        }
    }

    void OnGameSelected(bool selected) {
        UpdateComponents(selected);
    }
}
