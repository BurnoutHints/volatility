using Volatility.Resources;

namespace Volatility.Abstractions.Services;

public interface IShaderSourceStore
{
    void MaterializeImportedSource(ShaderBase shader, string resourcesDirectory);
}
