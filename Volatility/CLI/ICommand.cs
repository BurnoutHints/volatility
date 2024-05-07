namespace Volatility;

internal interface ICommand
{
    string CommandToken { get; }
    string CommandDescription { get; }
    string CommandParameters { get; }
    void Execute();
    void SetArgs(Dictionary<string, object> args);
    void ShowUsage() => Console.WriteLine($"Usage: {CommandToken} {CommandParameters}\n{CommandDescription}");
    static string[] GetFilesInDirectory(string path)
    {
        if (new DirectoryInfo(path).Exists)
        {
            List<string> f = Directory.GetFiles(path).ToList();
            for (int i = 0; i < f.Count(); i++)
            {
                var name = System.IO.Path.GetFileName(f[i]);
                if (!name.Contains(".dat") || name.Contains("_texture"))
                {
                    f.Remove(f[i]);
                }
            }
            return f.ToArray();
        }
        return new string[] { path };
    }
}