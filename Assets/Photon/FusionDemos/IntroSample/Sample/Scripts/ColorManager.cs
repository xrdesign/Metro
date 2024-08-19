using System.Linq;
using Fusion;
using UnityEngine;

namespace FusionDemo {
  /// <summary>
  /// Color merge struct will hold the color requested and the necessary colors to mix.
  /// </summary>
  internal struct ColorMerge {
    public Color Result;
    public Color[] Source;
  }

  /// <summary>
  /// Class responsible for displaying the requested color and checking if the provided colors are correct.
  /// </summary>
  public class ColorManager : NetworkBehaviour {
    // Reference to the objects that will be used to add the colors and the object that will show the resulting color
    [SerializeField] private ColorDeposit _colorProvider1;
    [SerializeField] private ColorDeposit _colorProvider2;
    [SerializeField] private Renderer _colorResultRenderer;

    // Networked index of the active color merge.
    [Networked] private int _currentColorRequestIndex { get; set; }

    // Change detector to detect changes on the index.
    private ChangeDetector _changeDetector;

    // The available colors.
    private ColorMerge[] ColorMerges = new[] {
      new ColorMerge() { Result = Color.yellow, Source = new[] { Color.red, Color.green } }, 
      new ColorMerge() { Result = Color.cyan, Source = new[] { Color.blue, Color.green } },
      new ColorMerge() { Result = Color.magenta, Source = new[] { Color.red, Color.blue } },
    };

    public override void Spawned() {
      // Get the change detector.
      _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState, false);
      // Setup first color request as state authority. No point in clients predicting this.
      if (Object.HasStateAuthority) {
        SetupColorRequest();
      }
    }

    // Reset colors providers and increment the request index.
    private void SetupColorRequest() {
      _colorProvider1.ResetColor();
      _colorProvider2.ResetColor();
      _currentColorRequestIndex = ++_currentColorRequestIndex % ColorMerges.Length;
    }

    // Set the result object material color to the request color.
    private void OnResultColorChanged() {
      _colorResultRenderer.material.color = ColorMerges[_currentColorRequestIndex].Result;
    }

    public void CheckColorsMatch() {
      // Run only on state authority
      if (Object.HasStateAuthority == false) return;

      // If same colors are provided, no match is detected.
      if (_colorProvider1.GetColor() == _colorProvider2.GetColor()) return;

      // Check if the first color provided is source for the result requested color.
      if (ColorMerges[_currentColorRequestIndex].Source.Contains(_colorProvider1.GetColor()) == false)
        return;

      // Check if the second color provided is source for the result requested color.
      if (ColorMerges[_currentColorRequestIndex].Source.Contains(_colorProvider2.GetColor()) == false)
        return;

      // If we made it here, the provided colors are correct. Setup a new color request.
      SetupColorRequest();
    }

    public override void Render() {
      // If the request index changed, update the result object material color.
      foreach (var change in _changeDetector.DetectChanges(this)) {
        if (change == nameof(_currentColorRequestIndex)) {
          OnResultColorChanged();
        }
      }
    }
  }
}