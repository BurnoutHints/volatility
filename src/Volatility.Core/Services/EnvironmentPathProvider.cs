using System.Diagnostics;
using System.Reflection;
using Volatility.Abstractions.Services;

namespace Volatility.Services;

public sealed class EnvironmentPathProvider : IPathProvider
{
    private static readonly IReadOnlyDictionary<VolatilityPathLocation, string[]> RelativePaths =
        new Dictionary<VolatilityPathLocation, string[]>
        {
            [VolatilityPathLocation.Executable] = [],
            [VolatilityPathLocation.Tools] = ["tools"],
            [VolatilityPathLocation.Data] = ["data"],
            [VolatilityPathLocation.ResourceDB] = ["data", "ResourceDB"],
            [VolatilityPathLocation.Resources] = ["data", "Resources"],
            [VolatilityPathLocation.Splicer] = ["data", "Splicer"],
        };

    public string GetDirectory(VolatilityPathLocation location)
    {
        string executableDirectory = GetExecutableDirectory();
        if (!RelativePaths.TryGetValue(location, out string[]? segments))
        {
            throw new ArgumentOutOfRangeException(nameof(location), location, "Unknown path location!");
        }

        return segments.Length == 0
            ? executableDirectory
            : Path.Combine([executableDirectory, .. segments]);
    }

    public string GetExecutableDirectory()
    {
        string? processPath = null;
        if (Assembly.GetEntryAssembly()?.Location is string entryAssemblyLocation &&
            !string.IsNullOrEmpty(entryAssemblyLocation))
        {
            processPath = entryAssemblyLocation;
        }

        if (string.IsNullOrEmpty(processPath))
        {
            processPath = Environment.ProcessPath;
        }

        if (string.IsNullOrEmpty(processPath))
        {
            processPath = Process.GetCurrentProcess().MainModule?.FileName;
        }

        if (string.IsNullOrEmpty(processPath))
        {
            throw new InvalidOperationException("Unable to determine the process executable path.");
        }

        string? executableDirectory = Path.GetDirectoryName(processPath);
        if (string.IsNullOrEmpty(executableDirectory))
        {
            throw new InvalidOperationException($"Cannot determine directory of executable: {processPath}");
        }

        return executableDirectory;
    }

    public string GetFullPath(string path)
    {
        return Path.GetFullPath(path);
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public string[] GetFilePaths(string path, VolatilityFilePathFilter filter, bool recurse = false)
    {
        List<string> files = [];
        string fullPath = GetFullPath(path);

        if (FileExists(fullPath))
        {
            files.Add(fullPath);
        }
        else if (DirectoryExists(fullPath))
        {
            SearchOption searchOption = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            files = [.. Directory.EnumerateFiles(fullPath, "*", searchOption)];
        }

        for (int i = files.Count - 1; i >= 0; i--)
        {
            string name = Path.GetFileName(files[i]);
            switch (filter)
            {
                case VolatilityFilePathFilter.Header:
                    if ((!name.Contains(".dat", StringComparison.OrdinalIgnoreCase) &&
                         !name.Contains("_1.bin", StringComparison.OrdinalIgnoreCase)) ||
                        name.Contains("_secondary", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("_texture", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("_imports", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("_model", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("_body", StringComparison.OrdinalIgnoreCase))
                    {
                        files.RemoveAt(i);
                    }
                    break;
            }
        }

        return [.. files];
    }

    public string GetRepositoryRoot()
    {
        foreach (string startPath in GetCandidateStartPaths())
        {
            string? current = GetFullPath(startPath);
            while (!string.IsNullOrEmpty(current))
            {
                if (FileExists(Path.Combine(current, "Volatility.sln")))
                {
                    return current;
                }

                current = Directory.GetParent(current)?.FullName;
            }
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root containing Volatility.sln.");
    }

    private IEnumerable<string> GetCandidateStartPaths()
    {
        yield return Directory.GetCurrentDirectory();
        yield return GetExecutableDirectory();
    }
}
