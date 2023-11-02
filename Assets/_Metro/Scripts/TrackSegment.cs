using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Physics;
using System;

public class TrackSegment : MonoBehaviour,  IMixedRealityPointerHandler {
    public MetroGame gameInstance;
    
    public TransportLine line;
    //track segment id
    public int index; 

    public bool needsUpdate = true;
    public int lengthOfLine = 20;

    public float segmentLengthSum;
    public float addPosition;

    public bool isAddingTrain;
    public bool addedTrain;

    public Vector3[] cp = new Vector3[]{
            new Vector3(0f,0f,0f),
            new Vector3(0f,0f,0f),
            new Vector3(0f,0f,0f),
            new Vector3(0f,0f,0f)
        };

    Mesh mesh;
    public LineRenderer lineRenderer;
    MeshCollider meshCollider;
    

    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        mesh = new Mesh();

        lineRenderer.material = Resources.Load("Materials/MRTKTransparent") as Material;
        var color = line.color; //new Color(line.color);
        color.a = 0.75f;
        SetColor(color);
        lineRenderer.widthMultiplier = 0.035f;
        lineRenderer.positionCount = lengthOfLine+1;
    }

    // Update is called once per frame
    void Update()
    {   
        //if(!needsUpdate) return;

        for (int i = 0; i <= lengthOfLine; i++)
        {
            Vector3 p = Interpolate(i*1.0f/lengthOfLine);
            lineRenderer.SetPosition(i, p); 
        }

        if(cp[0] != cp[3]) // Can't bake mesh from line renderer with empty position vector
        {
            try{
                lineRenderer.BakeMesh(mesh, true);
                meshCollider.sharedMesh = mesh;
            }
            catch{
                Debug.LogError("MeshCollider Error Here");
            }
        }
        needsUpdate = false;
    }

    public float getRoughSegmentDistance()
    {
        return Vector3.Distance(cp[0], cp[3]);
    }

    public void SetColor(Color c){
        if(c == null)
            return;
        var block = new MaterialPropertyBlock();
        block.SetColor("_Color", c);
        //Debug.Log("color " + lineRenderer);
        lineRenderer.SetPropertyBlock(block); 
    }

    public Vector3 Interpolate(float t){
        var t1 = 1.0f - t;
        return t1*t1*t1*cp[0] + 3f*t1*t1*t*cp[1] + 3f*t1*t*t*cp[2] + t*t*t*cp[3];
    }

    public Vector3 Derivative(float t){
        var t1 = 1.0f - t;
        return 3f*t1*t1*(cp[1]-cp[0]) + 6f*t1*t*(cp[2]-cp[1]) + 3f*t*t*(cp[3]-cp[2]);
    }
    
    public float getLengthSum(int id)
    {
        float sum = 0;
        for(int i = 0; i < id; i++)
        {
            sum += this.line.tracks.lengths[i];
        }
        return sum;
    }
    
    void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData){
        Debug.Log("track down");
        
        Debug.Log("line id: " + this.line.id);
        Debug.Log("trackSegment index: " + this.index);
        
        this.segmentLengthSum = this.getLengthSum(this.index);
        Debug.Log("trackSegment length sum: " + this.segmentLengthSum);
        //calculates the position of train 
        addPosition = this.segmentLengthSum / gameInstance.totalTrackLength;

        isAddingTrain = gameInstance.addingTrain;
        addedTrain = gameInstance.addedTrain;
        
        //check if the player has clicked the addTrainUI
        //if yes, add a train on the segment where the player clicks
        if (isAddingTrain & !addedTrain)
        {
            Debug.Log("isAddingTrain: " + isAddingTrain);
            Debug.Log("addedTrain: " + addedTrain);
            this.line.AddTrain(addPosition, 1.0f);
            gameInstance.addedTrain = true;
            gameInstance.addingTrain = false;
            Debug.Log("addedTrain: " + addedTrain);
        } 
        
        
        var dist = eventData.Pointer.Result.Details.RayDistance;
        var insert = index >= 0 && index < line.stops.Count-1;
        MetroGame.StartEditingLine(line, index, dist, insert);

        eventData.Pointer.IsFocusLocked = false;
        eventData.Pointer.IsTargetPositionLockedOnFocusLock = false;

        var hapticController = eventData.Pointer?.Controller as IMixedRealityHapticFeedback;
        hapticController?.StartHapticImpulse(0.4f, 0.05f);
    }
    
    void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData){

    }

    void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData){

    }

    void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData){
        if(gameInstance.selectedTrain != null){
            gameInstance.selectedTrain.nextLine = this.line;
            gameInstance.selectedTrain = null;
        }
    }

    
}
