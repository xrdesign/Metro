using System.Linq;
using UnityEngine;
using UnityEngine.XR.Management;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;  // for Handedness


public class FallbackCameraController : MonoBehaviour
{
    [Tooltip("Movement speed in units/sec")]
    public float moveSpeed = 5f;
    [Tooltip("Mouse look sensitivity")]
    public float lookSpeed = 2f;

    float yaw;
    float pitch;
    MixedRealityInputAction selectAction;
    MixedRealityInputAction speechAction;
    private IMixedRealityInputSource keyboardInputSource;
    void Awake()
    {
        // disable in Editor or when any XR loader (headset) is active
        if (Application.isEditor ||
            XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            enabled = false;
            return;
        }

        // grab the “Select” action from the MRTK Input Actions Profile
        var actionsProfile = CoreServices.InputSystem
            .InputSystemProfile.InputActionsProfile;
        selectAction = actionsProfile.InputActions
            .First(a => a.Description == "Select");
        speechAction = actionsProfile.InputActions
            .First(a => a.Description == "Speech");

        // init yaw/pitch from current rotation
        var e = transform.rotation.eulerAngles;
        yaw = e.y;
        pitch = e.x;

        keyboardInputSource = new BaseGenericInputSource(
            "KeyboardInput",
            new IMixedRealityPointer[0],
            InputSourceType.Controller
        );


    }

    void Update()
    {
        // right-drag → adjust yaw & pitch
        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * lookSpeed;
            pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
            pitch = Mathf.Clamp(pitch, -80f, 80f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // WASD/arrows → move
        var dir = new Vector3(
            Input.GetAxis("Horizontal"),
            0f,
            Input.GetAxis("Vertical")
        );
        transform.Translate(dir * moveSpeed * Time.deltaTime, Space.Self);

        // left-click → MRTK pointer events
        var inputSystem = CoreServices.InputSystem;
        if (inputSystem == null) return;

        var pointer = inputSystem.FocusProvider.PrimaryPointer;
        if (pointer == null) return;

        var handedness = pointer.Controller != null
            ? pointer.Controller.ControllerHandedness
            : Handedness.None;

        if (Input.GetMouseButtonDown(0))
            inputSystem.RaisePointerDown(pointer, selectAction, handedness);

        if (Input.GetMouseButton(0))
            inputSystem.RaisePointerDragged(pointer, selectAction, handedness);

        if (Input.GetMouseButtonUp(0))
            inputSystem.RaisePointerUp(pointer, selectAction, handedness);


        // when P is pressed, triggle on input down with the speech action
        //         void IMixedRealityInputHandler.OnInputDown(InputEventData eventData)
        //   {
        //             // Debug.Log("Input Down:" + eventData.MixedRealityInputAction.Description);
        //             // if the description contains "Speech", then DeepgramConnection.StartDeepgram()
        //             if (eventData.MixedRealityInputAction.Description.Contains("Speech"))
        //             {
        //                 deepgramConnection.StartDeepgram();
        //             }
        //         }
        if (Input.GetKeyDown(KeyCode.P))
        {
            // mimic speech-action press
            inputSystem.RaiseOnInputDown(keyboardInputSource, handedness, speechAction);
        }

        if (Input.GetKeyUp(KeyCode.P))
        {
            // mimic speech-action release
            inputSystem.RaiseOnInputUp(keyboardInputSource, handedness, speechAction);
        }
    }
}
