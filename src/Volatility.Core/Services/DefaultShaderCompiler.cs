using System.Diagnostics;
using System.Runtime.InteropServices;
using Volatility.Abstractions.Services;
using Volatility.Resources;

namespace Volatility.Services;

public sealed class DefaultShaderCompiler(
    IPathProvider pathProvider,
    IProcessRunner processRunner)
    : IShaderCompiler
{
    private const string DXCPathEnvVar = "VOLATILITY_DXC_PATH";

    public void CompileStagesToCSO(ShaderBase shader, IReadOnlyList<ShaderStageCompile> stages, Func<ShaderStageCompile, string> outputPathFactory)
    {
        ArgumentNullException.ThrowIfNull(shader);

        if (stages == null || stages.Count == 0)
        {
            throw new InvalidOperationException("No shader stages were provided.");
        }

        foreach (ShaderStageCompile stage in stages)
        {
            CompileToCSO(shader, stage, outputPathFactory(stage));
        }
    }

    public void CompileToCSO(ShaderBase shader, ShaderStageCompile stage, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(shader);
        ArgumentNullException.ThrowIfNull(stage);

        string? outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string dxcPath = ResolveDxcPath();
        string sourcePath = ResolveSourcePath(shader);
        string entryPoint = ResolveEntryPoint(shader, stage);
        string targetProfile = ResolveTargetProfile(shader, stage);

        processRunner.RunAndCapture(BuildStartInfo(dxcPath, sourcePath, shader, stage, entryPoint, targetProfile, outputPath));
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
        ProcessStartInfo startInfo = new()
        {
            FileName = dxcPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-nologo");
        startInfo.ArgumentList.Add("-E");
        startInfo.ArgumentList.Add(entryPoint);
        startInfo.ArgumentList.Add("-T");
        startInfo.ArgumentList.Add(targetProfile);
        startInfo.ArgumentList.Add("-Fo");
        startInfo.ArgumentList.Add(outputPath);

        foreach (ShaderDefine define in EnumerateDefines(shader, stage))
        {
            if (string.IsNullOrWhiteSpace(define.Name))
            {
                continue;
            }

            startInfo.ArgumentList.Add("-D");
            startInfo.ArgumentList.Add(string.IsNullOrWhiteSpace(define.Value)
                ? define.Name
                : $"{define.Name}={define.Value}");
        }

        if (shader.IncludeDirectories != null)
        {
            foreach (string includeDirectory in shader.IncludeDirectories)
            {
                if (string.IsNullOrWhiteSpace(includeDirectory))
                {
                    continue;
                }

                startInfo.ArgumentList.Add("-I");
                startInfo.ArgumentList.Add(includeDirectory);
            }
        }

        if (shader.AdditionalArguments != null)
        {
            foreach (string arg in shader.AdditionalArguments)
            {
                if (!string.IsNullOrWhiteSpace(arg))
                {
                    startInfo.ArgumentList.Add(arg);
                }
            }
        }

        if (stage.AdditionalArguments != null)
        {
            foreach (string arg in stage.AdditionalArguments)
            {
                if (!string.IsNullOrWhiteSpace(arg))
                {
                    startInfo.ArgumentList.Add(arg);
                }
            }
        }

        startInfo.ArgumentList.Add(sourcePath);
        return startInfo;
    }

    private static IEnumerable<ShaderDefine> EnumerateDefines(ShaderBase shader, ShaderStageCompile stage)
    {
        if (shader.Defines != null)
        {
            foreach (ShaderDefine define in shader.Defines)
            {
                yield return define;
            }
        }

        if (stage.Defines != null)
        {
            foreach (ShaderDefine define in stage.Defines)
            {
                yield return define;
            }
        }
    }

    private static string ResolveEntryPoint(ShaderBase shader, ShaderStageCompile stage)
    {
        if (!string.IsNullOrWhiteSpace(stage.EntryPoint))
        {
            return stage.EntryPoint;
        }

        if (!string.IsNullOrWhiteSpace(shader.EntryPoint))
        {
            return shader.EntryPoint;
        }

        return "main";
    }

    private static string ResolveTargetProfile(ShaderBase shader, ShaderStageCompile stage)
    {
        if (!string.IsNullOrWhiteSpace(stage.TargetProfile))
        {
            return stage.TargetProfile;
        }

        string? prefix = ShaderStageCompile.GetProfilePrefix(stage.ResolveStage());
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            return $"{prefix}_5_0";
        }

        if (!string.IsNullOrWhiteSpace(shader.TargetProfile))
        {
            return shader.TargetProfile;
        }

        return "ps_5_0";
    }

    private static string ResolveSourcePath(ShaderBase shader)
    {
        string? resolvedPath = shader.ResolveShaderSourcePath();
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            throw new InvalidOperationException("ShaderSourcePath is empty.");
        }

        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException($"Shader source file not found: {resolvedPath}");
        }

        return resolvedPath;
    }

    private string ResolveDxcPath()
    {
        string? overridePath = Environment.GetEnvironmentVariable(DXCPathEnvVar);
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            if (!File.Exists(overridePath))
            {
                throw new FileNotFoundException($"DXC override path not found: {overridePath}");
            }

            return overridePath;
        }

        string executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dxc.exe" : "dxc";
        string toolsDirectory = pathProvider.GetDirectory(VolatilityPathLocation.Tools);
        string runtimeRid = GetRuntimeRid();

        string[] candidates =
        [
            Path.Combine(toolsDirectory, "dxc", executableName),
            Path.Combine(toolsDirectory, "dxc", runtimeRid, executableName),
            Path.Combine(toolsDirectory, "dxc", "bin", executableName)
        ];

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        string? pathCandidate = FindOnPath(executableName);
        if (!string.IsNullOrEmpty(pathCandidate))
        {
            return pathCandidate;
        }

        throw new FileNotFoundException($"dxc not found. Set {DXCPathEnvVar} or place it under {Path.Combine(toolsDirectory, "dxc")}.");
    }

    private static string? FindOnPath(string executableName)
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (string entry in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            string candidate = Path.Combine(entry.Trim(), executableName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
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
