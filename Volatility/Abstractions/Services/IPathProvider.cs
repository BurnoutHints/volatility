namespace Volatility.Abstractions.Services;

public interface IPathProvider
{
    string GetDirectory(VolatilityPathLocation location);
    string GetExecutableDirectory();
    string GetRepositoryRoot();
    string GetFullPath(string path);
    bool FileExists(string path);
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    string[] GetFilePaths(string path, VolatilityFilePathFilter filter, bool recurse = false);
}
