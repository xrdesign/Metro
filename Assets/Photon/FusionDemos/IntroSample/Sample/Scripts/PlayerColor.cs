using System;
using Fusion;
using UnityEngine;

namespace FusionDemo {
  /// <summary>
  /// Class for indicate the player active color.
  /// </summary>
  public class PlayerColor : NetworkBehaviour {
    /// <summary>
    /// Action event triggered when the player active color is changed.
    /// </summary>
    public event Action<Color> OnColorChanged;

    // The networked color value for this player.
    [Networked] private Color _color { get; set; }

    // Change detector to detect changes on the networked color value.
    private ChangeDetector _changeDetector;

    public override void Spawned() {
      // Get the change detector.
      _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotTo, false);
      _color = Color.white;
    }

    /// <summary>
    /// Set the player active color.
    /// </summary>
    public void SetColor(Color color) {
      _color = color;
    }

    /// <summary>
    /// Get the player active color.
    /// </summary>
    public Color GetColor() {
      return _color;
    }

    public override void Render() {
      // If detect change on the color value, invoke the OnColorChangedEvent.
      foreach (var change in _changeDetector.DetectChanges(this)) {
        if (change == nameof(_color)) {
          OnColorChanged?.Invoke(_color);
        }
      }
    }
  }
}