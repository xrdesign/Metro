using TMPro;
using UnityEngine;

namespace FusionDemo {
  public class GamePlayInstructions : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI instructionsText;

    private const string InstructionsStandalone = @"Pick colors and blend them together to make the highlighted color.

WASD: Move
E: Pick&Place Color";

    private const string InstructionsMobile = @"Pick colors and blend them together to make the highlighted color.

Left Thumb: Move
Interact Button: Pick&Place Color";

    private void Awake() {
#if UNITY_ANDROID || UNITY_IOS
      instructionsText.text = InstructionsMobile;
#else
      instructionsText.text = InstructionsStandalone;
#endif
    }
  }
}