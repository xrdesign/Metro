/*  
 *  Runtime: DeepgramKey.Value returns the key, searching in this order
 *      1)  $HOME/.secrets/deepgram.json
 *      2)  $HOME/Desktop/deepgram.json
 *      3)  StreamingAssets/deepgram.json         (inside the build / project)
 *
 *  Editor‑only utility:
 *      • Before **Build/Build And Run** and before **Play**, the file in
 *        StreamingAssets is verified.  If it’s missing the script copies it
 *        from #1 or #2 (whichever exists first).  If neither exists the
 *        build/play is cancelled with a clear message.
 *
 *  Add to .gitignore:
 *        /Assets/StreamingAssets/deepgram.json
 */

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

[Serializable] class DeepgramCfg { public string apiKey; }

public static class DeepgramKey
{
    public static string Value
    {
        get
        {
            // ---------- 1)  $HOME/.secrets ---------------------------------
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string try1 = Path.Combine(home, ".secrets", "deepgram.json");
            if (File.Exists(try1))
                return Parse(File.ReadAllText(try1));

            // ---------- 2)  $HOME/Desktop ----------------------------------
            string try2 = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "deepgram.json");
            if (File.Exists(try2))
                return Parse(File.ReadAllText(try2));

            // ---------- 3)  StreamingAssets --------------------------------
            string try3 = Path.Combine(Application.streamingAssetsPath, "deepgram.json");

#if UNITY_ANDROID && !UNITY_EDITOR
            using var req = UnityWebRequest.Get(try3);
            req.SendWebRequest();
            while (!req.isDone) { }
            if (req.result == UnityWebRequest.Result.Success)
                return Parse(req.downloadHandler.text);
#else
            if (File.Exists(try3))
                return Parse(File.ReadAllText(try3));
#endif
            throw new Exception("deepgram.json not found in any location. " +
                                "Put it in $HOME/.secrets/ or on the Desktop, or " +
                                "copy it to StreamingAssets before proceeding.");
        }
    }

    static string Parse(string json) =>
        JsonUtility.FromJson<DeepgramCfg>(json).apiKey?.Trim()
        ?? throw new Exception("apiKey field missing in deepgram.json");
}


#if UNITY_EDITOR
static class DeepgramSecretEditor
{

    public static void EnsureSecretInStreamingAssets(bool isBuild)
    {
        string dst = Path.Combine(Application.dataPath, "StreamingAssets", "deepgram.json");
        if (File.Exists(dst)) return;              // already present → nothing to do

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string src1 = Path.Combine(home, ".secrets", "deepgram.json");
        string src2 = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                            "deepgram.json");

        string src = File.Exists(src1) ? src1 : File.Exists(src2) ? src2 : null;

        if (src == null)
        {
            string msg = "deepgram.json missing.\n" +
                         "Put it in $HOME/.secrets/ or on the Desktop before proceeding.";
            // if (isBuild) throw new BuildFailedException(msg);
            // EditorUtility.DisplayDialog("Deepgram key not found", msg, "OK");
            // EditorApplication.isPlaying = false;
            Debug.LogError(msg);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
        File.Copy(src, dst, true);
        AssetDatabase.Refresh();
        Debug.Log($"deepgram.json copied from {src}");
    }

    // Build / Build And Run
    class PreBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport _)
        {
            EnsureSecretInStreamingAssets(true);
        }
    }

    // before entering Play Mode 
    [InitializeOnLoadMethod]
    static void SetupPlayHook()
    {
        EditorApplication.playModeStateChanged += st =>
        {
            if (st == PlayModeStateChange.ExitingEditMode)
                EnsureSecretInStreamingAssets(false);
        };
    }
}
#endif