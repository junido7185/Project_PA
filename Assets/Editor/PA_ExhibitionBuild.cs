using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 전시 촬영용 Windows 릴리스 빌드(batch mode 전용 진입점).
// Unity.exe -batchmode -projectPath <프로젝트> -executeMethod PA_ExhibitionBuild.BuildBatch [-pa-build-out <exe 경로>] -logFile <log>
// 씬 = EditorBuildSettings의 활성 씬, 대상 = StandaloneWindows64, BuildOptions.None.
// exe 옆에 exhibition-build-report.json을 남기고 "PA_EXHIBITION_BUILD result=..." 한 줄을 기록한 뒤 0(성공)/1(실패)로 종료한다.
public static class PA_ExhibitionBuild
{
    public const string DefaultOutput = "Builds/Windows/ProjectPA-Exhibition/ProjectPA.exe";
    public const string ReportName = "exhibition-build-report.json";
    const int MaxMessages = 60;

    [Serializable]
    sealed class Report
    {
        public string result = "NotStarted";
        public bool success;
        public int errors, warnings;
        public double seconds;
        public long sizeBytes, folderBytes;
        public string output, reportPath, target = "StandaloneWindows64", options = "None", unityVersion, startedLocal, finishedLocal;
        public string[] scenes = new string[0];
        public List<string> errorMessages = new List<string>();
        public List<string> warningMessages = new List<string>();
    }

    public static void BuildBatch()
    {
        var report = new Report
        {
            unityVersion = Application.unityVersion,
            startedLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        DateTime started = DateTime.UtcNow;
        string reportPath = null;
        try
        {
            string output = ResolveOutput(Arg("-pa-build-out"));
            report.output = output;
            reportPath = Path.Combine(Path.GetDirectoryName(output), ReportName);
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode 중에는 빌드할 수 없습니다.");
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s != null && s.enabled && !string.IsNullOrEmpty(s.path)).Select(s => s.path).ToArray();
            report.scenes = scenes;
            if (scenes.Length == 0) throw new InvalidOperationException("EditorBuildSettings에 활성 씬이 없습니다.");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport build = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            });
            BuildSummary summary = build.summary;
            report.result = summary.result.ToString();
            report.success = summary.result == BuildResult.Succeeded;
            report.errors = summary.totalErrors;
            report.warnings = summary.totalWarnings;
            report.sizeBytes = (long)summary.totalSize;
            report.seconds = summary.totalTime.TotalSeconds;
            if (build.steps != null)
                foreach (BuildStep step in build.steps)
                {
                    if (step.messages == null) continue;
                    foreach (BuildStepMessage message in step.messages)
                    {
                        if ((message.type == LogType.Error || message.type == LogType.Exception || message.type == LogType.Assert) &&
                            report.errorMessages.Count < MaxMessages) report.errorMessages.Add(message.content);
                        else if (message.type == LogType.Warning && report.warningMessages.Count < MaxMessages)
                            report.warningMessages.Add(message.content);
                    }
                }
            report.folderBytes = FolderBytes(Path.GetDirectoryName(output));
        }
        catch (Exception e)
        {
            report.result = "Exception";
            report.success = false;
            report.errorMessages.Add(e.GetType().Name + ": " + e.Message);
            Debug.LogException(e);
        }
        if (report.seconds <= 0d) report.seconds = (DateTime.UtcNow - started).TotalSeconds;
        report.finishedLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        report.reportPath = WriteReport(report, reportPath);
        Debug.Log($"PA_EXHIBITION_BUILD result={report.result} success={report.success} errors={report.errors} warnings={report.warnings} " +
                  $"seconds={report.seconds:F1} sizeBytes={report.sizeBytes} output={report.output} report={report.reportPath}");
        EditorApplication.Exit(report.success ? 0 : 1);
    }

    static string ResolveOutput(string requested)
    {
        string path = string.IsNullOrWhiteSpace(requested) ? DefaultOutput : requested.Trim().Trim('"');
        if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) path = Path.Combine(path, "ProjectPA.exe");
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(projectRoot, path));
    }

    static string WriteReport(Report report, string preferredPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        foreach (string path in new[] { preferredPath, Path.Combine(projectRoot, "Logs", ReportName) })
        {
            if (string.IsNullOrEmpty(path)) continue;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(report, true));
                return path;
            }
            catch (Exception e) { Debug.LogWarning("[PA_ExhibitionBuild] report 저장 실패 " + path + ": " + e.Message); }
        }
        return string.Empty;
    }

    static long FolderBytes(string folder)
    {
        try
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return 0L;
            return Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
        }
        catch (Exception) { return 0L; }
    }

    static string Arg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        return null;
    }
}
