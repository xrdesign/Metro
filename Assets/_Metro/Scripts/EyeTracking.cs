using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using ViveSR.anipal.Eye;
using LSL;
using System.Runtime.InteropServices;
using Microsoft.MixedReality.Toolkit;

public class EyeTracking : MonoBehaviour
{
  [Tooltip("The size of the eye tracking collider (1.0 is the same size).")]
  public static float EyeColliderSize = 1.2f;
  public string folderName = "MetroData";
  public float gazeRadius = 0.1f;

  static float leftDiameter = 0;
  static float rightDiameter = 0;

  static Vector3 GazeOriginCombinedLocal, GazeDirectionCombinedLocal;
  static Vector3 GazeOriginLeftLocal, GazeDirectionLeftLocal;
  static Vector3 GazeOriginRightLocal, GazeDirectionRightLocal;
  static Vector2 pupilPos_L;
  static Vector2 pupilPos_R;
  static float leftOpenness;
  static float rightOpenness;

  bool eye_callback_registered = false;
  private StreamWriter _writer;
  private liblsl.StreamOutlet eyeStream;
  private liblsl.StreamOutlet markerStream;
  private Collider lastHit = null;

  public Transform gazePositionT;

  // private VerboseData eyeData = new VerboseData();

  // Use this for initialization
  void Start()
  {
    if (!SRanipal_Eye_Framework.Instance.EnableEye)
    {
      enabled = false;
      return;
    }

    gazePositionT = new GameObject().transform;
    ScreenPositionRecorder.instance.target = gazePositionT;

    // print(aGlass.Instance.aGlassStart());
    string filename = String.Format("{1}_{0:MMddyyyy-HHmmss}{2}", DateTime.Now,
                                    "Eyetracking", ".txt");
    // check folder exist
    if (!Directory.Exists(@"C:\" + folderName))
    {
      Directory.CreateDirectory(@"C:\" + folderName);
    }

    string path = Path.Combine(@"C:\" + folderName, filename);
    _writer = File.CreateText(path);
    _writer.Write("\n\n=============== Game started ================\n\n");

    liblsl.StreamInfo inf =
        new liblsl.StreamInfo("Eyetracking", "Gaze", 35, 50,
                              liblsl.channel_format_t.cf_float32, "ProEye");
    eyeStream = new liblsl.StreamOutlet(inf);
    // event marker for gaze object
    liblsl.StreamInfo markerInfo = new liblsl.StreamInfo(
        "EyetrackingMarker", "Markers", 1, 0, liblsl.channel_format_t.cf_string,
        "ProEyeMarker");
    markerStream = new liblsl.StreamOutlet(markerInfo);
  }

  private static void EyeCallback(ref EyeData eye_data)
  {

    // Debug.Log("EyeCallback");
    leftDiameter = eye_data.verbose_data.left.pupil_diameter_mm;
    rightDiameter = eye_data.verbose_data.right.pupil_diameter_mm;

    if (!eye_data.verbose_data.left.GetValidity(
            SingleEyeDataValidity.SINGLE_EYE_DATA_PUPIL_DIAMETER_VALIDITY))
    {
      leftDiameter = -1;
    }

    if (!eye_data.verbose_data.right.GetValidity(
            SingleEyeDataValidity.SINGLE_EYE_DATA_PUPIL_DIAMETER_VALIDITY))
    {
      rightDiameter = -1;
    }

    // check if the data is valid
    if (!SRanipal_Eye.GetGazeRay(GazeIndex.COMBINE, out GazeOriginCombinedLocal,
                                 out GazeDirectionCombinedLocal, eye_data))
    {
      GazeDirectionCombinedLocal = Vector3.zero;
      GazeOriginCombinedLocal = Vector3.zero;
    }

    if (!SRanipal_Eye.GetGazeRay(GazeIndex.LEFT, out GazeOriginLeftLocal,
                                 out GazeDirectionLeftLocal, eye_data))
    {
      GazeDirectionLeftLocal = Vector3.zero;
      GazeOriginLeftLocal = Vector3.zero;
    }

    if (!SRanipal_Eye.GetGazeRay(GazeIndex.RIGHT, out GazeOriginRightLocal,
                                 out GazeDirectionRightLocal, eye_data))
    {
      GazeDirectionRightLocal = Vector3.zero;
      GazeOriginRightLocal = Vector3.zero;
    }

    if (!SRanipal_Eye.GetEyeOpenness(EyeIndex.LEFT, out leftOpenness,
                                     eye_data))
    {
      // leftOpenness = -1;
    }

    if (!SRanipal_Eye.GetEyeOpenness(EyeIndex.RIGHT, out rightOpenness,
                                     eye_data))
    {
      // rightOpenness = -1;
    }

    if (!SRanipal_Eye.GetPupilPosition(EyeIndex.LEFT, out pupilPos_L,
                                       eye_data))
    {
      pupilPos_L = Vector2.zero;
    }

    if (!SRanipal_Eye.GetPupilPosition(EyeIndex.RIGHT, out pupilPos_R,
                                       eye_data))
    {
      pupilPos_R = Vector2.zero;
    }
  }

  void Update()
  {
    // for now, we should probably put a button instead of a hotkey
    if (Input.GetKeyDown(KeyCode.O))
    {
      Debug.Log("Calibration Key pressed");
      SRanipal_Eye.LaunchEyeCalibration();
    }
  }

  // Update is called once per frame
  void FixedUpdate()
  {

    if (SRanipal_Eye_Framework.Status !=
            SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
        SRanipal_Eye_Framework.Status !=
            SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT)
      return;

    if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true &&
        eye_callback_registered == false)
    {
      SRanipal_Eye.WrapperRegisterEyeDataCallback(
          Marshal.GetFunctionPointerForDelegate(
              (SRanipal_Eye.CallbackBasic)EyeCallback));
      eye_callback_registered = true;
    }

    Ray rayGlobal = new Ray(
        Camera.main.transform.position,
        Camera.main.transform.TransformDirection(GazeDirectionCombinedLocal));
    RaycastHit hit;
    if (gazeRadius > 0)
    {
      Physics.SphereCast(rayGlobal, gazeRadius, out hit, 1000);
    }
    else
    {
      Physics.Raycast(rayGlobal, out hit, 1000);
    }

    if (hit.collider == null)
    {
      // fill dummy
      hit.point =
          Camera.main.transform.position +
          Camera.main.transform.TransformDirection(GazeDirectionCombinedLocal) *
              1000;
    }

    // if hit object is different from last hit object, send marker
    if (hit.collider != lastHit)
    {
      if (hit.collider != null)
      {
        var go = hit.collider.gameObject;
        Train t = go.GetComponent<Train>();
        Station s = go.GetComponent<Station>();
        TrackSegment l = go.GetComponent<TrackSegment>();

        if (t != null)
        {
          // Train
          Debug.Log("[EyeTracking] Looking at Train");
          markerStream.push_sample(new string[] {
            $"Type: Train, Game: {t.gameInstance.gameId}, Line: {t.line.id}"
          });
        }
        else if (s != null)
        {
          Debug.Log("[EyeTracking] Looking at Station");
          markerStream.push_sample(new string[] {
            $"Type: Station, Game: {s.gameInstance.gameId}, Shape: {s.type}, ID: {s.id}"
          });
        }
        else if (l != null)
        {
          Debug.Log("[EyeTracking] Looking at Line");
          markerStream.push_sample(new string[] {
            $"Type: TrackSegment, Game: {l.gameInstance.gameId}, Line: {l.line.id}"
          });
        }
        else if (hit.collider.gameObject.CompareTag("MetroUI"))
        {
          Debug.Log("[EyeTracking] Looking at MetroUI");
          markerStream.push_sample(new string[] { $"Type: MetroUI" });

        }
        else
        {
          Debug.Log("[EyeTracking] Looking at " + hit.collider.name);
          markerStream.push_sample(new string[] { hit.collider.name });
        }
      }
      else
      {
        markerStream.push_sample(new string[] { "null" });
      }
      lastHit = hit.collider;
    }

    _writer.WriteLine(
        String.Format("{0:HH:mm:ss.fff}", DateTime.Now) + " - " +
        Time.time.ToString() + ": 2DEyeL " + pupilPos_L +
        ", 2DEyeR: " + pupilPos_R + ", 3DEyeL: " + GazeDirectionLeftLocal +
        ", 3DEyeR: " + GazeDirectionRightLocal + ", 3DCombined: " +
        GazeDirectionCombinedLocal + ", 3DEyeOriginL: " + GazeOriginLeftLocal +
        ", 3DEyeOriginR: " + GazeOriginRightLocal + ", 3DCombinedOrigin: " +
        GazeOriginCombinedLocal + ", 3DHit: " + hit.point +
        ", 3DHead: " + Camera.main.transform.position + ", 3DHeadForward: " +
        Camera.main.transform.forward + ", LeftEyeOpenness: " + leftOpenness +
        ", RightEyeOpenness: " + rightOpenness + ", LeftPupilDiameter:" +
        leftDiameter + ", RightPupilDiameter:" + rightDiameter);

    // 0 - 2d coordinate of left eye
    // 2 - 2d coordinate of right eye
    // 4 - 3d direction of left eye
    // 7 - 3d direction of right eye
    // 10 - 3d direction of combined eye
    // 13 - 3d position of left eye
    // 16 - 3d position of right eye
    // 19 - 3d position of combined eye
    // 22 - 3d position of hit
    // 25 - 3d position of head
    // 28 - 3d forward of head
    // 31 - left eye openness
    // 32 - right eye openness
    // 33 - left pupil diameter
    // 34 - right pupil diameter

    float[] tempSample = { pupilPos_L.x,
                           pupilPos_L.y,
                           pupilPos_R.x,
                           pupilPos_R.y,
                           GazeDirectionLeftLocal.x,
                           GazeDirectionLeftLocal.y,
                           GazeDirectionLeftLocal.z,
                           GazeDirectionRightLocal.x,
                           GazeDirectionRightLocal.y,
                           GazeDirectionRightLocal.z,
                           GazeDirectionCombinedLocal.x,
                           GazeDirectionCombinedLocal.y,
                           GazeDirectionCombinedLocal.z,
                           GazeOriginLeftLocal.x,
                           GazeOriginLeftLocal.y,
                           GazeOriginLeftLocal.z,
                           GazeOriginRightLocal.x,
                           GazeOriginRightLocal.y,
                           GazeOriginRightLocal.z,
                           GazeOriginCombinedLocal.x,
                           GazeOriginCombinedLocal.y,
                           GazeOriginCombinedLocal.z,
                           hit.point.x,
                           hit.point.y,
                           hit.point.z,
                           Camera.main.transform.position.x,
                           Camera.main.transform.position.y,
                           Camera.main.transform.position.z,
                           Camera.main.transform.forward.x,
                           Camera.main.transform.forward.y,
                           Camera.main.transform.forward.z,
                           leftOpenness,
                           rightOpenness,
                           leftDiameter,
                           rightDiameter };
    eyeStream.push_sample(tempSample);

    LogRecorder.RecordPosition(Camera.main.transform.position,
                               Camera.main.transform.rotation, hit.point);
    gazePositionT.transform.position = hit.point;
  }

  void OnDestroy() { _writer.Close(); }
}
