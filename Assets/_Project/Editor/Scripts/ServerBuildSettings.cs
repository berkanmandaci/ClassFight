using UnityEditor;
using UnityEngine;

public class ServerBuildSettings
{
    [MenuItem("Server/Build Dedicated Server")]
    public static void BuildServer()
    {
        // Build player settings
        PlayerSettings.productName = "GameServer";
        PlayerSettings.companyName = "YourCompany";

        // Minimal graphics settings
        QualitySettings.SetQualityLevel(0, true);

        // Build scenes
        string[] scenes = { "Assets/_Project/Server/Scenes/ServerScene.unity" };

        // Build
        BuildPipeline.BuildPlayer(scenes,
            "Builds/Server/GameServer.x86_64",
            BuildTarget.StandaloneLinux64,
            BuildOptions.EnableHeadlessMode | BuildOptions.Development);
    }
}
