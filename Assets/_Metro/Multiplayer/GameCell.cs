using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameCell : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TMP_Text freeTrains;
    [SerializeField] TMP_Text freeLines;
    public gameStruct data;
    bool dirty;

    void Start(){
        dirty = true;
    }

    void Update(){
        if(dirty)
            UpdateUI();
    }

    void UpdateUI(){
        freeTrains.text = data.cnt_t.ToString();
        freeLines.text = data.cnt_l.ToString();
        dirty = false;
    }

}
