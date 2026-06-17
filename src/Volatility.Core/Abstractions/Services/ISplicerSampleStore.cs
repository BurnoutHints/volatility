using Volatility.Resources;

namespace Volatility.Abstractions.Services;

public interface ISplicerSampleStore
{
    void PopulateDependentSamples(Splicer splicer, string splicerDirectory, bool recurse = false);
}
