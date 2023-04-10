using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;





// The purpose of this behavior is to listen to changes to whether the game this component is a child of and to disable certain components (more accurately behaviors) on the same gameobject based on that state.
public class SelectedGameComponentCuller : MonoBehaviour {
    
    [Serializable]
    public enum SelectedGameComponentCullerOption {
        EnableWhenSelected,
        DisableWhenSelected,
    }
    
    [Serializable]
    public struct SelectedGameCullerDisableType {
        public Behaviour DisabledBehavior;
        public SelectedGameComponentCullerOption DisabledBehaviorOption;
    }
    
    
    public List<SelectedGameCullerDisableType> DisableList = new List<SelectedGameCullerDisableType>();
    

    private void Start() {
        var game = GetComponentInParent<MetroGame>();
        
        game.GameSelectionDelegate += OnGameSelected;
        // Pretty sure we never need to remove our method.
        
        
        UpdateComponents(game.IsGameSelected());
    }

    void UpdateComponents(bool gameSelected) {
        foreach (var cullerOption in DisableList) {
            if (cullerOption.DisabledBehavior.gameObject != gameObject) {
                Debug.LogError(string.Format("{0} is set to disable a component ({1}) that don't share a gameObject!", name, cullerOption.DisabledBehavior.name));
                continue;
            }
            
            
            switch (cullerOption.DisabledBehaviorOption) {
                case SelectedGameComponentCullerOption.EnableWhenSelected:
                    cullerOption.DisabledBehavior.enabled = gameSelected;
                    break;
                case SelectedGameComponentCullerOption.DisableWhenSelected:
                    cullerOption.DisabledBehavior.enabled = !gameSelected;
                    break;
            }
        }
    }

    void OnGameSelected(bool selected) {
        UpdateComponents(selected);
    }
}
