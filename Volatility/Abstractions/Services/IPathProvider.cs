namespace Volatility.Abstractions.Services;

public interface IPathProvider
{
    string GetDirectory(VolatilityPathLocation location);
    string GetExecutableDirectory();
}
