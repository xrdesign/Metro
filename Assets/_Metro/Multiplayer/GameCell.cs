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

    [SerializeField] TMP_Text sphere;
    [SerializeField] TMP_Text cube;
    [SerializeField] TMP_Text cone;
    [SerializeField] TMP_Text star;

    [SerializeField] TMP_Text gameID;
    
    public gameStruct data;

    void Awake(){
        button.onClick.AddListener( () => {
            var server = GameObject.FindWithTag("ServerConnection").
                GetComponent<ServerConnection>();

            server.Alert(data.id);
            });
    }

    public void SetData(gameStruct data){
        freeTrains.text = data.free_trains.ToString();
        freeLines.text = data.free_lines.ToString();

        sphere.text = data.p_sphere.ToString() + " passengers, " + data.s_sphere.ToString() + " stations.";
        cube.text = data.p_cube.ToString() + " passengers, " + data.s_cube.ToString() + " stations.";
        cone.text = data.p_cone.ToString() + " passengers, " + data.s_cone.ToString() + " stations.";
        star.text = data.p_star.ToString() + " passengers, " + data.s_star.ToString() + " stations.";

        gameID.text = $"Games:\n{data.id.ToString()}";
    }


}
