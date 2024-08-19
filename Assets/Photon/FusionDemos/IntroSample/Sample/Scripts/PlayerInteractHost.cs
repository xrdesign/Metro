using Fusion;
using UnityEngine;

namespace FusionDemo {
  /// <summary>
  /// Class responsible for make the player able to interact with Interactable objects on the world in host mode.
  /// </summary>
  public class PlayerInteractHost : NetworkBehaviour {
    // Interact sphere radius.
    [SerializeField] private float _interactRadius = 1.25f;

    // Interact layer mask.
    [SerializeField] private LayerMask _interactLayer;

    // Previous NetworkButtons. Used to detect if a button was pressed in the previous tick.
    private NetworkButtons _prevInputButtons;

    // Collider array to store the interaction overlap query result.
    private Collider[] _interactQueryResult = new Collider[1];

    public override void FixedUpdateNetwork() {
      // Get input for this tick.
      if (GetInput<DemoNetworkInput>(out var input)) {
        // If the interact button was pressed.
        if (input.WasPressed(_prevInputButtons, DemoNetworkInput.BUTTON_INTERACT)) {
          // Query for objects on the interact area.
          var hits = Runner.GetPhysicsScene().OverlapSphere(transform.position + transform.forward * 1.5f, _interactRadius, _interactQueryResult, _interactLayer, QueryTriggerInteraction.UseGlobal);
          // For each hit detected, if the object implements IInteractable interface call the interact object for the first one detected.
          if (hits > 0) {
            for (int i = 0; i < hits && i < _interactQueryResult.Length; i++) {
              if (_interactQueryResult[i].TryGetComponent<IInteractable>(out var interactable)) {
                interactable.Interact(Runner, Object.InputAuthority);

                // Make sure to only interact with one object.
                break;
              }
            }
          }
        }

        // Store the input buttons to use on the next tick.
        _prevInputButtons = input.Buttons;
      }
    }

    private void OnDrawGizmosSelected() {
      // Draw the interact area when selected.
      Gizmos.DrawSphere(transform.position + transform.forward * 1.5f, _interactRadius);
    }
  }
}