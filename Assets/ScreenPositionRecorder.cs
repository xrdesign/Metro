using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using System.IO;
using System;
using LSL;

public class ScreenPositionRecorder : MonoBehaviour
{
  public static ScreenPositionRecorder instance;

  void OnEnable()
  {
    if (instance != null)
    {
      Destroy(this);
      return;
    }
    instance = this;
  }

  public Camera cam;
  public Transform target;
  public bool start = false;
  public bool running = false;
  public Image startImage;
  StreamWriter output;
  float time = 0;
  string filename;

  private liblsl.StreamOutlet markerStream;

  void Start()
  {
#if Unity_EDITOR_OSX || UNITY_STANDALONE
#else
    liblsl.StreamInfo inf =
        new liblsl.StreamInfo("ScreenPositionEyeTracking", "Markers", 1, 0,
                              liblsl.channel_format_t.cf_string);
    markerStream = new liblsl.StreamOutlet(inf);
#endif

    startImage.enabled = true;
    output = new StreamWriter(
        Path.Join(LogRecorder.logDir, "EyetrackingScreenPosition.csv"));
    output.WriteLine("leftX,leftY,leftPupilDiameter,rightX,rightY," +
                     "rightPupilDiameter,timestamp");
    running = true;
  }
  void LateUpdate()
  {
    if (start)
    {
      start = false;
      startImage.enabled = false;
      MetroManager.SendEvent("Event: VideoSyncedThisFrame");
      LogRecorder.SendEvent(0, new SyncGamesEvent());
    }
    if (running)
    {
      time = MetroManager.GetSelectedGame().time;
      var s = target.position;

      Matrix4x4 lpm =
          cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
      Matrix4x4 wtl = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
      Vector4 lTemp = wtl * new Vector4(s.x, s.y, s.z, 1);
      lTemp = lpm * lTemp;
      Vector2 lPos = new Vector2(lTemp.x, lTemp.y) / lTemp.z;
      lPos = lPos * .5f;
      lPos += Vector2.one * .5f;

      Matrix4x4 wtr = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
      Matrix4x4 rpm =
          cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
      Vector4 rTemp = wtr * new Vector4(s.x, s.y, s.z, 1);
      rTemp = rpm * rTemp;
      Vector2 rPos = new Vector2(rTemp.x, rTemp.y) / rTemp.z;
      rPos = rPos * .5f;
      rPos += Vector2.one * .5f;
      output.WriteLine(
          $"{lPos.x},{lPos.y},{EyeTracking.leftPupilDiamter},{rPos.x},{rPos.y},{EyeTracking.rightPupilDiameter},{time}");
#if Unity_EDITOR_OSX || UNITY_STANDALONE
#else
      markerStream.push_sample(new string[] {
        $"{lPos.x},{lPos.y},{EyeTracking.leftPupilDiamter},{rPos.x},{rPos.y},{EyeTracking.rightPupilDiameter},{time}"
      });
#endif
    }
  }
}
