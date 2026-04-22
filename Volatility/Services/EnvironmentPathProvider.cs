using Volatility.Abstractions.Services;
using Volatility.Utilities;

namespace Volatility.Services;

public sealed class EnvironmentPathProvider : IPathProvider
{
    public string GetDirectory(VolatilityPathLocation location)
    {
        return EnvironmentUtilities.GetEnvironmentDirectory(location switch
        {
            VolatilityPathLocation.Executable => EnvironmentUtilities.EnvironmentDirectory.Executable,
            VolatilityPathLocation.Tools => EnvironmentUtilities.EnvironmentDirectory.Tools,
            VolatilityPathLocation.Data => EnvironmentUtilities.EnvironmentDirectory.Data,
            VolatilityPathLocation.ResourceDB => EnvironmentUtilities.EnvironmentDirectory.ResourceDB,
            VolatilityPathLocation.Resources => EnvironmentUtilities.EnvironmentDirectory.Resources,
            VolatilityPathLocation.Splicer => EnvironmentUtilities.EnvironmentDirectory.Splicer,
            _ => throw new ArgumentOutOfRangeException(nameof(location), location, "Unknown path location!")
        });
    }

    public string GetExecutableDirectory()
    {
        return EnvironmentUtilities.GetExecutableDirectory();
    }
}
