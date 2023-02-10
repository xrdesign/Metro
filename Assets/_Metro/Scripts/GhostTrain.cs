using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.Input;

//ghostTrain just for visualization
public class GhostTrain : MonoBehaviour
{
    private Renderer rend;

    public float position; // index position along line
    public float speed;
    public float direction;
    public int cars = 0;
    public int nextStop = 0;
    public List<Passenger> passengers = new List<Passenger>();
    public Color color;

    private Image[] seats;

    public TransportLine line = null;

    private GameObject prefab;
    private GameObject train;

    // Start is called before the first frame update
    void Start()
    {

        //rend.enabled = false;
        prefab = Resources.Load("Prefabs/GhostTrain_v2") as GameObject;
        train = GameObject.Instantiate(prefab, new Vector3(0, 0, 0), prefab.transform.rotation) as GameObject;
        train.transform.SetParent(this.gameObject.transform, false);

        //var trainModel = transform.Find("train");
        rend = prefab.GetComponent<Renderer>();
        //rend.material.color.a = 0.5f;

        // seat[0] is image frame..
        seats = train.GetComponentsInChildren<Image>(true);

        //this.Hide();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
