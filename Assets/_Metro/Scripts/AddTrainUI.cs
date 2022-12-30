using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Physics;


public class AddTrainUI : MonoBehaviour, IMixedRealityPointerHandler
{
    private TMP_Text trainCountTxt;
    public bool hasGenerated;
    public bool hasAdded;

    public GameObject train;
    public int trackID;
    public int trackLineID;

    // Start is called before the first frame update
    void Start()
    {     
        this.transform.SetParent(this.gameObject.transform, false);
        trainCountTxt = this.transform.Find("Train_count").GetComponent<TMP_Text>();
        trainCountTxt.text = MetroManager.Instance.freeTrains.ToString();
        Debug.Log("freeTrains: " + MetroManager.Instance.freeTrains);
    }

    // Update is called once per frame
    void Update()
    {
        trainCountTxt.text = MetroManager.Instance.freeTrains.ToString();
        hasGenerated = true;
    }

    public void wasClicked()
    {
        Debug.Log("item clicked");
    }

    public void AddTrain()
    {
        //get positin of the UI element
        var UIpos = GameObject.Find("TestButton").transform.position;

        //check if there are trains available and prevent generating multiple trains at the same time
        if (MetroManager.Instance.freeTrains > 0 && !hasGenerated)
        {
            MetroManager.Instance.addingTrain = true;
            train = new GameObject();
            train.name = "Train";
            var t = train.AddComponent<Train>();
            //get and set train's position next to the UI panel
            var TrainPos = t.transform.position;
            TrainPos = UIpos;
            TrainPos.x = UIpos.x + 0.2f;
            t.transform.position = TrainPos;

            MetroManager.Instance.freeTrains--;
            //trainCountTxt.text = MetroManager.Instance.freeTrains.ToString();
            //hasGenerated = true;
        }
        
    }

    void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData)
    {
        Debug.Log("UI clicked");
        Debug.Log(eventData.Pointer.Position);
        //prevent adding multiple trains when the player only clicks UI once
        MetroManager.Instance.addingTrain = true;
        MetroManager.Instance.addedTrain = false;

    }

    void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData)
    {

    }

    void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData)
    {

    }

    void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData)
    {

    }
}
