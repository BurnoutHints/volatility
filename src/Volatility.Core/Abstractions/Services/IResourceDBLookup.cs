using Volatility.Resources;

namespace Volatility.Abstractions.Services;

public interface IResourceDBLookup
{
    string GetNameByResourceId(string id);
    string GetNameByResourceId(ResourceID id);
}
