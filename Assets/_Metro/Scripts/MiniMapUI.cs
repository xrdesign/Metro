using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MapUI : MonoBehaviour
{
    //public float passengerCount;
    public TMP_Text passengerCountTxt;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("current score" + MetroManager.Instance.score);
        //passengerCount = MetroManager.Instance.score;
        Debug.Log(MetroManager.Instance.score);
        this.transform.SetParent(this.gameObject.transform, false);
        passengerCountTxt = this.transform.Find("passengerInt").GetComponent<TMP_Text>();

    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(MetroManager.Instance.score);
        passengerCountTxt.text = MetroManager.Instance.score.ToString();
    }
}
