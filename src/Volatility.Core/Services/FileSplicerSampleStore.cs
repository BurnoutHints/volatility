using Volatility.Abstractions.Services;
using Volatility.Resources;

namespace Volatility.Services;

public sealed class FileSplicerSampleStore : ISplicerSampleStore
{
    public void PopulateDependentSamples(Splicer splicer, string splicerDirectory, bool recurse = false)
    {
        ArgumentNullException.ThrowIfNull(splicer);

        List<SnrID> needed = splicer.Splices
            .SelectMany(splice => splice.SampleRefs.Select(sampleReference => sampleReference.Sample))
            .Distinct()
            .ToList();

        string sampleDirectory = Path.Combine(splicerDirectory, "Samples");
        string[] files = Directory.GetFiles(sampleDirectory, "*.snr", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        Dictionary<SnrID, byte[]> sampleMap = new(needed.Count);
        foreach (string filePath in files)
        {
            byte[] data = File.ReadAllBytes(filePath);
            SnrID sampleId = SnrID.HashFromBytes(data);
            if (!sampleMap.ContainsKey(sampleId) && needed.Contains(sampleId))
            {
                sampleMap[sampleId] = data;
            }
        }

        foreach (SnrID sampleId in needed.Where(sampleId => !sampleMap.ContainsKey(sampleId)))
        {
            throw new FileNotFoundException($"Missing sample for {sampleId}");
        }

        splicer.SetLoadedSamples(needed.Select(sampleId => new Splicer.SpliceSample
        {
            SampleID = sampleId,
            Data = sampleMap[sampleId]
        }).ToList());
    }
}
