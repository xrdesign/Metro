using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TransportLineUI : MonoBehaviour
{

    public TransportLine line = null;

    public GameObject circle;
    public GameObject demolish;

    bool hide = true;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(line == null) return;
        if(line.isDeployed){
            circle.transform.localScale = new Vector3(2,2,2);
        }else {
            circle.transform.localScale = new Vector3(1,1,1);
        }

        var p = circle.transform.localPosition;
        if(hide){
            p.x = 0f;
            circle.transform.localPosition = p;
            demolish.SetActive(false);
        } else {
            p.x = -0.2f;
            circle.transform.localPosition = p;
            demolish.SetActive(true);
        }
        
    }

    public void SetLine(TransportLine transportline){
        line = transportline;
        if(line != null) circle.GetComponent<Image>().color = line.color;
        else circle.GetComponent<Image>().color = new Color(0.3f,0.3f,0.3f,1.0f);
    }

    public void CircleClick(){
        if(line == null || !line.isDeployed) return;
        hide = !hide;
    }

    public void DemolishClick(){
        line.RemoveAll();
        demolish.SetActive(false);
        hide = true;
    }
    
}
