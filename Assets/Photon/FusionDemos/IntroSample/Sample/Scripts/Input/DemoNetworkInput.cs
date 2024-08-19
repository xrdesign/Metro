using Fusion;

namespace FusionDemo {
  /// <summary>
  /// Struct that will be used to read and write the inputs for a player
  /// </summary>
  public struct DemoNetworkInput : INetworkInput {
    //Const values to better identify buttons
    public const int BUTTON_FORWARD = 1;
    public const int BUTTON_BACKWARD = 2;
    public const int BUTTON_LEFT = 3;
    public const int BUTTON_RIGHT = 4;
    public const int BUTTON_INTERACT = 5;

    /// <summary>
    /// Network buttons state
    /// </summary>
    public NetworkButtons Buttons;

    /// <summary>
    /// Is this button up?
    /// </summary>
    public bool IsUp(int button) {
      return Buttons.IsSet(button) == false;
    }

    /// <summary>
    /// Is this button down?
    /// </summary>
    public bool IsDown(int button) {
      return Buttons.IsSet(button);
    }

    /// <summary>
    /// Was this button pressed?
    /// </summary>
    /// <param name="prev">A previously stored <see cref="NetworkButtons"/> to compare</param>
    public bool WasPressed(NetworkButtons prev, int button) {
      return Buttons.WasPressed(prev, button);
    }
  }
}