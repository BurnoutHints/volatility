using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using Volatility.Resources;

using static Volatility.Utilities.EnvironmentUtilities;

namespace Volatility.Utilities;

public static class DxcShaderCompiler
{
    private const string DxcPathEnvVar = "VOLATILITY_DXC_PATH";

    public static void CompileStagesToCSO(ShaderBase shader, IReadOnlyList<ShaderStageCompile> stages, Func<ShaderStageCompile, string> outputPathFactory)
    {
        if (shader == null)
            throw new ArgumentNullException(nameof(shader));
        if (stages == null || stages.Count == 0)
            throw new InvalidOperationException("No shader stages were provided.");

        foreach (var stage in stages)
        {
            string outputPath = outputPathFactory(stage);
            CompileToCSO(shader, stage, outputPath);
        }
    }

    public static void CompileToCSO(ShaderBase shader, ShaderStageCompile stage, string outputPath)
    {
        if (shader == null)
            throw new ArgumentNullException(nameof(shader));
        if (stage == null)
            throw new ArgumentNullException(nameof(stage));

        string entryPoint = ResolveEntryPoint(shader, stage);
        string targetProfile = ResolveTargetProfile(shader, stage);

        string? outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        string dxcPath = ResolveDxcPath();
        string sourcePath = ResolveSourcePath(shader);

        ProcessStartInfo startInfo = BuildStartInfo(dxcPath, sourcePath, shader, stage, entryPoint, targetProfile, outputPath);
        using Process process = new() { StartInfo = startInfo };

        StringBuilder output = new();
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                output.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                output.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            string message = output.Length > 0 ? output.ToString() : "No compiler output.";
            throw new InvalidOperationException($"dxc failed (exit {process.ExitCode}).\n{message}");
        }
    }

    private static ProcessStartInfo BuildStartInfo(
        string dxcPath,
        string sourcePath,
        ShaderBase shader,
        ShaderStageCompile stage,
        string entryPoint,
        string targetProfile,
        string outputPath)
    {
        var start = new ProcessStartInfo
        {
            FileName = dxcPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        start.ArgumentList.Add("-nologo");
        start.ArgumentList.Add("-E");
        start.ArgumentList.Add(entryPoint);
        start.ArgumentList.Add("-T");
        start.ArgumentList.Add(targetProfile);
        start.ArgumentList.Add("-Fo");
        start.ArgumentList.Add(outputPath);

        foreach (var define in EnumerateDefines(shader, stage))
        {
            if (define == null || string.IsNullOrWhiteSpace(define.Name))
                continue;

            start.ArgumentList.Add("-D");
            start.ArgumentList.Add(string.IsNullOrWhiteSpace(define.Value)
                ? define.Name
                : $"{define.Name}={define.Value}");
        }

        if (shader.IncludeDirectories != null)
        {
            foreach (var includeDir in shader.IncludeDirectories)
            {
                if (string.IsNullOrWhiteSpace(includeDir))
                    continue;

                start.ArgumentList.Add("-I");
                start.ArgumentList.Add(includeDir);
            }
        }

        if (shader.AdditionalArguments != null)
        {
            foreach (var arg in shader.AdditionalArguments)
            {
                if (string.IsNullOrWhiteSpace(arg))
                    continue;

                start.ArgumentList.Add(arg);
            }
        }

        if (stage.AdditionalArguments != null)
        {
            foreach (var arg in stage.AdditionalArguments)
            {
                if (string.IsNullOrWhiteSpace(arg))
                    continue;

                start.ArgumentList.Add(arg);
            }
        }

        start.ArgumentList.Add(sourcePath);
        return start;
    }

    private static IEnumerable<ShaderDefine> EnumerateDefines(ShaderBase shader, ShaderStageCompile stage)
    {
        if (shader.Defines != null)
        {
            foreach (var define in shader.Defines)
                yield return define;
        }

        if (stage.Defines != null)
        {
            foreach (var define in stage.Defines)
                yield return define;
        }
    }

    private static string ResolveEntryPoint(ShaderBase shader, ShaderStageCompile stage)
    {
        if (!string.IsNullOrWhiteSpace(stage.EntryPoint))
            return stage.EntryPoint;

        if (!string.IsNullOrWhiteSpace(shader.EntryPoint))
            return shader.EntryPoint;

        return "main";
    }

    private static string ResolveTargetProfile(ShaderBase shader, ShaderStageCompile stage)
    {
        if (!string.IsNullOrWhiteSpace(stage.TargetProfile))
            return stage.TargetProfile;

        string? prefix = ShaderStageCompile.GetProfilePrefix(stage.ResolveStage());
        if (!string.IsNullOrWhiteSpace(prefix))
            return $"{prefix}_5_0";

        if (!string.IsNullOrWhiteSpace(shader.TargetProfile))
            return shader.TargetProfile;

        return "ps_5_0";
    }

    private static string ResolveSourcePath(ShaderBase shader)
    {
        string? resolvedPath = shader.ResolveShaderSourcePath();
        if (string.IsNullOrWhiteSpace(resolvedPath))
            throw new InvalidOperationException("ShaderSourcePath is empty.");

        if (!File.Exists(resolvedPath))
            throw new FileNotFoundException($"Shader source file not found: {resolvedPath}");

        return resolvedPath;
    }

    private static string ResolveDxcPath()
    {
        string? overridePath = Environment.GetEnvironmentVariable(DxcPathEnvVar);
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            if (!File.Exists(overridePath))
                throw new FileNotFoundException($"DXC override path not found: {overridePath}");
            return overridePath;
        }

        string exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dxc.exe" : "dxc";
        string toolsDir = GetEnvironmentDirectory(EnvironmentDirectory.Tools);
        string rid = GetRuntimeRid();

        string[] candidates =
        [
            Path.Combine(toolsDir, "dxc", exeName),
            Path.Combine(toolsDir, "dxc", rid, exeName),
            Path.Combine(toolsDir, "dxc", "bin", exeName)
        ];

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        string? pathCandidate = FindOnPath(exeName);
        if (!string.IsNullOrEmpty(pathCandidate))
            return pathCandidate;

        throw new FileNotFoundException($"dxc not found. Set {DxcPathEnvVar} or place it under {Path.Combine(toolsDir, "dxc")}.");
    }

    private static string? FindOnPath(string exeName)
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
            return null;

        foreach (string entry in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            string candidate = Path.Combine(entry.Trim(), exeName);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static string GetRuntimeRid()
    {
        string os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "win"
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? "linux"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? "osx"
                    : "unknown";

        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => "x64"
        };

        return $"{os}-{arch}";
    }
}
