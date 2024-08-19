using Fusion;
using UnityEngine;

namespace FusionDemo {
  /// <summary>
  /// Interactable class for the object that will be colored by the player in host mode.
  /// </summary>
  public class ColorDepositHost : ColorDeposit {
    // Networked color of this object.
    [Networked] private Color _color { get; set; }

    // Color manager reference to trigger the color match check.
    [SerializeField] private ColorManager _colorManager;

    // Change detector to react to color changes.
    private ChangeDetector _changeDetector;

    public override void Spawned() {
      // Get the change detector for this object.
      _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotTo, false);
    }

    public override void Interact(NetworkRunner runner, PlayerRef playerInteracting) {
      // Get the object registered as player object of the player who is interacting.
      var playerObj = runner.GetPlayerObject(playerInteracting);

      // If able to get the player color behaviour, get the player color and set as this object color.
      // Also trigger the colors match check on the color manager.
      if (playerObj.TryGetBehaviour<PlayerColor>(out var playerColor)) {
        var color = playerColor.GetColor();
        _color = color;
        _colorManager.CheckColorsMatch();
      }
    }

    /// <summary>
    /// Reset this object color to white.
    /// </summary>
    public override void ResetColor() {
      _color = Color.white;
    }

    /// <summary>
    /// Get the current object color.
    /// </summary>
    public override Color GetColor() {
      return _color;
    }

    // Set the object material color
    private void SetMaterialColor(Color color) {
      if (TryGetComponent<Renderer>(out var rederer)) {
        rederer.material.color = _color;
      }
    }

    public override void Render() {
      // If we detect a color change, update the object material color.
      foreach (var change in _changeDetector.DetectChanges(this)) {
        if (change == nameof(_color)) {
          SetMaterialColor(_color);
        }
      }
    }
  }
}