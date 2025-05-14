using UnityEditor;
using System.IO;

public static class BuildScript
{
    // Path to the scene(s) to include in the build
    static readonly string[] Scenes = { "Assets/_Metro/Scenes/Metro.unity" };

    // Builds Windows Standalone (64-bit)
    public static void BuildWindows()
    {
        string outputPath = "build/windows/Metro.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        BuildPipeline.BuildPlayer(Scenes, outputPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
    }

    // Builds macOS Standalone
    public static void BuildMacOS()
    {
        string outputPath = "build/macos/Metro.app";
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        BuildPipeline.BuildPlayer(Scenes, outputPath, BuildTarget.StandaloneOSX, BuildOptions.None);
    }

    // Builds WebGL
    public static void BuildWebGL()
    {
        string outputPath = "build/webgl";
        Directory.CreateDirectory(outputPath);
        BuildPipeline.BuildPlayer(Scenes, outputPath, BuildTarget.WebGL, BuildOptions.None);
    }

    // Builds Android APK (Quest)
    public static void BuildAndroid()
    {
        string outputPath = "build/android/Metro.apk";
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        BuildPipeline.BuildPlayer(Scenes, outputPath, BuildTarget.Android, BuildOptions.None);
    }
}
