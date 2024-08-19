using Fusion;
using UnityEngine;

namespace FusionDemo {
  public abstract class ColorDeposit : NetworkBehaviour, IInteractable {
    public abstract void Interact(NetworkRunner runner, PlayerRef playerInteracting);

    public abstract Color GetColor();
    public abstract void ResetColor();
  }
}