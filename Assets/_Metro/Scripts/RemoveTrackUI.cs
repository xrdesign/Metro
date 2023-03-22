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

public class RemoveTrackUI : MonoBehaviour, 
    IMixedRealityPointerHandler,
    IMixedRealitySourceStateHandler,
    IMixedRealitySourcePoseHandler
{

    public static bool inRemovalMode; 
    public Sprite addMode;
    public Sprite removeMode;

    // Start is called before the first frame update
    void Start()
    {   
        inRemovalMode = false;
        this.transform.SetParent(this.gameObject.transform, false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void wasClicked()
    {
        Debug.Log("item clicked");
    }

    public void toggleRemovalMode() {
        inRemovalMode = !inRemovalMode;
        Debug.Log("inRemovalMode: " + inRemovalMode);
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
