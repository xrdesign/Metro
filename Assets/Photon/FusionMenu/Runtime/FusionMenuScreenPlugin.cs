namespace Fusion.Menu {
  /// <summary>
  /// Screen plugin are usually a UI features that is shared between multiple screens.
  /// The plugin must be registered at <see cref="FusionMenuUIScreen.Plugins"/> and receieve Show and Hide callbacks.
  /// </summary>
  public class FusionMenuScreenPlugin : FusionMonoBehaviour {
    /// <summary>
    /// The parent screen is shown.
    /// </summary>
    /// <param name="screen">Parent screen</param>
    public virtual void Show(FusionMenuUIScreen screen) {
    }

    /// <summary>
    /// The parent screen is hidden.
    /// </summary>
    /// <param name="screen">Parent screen</param>
    public virtual void Hide(FusionMenuUIScreen screen) {
    }
  }
}
