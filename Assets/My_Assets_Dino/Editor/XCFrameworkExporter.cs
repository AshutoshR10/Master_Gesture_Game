#if UNITY_EDITOR && UNITY_IOS
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;

public class XCFrameworkExporter
{
    private const string BuildPath = "Builds/iOS";
    private const string XCFrameworkOutput = "Builds/UnityFramework.xcframework";

    [MenuItem("Unity/Build/Export UnityFramework.xcframework")]
    public static void ExportXCFramework()
    {
        // 1. Build iOS project
        //Debug.Log(" Building Unity iOS Xcode project...");
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = BuildPath,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };
        BuildPipeline.BuildPlayer(options);

        // 2. Paths for builds
        string iosProj = Path.GetFullPath(BuildPath);
        string buildDir = Path.Combine(iosProj, "build");
        string deviceFramework = Path.Combine(buildDir, "Release-iphoneos/UnityFramework.framework");
        string simFramework = Path.Combine(buildDir, "Release-iphonesimulator/UnityFramework.framework");

        // Ensure output folder
        if (Directory.Exists(XCFrameworkOutput)) Directory.Delete(XCFrameworkOutput, true);

        // 3. Build Device + Simulator frameworks
        RunXcodeBuild(iosProj, "iphoneos");
        RunXcodeBuild(iosProj, "iphonesimulator");

        // 4. Create XCFramework
        RunCreateXCFramework(deviceFramework, simFramework, XCFrameworkOutput);

        //Debug.Log($" Export complete: {XCFrameworkOutput}");
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        string[] enabled = new string[scenes.Length];
        for (int i = 0; i < scenes.Length; i++) enabled[i] = scenes[i].path;
        return enabled;
    }

    private static void RunXcodeBuild(string projectPath, string sdk)
    {
        string args = $"-project \"{projectPath}/Unity-iPhone.xcodeproj\" -scheme Unity-iPhone -sdk {sdk} -configuration Release CONFIGURATION_BUILD_DIR=\"{projectPath}/build/Release-{sdk}\" clean build";
        RunProcess("xcodebuild", args);
        //Debug.Log($" Built for {sdk}");
    }

    private static void RunCreateXCFramework(string devicePath, string simPath, string outputPath)
    {
        string args = $"-create-xcframework -framework \"{devicePath}\" -framework \"{simPath}\" -output \"{outputPath}\"";
        RunProcess("xcodebuild", args);
    }

    private static void RunProcess(string fileName, string arguments)
    {
        Process process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(output)) UnityEngine.Debug.Log(output);
        if (!string.IsNullOrEmpty(error)) UnityEngine.Debug.LogError(error);
    }
}
#endif
