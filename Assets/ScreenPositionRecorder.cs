using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using System.IO;
using System;

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

  void Start()
  {
    filename = String.Format("{1}_{0:MMddyyyy-HHmmss}{2}", DateTime.Now,
                             "EyetrackingScreenPosition", ".csv");
  }
  void LateUpdate()
  {
    if (running && start)
    {
      startImage.enabled = false;
      start = false;
    }
    if (start)
    {
      startImage.enabled = true;
      output =
          new StreamWriter(Path.Join(Application.persistentDataPath, filename));
      output.WriteLine("leftX,leftY,rightX,rightY,timestamp");
      running = true;
    }
    if (running)
    {
      if (!start)
      {
        time += Time.deltaTime;
      }
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
      output.WriteLine($"{lPos.x}, {lPos.y}, {rPos.x}, {rPos.y}, {time}");
    }
  }
}
