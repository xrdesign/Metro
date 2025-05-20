using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
// TMP import API
using TMPro;
// MRTK URP shader upgrade API
using Microsoft.MixedReality.Toolkit.Editor;

public static class BuildScript
{
    // Path to the scene(s) to include in the build
    static readonly string[] Scenes = { "Assets/_Metro/Scenes/Metro.unity" };
    const string ControllersFolder =
        "Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/StandardAssets/Controllers/Visuals/Models";

    static void UpgradeShaderForUniversalRenderPipeline()
    {

        string path = AssetDatabase.GetAssetPath(Microsoft.MixedReality.Toolkit.Utilities.StandardShaderUtility.MrtkStandardShader);

        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                string upgradedShader = File.ReadAllText(path);

#if UNITY_2019_1_OR_NEWER
                upgradedShader = upgradedShader.Replace("Tags{ \"RenderType\" = \"Opaque\" \"LightMode\" = \"ForwardBase\" }",
                                                        "Tags{ \"RenderType\" = \"Opaque\" \"LightMode\" = \"UniversalForward\" }");
#else
                    upgradedShader = upgradedShader.Replace("Tags{ \"RenderType\" = \"Opaque\" \"LightMode\" = \"ForwardBase\" }",
                                                            "Tags{ \"RenderType\" = \"Opaque\" \"LightMode\" = \"LightweightForward\" }");
#endif

                upgradedShader = upgradedShader.Replace("//#define _RENDER_PIPELINE",
                                                        "#define _RENDER_PIPELINE");

                File.WriteAllText(path, upgradedShader);
                AssetDatabase.Refresh();

#if UNITY_2019_1_OR_NEWER
                Debug.LogFormat("Upgraded {0} for use with the Universal Render Pipeline.", path);
#else
                    Debug.LogFormat("Upgraded {0} for use with the Lightweight Render Pipeline.", path);
#endif
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        else
        {
            Debug.LogErrorFormat("Failed to get asset path to: {0}", Microsoft.MixedReality.Toolkit.Utilities.StandardShaderUtility.MrtkStandardShaderName);
        }
    }

    static void PreBuild()
    {
        // 1. Open the scene so any pop-ups or data refresh happen
        EditorSceneManager.OpenScene(Scenes[0]);

        // 2. Upgrade all MRTK Standard shaders for URP via the API
        UpgradeShaderForUniversalRenderPipeline();
        // var inspectorAsm = AppDomain.CurrentDomain
        //     .GetAssemblies()
        //     .FirstOrDefault(a => a.GetName().Name == "Microsoft.MixedReality.Toolkit.Editor.Inspectors");
        // if (inspectorAsm != null)
        // {
        //     var shaderGUIType = inspectorAsm
        //         .GetType("Microsoft.MixedReality.Toolkit.Editor.MixedRealityStandardShaderGUI");
        //     if (shaderGUIType != null)
        //     {
        //         var mi = shaderGUIType.GetMethod(
        //             "UpgradeShaderForUniversalRenderPipeline",
        //             BindingFlags.Static | BindingFlags.NonPublic
        //         );
        //         mi?.Invoke(null, null);
        //     }
        // }

        // 3. Import TextMeshPro Essential Resources via the API
        TMP_PackageUtilities.ImportProjectResourcesMenu();

        // 4. Force re-import of controller models so their materials pick up the new URP shaders
        AssetDatabase.ImportAsset(ControllersFolder, ImportAssetOptions.ImportRecursive);
        AssetDatabase.Refresh();
    }

    // Builds Windows Standalone (64-bit)
    public static void BuildWindows()
    {
        PreBuild();
        string outputPath = "build/windows/Metro.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        BuildPipeline.BuildPlayer(Scenes, outputPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
    }

    // Builds macOS Standalone
    public static void BuildMacOS()
    {
        PreBuild();
        string outputPath = "build/macos/Metro.app";
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        BuildPipeline.BuildPlayer(Scenes, outputPath, BuildTarget.StandaloneOSX, BuildOptions.None);
    }

    // Builds WebGL
    public static void BuildWebGL()
    {
        PreBuild();
        string outputPath = "build/webgl";
        Directory.CreateDirectory(outputPath);
        BuildPipeline.BuildPlayer(Scenes, outputPath, BuildTarget.WebGL, BuildOptions.None);
    }

    // Builds Android APK (Quest)
    public static void BuildAndroid()
    {
        PreBuild();
        string outputPath = "build/android/Metro.apk";
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        BuildPipeline.BuildPlayer(Scenes, outputPath, BuildTarget.Android, BuildOptions.None);
    }
}
