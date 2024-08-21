using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeTrackingTest : MonoBehaviour {
  Collider lastHit = null;
  // Start is called before the first frame update
  void Start() {}

  // Update is called once per frame
  void Update() {
    Ray rayGlobal =
        new Ray(Camera.main.transform.position, Camera.main.transform.forward);
    RaycastHit hit;

    Physics.Raycast(rayGlobal, out hit, 1000);
    if (hit.collider == null) {
      // fill dummy
      hit.point =
          Camera.main.transform.position + Camera.main.transform.forward * 1000;
    }

    // if hit object is different from last hit object, send marker
    if (hit.collider != lastHit) {
      if (hit.collider != null) {
        var go = hit.collider.gameObject;
        Train t = go.GetComponent<Train>();
        Station s = go.GetComponent<Station>();
        TrackSegment l = go.GetComponent<TrackSegment>();

        if (t != null) {
          // Train
          Debug.Log("[EyeTracking] Looking at Train");
        } else if (s != null) {
          Debug.Log("[EyeTracking] Looking at Station");
        } else if (l != null) {
          Debug.Log("[EyeTracking] Looking at Line");
        } else if (hit.collider.gameObject.CompareTag("MetroUI")) {
          Debug.Log("[EyeTracking] Looking at MetroUI");

        } else {
          Debug.Log("[EyeTracking] Looking at " + hit.collider.name);
        }
      } else {
      }
      lastHit = hit.collider;
    }
  }
}
