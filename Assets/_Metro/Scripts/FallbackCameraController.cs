using System.Linq;
using UnityEngine;
using UnityEngine.XR.Management;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;  // for Handedness
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using System.Collections.Generic;


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
    private IMixedRealityInputSystem inputSystem = null;

    // OLD: TODO use new Input System
    private List<InputDevice> xrDevices = new List<InputDevice>();
    private Dictionary<InputDevice, bool> aButtonStates = new Dictionary<InputDevice, bool>();



    private bool needWASD = true;

    void Awake()
    {
        // disable in Editor or when any XR loader (headset) is active
        if (Application.isEditor ||
            XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            needWASD = false;
            // return;
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
        inputSystem = CoreServices.InputSystem;
        if (inputSystem == null) return;

        var pointer = inputSystem.FocusProvider.PrimaryPointer;
        if (pointer == null) return;

        var handedness = pointer.Controller != null
            ? pointer.Controller.ControllerHandedness
            : Handedness.None;

        if (needWASD)
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
            if (Input.GetMouseButtonDown(0))
                inputSystem.RaisePointerDown(pointer, selectAction, handedness);

            if (Input.GetMouseButton(0))
                inputSystem.RaisePointerDragged(pointer, selectAction, handedness);

            if (Input.GetMouseButtonUp(0))
                inputSystem.RaisePointerUp(pointer, selectAction, handedness);
        }

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

        // get all right-hand controllers (Quest Touch, Index, etc.)
        xrDevices.Clear();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.HeldInHand |
            InputDeviceCharacteristics.Controller |
            InputDeviceCharacteristics.Right,
            xrDevices);

        bool aOnPressed = false;
        bool aOnRelease = false;

        foreach (var device in xrDevices)
        {
            // CommonUsages.primaryButton maps to “A” on Quest and Index
            if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool isPressed))
            {
                // previous state or false if first frame
                aButtonStates.TryGetValue(device, out bool wasPressed);

                if (isPressed && !wasPressed)
                {
                    // A just pressed
                    // inputSystem.RaiseOnInputDown(keyboardInputSource, handedness, speechAction);
                    aOnPressed = true;
                }
                else if (!isPressed && wasPressed)
                {
                    // A just released
                    // inputSystem.RaiseOnInputUp(keyboardInputSource, handedness, speechAction);
                    aOnRelease = true;
                }

                aButtonStates[device] = isPressed;
            }
        }

        if (Input.GetKeyDown(KeyCode.P) || aOnPressed)
        {
            // mimic speech-action press
            inputSystem.RaiseOnInputDown(keyboardInputSource, handedness, speechAction);
        }

        if (Input.GetKeyUp(KeyCode.P) || aOnRelease)
        {
            // mimic speech-action release
            inputSystem.RaiseOnInputUp(keyboardInputSource, handedness, speechAction);
        }
    }
}
