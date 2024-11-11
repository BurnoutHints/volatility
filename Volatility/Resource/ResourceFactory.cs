
namespace Volatility.Resource;

public interface IResourceFactory
{
    Resource CreateResource(string format, string sourceFile);
}