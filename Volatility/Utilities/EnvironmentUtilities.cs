using System.Diagnostics;
using System.Reflection;

namespace Volatility.Utilities;

public static class EnvironmentUtilities
{
    public enum EnvironmentDirectory
    {
        Executable,
        Tools,
        Data,
        ResourceDB,
        Resources,
        Splicer,
    }

    private static readonly IReadOnlyDictionary<EnvironmentDirectory, string[]> _relativePaths
        = new Dictionary<EnvironmentDirectory, string[]>
        {
            [EnvironmentDirectory.Executable] = [],
            [EnvironmentDirectory.Tools] = ["tools"],
            [EnvironmentDirectory.Data] = ["data"],
            [EnvironmentDirectory.ResourceDB] = ["data", "ResourceDB"],
            [EnvironmentDirectory.Resources] = ["data", "Resources"],
            [EnvironmentDirectory.Splicer] = ["data", "Splicer"],
        };

    public static string GetEnvironmentDirectory(EnvironmentDirectory dir)
    {
        var baseDir = GetExecutableDirectory();

        if (!_relativePaths.TryGetValue(dir, out var segments))
            throw new ArgumentOutOfRangeException(nameof(dir), dir, "Unknown environment directory type!");

        return segments.Length == 0
            ? baseDir
            : Path.Combine(new[] { baseDir }.Concat(segments).ToArray());
    }

    public static string GetExecutableDirectory()
    {
        string? processPath = null;
        var ppProp = typeof(Environment).GetProperty("ProcessPath", BindingFlags.Static | BindingFlags.Public);
        if (ppProp != null)
        {
            processPath = ppProp.GetValue(null) as string;
        }

        if (string.IsNullOrEmpty(processPath) &&
            Assembly.GetEntryAssembly()?.Location is string entryLoc &&
            !string.IsNullOrEmpty(entryLoc))
        {
            processPath = entryLoc;
        }

        if (string.IsNullOrEmpty(processPath))
        {
            processPath = Process.GetCurrentProcess().MainModule?.FileName;
        }

        if (string.IsNullOrEmpty(processPath))
            throw new InvalidOperationException("Unable to determine the process executable path.");

        var exeDir = Path.GetDirectoryName(processPath);
        if (string.IsNullOrEmpty(exeDir))
            throw new InvalidOperationException($"Cannot determine directory of executable: {processPath}");

        return exeDir;
    }
}
