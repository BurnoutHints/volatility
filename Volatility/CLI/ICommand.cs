namespace Volatility;

internal interface ICommand
{
    string CommandToken { get; }
    string CommandDescription { get; }
    string CommandParameters { get; }

    void Execute();
    void SetArgs(Dictionary<string, object> args);
    void ShowUsage() => Console.WriteLine($"Usage: {CommandToken} {CommandParameters}\n{CommandDescription}");
    static string[] GetFilePathsInDirectory(string path, TargetFileType filter, bool recurse = false)
    {
        if (new DirectoryInfo(path).Exists)
        {
            SearchOption searchOption = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            List<string> files = new List<string>(Directory.EnumerateFiles(path, "*", searchOption));

            for (int i = files.Count - 1; i >= 0; i--)
            {
                var name = Path.GetFileName(files[i]);
                switch (filter)
                {
                    case TargetFileType.TextureHeader:
                        if (!name.Contains(".dat") || name.Contains("_texture"))
                            files.RemoveAt(i);
                        break;
                    default:
                        break;
                }
            }
            return files.ToArray();
        }
        return new string[] { };
    }

    public enum TargetFileType
    {
        Any,
        TextureHeader
    }
}