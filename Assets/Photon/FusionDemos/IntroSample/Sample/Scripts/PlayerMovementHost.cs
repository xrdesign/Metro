using Fusion;
using UnityEngine;

namespace FusionDemo {
  /// <summary>
  /// A simple networked player movement class for host/server mode.
  /// </summary>
  [RequireComponent(typeof(NetworkCharacterController))]
  public class PlayerMovementHost : NetworkBehaviour {
    private NetworkCharacterController _cc;
    
    [Networked] private NetworkButtons NetworkButtons { get; set; }

    public override void Spawned() {
      // get the NetworkCharacterController reference
      _cc = GetBehaviour<NetworkCharacterController>();
    }

    public override void FixedUpdateNetwork() {
      // If we received input from the input authority
      // The NetworkObject input authority AND the server/host will have the inputs
      if (GetInput<DemoNetworkInput>(out var input)) {
        var dir = default(Vector3);

        // Handle horizontal input
        if (input.IsDown(DemoNetworkInput.BUTTON_RIGHT)) {
          dir += Vector3.right;
        } else if (input.IsDown(DemoNetworkInput.BUTTON_LEFT)) {
          dir += Vector3.left;
        }

        // Handle vertical input
        if (input.IsDown(DemoNetworkInput.BUTTON_FORWARD)) {
          dir += Vector3.forward;
        } else if (input.IsDown(DemoNetworkInput.BUTTON_BACKWARD)) {
          dir += Vector3.back;
        }

        // Move with the direction calculated
        _cc.Move(dir.normalized);

        // Store the current buttons to use them on the next FUN (FixedUpdateNetwork) call
        NetworkButtons = input.Buttons;
      }
    }
  }
}