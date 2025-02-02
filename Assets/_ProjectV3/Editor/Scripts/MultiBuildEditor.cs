using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace _ProjectV3.Editor.Scripts 
{
    public class MultiBuildEditor : EditorWindow 
    {
        private const string BUILD_ROOT = "Builds";
        private const string WINDOWS_FOLDER = "Windows";
        private const string CLIENT_NAME = "ClientBuild.exe";
        private const string SERVER_NAME = "ServerBuild.exe";

        private const string CLIENT_SCENE = "Assets/_ProjectV3/Runtime/Client/Scenes/Client.unity";
        private const string SERVER_SCENE = "Assets/_ProjectV3/Runtime/Server/Scenes/Server.unity";
        private const string GAME_SCENE = "Assets/_ProjectV3/Runtime/Shared/Scenes/Game.unity";

        [MenuItem("ProjectV3/Build/Build Client & Server")]
        public static void BuildClientAndServer()
        {
            string buildPath = Path.Combine(Directory.GetCurrentDirectory(), BUILD_ROOT, WINDOWS_FOLDER);
            string clientPath = Path.Combine(buildPath, CLIENT_NAME);
            string serverPath = Path.Combine(buildPath, SERVER_NAME);

            try 
            {
                Debug.Log("Build işlemi başlatılıyor...");

                string[] clientScenes = { CLIENT_SCENE, GAME_SCENE };
                string[] serverScenes = { SERVER_SCENE, GAME_SCENE };

                // Client build
                Debug.Log("Client build başlatılıyor...");
                BuildResult clientResult = Build(clientScenes, clientPath);
                
                // Server build
                Debug.Log("Server build başlatılıyor...");
                BuildResult serverResult = Build(serverScenes, serverPath);

                if (clientResult == BuildResult.Succeeded && serverResult == BuildResult.Succeeded)
                {
                    Debug.Log("Tüm buildler başarıyla tamamlandı!");
                    EditorUtility.RevealInFinder(buildPath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Build sırasında hata oluştu: {e.Message}");
            }
        }

        private static BuildResult Build(string[] scenes, string path)
        {
            try
            {
                // Build klasörünü oluştur
                string directoryPath = Path.GetDirectoryName(path);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Build ayarlarını yapılandır
                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = path,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None  // Normal Windows build
                };

                // Build'i gerçekleştir
                BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                
                if (report.summary.result == BuildResult.Succeeded)
                {
                    Debug.Log($"Build başarıyla tamamlandı: {path}");
                }
                else
                {
                    Debug.LogError($"Build başarısız oldu: {path}");
                }

                return report.summary.result;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Build sırasında hata oluştu: {e.Message}");
                return BuildResult.Failed;
            }
        }
    }
}