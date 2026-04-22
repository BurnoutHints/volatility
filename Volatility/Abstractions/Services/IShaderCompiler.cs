using Volatility.Resources;

namespace Volatility.Abstractions.Services;

public interface IShaderCompiler
{
    void CompileStagesToCSO(ShaderBase shader, IReadOnlyList<ShaderStageCompile> stages, Func<ShaderStageCompile, string> outputPathFactory);
    void CompileToCSO(ShaderBase shader, ShaderStageCompile stage, string outputPath);
}
