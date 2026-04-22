using Volatility.Abstractions.Services;
using Volatility.Resources;
using Volatility.Utilities;

namespace Volatility.Services;

public sealed class DxcShaderCompilerAdapter : IShaderCompiler
{
    public void CompileStagesToCSO(ShaderBase shader, IReadOnlyList<ShaderStageCompile> stages, Func<ShaderStageCompile, string> outputPathFactory)
    {
        DXCShaderCompiler.CompileStagesToCSO(shader, stages, outputPathFactory);
    }

    public void CompileToCSO(ShaderBase shader, ShaderStageCompile stage, string outputPath)
    {
        DXCShaderCompiler.CompileToCSO(shader, stage, outputPath);
    }
}
