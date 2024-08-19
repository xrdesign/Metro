namespace Fusion.Editor {
  using Menu;
  using UnityEditor.AssetImporters;
  using UnityEngine;

  /// <summary>
  /// All asset ending with .id will be tried for a <see cref="FusionMenuMachineId"/> script.
  /// A local id is created for it that will never go into version control.
  /// </summary>
  [ScriptedImporter(1, "id")]
  public class FusionMenuMachineIdImporter : ScriptedImporter {
    public override void OnImportAsset(AssetImportContext ctx) {
      var mainAsset = ScriptableObject.CreateInstance<FusionMenuMachineId>();
      if (mainAsset != null) {
        //mainAsset.Id = System.Guid.NewGuid().ToString();
        // Random readable code should be enough and it's readable.
        mainAsset.Id = FusionMenuPartyCodeGenerator.Create(8, "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789");
        ctx.AddObjectToAsset("root", mainAsset);
      }
    }
  }
}
