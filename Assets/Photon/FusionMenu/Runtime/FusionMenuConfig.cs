namespace Fusion.Menu {
  using System.Collections.Generic;
  using UnityEngine;

  /// <summary>
  /// Photon menu config file implements <see cref="IFusionMenuConfig"/>.
  /// Stores static options that affect parts of the menu behavior and selectable configurations.
  /// </summary>
  [ScriptHelp(BackColor = ScriptHeaderBackColor.Blue)]
  [CreateAssetMenu(menuName = "Fusion/Menu/Menu Config")]
  public class FusionMenuConfig : FusionScriptableObject, IFusionMenuConfig {
    /// <summary>
    /// The maximum player count allowed for all game modes.
    /// </summary>
    [InlineHelp, SerializeField] protected int _maxPlayers = 6;
    /// <summary>
    /// Force 60 FPS during menu animations.
    /// </summary>
    [InlineHelp, SerializeField] protected bool _adaptFramerateForMobilePlatform = true;
    /// <summary>
    /// The available Photon AppVersions to be selecteable by the user.
    /// An empty list will hide the related dropdown on the settings screen.
    /// </summary>
    [InlineHelp, SerializeField] protected List<string> _availableAppVersions = new List<string> { "1.0" };
    /// <summary>
    /// Static list of regions available in the settings.
    /// An empty entry symbolizes best region option.
    /// An empty list will hide the related dropdown on the settings screen.
    /// </summary>
    [InlineHelp, SerializeField] protected List<string> _availableRegions = new List<string> { "asia", "eu", "sa", "us" };
    /// <summary>
    /// Static list of scenes available in the scenes menu.
    /// An empty list will hide the related button in the main screen.
    /// PhotonMeneSceneInfo.Name = displayed name
    /// PhotonMeneSceneInfo.ScenePath = the actual Unity scene (must be included in BuildSettings)
    /// PhotonMeneSceneInfo.Preview = a sprite with a preview of the scene (screenshot) that is displayed in the main menu and scene selection screen (can be null)
    /// </summary>
    [InlineHelp, SerializeField] protected List<PhotonMenuSceneInfo> _availableScenes = new List<PhotonMenuSceneInfo>();
    /// <summary>
    /// The <see cref="FusionMenuMachineId"/> ScriptableObject that stores local ids to use as an option in for AppVersion.
    /// Designed as a convenient development feature.
    /// Can be null.
    /// </summary>
    [InlineHelp, SerializeField] protected FusionMenuMachineId _machineId;
    /// <summary>
    /// The <see cref="FusionMenuPartyCodeGenerator"/> ScriptableObject that is required for party code generation.
    /// Also used to create random player names.
    /// </summary>
    [InlineHelp, SerializeField] protected FusionMenuPartyCodeGenerator _codeGenerator;

    public List<string> AvailableAppVersions => _availableAppVersions;
    public List<string> AvailableRegions => _availableRegions;
    public List<PhotonMenuSceneInfo> AvailableScenes => _availableScenes;
    public int MaxPlayerCount => _maxPlayers;
    public virtual string MachineId => _machineId?.Id;
    public FusionMenuPartyCodeGenerator CodeGenerator => _codeGenerator;
    public bool AdaptFramerateForMobilePlatform => _adaptFramerateForMobilePlatform;
  }
}
