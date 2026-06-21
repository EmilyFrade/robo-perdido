#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RoboPerdido.EditorTools
{
    /// <summary>
    /// Build do executavel Windows pela linha de comando:
    ///   Unity.exe -batchmode -quit -projectPath . -executeMethod RoboPerdido.EditorTools.Builder.BuildWindows
    /// Saida em Builds/Windows/RoboPerdido.exe (pasta ignorada pelo git).
    /// </summary>
    public static class Builder
    {
        [MenuItem("Robo Perdido/Gerar EXE Windows (Setor Montagem)")]
        public static void BuildWindows()
        {
            string[] scenes = { "Assets/Scenes/SectorMontagem.unity" };
            const string dir = "Builds/Windows";
            const string exe = dir + "/RoboPerdido.exe";
            System.IO.Directory.CreateDirectory(dir);

            PlayerSettings.productName = "Robo Perdido - Setor Montagem";
            PlayerSettings.companyName = "UFOP - DDJ";

            // Janela: inicia em modo janela (mostra os botoes de maximizar/minimizar),
            // redimensionavel, e permite alternar para tela cheia (Alt+Enter / F11 no jogo).
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.allowFullscreenSwitch = true;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.runInBackground = true;

            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            BuildSummary s = report.summary;
            if (s.result == BuildResult.Succeeded)
                Debug.Log("BUILD_OK bytes=" + s.totalSize + " path=" + exe);
            else
                Debug.LogError("BUILD_FAILED result=" + s.result + " errors=" + s.totalErrors);
        }
    }
}
#endif
