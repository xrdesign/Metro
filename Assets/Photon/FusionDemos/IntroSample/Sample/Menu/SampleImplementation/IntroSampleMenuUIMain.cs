using Fusion.Menu;

namespace FusionDemo {
  public class IntroSampleMenuUIMain : FusionMenuUIMain
  {
    protected override void OnCharacterButtonPressed() {
      base.OnCharacterButtonPressed();
      Controller.Show<IntroSampleCharacterSelectionUI>();
    }
  }
}
