using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MiniMapUI : MonoBehaviour
{
    //public float passengerCount;
    public TMP_Text passengerCountTxt;
    public TMP_Text dayCountTxt;
    private int dayCount;
    // Start is called before the first frame update
    void Start()
    {
        //Debug.Log("current score: " + MetroManager.Instance.score);
        //Debug.Log("current day" + MetroManager.Instance.score);
        this.transform.SetParent(this.gameObject.transform, false);
        passengerCountTxt = this.transform.Find("passengerInt").GetComponent<TMP_Text>();
        dayCountTxt = this.transform.Find("Time").GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        passengerCountTxt.text = MetroManager.GetSelectedGame().score.ToString();
        dayCount = MetroManager.GetSelectedGame().day;

        switch (dayCount)
        {
            case 0:
                dayCountTxt.text = "Mon.";
                break;
            case 1:
                dayCountTxt.text = "Tue.";
                break;
            case 2:
                dayCountTxt.text = "Wed.";
                break;
            case 3:
                dayCountTxt.text = "Thur.";
                break;
            case 4:
                dayCountTxt.text = "Fri.";
                break;
            case 5:
                dayCountTxt.text = "Sat.";
                break;
            case 6:
                dayCountTxt.text = "Sun.";
                break;
        }
    }
}
