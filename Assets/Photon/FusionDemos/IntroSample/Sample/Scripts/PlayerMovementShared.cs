using System;
using Fusion;
using UnityEngine;

namespace FusionDemo {
  /// <summary>
  /// A simple networked player movement class for shared mode.
  /// </summary>
  [RequireComponent(typeof(NetworkCharacterController))]
  public class PlayerMovementShared : NetworkBehaviour {
    private NetworkCharacterController _cc;

#if UNITY_IOS || UNITY_ANDROID
    private MobileInput _mobileInput;

    private void Awake() {
      _mobileInput = FindFirstObjectByType<MobileInput>();
    }
#endif

    public override void Spawned() {
      // get the NetworkCharacterController reference
      _cc = GetBehaviour<NetworkCharacterController>();
    }

    public override void FixedUpdateNetwork() {
      var dir = GetMoveInput();

      // Move with the direction calculated
      _cc.Move(dir.normalized);
    }

    private Vector3 GetMoveInput() {
      // initial direction, no movement
      var dir = Vector3.zero;

#if UNITY_IOS || UNITY_ANDROID
      // Handle mobile input
      dir = new Vector3(_mobileInput.JoystickDirection.x, 0, _mobileInput.JoystickDirection.y);
#else
      // Handle horizontal input
      dir += Vector3.right * Input.GetAxisRaw("Horizontal");

      // Handle vertical input
      dir += Vector3.forward * Input.GetAxisRaw("Vertical");
#endif
      return dir.normalized;
    }
  }
}