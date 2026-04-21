namespace Volatility.Utilities;

internal static class WorkspaceUtilities
{
    public static string FindRepositoryRoot()
    {
        foreach (string startPath in GetCandidateStartPaths())
        {
            string? current = Path.GetFullPath(startPath);
            while (!string.IsNullOrEmpty(current))
            {
                if (File.Exists(Path.Combine(current, "Volatility.sln")))
                {
                    return current;
                }

                current = Directory.GetParent(current)?.FullName;
            }
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root containing Volatility.sln.");
    }

    private static IEnumerable<string> GetCandidateStartPaths()
    {
        yield return Directory.GetCurrentDirectory();
        yield return EnvironmentUtilities.GetExecutableDirectory();
    }
}
