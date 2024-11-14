using static Volatility.Utilities.ClassUtilities;

namespace Volatility;

internal interface ICommand
{
    static string CommandToken { get; }
    static string CommandDescription { get; }
    static string CommandParameters { get; }

    async Task Execute() { }
    void SetArgs(Dictionary<string, object> args);
    public void ShowUsage() 
    {
        Type thisType = GetType();

        var token = GetStaticPropertyValue(thisType, "CommandToken");
        var parameters = GetStaticPropertyValue(thisType, "CommandParameters");
        var description = GetStaticPropertyValue(thisType, "CommandDescription");

        Console.WriteLine($"Usage:\n   {token} {parameters}\n{description}"); 
    }
    static string[] GetFilePathsInDirectory(string path, TargetFileType filter, bool recurse = false)
    {
        List<string> files = new();

        path = Path.GetFullPath(path);

        if (File.Exists(path))
        {
            files.Add(path);
        }
        else if (Directory.Exists(path))
        {
            SearchOption searchOption = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            files = new List<string>(Directory.EnumerateFiles(path, "*", searchOption));
        }
        for (int i = files.Count - 1; i >= 0; i--)
        {
            var name = Path.GetFileName(files[i]);
            switch (filter)
            {
                case TargetFileType.Header:
                    // bnd2-manager
                    if (name.Contains("_1.bin"))
                        break;
                    // DGI's tools & YAP
                    else if (!name.Contains(".dat")
                        || name.Contains("_texture")
                        || name.Contains("_imports")
                        || name.Contains("_model")
                        || name.Contains("_body"))
                        files.RemoveAt(i);
                    break;
                default:
                    break;
            }
        }
        return files.ToArray();
    }

    public enum TargetFileType
    {
        Any,
        Header
    }
}