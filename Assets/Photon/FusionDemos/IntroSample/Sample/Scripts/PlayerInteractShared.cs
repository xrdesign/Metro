using System;
using Fusion;
using UnityEngine;

namespace FusionDemo {
  /// <summary>
  /// Class responsible for make the player able to interact with Interactable objects on the world in shared mode.
  /// </summary>
  public class PlayerInteractShared : NetworkBehaviour {
    // Interact sphere radius.
    [SerializeField] private float _interactRadius = 1.25f;

    // Interact layer mask.
    [SerializeField] private LayerMask _interactLayer;

#if !UNITY_IOS && !UNITY_ANDROID
    // Used to detect if the interact button was pressed in shared mode context.
    private bool _interactPressed;
#endif
    
    // Collider array to store the interaction overlap query result.
    private Collider[] _interactQueryResult = new Collider[1];

#if UNITY_IOS || UNITY_ANDROID
    private MobileInput _mobileInput;

    private void Awake() {
      _mobileInput = FindFirstObjectByType<MobileInput>();
    }
#endif

    public override void FixedUpdateNetwork() {
      // If the interact button was pressed.
      if (GetInteractInput()) {
        // Query for objects on the interact area.
        var hits = Runner.GetPhysicsScene().OverlapSphere(transform.position + transform.forward * 1.5f,
          _interactRadius, _interactQueryResult, _interactLayer, QueryTriggerInteraction.UseGlobal);
        // For each hit detected, if the object implements IInteractable interface call the interact object for the first one detected.
        if (hits > 0) {
          for (int i = 0; i < hits && i < _interactQueryResult.Length; i++) {
            if (_interactQueryResult[i].TryGetComponent<IInteractable>(out var interactable)) {
              interactable.Interact(Runner, Object.StateAuthority);

              // Make sure to only interact with one object.
              break;
            }
          }
        }
      }
#if !UNITY_IOS && !UNITY_ANDROID
      _interactPressed = false;
#endif
    }

#if !UNITY_IOS && !UNITY_ANDROID
    private void Update() {
      // Detect interact input in update and store it to use it in FUN.
      if (Object.HasStateAuthority == false) return;

      if (Input.GetKeyDown(KeyCode.E)) {
        _interactPressed = true;
      }
    }
#endif

    private bool GetInteractInput() {
#if UNITY_IOS || UNITY_ANDROID
      return _mobileInput.ConsumeInteractInput();
#else
      return _interactPressed;
#endif
    }

    private void OnDrawGizmosSelected() {
      // Draw the interact area when selected.
      Gizmos.DrawSphere(transform.position + transform.forward * 1.5f, _interactRadius);
    }
  }
}