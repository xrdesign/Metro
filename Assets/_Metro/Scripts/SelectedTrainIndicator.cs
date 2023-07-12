using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;

public class SelectedTrainIndicator : MonoBehaviour, IMixedRealityPointerHandler
{
    public MetroGame gameInstance;
    public Train targetTrain;
    
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position = MetroManager.Instance.LController.transform.position;
    }

    void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData){
        this.gameInstance.selectedTrain = null;
        this.targetTrain.ghost = null;
        Destroy(this.gameObject);
    }

    public void SetColor(Color color){
        var material = gameObject.GetComponent<Renderer>().materials[0];
        material.SetColor("_BaseColor", color);
    }
    void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData){}
    void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData){}
    void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData){}
}
