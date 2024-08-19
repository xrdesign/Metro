namespace Fusion.Menu {
  /// <summary>
  /// A scriptable object that has an id used by the FusionMenu as appversion.
  /// Mostly a developement feature to ensure to only meet compatible clients in the Photon matchmaking.
  /// </summary>
  //[CreateAssetMenu(menuName = "Photon/Menu/MachineId")]
  [ScriptHelp(BackColor = ScriptHeaderBackColor.Blue)]
  public class FusionMenuMachineId : FusionScriptableObject {
    /// <summary>
    /// An id that should be unique to this machine, used by the FusionMenu as AppVersion.
    /// An explicit asset importer is used to create local ids during import (see FusionMenuMachineIdImporter).
    /// </summary>
    [InlineHelp] public string Id;
  }
}
