using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Physics;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;

public class AddTrainUI : MonoBehaviour, 
    IMixedRealityPointerHandler,
    IMixedRealitySourceStateHandler,
    IMixedRealitySourcePoseHandler
{
    private TMP_Text trainCountTxt;
    public bool hasAdded;

    //[SerializeField] public GameObject ghostTrain;
    public GameObject ghostTrain;
    public int trackID;
    public int trackLineID;

    public MixedRealityPose LeftPose;
    public MixedRealityPose RightPose;

    // Start is called before the first frame update
    void Start()
    {     
        this.transform.SetParent(this.gameObject.transform, false);
        trainCountTxt = this.transform.Find("Train_count").GetComponent<TMP_Text>();
        trainCountTxt.text = MetroManager.GetSelectedGame().freeTrains.ToString();
        Debug.Log("freeTrains: " + MetroManager.GetSelectedGame().freeTrains);
        
        //generate a ghostTrain for visual representation      
        ghostTrain = new GameObject();
        ghostTrain.name = "GhostTrain";
        var t = ghostTrain.AddComponent<GhostTrain>();
        
        ghostTrain.transform.position = LeftPose.Position;
        this.ghostTrain.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        trainCountTxt.text = MetroManager.GetSelectedGame().freeTrains.ToString();
        //update the ghostTrain's position to match that of left controller
        ghostTrain.transform.position = MetroManager.GetSelectedGame().LController.transform.position;
        //ghostTrain.transform.position = LeftPose.Position;
        

        
        //check if the ghostTrain should be placed and make it Hide/show      
        if (ghostTrain!= null && MetroManager.GetSelectedGame().addedTrain)
        {
            this.ghostTrain.SetActive(false);

            //GhostTrain trainComponent = this.ghostTrain.GetComponent<GhostTrain>();
        } else if (MetroManager.GetSelectedGame().addingTrain)
        {
            this.ghostTrain.SetActive(true);
        }
    }

    public void wasClicked()
    {
        Debug.Log("item clicked");
    }

    public bool CanPlaceTrain()
    {
        return MetroManager.GetSelectedGame().freeTrains > 0;
    }
    
    public void AddTrain()
    {   
        //prevent adding multiple trains when the player only clicks UI once
        MetroManager.GetSelectedGame().addedTrain = false;

        //check if there is available trains
        if (MetroManager.GetSelectedGame().freeTrains > 0)
        {
            MetroManager.GetSelectedGame().addingTrain = true;
        }

    }

    void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData)
    {

        
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

    void IMixedRealitySourcePoseHandler.OnSourcePoseChanged(SourcePoseEventData<MixedRealityPose> eventData)
    {
        //Debug.Log(eventData.Controller.ControllerHandedness);
        if (eventData.Controller.ControllerHandedness == Handedness.Left)
        {
            LeftPose = eventData.SourceData;
            //Debug.Log("controller pos" + LeftPose.Position);
        }
        else if (eventData.Controller.ControllerHandedness == Handedness.Right)
        {
            RightPose = eventData.SourceData;
        }
        //Debug.Log(eventData.SourceData);
    }
    void IMixedRealitySourcePoseHandler.OnSourcePoseChanged(SourcePoseEventData<TrackingState> eventData)
    {
        // Debug.Log(eventData.SourceData);
    }
    void IMixedRealitySourcePoseHandler.OnSourcePoseChanged(SourcePoseEventData<Vector2> eventData)
    {
        // Debug.Log(eventData.SourceData);
    }
    void IMixedRealitySourcePoseHandler.OnSourcePoseChanged(SourcePoseEventData<Vector3> eventData)
    {
        Debug.Log(eventData.SourceData);
    }
    void IMixedRealitySourcePoseHandler.OnSourcePoseChanged(SourcePoseEventData<Quaternion> eventData)
    {
        // Debug.Log(eventData.SourceData);
    }

    void IMixedRealitySourceStateHandler.OnSourceDetected(SourceStateEventData eventData)
    {
        // Debug.Log(eventData);
    }
    void IMixedRealitySourceStateHandler.OnSourceLost(SourceStateEventData eventData)
    {
        // Debug.Log(eventData);
    }
}
