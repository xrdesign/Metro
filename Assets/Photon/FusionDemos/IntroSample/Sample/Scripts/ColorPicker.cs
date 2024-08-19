using Fusion;
using UnityEngine;

namespace FusionDemo {
  /// <summary>
  /// Interactable class for providing a specific color to the player.
  /// </summary>
  public class ColorPicker : MonoBehaviour, IInteractable {
    // Colors available.
    enum ColorPickerColors { Red, Green, Blue };

    // The color this ColorPicker will provide.
    [SerializeField] private ColorPickerColors _providedColor;

    private void Awake() {
      // Set the object material color as the color it will provide.
      GetComponent<Renderer>().material.color = GetProvidedColor();
    }

    public void Interact(NetworkRunner runner, PlayerRef playerInteracting) {
      // Get the player object for the interacting player.
      var playerObj = runner.GetPlayerObject(playerInteracting);

      // Give the PlayerColor of the interacting player the color this ColorPicker provide.
      if (playerObj.TryGetBehaviour<PlayerColor>(out var playerColor)) {
        playerColor.SetColor(GetProvidedColor());
      }
    }

    // Get the provided color for this ColorPicker.
    private Color GetProvidedColor() {
      switch (_providedColor) {
        case ColorPickerColors.Red:
          return Color.red;
        case ColorPickerColors.Green:
          return Color.green;
        case ColorPickerColors.Blue:
          return Color.blue;
        default:
          return Color.white;
      }
    }
  }
}